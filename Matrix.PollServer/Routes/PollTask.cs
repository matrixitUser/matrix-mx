using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Nodes;
using Matrix.PollServer.Storage;
using Matrix.PollServer.Nodes.Csd;
using Matrix.PollServer.Nodes.Tube;
using NLog;

namespace Matrix.PollServer.Routes
{
    class PollTask
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public const int PRIORITY_AUTO = 1;
        public const int PRIORITY_USER = 10;
        public const int PRIORITY_VCOM = 15;

        private const int SLEEP_TIME_SEC = 30;

        /// <summary>
        /// инициатор, начало пути
        /// </summary>
        public PollNode Owner { get; private set; }
        /// <summary>
        /// число попыток выполнения (используется в tubenode пока)
        /// </summary>
        public int Repeats { get; set; }
        /// <summary>
        /// дата создания (обновления) таска
        /// </summary>
        public DateTime CreationDate { get; private set; }
        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
        }
        public int Priority { get; set; }
        public string What { get; private set; }
        public dynamic Arg { get; private set; }
        public DateTime LastActivityTime { get; set; }
        
        private readonly object busyLocker = new object();
        private bool isBusy = false;
        
        public void Update(PollTask newTask)
        {
            if (this.Owner != newTask.Owner || this.What != newTask.What) return;

            CreationDate = newTask.CreationDate;
            Priority = newTask.Priority > Priority ? newTask.Priority : Priority;
            paths = newTask.paths;
            LastActivityTime = DateTime.Now;//DateTime.Now.AddSeconds(-SLEEP_TIME_SEC - 1);
        }
        
        public PollTask(string what, PollNode owner, dynamic arg, IEnumerable<IEnumerable<PollNodePathWrapper>> paths, int priority)
        {
            What = what;
            Owner = owner;
            Arg = arg;

            Repeats = 0;

            Priority = priority;
            isBusy = false;
            LastActivityTime = DateTime.Now;
            CreationDate = DateTime.Now;            

            this.paths = GetActualPaths(paths, priority);            
        }

        private IEnumerable<IEnumerable<PollNodePathWrapper>> paths = new List<IEnumerable<PollNodePathWrapper>>() { new List<PollNodePathWrapper>() };

        public bool HasPaths()
        {
            return paths.Count() > 0;
        }

        private IEnumerable<IEnumerable<PollNodePathWrapper>> GetActualPaths(IEnumerable<IEnumerable<PollNodePathWrapper>> paths, int priority)
        {
            var actualPaths = new List<IEnumerable<PollNodePathWrapper>>();
            foreach (var path in paths)
            {
                var skip = false;
                foreach (var nodew in path)
                {
                    if (priority < PRIORITY_USER && nodew.Node is ConnectionNode && Owner is TubeNode)
                    {                        
                        var period = (nodew.Node as ConnectionNode).GetPeriod();
                        var lastPolling = (Owner as TubeNode).TimeWithoutPolling(period.type);

                        log.Debug("проверка пути. период: {0:dd.MM.yy HH:mm:ss}, последний опрос: {1:dd.MM.yy HH:mm:ss}", period.value, lastPolling);

                        if (lastPolling < period.value)
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (!skip) actualPaths.Add(path);
            }
            return actualPaths;
        }

        private static PollNode GetFinalNode(PollNodePathWrapper nodeWrapper)
        {
            return nodeWrapper == null? null : nodeWrapper.Node;
        }

        /// <summary>
        /// проверка на различие тасков
        /// </summary>
        public static bool Different(PollTask task1, PollTask task2)
        {
            if (task1 == null || task2 == null) return true;
            if (task1.paths.Count() != task2.paths.Count()) return true;

            var final1 = task1.paths.Select(p => GetFinalNode(p.LastOrDefault())).ToArray();
            var final2 = task2.paths.Select(p => GetFinalNode(p.LastOrDefault())).ToArray();

            var except = final1.Except(final2).ToArray();
            return except.Length > 0;
        }

        /// <summary>
        /// незалоченный путь
        /// /// </summary>
        public bool HasPathFreeForFinal(PollNode final, out string reason)
        {

            var path = paths.FirstOrDefault(p => p.LastOrDefault().Node == final);
            if (path == null)
            {
                reason = "путь обнулён";
                return false;
            }
            if (!path.Any())
            {
                reason = "не найдены пути для финала";
                return false;
            }
            foreach (var node in path.Select(n => n.Node))
            {
                if (node.IsLocked())
                {
                    reason = string.Format("нод [{0}] залочен", node.ToString());
                    return false;
                }
            }
            reason = "";
            return true;
        }

        /// <summary>
        /// запуск задачи, для финального узла
        /// </summary>
        /// <param name="final"></param>
        public void Begin(PollNode final)
        {           
            var path = paths.Where(p => p.LastOrDefault() != null && p.LastOrDefault().Node == final).FirstOrDefault();
            if (path == null) return;

            var code = ExecutePath(path);

            lock (busyLocker)
            {
                isBusy = false;
            }
        }

        /// <summary>
        /// запускает задачу по определенному пути        
        /// </summary>
        /// <param name="path"></param>
        /// <returns>возвращает код ошибки (0-нет ошибки)</returns>     
        public int ExecutePath(IEnumerable<PollNodePathWrapper> path)
        {
            GetStart().UpdateState(Codes.TASK_BEGIN, "");
            var route = new Route();
            Func<int> executePath = () =>
            {
                route.SetPath(path);

                // РЕЗЕРВИРОВАНИЕ: блокировка узлов в прямом направлении (от точки учёта до сервера опроса)
                log.Trace("блокировка пути, попытка залочить элементы " + string.Join("->", path.Select(p => $"{p.Node}")));
                foreach (var step in path)
                {
                    if (!step.Node.Lock(route, this))
                    {
                        log.Debug(string.Format("узел {0} заблокирован, выполнение задачи по указанному пути прервано", step.Node));
                        return Codes.NODE_LOCKED;
                    }
                }

                // ОПРОС: подготовка узлов в обратном направлении (от сервера опроса до точки учёта)
                foreach (var step in path.Reverse())
                {
                    var prepareCode = step.Node.Prepare(route, step.Left, this);
                    if (prepareCode != 0)
                    {
                        log.Debug(string.Format("не удалось подготовить узел {0}, выполнение по указанному пути прервано с кодом {1}", step.Node, prepareCode));

                        //if (!step.Node.HasChance(this))
                        //{
                        //    RemoveBrokenPath(path);
                        //}

                        foreach (var checkedStep in path.SkipWhile(n => step != n))
                        {
                            checkedStep.Node.Release(route, checkedStep.Left);
                        }
                        return prepareCode;
                    }
                }

                // РЕЛИЗ: освобождение узлов, учавствовавших в опросе, при успешном исходе
                foreach (var step in path)
                {
                    step.Node.Release(route, step.Left);
                }

                return 0;
            };

            int code = 999;

            try
            {
                code = executePath();
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                log.Trace("путь исполнен, попытка разлочить элементы " + string.Join("->", path.Select(p => $"{p.Node}")));
                foreach (var step in path)
                {
                    step.Node.Unlock(route);
                }

                this.LastActivityTime = DateTime.Now;
                GetStart().UpdateState(code, "");

                if (code == 0)
                {
                    Destroy();
                }
                else
                {
                    Repeats--;
                    if (Repeats <= 0)
                    {
                        Destroy();
                        //return - выход
                    }
                    else
                    {
                        //задачу перемещаем вконец очереди
                    }
                }
            }

            return code;
        }

        public void Destroy()
        {
            lock (busyLocker)
            {
                isBusy = true;
            }

            PollTaskManager.Instance.RemoveTask(this);
        }

        public bool Lock()
        {
            lock (busyLocker)
            {
                //if (isBusy) return false;
                //if (LastActivityTime.AddSeconds(SLEEP_TIME_SEC) > DateTime.Now) return false;
                if (IsLock()) return false;
                isBusy = true;
                return true;
            }
        }

        public bool IsLock()
        {
            return isBusy;//|| LastActivityTime.AddSeconds(SLEEP_TIME_SEC) > DateTime.Now;
        }

        public IEnumerable<PollNode> GetFinals()
        {
            if (paths == null) return null;
            return paths.Where(p => p.Any()).Select(p => p.LastOrDefault().Node).Where(f => f != null);
        }

        public PollNode GetStart()
        {
            // return NodeManager2.Instance.GetById(Owner);
            return Owner;
        }

        private void RemoveBrokenPath(PollNode node)
        {
            var newpaths = paths.ToList();
            foreach (var path in paths)
            {
                if (path.Any(n => n.Node == node))
                    newpaths.Remove(path);
            }
            if (!newpaths.Any())
                Destroy();
            else
                paths = newpaths;
        }

        private void RemoveBrokenPath(IEnumerable<PollNodePathWrapper> path)
        {
            if (path == null) return;

            var newpaths = paths.ToList();
            var brokenPath = newpaths.FirstOrDefault(p => p.LastOrDefault() == path.LastOrDefault());
            if (brokenPath != null)
            {
                newpaths.Remove(brokenPath);
            }

            if (!newpaths.Any())
                Destroy();
            else
                paths = newpaths;
        }

        public override string ToString()
        {
            return $"{{Owner: {Owner}; What: {What}; Priority: {Priority}; IsBusy: {IsBusy}; Repeats: {Repeats}; CreationDate: {CreationDate:dd.MM.yyyy HH:mm:ss}; LastActivityTime: {LastActivityTime:dd.MM.yyyy HH:mm:ss}}}";//для {0} (приорит {1}; в работе {2})", Owner.GetId(), Priority, IsBusy);
               
        }
    }
}

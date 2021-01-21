using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;
using Matrix.PollServer.Storage;
using NLog;

namespace Matrix.PollServer.Nodes
{
    abstract class PollNode : IDisposable
    {
        public const string STATE_PROCCESSING = "process";
        public const string STATE_IDLE = "idle";
        public const string STATE_WAITING = "wait";
        public const string STATE_ERROR = "error";
        
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly object locker = new object();

        private void TraceRoute(string message)
        {
            log.Trace("ROUTE-{0} {1}", GetId(), message);
        }

        /// <summary>
        /// проверяет необходимость опроса
        /// например, если архив уже собран, отклоняем задачу
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public virtual bool NeedPoll(string what,dynamic arg)
        {
            return true;
        }

        public virtual bool IsDisabled()
        {
            var disabled = false;
            if (content == null) return false;
            var dcontent = content as IDictionary<string, object>;
            try
            {
                if (dcontent.ContainsKey("isDisabled"))
                {
                    disabled = (bool)content.isDisabled;
                }
                if (dcontent.ContainsKey("isDeleted"))
                {
                    disabled |= (bool)content.isDeleted;
                }
                return disabled;
            }
            catch { return false; }
            
        }

        public virtual Guid GetId()
        {
            return Guid.Parse(content.id.ToString());
        }

        public virtual void Update(dynamic content)
        {
            this.content = content;
        }

        public virtual void AcceptVirtualCom(dynamic message)
        {

        }

        public virtual void Receive(byte[] bytes)
        {

        }

        /// <summary>
        /// Запрос нейтрального кадра 
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetKeepAlive()
        {
            return new byte[] { 0xFA, 0x00, 0x50, 0x6C };
        }

        protected dynamic content;

        public virtual int GetFinalisePriority()
        {
            return 1;
        }

        private Route lockRoute = null;

        public virtual bool IsLocked()
        {
            return lockRoute != null;
        }

        /// <summary>
        /// пытается залочить нод для выполнения в указаном маршруте
        /// </summary>
        /// <returns>результат операции</returns>
        public bool Lock(Route route, PollTask initiator)
        {
            return OnLock(route, initiator);
        }

        /// <summary>
        /// блокировка канала узла,
        /// при переопределении важно сохранить потокобезопасность
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        protected virtual bool OnLock(Route route, PollTask initiator)
        {
            lock (locker)
            {
                log.Trace($"попытка залочить {this} - {(IsLocked() ? $"НЕ ЗАЛОЧЕН: route={route}; lockroute={lockRoute}" : "успех")}");
                if (IsLocked()) return false;
                lockRoute = route;
                return true;
            }
        }

        public bool Unlock(Route route)
        {
            return OnUnlock(route);
        }

        protected virtual bool OnUnlock(Route route)
        {
            lock (locker)
            {
                log.Trace($"попытка разлочить {this} - {(lockRoute == route? "успех" : $"НЕ РАЗЛОЧЕН: route={route}; lockroute={lockRoute}")}");
                if (lockRoute == route)
                {
                    lockRoute = null;
                    return true;
                }
            }
            return false;
        }

        public int Prepare(Route route, int port, PollTask initiator)
        {
            return OnPrepare(route, port, initiator);
        }

        /// <summary>
        /// имеет шанс при повторе
        /// </summary>
        /// <returns></returns>
        public virtual bool HasChance(PollTask task)
        {
            return false;
        }

        /// <summary>
        /// подготовка узла, звонок на номер, открытие порта, создание сокета и т.п.
        /// </summary>
        /// <returns></returns>
        protected virtual int OnPrepare(Route route, int port, PollTask initiator)
        {
            return 0;
        }

        public void Release(Route route, int port)
        {
            TraceRoute(string.Format("освободили порт {0}", port));
            OnRelease(route, port);
        }

        /// <summary>
        /// освобождение ресурсов, положить трубку, закрыть сокет и т.п.
        /// </summary>
        protected virtual void OnRelease(Route route, int port)
        {

        }

        /// <summary>
        /// определяет является ли узел конечным в цепочке, использующим ресурсы (сокет, ком-порт и т.п.)
        /// </summary>
        /// <returns></returns>
        public virtual bool IsFinalNode()
        {
            return false;
        }

        public virtual bool BeforeTaskAdd(PollTask task)
        {
            return true;
        }

        public virtual void AfterTaskAdd(PollTask task)
        {

        }

        public virtual void AfterTaskSkip(PollTask task)
        {

        }

        public virtual int GetPollPriority()
        {
            return 0;
        }

        public dynamic GetArguments()
        {
            return content;
        }

        public IEnumerable<IEnumerable<PollNodePathWrapper>> GetPaths()
        {
            return BuildPath(this);
        }

        /// <summary>
        /// построение
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private List<List<PollNodePathWrapper>> BuildPath(PollNode root)
        {
            if (root == null) return null;
            if (root.IsFinalNode())
            {
                return new List<List<PollNodePathWrapper>>() { new List<PollNodePathWrapper>() { new PollNodePathWrapper(root) } };
            }

            var allPaths = new List<List<PollNodePathWrapper>>();
            foreach (var relation in RelationManager.Instance.GetOutputs(root.GetId(), "contains"))
            {
                var node = NodeManager.Instance.GetById(relation.GetEndId());
                if (node.IsDisabled()) continue;

                //пропуск закрытых окон
                //if (node is Csd.CsdConnectionNode && !(node as Csd.CsdConnectionNode).InWindow(DateTime.Now)) continue;

                var paths = BuildPath(node);
                if (paths == null || !paths.Any()) continue;

                foreach (var path in paths)
                {
                    var newpath = new List<PollNodePathWrapper>() { new PollNodePathWrapper(root) };
                    path.First().Left = relation.GetPort();
                    newpath.AddRange(path);
                    allPaths.Add(newpath);
                }
            }
            return allPaths;
        }

        public virtual bool IsEnabled()
        {
            return true;
        }

        public void Log(string message, int level = 0)
        {
            dynamic record = new ExpandoObject();
            record.id = Guid.NewGuid();
            record.type = "LogMessage";
            record.date = DateTime.Now;
            record.objectId = GetId();
            record.s1 = message;
            record.i1 = level;

            RecordsAcceptor.Instance.Save(new dynamic[] { record });
        }

        /// <summary>
        /// уведомляем о добавлении новой задачи
        /// </summary>
        public void AfterAddTask(string what)
        {
            Log(string.Format("добавлена задача '{0}'", what));
        }

        public void Cancel()
        {
            UpdateState(Codes.TASK_CANCEL, "");
            AfterCancel();
        }

        protected virtual void AfterCancel()
        {

        }

        public virtual void Dispose()
        {
        }

        private readonly object notifyLocker = new object();
        private bool notifyProccess = false;

        private Thread worker = null;

        public void Notify()
        {
            if (worker != null) return;

            worker = new Thread(ExecuteTasksBackground);
            worker.IsBackground = true;
            worker.Name = string.Format("поток исполнения задач {0}", this);
            worker.Start();
        }

        protected virtual bool IsAlive()
        {
            return true;
        }

        private void ExecuteTasksBackground()
        {
            try
            {
                log.Debug(string.Format("запущен поток обработки заявки {0}", this));
                do
                {
                    if (!IsAlive())
                    {
                        log.Debug(string.Format("[{0}] уведомление НЕ успешно, узел не готов", this));

                        notifyProccess = false;
                        return;
                    }

                    var task = PollTaskManager.Instance.GetTaskForFinal(this);
                    if (task == null)
                    {
                        log.Debug(string.Format("[{0}] уведомление НЕ успешно, нет задачи", this));
                        notifyProccess = false;
                        return;
                    }

                    log.Debug(string.Format("[{0}] уведомление прошло успешно", this));

                    //начало выполнения задачи
                    task.Begin(this);

                    log.Info(string.Format("таск завершился"));
                    Thread.Sleep(100); // после исполнения спим
                }
                while (true);
            }
            catch (Exception ex)
            {
                log.Error(ex, "ошибка в исполнении таска", ex.Message);
            }
            finally
            {
                worker = null;
                log.Debug(string.Format("остановлен поток обработки заявки {0}", this));
            }
        }

        public void UpdateState(int code, string description)
        {
            dynamic state = new ExpandoObject();
            state.code = code;
            state.date = DateTime.Now;
            state.description = description;
            state.nodeId = GetId();
            Api.StateSaveCollector.Instance.Add(state);
        }

        /// <summary>
        /// допустимое число неудачных попыток подготовить узел к опросу
        /// </summary>
        private const int COUNT_ATTEMPTS = 5;
        private int currentAttempt = 0;


        /// <summary>
        /// решаем, стоит ли данному ноду немного отдохнуть после серии неудач в опросе
        /// </summary>
        /// <param name="success"></param>
        /// <param name="priority"></param>
        protected void CheckAvailability(bool success, int priority)
        {
            if (success || priority >= PollTask.PRIORITY_USER)
            {
                currentAttempt = 0; return;
            }

            currentAttempt++;
            if (currentAttempt > COUNT_ATTEMPTS)
            {
                Thread thread = new Thread(Sleep);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private double timesleep = 0.5;
        private void Sleep()
        {
            lock (locker)
            {
                lockRoute = new Route();
            }
            var time = TimeSpan.FromHours(timesleep);
            log.Debug(string.Format("{0} уснул на время {1}", this, time));
            Thread.Sleep(time);
            log.Debug(string.Format("{0} проснулся после спячки ({1})", this, time));
            currentAttempt = 0;
            lock (locker)
            {
                lockRoute = null;
            }
        }

        public override string ToString()
        {
            return $"{{Id: {GetId()}; IsDisabled: {IsDisabled()}}}";
        }
    }
}

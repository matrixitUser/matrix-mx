using Matrix.PollServer.Nodes;
using Matrix.PollServer.Storage;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Matrix.PollServer.Routes
{
    class PollTaskManager : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<PollNode, List<PollTask>> executers = new Dictionary<PollNode, List<PollTask>>();
        private readonly Dictionary<PollNode, List<PollTask>> initiators = new Dictionary<PollNode, List<PollTask>>();

        /// <summary>
        /// добавление задач в очередь и начало опроса
        /// </summary>
        /// <param name="tasks"></param>
        private void AddTasks(IEnumerable<PollTask> tasks)
        {
            logger.Debug("добавлено {0} задач", tasks.Count());
            var needNotifyNodes = new List<PollNode>();
            foreach (var task in tasks)
            {
                //фильтр дубликатов
                var duplicate = FindDuplicate(task);
                if (duplicate != null)
                {
                    if (duplicate.Priority < task.Priority)
                    {
                        if (!duplicate.IsLock()) duplicate.Destroy();
                    }
                    else
                    {
                        if (!PollTask.Different(task, duplicate))
                        {
                            Log(string.Format("задача '{0}' уже находится в очереди", task.What), task.Owner.GetId());
                            continue;
                        }
                    }
                }

                //инициаторы
                var start = task.GetStart();
                if (start == null) continue;

                if (start.BeforeTaskAdd(task)) 
                {
                    //поиск конечных нодов (с них начинается опрос)
                    foreach (var final in task.GetFinals())
                    {
                        lock (executers)
                        {
                            if (!executers.ContainsKey(final))
                                executers.Add(final, new List<PollTask>());
                        }
                        if (!executers[final].Contains(task))
                        {
                            executers[final].Add(task);
                        }
                        needNotifyNodes.Add(final);
                    }

                    Log(string.Format("задача '{0}' добавлена, инициатор: {1}", task.What, task.Priority < PollTask.PRIORITY_USER ? "автоопрос" : "пользователь"), task.Owner.GetId());

                    lock (initiators)
                    {
                        if (!initiators.ContainsKey(start))
                        {
                            initiators.Add(start, new List<PollTask>());
                        }
                    }
                    if (!initiators[start].Contains(task))
                    {
                        initiators[start].Add(task);
                    }

                    //действия после добавления задачи
                    start.AfterTaskAdd(task);
                }
                else
                {
                    //действие после пропуска задачи
                    start.AfterTaskSkip(task);
                }
            }
            logger.Debug(string.Format("добавлено {0} задач", tasks.Count()));

            //начало опроса
            NotifyExecuters(needNotifyNodes.Distinct());
        }

        private void AddTask(PollTask task)
        {
            AddTasks(new PollTask[] { task });
        }

        private PollTask FindDuplicate(PollTask task)
        {
            if (!initiators.ContainsKey(task.Owner)) return null;

            List<PollTask> tasks = null;
            lock (initiators[task.Owner])
                tasks = initiators[task.Owner];

            foreach (var t in tasks)
            {
                if (t.What == task.What) return t;
            }
            return null;
        }

        //сбор аргументов задачи - args(приоритет)+node.arguments
        private dynamic FillArgs(dynamic nodeArguments, dynamic args)
        {
            dynamic arguments = new ExpandoObject();
            var nodeDic = nodeArguments as IDictionary<string, object>;
            var newArgsDic = arguments as IDictionary<string, object>;
            foreach (var key in nodeDic.Keys)
            {
                newArgsDic.Add(key, nodeDic[key]);
            }

            if (args != null)
            {
                var argsDic = args as IDictionary<string, object>;
                foreach (var key in argsDic.Keys)
                {
                    if (newArgsDic.ContainsKey(key))
                        newArgsDic[key] = argsDic[key];
                    else
                        newArgsDic.Add(key, argsDic[key]);
                }
            }
            return arguments;
        }

        public void CreateTasksIterative(string what, IEnumerable<PollNode> nodes, dynamic args, int priority)
        {
            List<PollTask> tasks = new List<PollTask>();

            var nds = nodes.Where(n => !n.IsDisabled()).ToArray();

            logger.Debug(string.Format("назначена задача {0} для {1} нодов", what, nds.Count()));

            var cntr = 0;
            foreach (var node in nds)
            {
                //сбор аргументов задачи
                dynamic arguments = FillArgs(node.GetArguments(), args);

                var task = new PollTask(what, node, arguments, node.GetPaths(), priority);
                if (!task.HasPaths())
                {
                    if (task.Priority != PollTask.PRIORITY_AUTO)
                        node.Log("задача отклонена, отсутствует возможность связаться с вычислителем");
                    logger.Debug("нет путей для {0}, задача отклонена", node.GetId());
                    continue;
                }
                tasks.Add(task);
                logger.Debug("таск пойман");
                cntr++;
                if (cntr >= 100)
                {
                    cntr = 0;
                    AddTasks(tasks);
                    logger.Debug("порция {0} задач ушла", tasks.Count);
                    tasks.Clear();
                }

                //var task = new PollTask(what, node, arguments, node.GetPaths(), priority);
                //if (!task.HasPaths())
                //{
                //    if (task.Priority != PollTask.PRIORITY_AUTO)
                //        node.Log("задача отклонена, отсутствует возможность связаться с вычислителем");
                //    return;
                //}                
                //tasks.Add(task);
                //cntr++;
                //if(cntr>=100)
                //{
                //    cntr = 0;
                //    AddTasks(tasks);
                //    tasks.Clear();
                //}
            }


            AddTasks(tasks);
        }

        /// <summary>
        /// Начинает опрос нодов, которые можно опросить - вычислители и, например, матриксы
        /// </summary>
        /// <param name="what"></param>
        /// <param name="nodes"></param>
        /// <param name="args"></param>
        /// <param name="priority"></param>
        public void CreateTasks(string what, IEnumerable<PollNode> nodes, dynamic args, int priority)
        {
            List<PollTask> tasks = new List<PollTask>();
            logger.Debug($"назначена задача {what} для {nodes.Count()} нодов");

            foreach (var node in nodes.Where(n => !n.IsDisabled()))
            {
                //аргументы задачи = аргументы нода (networkAddress и т.п) + аргументы опроса (start, end, components)
                dynamic arguments = FillArgs(node.GetArguments(), args);

                //создание задачи для помещения в очередь
                string createText = $"создание задачи {what} для {node.GetId()} с приоритетом {priority}";
                var task = new PollTask(what, node, arguments, node.GetPaths(), priority);
                if (!task.HasPaths())
                {
                    logger.Trace("{0}: отсутствует возможность связаться с вычислителем", createText);
                    if (task.Priority != PollTask.PRIORITY_AUTO)
                    {
                        node.Log("задача отклонена, отсутствует возможность связаться с вычислителем");
                    }
                    return;
                }

                logger.Trace("{0}: добавлено в очередь", createText);
                tasks.Add(task);
            }

            //добавление задач в очередь
            AddTasks(tasks);
        }

        public void RemoveTask(PollTask task)
        {
            if (task == null)
                return;

            foreach (var executer in executers.Keys)
            {
                List<PollTask> taskscopy = null;
                lock (executers)
                {
                    taskscopy = executers[executer];
                }

                if (!taskscopy.Contains(task)) continue;

                lock (executers)
                {
                    executers[executer].Remove(task);
                }
            }

            var start = task.GetStart();
            lock (initiators)
            {
                if (start != null && initiators.ContainsKey(start) && initiators[start].Contains(task))
                {
                    initiators[start].Remove(task);
                }
            }
        }

        public void RemoveTaskFor(PollNode initiator)
        {
            List<PollTask> tasks = new List<PollTask>();
            lock (initiators)
            {
                if (initiators.ContainsKey(initiator))
                    tasks = initiators[initiator];
            }
            if (!tasks.Any())
                return;

            foreach (var task in tasks)
            {
                foreach (var executer in executers.Keys)
                {
                    List<PollTask> taskscopy = null;
                    lock (executers)
                    {
                        taskscopy = executers[executer];
                    }

                    if (!taskscopy.Contains(task)) continue;

                    lock (executers)
                    {
                        executers[executer].Remove(task);
                    }
                }
            }

            lock (initiators)
            {
                initiators[initiator].Clear();
            }
        }

        public void RemoveTasks()
        {
            lock (initiators)
            {
                foreach (var node in initiators.Keys)
                {
                    initiators[node].Clear();
                }
            }
            lock (executers)
            {
                foreach (var node in executers.Keys)
                {
                    executers[node].Clear();
                }
            }
            logger.Debug("все задачи были удалены");
        }

        public void CancelTaskFor(PollNode initiator)
        {
            if (!HasTaskForStarter(initiator))
            {
                Log("задачи отсутствуют", initiator.GetId());
                return;
            }
            RemoveTaskFor(initiator);

            if (initiator.IsLocked())
                initiator.Cancel();
            else
                Log("задачи отменены", initiator.GetId());
        }

        public void CancelTasks()
        {
            lock (initiators)
            {
                foreach (var node in initiators.Keys)
                {
                    if (node.IsLocked())
                        node.Cancel();

                    initiators[node].Clear();
                }
            }
            lock (executers)
            {
                foreach (var node in executers.Keys)
                {
                    executers[node].Clear();
                }
            }
            logger.Debug("все задачи были удалены");
        }

        public PollTask GetTaskForFinal(PollNode final)
        {
            IEnumerable<PollTask> tasks = null;
            lock (executers)
            {
                if (!executers.ContainsKey(final) || executers[final].Count == 0)
                {
                    logger.Debug(string.Format("для {0} НЕ содержится задач", final));
                    return null;
                }
                tasks = executers[final].ToArray();
            }

            foreach (var task in tasks.OrderByDescending(p => p.Priority))
            {
                string reason;
                if (!task.HasPathFreeForFinal(final, out reason))
                {
                    logger.Debug(string.Format("нет пути у таска для финала [{0}], причина: {1}", final, reason));
                    continue;
                }
                if (!task.Lock())
                {
                    logger.Debug("таск залочен");
                    continue;
                }
                logger.Debug(string.Format("для {0} есть таск {1}", final, task));

                return task;
            }
            return null;
        }

        public bool HasTaskForFinal(PollNode final)
        {
            lock (executers)
            {
                return executers.ContainsKey(final) && executers[final].Count > 0;
            }
        }

        public bool HasTaskForStarter(PollNode starter)
        {
            lock (initiators)
            {
                return initiators.ContainsKey(starter) && initiators[starter].Count > 0;
            }
        }

        public PollTask GetTaskForStarter(PollNode starter)
        {
            if (!HasTaskForStarter(starter)) return null;
            lock (initiators)
            {
                var task = initiators[starter].OrderByDescending(p => p.Priority).FirstOrDefault();
                if (task != null)
                    task.Lock();
                return task;
            }
        }

        public void ToEndQueue(PollTask task)
        {

        }

        private Random rnd = new Random();

        private void NotifyExecuters(IEnumerable<PollNode> nodes)
        {
            logger.Debug(string.Format("оповещено {0} исполнителей", nodes.Count()));
            foreach (var node in nodes.OrderByDescending(o => o.GetPollPriority() + rnd.NextDouble()))
            {
                if (executers[node].Count != 0)
                {
                    node.Notify();
                }
            }
        }

        public string Dump()
        {
            var report = new StringBuilder();
            report.AppendLine(string.Format("---дамп {0:dd.MM.yy HH:mm:ss.fff}---", DateTime.Now));
            foreach (var node in executers.Keys.OrderBy(x => x.ToString()))
            {
                report.AppendLine(string.Format("{0} (залочен {1}): {2}", node, node.IsLocked(), string.Join(",", executers[node])));
            }
            return report.ToString();
        }

        private void Log(string message, Guid owner)
        {
            dynamic record = new ExpandoObject();
            record.id = Guid.NewGuid();
            record.type = "LogMessage";
            record.date = DateTime.Now;
            record.objectId = owner;
            record.s1 = message;

            RecordsAcceptor.Instance.Save(new dynamic[] { record });
        }

        private PollTaskManager() { }
        static PollTaskManager() { }
        private static readonly PollTaskManager instance = new PollTaskManager();
        public static PollTaskManager Instance
        {
            get { return instance; }
        }

        public void Dispose()
        {
            CancelTasks();
        }

        public override string ToString()
        {
            return $"{{Executers: [{string.Join(", ", executers.Keys.Select(k => k.ToString()))}]; Initiators: [{string.Join(", ", initiators.Keys.Select(k => k.ToString()))}]}}";
        }

        public string GetInfo()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Executers:");
            text.AppendElement(executers);
            text.AppendLine("");
            text.AppendLine("Initiators:");
            text.AppendElement(initiators);
            return text.ToString();
        }
    }

    static class StringBuilderExtensions
    {
        public static void AppendElement(this StringBuilder text, Dictionary<PollNode, List<PollTask>> elements)
        {
            int i = 1;
            foreach (KeyValuePair<PollNode, List<PollTask>> element in elements)
            {
                PollNode node = element.Key;
                List<PollTask> tasks = element.Value;
                text.AppendLine($"{i}) {node.GetId()} - {node.ToString()}");
                foreach (PollTask task in tasks)
                {
                    text.AppendLine($" {task.ToString()}");
                }
                text.AppendLine("");
                i++;
            }
        }
    }
}

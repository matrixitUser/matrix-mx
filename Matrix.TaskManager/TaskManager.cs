using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Neo4jClient;
using NLog;
using System.Dynamic;

namespace Matrix.TaskManager
{
    public class TaskManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<Guid, PollTask> tasks = new Dictionary<Guid, PollTask>();
        public IEnumerable<PollTask> Tasks { get { return tasks.Values; } }

        private readonly NodeLocker locker = new NodeLocker();

        [Dependency]
        public Bus Bus { get; set; }

        public TaskManager()
        {
        }
        
        /// <summary>
        /// помещает задачу на опрос на обработку
        /// </summary>
        /// <param name="targetIds">объекты опроса</param>
        /// <param name="details">детали опроса, зависят от типа опроса</param>
        /// <param name="type">тип целевых узлов</param>
        public void Push(Guid[] targetIds, dynamic details, string type = "Tube")
        {
            //определяем не находятся ли указанные цели в процессе опроса
            //если да, то игнорируем новую задачу, иначе удаляем старую задачу
            var newTargetIds = new List<Guid>();
            foreach (var targetId in targetIds)
            {
                if (tasks.ContainsKey(targetId))
                {
                    if (!tasks[targetId].Routes.Any(r => r.State == RouteState.InProccess))
                    {
                        tasks.Remove(targetId);
                        logger.Debug("таск {0} удален", targetId);
                    }
                }
                else
                {
                    newTargetIds.Add(targetId);
                }
            }

            //поиск всех маршрутов для целей
            //формирование новых задач
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();
            var q = client.Cypher.Match("(n:" + type + ")").Where("n.id in {ids}").Match("(port {isPort:true})").Match("pth=(n)-[*]->(port)").
                With("n.id as target, extract(po in nodes(pth)|po.id) as points,(nodes(pth)[length(nodes(pth))-1]).name as port").
                WithParams(new { ids = newTargetIds }).
                Return((target, points, port) => new { target = target.As<Guid>(), points = points.As<IEnumerable<Guid>>(), port = port.As<string>() });
            var bar = q.Results;
            foreach (var barbaroi in bar.GroupBy(b => b.target))
            {
                if (tasks.ContainsKey(barbaroi.Key))
                {
                    logger.Debug("уже есть таск для {0}", barbaroi.Key);
                    continue;
                }
                var task = new PollTask(barbaroi.Key);
                task.Routes = barbaroi.Select(b => new Route(b.port, 1, b.points.ToArray())).ToList();
                task.Details = details;
                tasks.Add(barbaroi.Key, task);
            }

            Notify(newTargetIds.ToArray());
        }

        /// <summary>
        /// рассылает сообщение о опросе портам опроса
        /// </summary>
        /// <param name="targets"></param>
        public void Notify(IEnumerable<Guid> targets)
        {
            foreach (var target in targets)
            {
                if (!tasks.ContainsKey(target))
                {
                    logger.Debug("таск {0} не найден", target);
                    continue;
                }

                var task = tasks[target];

                //проверяем не в работе ли задача
                if (task.Routes.Any(r => r.State == RouteState.InProccess))
                {
                    logger.Debug("маршрут для {0} уже в работе", target);
                    continue;
                }

                var route = task.Routes.OrderBy(r=>r.Priority).FirstOrDefault(r => r.State == RouteState.Wait && !locker.IsLock(target, r.Points));

                if (route == null)
                {
                    logger.Debug("маршрут для {0} не имеет путей", target);
                    continue;
                }
                //route.PortName
                var msg = Bus.MakeMessageStub("poll");
                msg.body.details = task.Details;
                msg.body.path = route.Points;
                msg.body.targetId = task.TargetId;
                route.State = RouteState.InProccess;

                locker.Lock(route.Points.Take(route.Points.Count() - 1));
                Bus.SendToPort(msg, route.PortName);
            }
        }

        public void CloseTask(Guid targetId)
        {
            if (tasks.ContainsKey(targetId))
            {
                foreach (var route in tasks[targetId].Routes.Where(r => r.State == RouteState.InProccess))
                {
                    Notify(locker.Unlock(route.Points));
                }
                tasks.Remove(targetId);
                logger.Debug("таск {0} удален, завершился", targetId);
            }
        }

        public void RouteReject(Guid targetId)
        {
            if (!tasks.ContainsKey(targetId)) return;

            var task = tasks[targetId];
            foreach (var route in task.Routes.Where(r => r.State == RouteState.InProccess))
            {
                route.State = RouteState.Reject;
                Notify(locker.Unlock(route.Points));
            }

            if (!task.Routes.Any(r => r.State == RouteState.Wait || r.State == RouteState.InProccess))
            {
                tasks.Remove(targetId);
                logger.Debug("таск {0} удален, не имеет возможности опроситься", targetId);
            }
            else
            {
                Notify(new Guid[] { targetId });
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient.Cypher;
using NLog;

namespace Matrix.Scheduler
{
    public class TaskManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private List<Task> tasks = new List<Task>();

        public IEnumerable<Task> Tasks { get { return tasks; } }

        /// <summary>
        /// загрузка задач
        /// </summary>
        public void Start()
        {
            tasks.Clear();
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var cli = new Neo4jClient.GraphClient(new Uri(url));
            cli.Connect();
            var dbTasks = cli.Cypher.Match("(t:Task)").Return(t => t.Node<string>()).Results.ToDynamic();
            foreach (var dbTask in dbTasks)
            {
                var task = new Task(dbTask);
                tasks.Add(task);
            }

            logger.Info("менеджер задач запущен, {0} задач", tasks.Count);
        }

        public void Stop()
        {
            foreach (var task in tasks)
            {
                task.Stop();
            }
            tasks.Clear();
            logger.Info("менеджер задач остановлен");
        }

        public IEnumerable<Guid> GetTaskTubeIds(Guid taskId)
        {
            var task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                return null;
            }

            return task.GetTubeIds();
            //var url = ConfigurationManager.AppSettings["neo4j-url"];
            //var cli = new Neo4jClient.GraphClient(new Uri(url));
            //cli.Connect();
            //var dbTasks = cli.Cypher.Match("(ts:Task)<--(foo)-[r*0..]->(tb:Tube)").With("tb.id as tbeId,ts.id as tskId,length(r) as len").Return((tbeId, tskId, len) => new { tubeId = tbeId.As<Guid>(), taskId = tskId.As<Guid>(), len = len.As<int>() }).Results;
            //var foo = dbTasks.GroupBy(t => t.tubeId).Select(g => new { tubeId = g.Key, taskId = g.OrderBy(t => t.len).First().taskId }).Where(t => t.taskId == taskId).Select(t => t.tubeId);
            //return foo;
        }
    }
}

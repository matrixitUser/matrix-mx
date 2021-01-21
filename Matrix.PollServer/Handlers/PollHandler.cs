using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Nodes;
using Matrix.PollServer.Routes;
using NLog;

namespace Matrix.PollServer.Handlers
{
    class PollHandler : IHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool CanHandle(string what)
        {
            return what.StartsWith("poll");
        }

        public void Handle(dynamic message)
        {

            string what = message.head.what;

            if (what == "poll")
            {
                log.Debug(string.Format("получена задача {0} для {1} объектов", what, message.body.objectIds.Count));
                var ids = new List<Guid>();
                foreach (dynamic id in message.body.objectIds)
                {
                    Guid nodeId = Guid.Empty;
                    if (Guid.TryParse((string)id, out nodeId))
                    {
                        ids.Add(nodeId);
                    }
                }

                //var task = new PollTask(PollTask.PRIORITY_USER, message.body.what, message.body.arg);
                var nodes = NodeManager.Instance.GetByIds(ids);

                var priority = PollTask.PRIORITY_USER;

                var darg = message.body.arg as IDictionary<string, object>;
                if (darg.ContainsKey("auto") && message.body.arg.auto == true)
                {
                    priority = PollTask.PRIORITY_AUTO;
                }

                PollTaskManager.Instance.CreateTasks(message.body.what, nodes, message.body.arg, priority);
                //Router.Instance.AddTask(task, nodes);
            }

            if (what == "poll-vcom-request")
            {
                log.Debug(string.Format("получен VCOM {0} для объекта {1}", what, message.body.objectId));

                Guid id = Guid.Parse(message.body.objectId.ToString());
                var node = NodeManager.Instance.GetById(id);

                if (message.body.what == "vcom-open")
                {
                    PollTaskManager.Instance.CreateTasks(message.body.what, new PollNode[] { node }, new ExpandoObject(), PollTask.PRIORITY_VCOM);
                }
                else
                {
                    node.AcceptVirtualCom(message);
                }
            }

            if (what == "poll-cancel")
            {
                var ids = new List<Guid>();
                foreach (dynamic id in message.body.objectIds)
                {
                    Guid nodeId = Guid.Empty;
                    if (Guid.TryParse((string)id, out nodeId))
                    {
                        ids.Add(nodeId);
                    }
                }

                //  NodeManager2.Instance.GetByIds(ids).AsParallel().ForAll(n => n.Cancel());
                NodeManager.Instance.GetByIds(ids).AsParallel().ForAll(node => PollTaskManager.Instance.CancelTaskFor(node));
            }
        }
    }
}

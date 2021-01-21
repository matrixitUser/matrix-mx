using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Diagnostics;

namespace Matrix.PollServer.Nodes
{
    class NodeManager : IDisposable
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(NodeManager));
        private Dictionary<Guid, PollNode> nodes = new Dictionary<Guid, PollNode>();

        //public void Load()
        //{
        //    lock (nodes)
        //    {
        //        try
        //        {
        //            var connector = UnityManager.Instance.Resolve<IConnector>();
        //            dynamic request = Helper.BuildMessage("rows-for-server");
        //            request.body.serverName = ConfigurationManager.AppSettings["name"];
        //            dynamic answer = connector.SendMessage(request);

        //            if (answer.head.what == "error")
        //            {
        //                log.Error(string.Format("{0}: {1}", answer.head.what, answer.body.message));
        //                return;
        //            }

        //            foreach (var node in answer.body.server.nodes)
        //            {
        //                try
        //                {
        //                    var newNode = NodeFactory2.Create(node);
        //                    if (newNode != null)
        //                    {
        //                        nodes.Add(newNode.GetId(), newNode);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    log.Error(string.Format("ошибка при загрузке нода"), ex);
        //                }
        //            }

        //            log.Info(string.Format("узлы загружены, {0} шт.", nodes.Count));

        //            RelationManager.Instance.ReSet(answer.body.server.relations);
        //            //    Synchronizer.Instance.Check(nodes.Values.OfType<TubeNode>().Select(t => (Guid)t.Id));                
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(string.Format("ошибка при загрузке нодов"), ex);
        //        }
        //    }
        //}


        public void Load()
        {
            try
            {
                var connector = UnityManager.Instance.Resolve<IConnector>();
                dynamic request = Helper.BuildMessage("rows-for-server");
                request.body.serverName = ConfigurationManager.AppSettings["name"];
                dynamic answer = connector.SendMessage(request);

                if (answer.head.what == "error")
                {
                    log.Error(string.Format("{0}: {1}", answer.head.what, answer.body.message));
                    return;
                }

                Dictionary<Guid, PollNode> newNodes = new Dictionary<Guid, PollNode>();

                foreach (var node in answer.body.server.nodes)
                {
                    try
                    {
                        var id = Guid.Parse(node.id.ToString());

                        var oldNode = GetById(id);
                        if (oldNode != null)
                        {
                            lock (nodes)
                            {
                                nodes.Remove(id);
                            }

                            newNodes.Add(oldNode.GetId(), oldNode);
                            oldNode.Update(node);
                        }
                        else
                        {
                            var newNode = NodeFactory.Create(node);
                            if (newNode != null)
                            {
                                newNodes.Add(newNode.GetId(), newNode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("ошибка при загрузке нода"), ex);
                    }
                }

                lock (nodes)
                {
                    while (nodes.Any())
                    {
                        var node = nodes.First();
                        node.Value.Dispose();
                        nodes.Remove(node.Key);
                    }
                    nodes = newNodes;
                }

                log.Info(string.Format("узлы загружены, {0} шт.", nodes.Count));

                RelationManager.Instance.ReSet(answer.body.server.relations);
                //    Synchronizer.Instance.Check(nodes.Values.OfType<TubeNode>().Select(t => (Guid)t.Id));                
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при загрузке нодов"), ex);
            }

        }

        public IEnumerable<PollNode> GetByIds(IEnumerable<Guid> ids)
        {
            lock (nodes)
            {
                var copy = new List<PollNode>();
                foreach (var id in ids)
                {
                    if (!nodes.ContainsKey(id)) continue;
                    copy.Add(nodes[id]);
                }
                return copy;
            }
        }

        public PollNode GetById(Guid id)
        {
            lock (nodes)
            {
                if (!nodes.ContainsKey(id))
                {
                    // log.Warn(string.Format("Запрашиваемый нод '{0}' не найден", id));
                    return null;
                }
                return nodes[id];
            }
        }

        public void Update(dynamic msgChange)
        {
            string status = msgChange.status;
            dynamic content = msgChange.content;
            Guid id = Guid.Parse((string)content.id);

            switch (status)
            {
                case "update":
                    {
                        PollNode node = null;
                        lock (nodes)
                        {
                            if (nodes.ContainsKey(id))
                            {
                                node = nodes[id];
                            }
                        }
                        if (node != null)
                        {
                            node.Update(content);
                            log.Warn(string.Format("нод '{0}' был обновлен", id));
                        }

                        break;
                    }
                case "remove":
                    {
                        lock (nodes)
                        {
                            if (nodes.ContainsKey(id))
                            {
                                nodes.Remove(id);
                                log.Warn(string.Format("нод '{0}' был удален", id));
                            }
                        }
                        break;
                    }
                case "add":
                    {
                        var newNode = NodeFactory.Create(content);
                        if (newNode != null)
                        {
                            log.Warn(string.Format("добавлен новый нод {0}", content.type));
                            lock (nodes)
                            {
                                nodes.Add(newNode.GetId(), newNode);
                            }
                        }
                        else
                        {
                            log.Debug(string.Format("не удалось добавить новый нод"));
                        }
                        break;
                    }
                default:
                    {
                        log.Warn(string.Format("действие не опознано"));
                        break;
                    }
            }
        }

        public IEnumerable<T> GetNodes<T>()
        {
            lock (nodes)
            {
                return nodes.Values.OfType<T>().ToList();
            }
        }

        private NodeManager() { }
        static NodeManager() { }
        private static readonly NodeManager instance = new NodeManager();
        public static NodeManager Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            lock (nodes)
            {
                var sw = new Stopwatch();
                sw.Start();
                foreach (var group in nodes.Values.GroupBy(n => n.GetFinalisePriority()).OrderByDescending(x => x.Key))
                {
                    List<Task> tasks = new List<Task>();
                    foreach (var node in group)
                    {
                        tasks.Add(Task.Factory.StartNew(node.Dispose));
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                sw.Stop();
                log.Debug(string.Format("{0} нодов уничтожено за {1} мс", nodes.Count, sw.ElapsedMilliseconds));
                nodes.Clear();
            }
            log.Info("менеджер нодов остановлен");
        }

        public void Add(PollNode node)
        {
            lock (nodes)
            {
                if (!nodes.ContainsKey(node.GetId()))
                {
                    nodes.Add(node.GetId(), node);
                }
            }
        }
    }
}

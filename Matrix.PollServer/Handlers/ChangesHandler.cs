using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Nodes;

namespace Matrix.PollServer.Handlers
{
    class ChangesHandler : IHandler
    {
        const string TARGET_RELATION = "relation";
        const string TARGET_NODE = "node";
        const string ACTION_ADD = "add";
        const string ACTION_DEL = "del";
        const string ACTION_UPD = "upd";

        public bool CanHandle(string what)
        {
            return what.StartsWith("changes");
        }

        public void Handle(dynamic message)
        {
            var connector = UnityManager.Instance.Resolve<IConnector>();

            string what = message.head.what;
            if (what == "changes")
            {
                foreach (var rule in message.body.rules)
                {
                    string action = rule.action;
                    string target = rule.target;
                    dynamic content = rule.content;

                    if (target == TARGET_RELATION)
                    {
                        Guid start = Guid.Parse(content.start.ToString());
                        Guid end = Guid.Parse(content.end.ToString());
                        string type = content.type;
                        dynamic body = content.body;
                        body.start = start;
                        body.end = end;

                        if (action == ACTION_ADD)
                        {
                            var rel = RelationManager.Instance.Get(start, end);
                            if (rel == null)
                            {
                                rel = new Relation(body);
                                RelationManager.Instance.Add(rel);
                                dynamic branchReq = new ExpandoObject();
                                branchReq.head = new ExpandoObject();
                                branchReq.head.what = "edit-get-branch";
                                branchReq.body = new ExpandoObject();
                                branchReq.body.id = rel.GetStartId();
                                var branchAns = connector.SendMessage(branchReq);
                                if (branchAns != null)
                                {
                                    foreach (var node in branchAns.body.branch.nodes)
                                    {
                                        Guid nodeId = Guid.Parse(node.id.ToString());
                                        var old = NodeManager.Instance.GetById(nodeId);
                                        if (old == null)
                                        {
                                            NodeManager.Instance.Add(NodeFactory.Create(node));
                                        }
                                    }
                                    foreach (var relation in branchAns.body.branch.relations)
                                    {
                                        Guid startId = Guid.Parse(relation.start.ToString());
                                        Guid endId = Guid.Parse(relation.end.ToString());
                                        var old = RelationManager.Instance.Get(startId, endId);
                                        if (old == null)
                                        {
                                            old = new Relation(relation);
                                            RelationManager.Instance.Add(old);
                                        }
                                    }
                                }
                            }
                        }
                        if (action == ACTION_UPD)
                        {
                            var rel = RelationManager.Instance.Get(start, end);
                            if (rel != null)
                            {
                                rel.Update(body);
                            }
                        }
                        if (action == ACTION_DEL)
                        {
                            RelationManager.Instance.Delete(start, end, type);
                            //todo delete all dead nodes
                        }
                    }

                    if (target == TARGET_NODE)
                    {
                        Guid id = Guid.Parse(content.id.ToString());
                        string type = content.type;
                        dynamic body = content.body;

                        if (action == ACTION_ADD || action == ACTION_UPD)
                        {
                            var node = Nodes.NodeManager.Instance.GetById(id);
                            if (node == null)
                            {
                                node = Nodes.NodeFactory.Create(body);
                                Nodes.NodeManager.Instance.Add(node);
                            }
                            else
                            {
                                node.Update(body);
                            }
                        }
                        if (action == ACTION_DEL)
                        {
                        }
                    }
                }
            }
        }
    }
}

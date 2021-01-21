using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Matrix.PollServer.Nodes;

namespace Matrix.PollServer.Routes
{
    class Route
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Guid id;

        public Route()
        {
            id = Guid.NewGuid();
        }

        private readonly LinkedList<PollNode> nodes = new LinkedList<PollNode>();

        public PollNode GetPrevious(PollNode node)
        {
            return nodes.Find(node).Previous.Value;
        }

        public PollNode GetNext(PollNode node)
        {
            var next = nodes.Find(node).Next;
            if (next == null) return null;
            return next.Value;
        }

        public void AddLast(PollNode node)
        {
            nodes.AddFirst(node);
        }

        public void RemoveLast(PollNode node)
        {
            nodes.Remove(node);
        }

        public void Send(PollNode sender, byte[] bytes, Direction dir)
        {
            PollNode next;
            if (dir == Direction.FromInitiator)
            {
                if (nodes.Find(sender) == null || nodes.Find(sender).Next == null)
                {
                    return;
                }
                next = nodes.Find(sender).Next.Value;
            }
            else
            {
                if (nodes.Find(sender) == null || nodes.Find(sender).Previous == null)
                {
                    return;
                }
                next = nodes.Find(sender).Previous.Value;
            }

            if (subscribers.ContainsKey(next))
            {
                subscribers[next](bytes, dir);
            }
            else
            {
            }
        }

        public void Subscribe(PollNode node, Action<byte[], Direction> callback)
        {
            if (subscribers.ContainsKey(node))
            {
                subscribers[node] = callback;
            }
            else
            {
                subscribers.Add(node, callback);
            }
        }

        private readonly Dictionary<PollNode, Action<byte[], Direction>> subscribers = new Dictionary<PollNode, Action<byte[], Direction>>();

        public override string ToString()
        {
            return string.Format("[{0}]", string.Join("->", nodes.Select(n => n.ToString())));
        }

        public void SetPath(IEnumerable<PollNodePathWrapper> path)
        {
            foreach (var node in path)
            {
                nodes.AddLast(node.Node);
            }
        }

        public PollNode GetLast()
        {
            if (nodes.Last == null) return null;
            return nodes.Last.Value;
        }
    }
}

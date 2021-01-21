using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes
{
    class Relation
    {
        private dynamic relation;

        public Guid GetStartId()
        {
            return Guid.Parse(relation.start.ToString());
        }

        public Guid GetEndId()
        {
            return Guid.Parse(relation.end.ToString());
        }

        public int GetPort()
        {
            var drel = relation as IDictionary<string, object>;
            if (!drel.ContainsKey("port")) return 1;
            return (int)relation.port;
        }

        public int GetPriority()
        {
            var drel = relation as IDictionary<string, object>;
            if (!drel.ContainsKey("priority")) return 1;
            return (int)relation.priority;
        }

        public string GetType()
        {
            var drel = relation as IDictionary<string, object>;
            if (!drel.ContainsKey("type")) return "contains";
            return (string)relation.type;
        }

        public dynamic GetBody()
        {
            return relation;
        }

        public Relation(dynamic relation)
        {
            this.relation = relation;
        }

        public void Update(dynamic body)
        {
            relation = body;
        }
    }
}

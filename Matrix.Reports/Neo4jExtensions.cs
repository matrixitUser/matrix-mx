using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    static class Neo4jExtensions
    {
        public static dynamic ToDynamic(this INode node)
        {
            dynamic res = new ExpandoObject();
            foreach (var p in node.Properties)
            {
                (res as IDictionary<string, object>).Add(p.Key, p.Value);
            }
            //dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(node.Data);
            //return obj;
            return res;
        }

        public static IEnumerable<dynamic> ToDynamics(this IEnumerable<INode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node.ToDynamic();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Newtonsoft.Json;

namespace Matrix.Scheduler
{
    static class CypherExtensions
    {
        public static dynamic ToDynamic(this Node<string> node)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(node.Data);
        }

        public static IEnumerable<dynamic> ToDynamic(this IEnumerable<Node<string>> nodes)
        {
            foreach(var node in nodes)
            {
                yield return node.ToDynamic();
            }
        }
    }
}

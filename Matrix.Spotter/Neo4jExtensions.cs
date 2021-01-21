using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using Newtonsoft.Json;

namespace Matrix.Spotter
{
    static class Neo4jExtensions
    {
        public static dynamic ToDynamic(this Node<string> node)
        {
            dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(node.Data);
            return obj;
        }

        public static IEnumerable<dynamic> ToDynamics(this IEnumerable<Node<string>> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node.ToDynamic();
            }
        }
    }
}

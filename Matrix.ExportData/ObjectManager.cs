using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using NLog;

namespace Matrix.ExportData
{
    public class ObjectManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<dynamic> GetObjects()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();

            var sw = new Stopwatch();
            sw.Start();

            var q = client.Cypher.Match("(d:Device)<--(t:Tube)<--(a:Area)").Return((t, d, a) => new
            {
                d = d.Node<string>(),
                t = t.Node<string>(),
                a = a.Node<string>()
            });

            var objs = q.Results.Select(foo =>
            {
                var t = foo.t.ToDynamic();
                var a = foo.a.ToDynamic();
                var d = foo.d.ToDynamic();

                var dt = t as IDictionary<string, object>;

                dynamic obj = new ExpandoObject();
                obj.id = t.id;
                obj.name = dt.ContainsKey("name") ? t.name : "";
                obj.name += " " + a.name;
                obj.device = d.name;
                return obj;
            }).ToArray();

            sw.Stop();
            logger.Debug("объекты ");

            return objs;
        }
    }
}

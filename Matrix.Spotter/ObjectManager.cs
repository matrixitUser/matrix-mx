using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using NLog;
using System.Data.SqlClient;

namespace Matrix.Spotter
{
    public class ObjectManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public IEnumerable<dynamic> GetAll()
        {
            var name = ConfigurationManager.AppSettings["port-name"];
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();

            var q = client.Cypher.Match("(c:SpotterConnection)-->(p:SpotterPort {name:{name}})").OptionalMatch("(a:Area)-->(t:Tube)-->(c)").WithParams(new { name = name }).Return((c, a, t) => new { c = c.Node<string>(), a = a.Node<string>(), t = t.Node<string>() });
            return q.Results.Select(r =>
            {
                dynamic row = new ExpandoObject();
                row.connection = r.c.ToDynamic();
                row.area = r.a == null ? null : r.a.ToDynamic();
                row.tube = r.t == null ? null : r.t.ToDynamic();
                return row;
            }).ToArray();
        }

        public IEnumerable<dynamic> GetData(Guid id, DateTime start, DateTime end)
        {
            var cs = ConfigurationManager.ConnectionStrings["sql"].ConnectionString;
            using (var connection = new SqlConnection(cs))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = @"select id, objectId, date, image, value, dateReceive from spotter where objectid=@id and date between @start and @end";
                    command.Parameters.AddWithValue("@id",id);
                    command.Parameters.AddWithValue("@start", start);
                    command.Parameters.AddWithValue("@end", end);
                    var reader = command.ExecuteReader();
                    while(reader.Read())
                    {
                        dynamic record = new ExpandoObject();
                        record.id = reader.GetGuid(0);
                        record.objectId= reader.GetGuid(1);
                        record.date = reader.GetDateTime(2);
                        record.image = reader.GetValue(3);
                        record.value = reader.GetValue(4);
                        record.dateReceive = reader.GetValue(5);
                        yield return record;
                    }
                }
            }
        }

        public dynamic Save(string id)
        {
            var name = ConfigurationManager.AppSettings["port-name"];
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();

            dynamic portBody = new ExpandoObject();
            portBody.id = Guid.NewGuid();
            portBody.type = "SpotterPort";
            portBody.creationDate = DateTime.Now;
            portBody.name = name;

            dynamic conBody = new ExpandoObject();
            conBody.id = Guid.NewGuid();
            conBody.type = "SpotterConnection";
            conBody.creationDate = DateTime.Now;
            conBody.spotterId = id;

            var q = client.Cypher.Merge("(p:SpotterPort {name:{portName}})").OnCreate().Set("p={port}").Merge("(c:SpotterConnection {spotterId:{conSpotterId}})").OnCreate().Set("c={con}").
                Merge("(c)-[:contains]->(p)").
                WithParams(new { con = conBody, port = portBody, conSpotterId = id, portName = name }).Return(c => c.Node<string>());
            var res = q.Results.ToDynamics();
            logger.Debug("соединение {0} обновлено", id);
            return res.FirstOrDefault();
        }
    }
}

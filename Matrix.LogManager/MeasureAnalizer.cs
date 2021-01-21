using Microsoft.Practices.Unity;
using Neo4j.Driver.V1;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.LogManager
{
    public class MeasureAnalizer
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Dependency]
        public Bus Bus { get; set; }

        private readonly Dictionary<Guid, dynamic> cache = new Dictionary<Guid, dynamic>();

        public void LoadTags()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            cache.Clear();
            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                var res = sess.Run("match(t:Tag)<--(tb:Tube)where t.column return t.name as name, t.parameter as parameter,t.dataType as type,t.calc as calc,tb.id as objectId");

                foreach (var r in res)
                {
                    Guid objectId = Guid.Parse(r["objectId"].As<string>());
                    var tags = new List<dynamic>();
                    if (cache.ContainsKey(objectId))
                    {
                        tags.AddRange(cache[objectId]);
                    }

                    dynamic info = new ExpandoObject();
                    info.type = r["type"].As<string>();
                    info.parameter = r["parameter"].As<string>();
                    info.calc = r["calc"].As<string>();
                    info.tag = r["name"].As<string>();
                    tags.Add(info);

                    if (cache.ContainsKey(objectId))
                    {
                        cache[objectId] = tags;
                    }
                    else
                    {
                        cache.Add(objectId, tags);
                    }
                }
                logger.Debug("кеш тегов загружен ({0} тегов)", cache.Count);
            }
        }

        public void Analize(IEnumerable<dynamic> records)
        {
            var changes = new List<Tuple<dynamic,dynamic>>();
            foreach (var record in records)
            {
                string tp = record.type;
                Guid objId = Guid.Parse(record.objectId);

                if (!cache.ContainsKey(objId))
                {
                    continue;
                }

                var tags = cache[objId];

                foreach(var tag in tags)
                {
                    if(cache[objId].type != record.type || cache[objId].parameter != record.parameter)
                    {
                        continue;
                    }

                    changes.Add(new Tuple<dynamic, dynamic>(tag, record));
                }                
            }

            if (changes.Any())
            {
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["sql"].ConnectionString))
                {
                    con.Open();
                    foreach (var change in changes)
                    {
                        using (var com = new SqlCommand())
                        {
                            com.Connection = con;

                            switch((string)change.Item1.name)
                            {
                                case "Q н.у.":
                                    com.CommandText = "update rowscache set measureValue=@val, measureDate=@date where id=@id";
                                    com.Parameters.AddWithValue("@val", change.Item2.value);
                                    com.Parameters.AddWithValue("@date", change.Item2.date);
                                    com.Parameters.AddWithValue("@id", change.Item2.objectId);
                                    break;
                                case "pin":
                                    com.CommandText = "update rowscache set pin=@val, measureDate=@date where id=@id";
                                    com.Parameters.AddWithValue("@val", change.Item2.value);
                                    com.Parameters.AddWithValue("@date", change.Item2.date);
                                    com.Parameters.AddWithValue("@id", change.Item2.objectId);
                                    break;
                                case "pout":
                                    com.CommandText = "update rowscache set pout=@val, measureDate=@date where id=@id";
                                    com.Parameters.AddWithValue("@val", change.Item2.value);
                                    com.Parameters.AddWithValue("@date", change.Item2.date);
                                    com.Parameters.AddWithValue("@id", change.Item2.objectId);
                                    break;
                            }
                            
                            com.ExecuteNonQuery();
                            logger.Debug("обновлен кеш строк {0}={1} {2:dd.MM.yy HH:mm:ss}", change.Item2.objectId, change.Item2.value, change.Item2.date);
                        }
                    }
                }
            }
        }
    }
}

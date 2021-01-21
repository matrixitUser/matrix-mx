using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo4jClient;
using NLog;

namespace Matrix.ExportData
{
    /// <summary>
    /// получаем сведения о тегах
    /// пивотим на уровне субд
    /// </summary>
    public class Pivoter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<dynamic> Pivot(Guid[] tubeIds, DateTime start, DateTime end, string type)
        {
            var sw = new Stopwatch();


            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();

            logger.Trace("запрос параметров");
            var query = client.Cypher.Match("(t:Tube)-->(tg:Tag {dataType:{type}})").Where("t.id in {tubeIds}").WithParams(new { tubeIds = tubeIds, type = type }).With("tg,t.id as tid").Return((tg, tid) => new { tag = tg.Node<string>(), tubeId = tid.As<Guid>() });

            var paramsIndex = new Dictionary<Guid, Dictionary<string, dynamic>>();
            foreach (var res in query.Results)
            {
                var dr = res.tag.ToDynamic();
                dr.tubeId = res.tubeId;

                if (!paramsIndex.ContainsKey(res.tubeId))
                {
                    paramsIndex.Add(res.tubeId, new Dictionary<string, dynamic>());
                }

                var tubeParams = paramsIndex[res.tubeId];
                if (!tubeParams.ContainsKey(dr.parameter))
                {
                    tubeParams.Add(dr.parameter, dr);
                }
            }

            sw.Start();
            var tags = query.Results.Select(r =>
            {
                var dr = r.tag.ToDynamic();
                dr.tubeId = r.tubeId;
                return dr;
            });
            sw.Stop();
            logger.Trace("параметры получены, {0} мс", sw.ElapsedMilliseconds);

            var records = new List<dynamic>();

            //todo load records here and pivot            
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["sql"].ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SqlCommand())
                    {
                        command.Connection = connection;

                        command.Parameters.AddWithValue("@start", start);
                        command.Parameters.AddWithValue("@end", end);                        

                        var step = 100;
                        for (var offset = 0; offset <= tubeIds.Count(); offset += step)
                        {
                            command.CommandText = string.Format(@"select date,objectid,parameter,value,unit from [{0}] where objectid in({1}) and date between @start and @end",type, string.Join(",", tubeIds.Skip(offset).Take(step).Select(t => string.Format("'{0}'", t))));

                            var index = new Dictionary<Guid, Dictionary<DateTime, Dictionary<string, double>>>();

                            sw.Restart();
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var date = reader.GetDateTime(0);
                                    var objectId = reader.GetGuid(1);
                                    var parameter = reader.GetString(2);
                                    var value = reader.GetDouble(3);

                                    if (!index.ContainsKey(objectId))
                                    {
                                        index.Add(objectId, new Dictionary<DateTime, Dictionary<string, double>> { { date, new Dictionary<string, double>() } });
                                        //todo add value here
                                    }

                                    var dates = index[objectId];
                                    if (!dates.ContainsKey(date))
                                    {
                                        //add
                                        dates.Add(date, new Dictionary<string, double>());
                                    }

                                    var values = dates[date];
                                    if (!values.ContainsKey(parameter))
                                    {
                                        values.Add(parameter, value);
                                    }
                                }
                            }
                            sw.Stop();
                            logger.Trace("архивы по объектам получены, {0} мс", sw.ElapsedMilliseconds);
                            var foo = index;

                            sw.Restart();
                            records.AddRange(index.SelectMany(x => x.Value.Select(c =>
                            {
                                dynamic row = new ExpandoObject();
                                row.date = c.Key;
                                row.objectId = x.Key;
                                foreach (var v in c.Value)
                                {
                                    if (paramsIndex.ContainsKey(x.Key) && paramsIndex[x.Key].ContainsKey(v.Key))
                                    {
                                        var tag = paramsIndex[x.Key][v.Key].name;
                                        if (!(row as IDictionary<string, object>).ContainsKey(tag))
                                        {
                                            (row as IDictionary<string, object>).Add(tag, v.Value);
                                        }
                                    }
                                }
                                return row;
                            })).ToArray());
                            sw.Stop();
                            logger.Trace("расчет, за {0} мс", sw.ElapsedMilliseconds);
                        }


                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "ошибка при выполнении запроса");
                }
                finally
                {
                    connection.Close();
                }
            }

            return records;
        }
    }
}

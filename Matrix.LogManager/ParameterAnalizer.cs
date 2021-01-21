using Microsoft.Practices.Unity;
using Neo4jClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Matrix.LogManager
{
    public class ParameterAnalizer : IDisposable
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly ParametersCache cache = new ParametersCache();

        [Dependency]
        public Bus Bus { get; set; }

        private static readonly string[] types = new string[] { "Hour", "Day", "Current" };

        public ParameterAnalizer()
        {
            cache.Load();
        }

        public void Analize(IEnumerable<dynamic> records)
        {
            var parameteredRecords = records.Where(r => types.Contains((string)r.type));
            if (!parameteredRecords.Any()) return;

            parameteredRecords.GroupBy(r => r.objectId).ToList().ForEach(g => cache.Update(Guid.Parse(g.Key), g.Select(p => (string)p.parameter)));
        }

        public void Dispose()
        {
            cache.Dispose();
        }
    }

    class ParametersCache : IDisposable
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Dictionary<Guid, HashSet<string>> cache = new Dictionary<Guid, HashSet<string>>();
        private readonly Dictionary<Guid, HashSet<string>> newParameters = new Dictionary<Guid, HashSet<string>>();
        private readonly Timer syncTimer = new Timer();

        public ParametersCache()
        {
            syncTimer.Interval = 20 * 1000;
            syncTimer.Elapsed += (se, ea) =>
            {
                Save();
            };
            syncTimer.Start();
        }

        public void Load()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var client = new GraphClient(new Uri(url));
            client.Connect();
            var q = client.Cypher.Match("(t:Tube)-->(p:Parameter)").With("t.id as tubeId,extract(par in collect(p) | par.name) as parameters").Return((tubeId, parameters) => new { tubeId = tubeId.As<Guid>(), parameters = parameters.As<IEnumerable<string>>() });
            var res = q.Results;

            cache.Clear();
            res.ToList().ForEach(r =>
            {
                var set = new HashSet<string>();
                foreach (var parameter in r.parameters) set.Add(parameter);
                if (!cache.ContainsKey(r.tubeId)) cache.Add(r.tubeId, set);
            });

            logger.Debug("загружен кеш параметров по {0} объектам", res.Count());
        }

        public void Save()
        {
            if (newParameters.Any())
            {
                var url = ConfigurationManager.AppSettings["neo4j-url"];
                var client = new GraphClient(new Uri(url));
                client.Connect();
                foreach (var tubeId in newParameters.Keys)
                {
                    foreach (var parameter in newParameters[tubeId])
                    {
                        dynamic par = new ExpandoObject();
                        par.id = Guid.NewGuid();
                        par.type = "Parameter";
                        par.name = parameter;

                        var q = client.Cypher.Match("(t:Tube {id:{tubeId}})").With("t").Merge("(t)-[:parameter]->(p:Parameter {name:{name}})").
                            OnCreate().Set("p={parameter}").WithParams(new { tubeId = tubeId, name = parameter, parameter = par });
                        q.ExecuteWithoutResults();
                        logger.Debug("в базу добавлен параметр {0} для объекта {1}", parameter, tubeId);
                    }
                }
                logger.Debug("база обновлена");
            }
        }

        public void Update(Guid tubeId, IEnumerable<string> parameters)
        {
            var paramsSet = new HashSet<string>();
            if (!cache.ContainsKey(tubeId))
            {
                cache.Add(tubeId, paramsSet);
            }
            else
            {
                paramsSet = cache[tubeId];
            }

            foreach (var parameter in parameters)
            {
                if (!paramsSet.Contains(parameter))
                {

                    if (!newParameters.ContainsKey(tubeId)) newParameters.Add(tubeId, new HashSet<string>());
                    if (!newParameters[tubeId].Contains(parameter)) newParameters[tubeId].Add(parameter);

                    paramsSet.Add(parameter);
                    logger.Debug("новый параметр {0} по объекту {1}", parameter, tubeId);
                }
            }
        }

        public void Dispose()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
        }
    }
}

using Neo4j.Driver.V1;
using Newtonsoft.Json;
using NLog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    class CacheRepository : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ConnectionMultiplexer redis;

        private CacheRepository()
        {
            var host = ConfigurationManager.AppSettings["redis-host"];
            var port = int.Parse(ConfigurationManager.AppSettings["redis-port"]);
            redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints =
                {
                    { host, port }
                },
                KeepAlive = 180,
                AllowAdmin = true,
                SyncTimeout = 1000,
                ConnectTimeout = 5000
            });
            redis.PreserveAsyncOrder = false;
        }

        public dynamic Get(string type, Guid id)
        {
            var db = redis.GetDatabase();
            var key = new RedisKey().Append(type).Append(id.ToString());
            var result = db.StringGet(key);
            if (!result.HasValue) return null;

            dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
            return value;
        }

        public dynamic Get(string fullKey)
        {
            var db = redis.GetDatabase();
            var result = db.StringGet(fullKey);
            if (!result.HasValue) return null;

            dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
            return value;
        }


        static CacheRepository() { }

        private static readonly CacheRepository instance = new CacheRepository();
        public static CacheRepository Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            redis.Dispose();
        }

        public IEnumerable<dynamic> GetTags(IEnumerable<Guid> tubeIds)
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                var res = sess.Run("match(t:Tube)-[:tag]->(tag:Tag)where t.id in {ids} return tag.dataType,tag.name,tag.calc,tag.parameter,t.id", new { ids = tubeIds.Select(id=>id.ToString()).ToArray() }).
                    Select(r =>
                    {
                        dynamic tag = new ExpandoObject();
                        tag.dataType = r[0].As<string>();
                        tag.name = r[1].As<string>();
                        tag.calc = r[2].As<string>();
                        tag.parameter = r[3].As<string>();
                        tag.tubeId = r[4].As<string>();
                        return tag;
                    }).ToArray();
                logger.Debug("получены теги {0} шт", res.Length);
                return res;
            }            
        }
        
        public IEnumerable<dynamic> GetParameters(Guid tubeId)
        {
            var parameters = new List<dynamic>();

            //IEnumerable<RedisKey> keys = null;
            //lock (redis)
            //{
            var srv = redis.GetServer(redis.GetEndPoints().First());
            //keys = srv.Keys(pattern: string.Format("parameter{0}*", tubeId)).ToArray();
            //}

            foreach (var key in srv.Keys(pattern: string.Format("parameter{0}*", tubeId)))
            {
                var parameter = Get(key);
                parameters.Add(parameter);
            }
            return parameters;
        }
    }
}

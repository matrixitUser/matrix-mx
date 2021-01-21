using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Matrix.Web.Host.Data
{
    class CacheRepository : IDisposable
    {
        private readonly ConnectionMultiplexer redis;

        private IDictionary<string, RedisValue> cacheLocal = new Dictionary<string, RedisValue>();
        private List<string> sessionKeyList = new List<string>();
        private List<string> parameterKeyList = new List<string>();
        private CacheRepository()
        {
            try
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
            catch(Exception ex) { }
        }

        public dynamic Get(string type, Guid id)
        {
            
            try
            {
                var db = redis.GetDatabase();
                var key = new RedisKey().Append(type).Append(id.ToString());
                var result = db.StringGet(key);
                if (!result.HasValue) return null;
                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch
            {
                return GetLocal(type, id);
            }
        }

        public dynamic Get(string fullKey)
        {
            try
            {
                var db = redis.GetDatabase();
                var result = db.StringGet(fullKey);
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch
            {
                return GetLocal(fullKey);
            }
        }

        public void Set(string type, Guid id, dynamic value)
        {
            try
            {
                var db = redis.GetDatabase();
                var key = new RedisKey().Append(type).Append(id.ToString());
                var stringValue = JsonConvert.SerializeObject(value);
                db.StringSet(key, stringValue);
            }
            catch
            {
                SetLocal(type, id, value);
            }
        }

        public dynamic GetSession(Guid id)
        {
            var session = Get("session", id);
            //var session = GetLocal("session", id);
            if (session == null) return null;
            session.date = DateTime.Now;
            Set("session", id, session);
            //SetLocal("session", id, session);

            return session;
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
            try { redis.Dispose(); } catch { }
        }

       

        public IEnumerable<dynamic> GetSessions()
        {

            //IEnumerable<RedisKey> keys = null;
            //lock (redis)
            //{

            // TODO !!! StackExchange.Redis.RedisConnectionException !!!
            
            try
            {
                var srv = redis.GetServer(redis.GetEndPoints().First());

                var sessions = new List<dynamic>();
                //keys = srv.Keys(pattern: "session*").ToArray();
                //}
                foreach (var key in srv.Keys(pattern: "session*"))
                {
                    var session = Get(key);
                    if (session.date < DateTime.Now.AddDays(-1))
                    {
                        Del(key);
                        continue;
                    }
                    if (session is IDictionary<string, object>)
                    {
                        sessions.Add(session);
                    } 
                }
                
                return sessions;
            }
            catch
            {
                return GetSessionsLocal();
            }
        }

        public void SaveSession(dynamic session, Guid userId)
        {
            var sid = Guid.Parse(session.id.ToString());
            session.date = DateTime.Now;
            session.userId = userId;
            Set("session", sid, session);
            //SetLocal("session", sid, session);
        }

        public IEnumerable<dynamic> GetTags(Guid tubeId)
        {
            var wrap = Get("tags", tubeId);
            //var wrap = GetLocal("tags", tubeId);
            if (wrap == null) return null;
            return wrap.tags;
        }

        public void SetTags(Guid tubeId, dynamic tags)
        {
            dynamic wrap = new ExpandoObject();
            wrap.tags = tags;
            Set(wrap, "tags", tubeId);
            //SetLocal(wrap, "tags", tubeId);
        }

        public IEnumerable<dynamic> GetParameters(Guid tubeId)
        {
            //IEnumerable<RedisKey> keys = null;
            //lock (redis)
            //{
            try
            {
                var srv = redis.GetServer(redis.GetEndPoints().First());
                var parameters = new List<dynamic>();
                //keys = srv.Keys(pattern: string.Format("parameter{0}*", tubeId)).ToArray();
                //}

                foreach (var key in srv.Keys(pattern: string.Format("parameter{0}*", tubeId)))
                {
                    var parameter = Get(key);
                    parameters.Add(parameter);
                }
                return parameters;
            }
            catch { return GetParametersLocal(tubeId); }
        }

        public void DelParameters(Guid tubeId)
        {
            try
            {
                var srv = redis.GetServer(redis.GetEndPoints().First());
                var db = redis.GetDatabase();
                
                foreach (var key in srv.Keys(pattern: string.Format("parameter{0}*", tubeId)))
                {
                    db.KeyDelete(key);
                }
            }
            catch { DelParametersLocal(tubeId); }
        }

        public void SaveParameter(Guid tubeId, dynamic parameter)
        {
            Set(parameter, "parameter", tubeId, parameter.name);
        }

        public void Set(dynamic value, string type, params object[] args)
        {
            try
            {
                var db = redis.GetDatabase();
                var key = type;
                foreach (var arg in args)
                {
                    key += arg.ToString();
                }
                var stringValue = JsonConvert.SerializeObject(value);
                db.StringSet(key, stringValue);
            }
            catch
            {
                SetLocal(value, type, args);
            }
        }

        public dynamic GetCache(Guid nodeId)
        {
            var cache = Get("cache", nodeId);
            //var cache = GetLocal("cache", nodeId);
            return cache;
        }

        public void SaveCache(Guid nodeId, dynamic cache)
        {
            Set("cache", nodeId, cache);
            //SetLocal("cache", nodeId, cache);
        }

        public void Del(string type, Guid id)
        {
            try
            {
                var db = redis.GetDatabase();
                var key = new RedisKey().Append(type).Append(id.ToString());
                db.KeyDelete(key);
            }
            catch { DelLocal(type, id); }
        }

        public void Del(string key)
        {
            try
            {
                var db = redis.GetDatabase();
                db.KeyDelete(key);
            }
            catch { DelLocal(key); }
        }


        /*-------------- cache local ------------------*/
        public dynamic GetLocal(string type, Guid id)
        {
            try
            {
                var key = new RedisKey().Append(type).Append(id.ToString());
                var result = cacheLocal[key];
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch { return null; }
        }

        public dynamic GetLocal(string fullKey)
        {
            try
            {
                var result = cacheLocal[fullKey];
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch { return null; }
        }
        public dynamic GetLocal(RedisKey key)
        {
            try
            {
                var result = cacheLocal[key];
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch { return null; }
        }

        public IEnumerable<dynamic> GetSessionsLocal()
        {
            var sessions = new List<dynamic>();
            try
            {
                foreach (var key in sessionKeyList)
                {
                    var session = GetLocal(key);
                    if (session.date < DateTime.Now.AddDays(-1))
                    {
                        DelLocal(key);
                        continue;
                    }
                    if (session is IDictionary<string, object>)
                    {
                        sessions.Add(session);
                    }
                }
            }
            catch{}
            return sessions;
        }
        public void SetLocal(string type, Guid id, dynamic value)
        {
            try
            {
                var stringValue = JsonConvert.SerializeObject(value);
                var key = new RedisKey().Append(type).Append(id.ToString());
                if (cacheLocal.ContainsKey(key)) cacheLocal[key] = stringValue;
                else cacheLocal.Add(key, stringValue);
                if (type == "session")
                    if(!sessionKeyList.Contains(key))
                        sessionKeyList.Add(key);
                if (type == "parameter")
                    if (!parameterKeyList.Contains(key))
                        parameterKeyList.Add(key);
            }
            catch{ }
        }

        public IEnumerable<dynamic> GetParametersLocal(Guid tubeId)
        {

            var parameters = new List<dynamic>();
            try
            {
                foreach (var key in parameterKeyList)
                {
                    var parameter = GetLocal(key);
                    parameters.Add(parameter);
                }
            }
            catch { }
            return parameters;
        }

        public void DelParametersLocal(Guid tubeId)
        {
            try
            {
                foreach (var key in parameterKeyList)
                {
                    DelLocal(key);
                }
            }
            catch { }
        }


        public void SetLocal(dynamic value, string type, params object[] args)
        {
            var key = type;
            foreach (var arg in args)
            {
                key += arg.ToString();
            }
            
            var stringValue = JsonConvert.SerializeObject(value);
            if (cacheLocal.ContainsKey(key)) cacheLocal[key] = stringValue;
            else cacheLocal.Add(key, stringValue);

            if (type == "session")
                if (!sessionKeyList.Contains(key))
                    sessionKeyList.Add(key);
            if (type == "parameter")
                if (!parameterKeyList.Contains(key))
                    parameterKeyList.Add(key);
        }
        
        public void DelLocal(string type, Guid id)
        {
            var key = new RedisKey().Append(type).Append(id.ToString());

            if (parameterKeyList.Contains(key)) parameterKeyList.Remove(key);
            if (sessionKeyList.Contains(key)) sessionKeyList.Remove(key);
            if (cacheLocal.ContainsKey(key)) cacheLocal.Remove(key);
        }

        public void DelLocal(string key)
        {
            if (parameterKeyList.Contains(key)) parameterKeyList.Remove(key);
            if (sessionKeyList.Contains(key)) sessionKeyList.Remove(key);
            if(cacheLocal.ContainsKey(key)) cacheLocal.Remove(key);
        }
    }
}

using log4net;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Storage
{
    /*
    class RecordsRepository : IDisposable
    {

        private readonly static ILog log = LogManager.GetLogger(typeof(RecordsRepository));
        
        private ConnectionMultiplexer redis;

        public dynamic Get(Guid id)
        {
            try
            {
                var db = redis.GetDatabase();
                var key = new RedisKey().Append("poll-cache").Append(id.ToString());
                var result = db.StringGet(key);
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch (Exception ex) { log.Info(ex.Message + " in Get(Guid id)"); return null; }
        }

        public dynamic Get(string key)
        {
            try
            {
                var db = redis.GetDatabase();
                var result = db.StringGet(key);
                if (!result.HasValue) return null;

                dynamic value = JsonConvert.DeserializeObject<ExpandoObject>(result);
                return value;
            }
            catch (Exception ex) { log.Info(ex.Message + " in Get(string key)"); return null; }
        }

        public void Set(string key, dynamic value)
        {
            try
            {
                var db = redis.GetDatabase();
                var stringValue = JsonConvert.SerializeObject(value);
                db.StringSet(key, stringValue);
            }
            catch(Exception ex) { log.Info(ex.Message+ " in Set(string key, dynamic value)"); }
        }

        public void Set(Guid id, dynamic value)
        {
            try
            {
                var db = redis.GetDatabase();
                var stringValue = JsonConvert.SerializeObject(value);
                var key = new RedisKey().Append("poll-cache").Append(id.ToString());
                db.StringSet(key, stringValue);
            }
            catch (Exception ex) { log.Info(ex.Message + " in Set(Guid id, dynamic value)"); }
        }

        static RecordsRepository() { }

        private RecordsRepository()
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
                    AllowAdmin = true
                });
                redis.PreserveAsyncOrder = false;
            }
            catch (Exception ex) { log.Info(ex.Message + " in RecordsRepository()"); }
        }

        private static RecordsRepository instance = new RecordsRepository();
        public static RecordsRepository Instance
        {
            get { return instance; }
        }

        public void Dispose()
        {
            log.Info("работа репозитория завершена");
        }
    }
    */
}

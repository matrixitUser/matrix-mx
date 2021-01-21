using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using NLog;
using System.Web.Http.Cors;

namespace Matrix.Web.Host.Transport
{
    /// <summary>
    /// транспортный REST API контроллер, обрабатывает GET и POST запросы
    /// </summary>
    [EnableCors("*", "*", "*")]
    public class TransportController : ApiController
    {        
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public async Task<dynamic> Get(string message) //api/transport?message=... => "{head:{}, body{}}"
        {
            if (string.IsNullOrEmpty(message)) return null;

            dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(message);
            return await MessageProccessor.Process(msg);
        }

        // POST api/transport [FromBody]
        [HttpPost]
        public async Task<dynamic> Post(dynamic message)
        {
            if (message == null) return null;
            var raw = message.ToString();
            dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(raw, new Newtonsoft.Json.JsonSerializerSettings { DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local });

            var ans = await MessageProccessor.Process(msg);

            var ass = Newtonsoft.Json.JsonConvert.SerializeObject(ans, new Newtonsoft.Json.JsonSerializerSettings { DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local });

            var poo = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(ass, new Newtonsoft.Json.JsonSerializerSettings { DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local });
            return poo;
        }

        
    }
}

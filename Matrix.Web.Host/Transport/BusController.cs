using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using NLog;

namespace Matrix.Web.Host.Transport
{
    public class BusController : ApiController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        public async Task<dynamic> Get(string message)
        {
            dynamic msg = JsonConvert.DeserializeObject<ExpandoObject>(message);
            var bus = ServiceLocator.Current.GetInstance<Bus>();
            if (msg.head.what == "export")
            {
                var ans =await bus.SyncSend("export", msg);                
                return ans.Result;
            }

            return "sotona";
        }
    }
}

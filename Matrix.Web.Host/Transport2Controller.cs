using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Practices.ServiceLocation;
using NLog;

namespace Matrix.Web.Host
{
    public class Transport2Controller : ApiController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        public dynamic Get(string message)
        {
            logger.Trace("поступило сообщение по каналу GET2 {0}", message);

            if (string.IsNullOrEmpty(message)) return null;
            dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(message);

            var valid=MessageValuidator.Validate(msg);
            if (valid != string.Empty)
            {
                return valid;
            }

            var bus = ServiceLocator.Current.GetInstance<Bus>();
            //var task = bus.Send(msg);
            //task.Wait();
            //var res = task.Result;

            //return res.Result;
            return "";
        }
    }
}

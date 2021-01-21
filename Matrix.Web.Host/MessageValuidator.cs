using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host
{
    class MessageValuidator
    {
        public static string Validate(dynamic message)
        {
            var dmsg = message as IDictionary<string, object>;
            if (!dmsg.ContainsKey("head"))            
                return "нет head";

            var dhead = message.head as IDictionary<string, object>;
            if (!dhead.ContainsKey("id"))
                return "нет head.id";

            if (!dhead.ContainsKey("what"))
                return "нет head.what";

            if (!dhead.ContainsKey("isSync"))
                return "нет head.isSync";
            //hhh

            return string.Empty;
        }
    }
}

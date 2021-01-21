using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Handlers
{
    class EventSubscribeHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("subscribe");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "subscribe-event")
            {

            }

            return Helper.BuildMessage(what);
        }
    }
}

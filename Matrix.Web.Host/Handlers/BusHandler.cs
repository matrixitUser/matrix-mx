using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using NLog;

namespace Matrix.Web.Host.Handlers
{
    /// <summary>
    /// пересылает сообщения в шину
    /// </summary>
    class BusHandler : IHandler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private Bus bus;

        public BusHandler(Bus bus)
        {
            this.bus = bus;
        }

        public bool CanAccept(string what)
        {
            return true;
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            logger.Debug("сообщение для шины");

            //string what = message.head.what;
            //if (what.StartsWith("export"))
            //{
            //    var res = await bus.SyncSend("export", message);
            //    return res;
            //}

            return null;
        }
    }
}

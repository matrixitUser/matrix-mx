using log4net;
using Matrix.PollServer.Nodes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Handlers
{
    class ManagmentHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ManagmentHandler));

        public bool CanHandle(string what)
        {
            return what.StartsWith("managment");
        }

        public void Handle(dynamic message)
        {
            string what = message.head.what;

            if (what == "managment-change")
            {
                foreach (dynamic node in message.body.nodes)
                {
                    NodeManager.Instance.Update(node);
                }

                foreach (dynamic relation in message.body.relations)
                {
                    RelationManager.Instance.Update(relation);
                }
            }

            if (what == "managment-pollserver-reset")
            {
                log.Info("перезагрузка нодов");                
                ManagmentHelper.ResetServer(message.body.serverName);
            }

            if (what == "managment-kill-com")
            {
                string port = message.body.port;
                ManagmentHelper.KillCom(port);
            }

            if (what == "managment-start-com")
            {
                string port = message.body.port;
                ManagmentHelper.StartCom(port);
            }

            if (what == "managment-ping")
            {
                
            }
        }
    }

    public static class ManagmentHelper
    {
        public static void ResetServer(string serverName)
        {
            if (serverName != ConfigurationManager.AppSettings["name"]) return;

            RelationManager.Instance.Cleare();
            NodeManager.Instance.Load();
        }

        public static void KillCom(string port)
        {
            var modems = NodeManager.Instance.GetNodes<Nodes.Csd.PoolModem>().Where(m => m.GetPort().ToLower() == port.ToLower());
            foreach (var modem in modems)
            {
                modem.Dispose();
            }
        }

        public static void StartCom(string port)
        {
            var modems = NodeManager.Instance.GetNodes<Nodes.Csd.PoolModem>().Where(m => m.GetPort().ToLower() == port.ToLower());
            foreach (var modem in modems)
            {
                modem.ReInit();
            }
        }
    }
}

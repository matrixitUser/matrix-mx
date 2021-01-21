using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;
using log4net;

namespace Matrix.PollServer.Nodes.Http
{
    class HttpPort : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HttpPort));

        private dynamic content;
        public HttpPort(dynamic content)
        {
            this.content = content;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        public string GetName()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("name")) return "";
            return (string)content.name;
        }

        //protected override bool OnLock(Route route)
        //{
        //    return true;
        //}

        //protected override bool OnUnlock(Route route)
        //{
        //    return true;
        //}

        protected override int OnPrepare(Routes.Route route, int port, PollTask initiator)
        {
            route.Subscribe(this, (bytes, dir) =>
            {
                log.Trace(string.Format("[{0}] {1} [{2}]", GetName(), dir == Routes.Direction.FromInitiator ? "->" : "<-", string.Join(",", bytes.Select(b => b.ToString("X2")))));
                route.Send(this, bytes, dir);
            });
            return Codes.SUCCESS;
        }
    }
}

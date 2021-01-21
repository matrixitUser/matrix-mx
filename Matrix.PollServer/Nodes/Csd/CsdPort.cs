using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Csd
{
    class CsdPort : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CsdPort));

        public CsdPort(dynamic raw)
        {
            content = raw;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        protected override bool OnLock(Route route, PollTask initiator)
        {
            return true;
        }

        protected override bool OnUnlock(Route route)
        {
            return true;
        }

        public string GetName()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("name")) return "";
            return (string)content.name;
        }

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            route.Subscribe(this, (bytes, dir) =>
            {
                log.Trace(string.Format("[{0}] {1} [{2}]", GetName(), dir == Routes.Direction.FromInitiator ? "->" : "<-", string.Join(",", bytes.Select(b => b.ToString("X2")))));
                route.Send(this, bytes, dir);
            });
            return 0;
        }

        protected override void OnRelease(Route route, int port)
        {
            
        }

        public override string ToString()
        {
            return GetName();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes
{
    class TransparentNode : PollNode
    {
        private readonly dynamic raw;
        public TransparentNode(dynamic raw)
        {
            this.raw = raw;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)raw.id);
        }
    }
}

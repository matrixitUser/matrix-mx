using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Com
{
    class ComPort : PollNode
    {
        public ComPort(dynamic content)
        {
            this.content = content;
        }
    }
}

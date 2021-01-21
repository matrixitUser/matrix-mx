using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Lan
{
    class LanPort : PollNode
    {
        
        public LanPort(dynamic content)
        {
            this.content = content;
        }       
    }
}

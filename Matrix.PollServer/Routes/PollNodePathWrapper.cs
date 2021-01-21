using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.PollServer.Nodes;

namespace Matrix.PollServer.Routes
{
    class PollNodePathWrapper
    {
        public PollNode Node { get; private set; }
        public int Left { get;  set; }
        
        public PollNodePathWrapper(PollNode node, int left = 1)
        {
            Node = node;
            Left = left;            
        }
    }
}

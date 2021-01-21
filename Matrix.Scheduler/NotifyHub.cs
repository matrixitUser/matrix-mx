using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Matrix.Scheduler
{
    public class NotifyHub : Hub
    {
        public void Send(string haa)
        {
            this.Clients.All.accept("hello " + haa);
        }
    }
}

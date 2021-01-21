using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    public interface IConnector : IDisposable
    {
        //void Restart();
        bool Relogin();
        void Subscribe();
        dynamic SendMessage(dynamic message);
        dynamic SendByAPI(dynamic message);
    }
}

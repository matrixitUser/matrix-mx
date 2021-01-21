using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.CheckServer
{
    public interface IConnector : IDisposable
    {
        bool Relogin();
        void Subscribe();
        dynamic SendMessage(dynamic message);
        dynamic SendByAPI(dynamic message);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.CheckServer.Handlers
{
    interface IHandler
    {
        bool CanHandle(string what);
        void Handle(dynamic message);
    }
}

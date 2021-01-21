using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SignalingServer.Handlers
{
    interface IHandler
    {
        bool CanAccept(string what);
        //void Handle(dynamic message);
        //dynamic Handle(dynamic session, dynamic message);
        Task<dynamic> Handle(dynamic session, dynamic message);
    }
}

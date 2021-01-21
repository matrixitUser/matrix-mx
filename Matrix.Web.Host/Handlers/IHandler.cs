using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Matrix.Web.Host.Handlers
{
    interface IHandler
    {
        bool CanAccept(string what);
        //dynamic Handle(dynamic session, dynamic message);
        Task<dynamic> Handle(dynamic session, dynamic message);
    }
}

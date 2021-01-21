using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Data
{
    interface ITokenHandler
    {
        void Handle(IEnumerable<dynamic> tokens);        
    }
}

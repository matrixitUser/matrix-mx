using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;

namespace Matrix.ModemPool
{
    public class ApiModule : NancyModule
    {
        public ApiModule()
        {
            Get[""] = (_) =>
            {
                return "hello";
            };
        }
    }
}

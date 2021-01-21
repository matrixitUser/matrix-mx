using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    class HelperHandler : IHandler
    {       
        public bool CanAccept(string what)
        {
            return what.StartsWith("helper");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            if (message.head.what == "helper-create-guid")
            {
                int count = int.Parse(message.body.count.ToString());

                var ans = Helper.BuildMessage("helper-guid");
                ans.body.guids = new List<Guid>();
                for (int i = 0; i < count; i++)
                {
                    ans.body.guids.Add(Guid.NewGuid());
                }                
                return ans;
            }


            if (message.head.what == "helper-create-md5")
            {
                string text = message.body.text;

                var ans = Helper.BuildMessage("helper-create-md5");
                ans.body.md5 = AuthHandler.GetHashString(text);                                
                return ans;
            }

            var nullAns = Helper.BuildMessage("helper-undefined");
            return nullAns;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    class DriverHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("driver");
        }

        public async  Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid sessionId = Guid.Parse(session.userId);

            if (what == "driver-list")
            {
                var drivers = StructureGraph.Instance.GetDrivers(Guid.Parse(session.userId));
                var ans = Helper.BuildMessage(what);
                ans.body.drivers = drivers;
                return ans;
            }

            if (what == "drivers-save")
            {
                if (false)
                {
                    var ans = Helper.BuildMessage("access-denied");
                    ans.body.message = "не хватает прав";
                    return ans;
                }

                foreach (var driver in message.body.drivers)
                {
                    dynamic msg = Helper.BuildMessage("driver-update");
                    msg.body.action = "update";
                    msg.body.driver = driver;
                    ClientsNotifier.Instance.NotifyAll(msg);
                    StructureGraph.Instance.SaveDriver(driver, Guid.Parse(session.userId));
                }
            }
            if (what == "drivers-create")
            {
                string name = message.body.name;
                dynamic msg = Helper.BuildMessage("driver-create");
                msg.body.action = "create";
                ClientsNotifier.Instance.NotifyAll(msg);
                StructureGraph.Instance.CreateDriver(name,Guid.Parse(session.userId));
            }
            return Helper.BuildMessage(what);
        }
    }
}

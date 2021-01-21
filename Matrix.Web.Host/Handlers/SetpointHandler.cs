using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Dynamic;
using System.Threading;

namespace Matrix.Web.Host.Handlers
{
    class SetpointHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("setpoint");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid userId = Guid.Parse(session.userId.ToString());
            if (what == "setpoint-event")
            {
                var events = CacheRepository.Instance.Get("setpoint-event");
                //var events = CacheRepository.Instance.GetLocal("setpoint-event");
                var ans = Helper.BuildMessage(what);
                ans.body.events = events;
                return ans;
            }
            if (what == "setpoint-recording-rows")
            {
                List<Guid> objectIds = new List<Guid>();
                foreach (var row in message.body.rows)
                {
                    Guid id = Guid.Parse((string)row.id);
                    Guid objectId = Guid.Parse((string)row.objectId);
                    DateTime dateStart = DateTime.Parse(row.dateStart.ToString());
                    string name = row.name.ToString();
                    double value = Double.Parse(row.value.ToString());
                    string parameter = row.parameter.ToString();
                    string tag = row.tag.ToString();
                    string message1 = row.message.ToString();
                    double setPoint = Double.Parse(row.setPoint.ToString());
                    TubeEvent.Instance.CreateRow(dateStart, objectId, message1, parameter, name, value, tag, DateTime.Now, setPoint);
                }
                var ans = Helper.BuildMessage(what);
                return ans;
            }
            if (what == "setpoint-updating-rows")
            {
                foreach (var row in message.body.rows)
                {
                    Guid id = Guid.Parse((string)row.id);
                    DateTime dateEnd = DateTime.Parse(row.dateEnd.ToString());
                    TubeEvent.Instance.UpdateDateEnd(dateEnd, id);
                }
                
                var ans = Helper.BuildMessage(what);
                return ans;
            }
            if (what == "setpoint-get-active-events")
            {
                List<Guid> objectIds = new List<Guid>();
                foreach (var objectId in message.body.objectIds)
                {
                    objectIds.Add(Guid.Parse(objectId.ToString()));
                }
                var rows = TubeEvent.Instance.GetActiveEvents(objectIds);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-get-ids-datestart")
            {
                List<Guid> objectIds = new List<Guid>();
                foreach (var objectId in message.body.objectIds)
                {
                    objectIds.Add(Guid.Parse(objectId.ToString()));
                }
                DateTime dateStart = DateTime.Parse(message.body.dateStart.ToString());
                var rows = TubeEvent.Instance.GetByObjectIdAndDateStart(objectIds, dateStart, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-get-datestart")
            {
                DateTime dateStart = DateTime.Parse(message.body.dateStart.ToString());
                var rows = TubeEvent.Instance.GetByDateStart(dateStart, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-get-by-date-startend")
            {
                DateTime startDate = DateTime.Parse(message.body.startDate.ToString());
                DateTime endDate = DateTime.Parse(message.body.endDate.ToString());
                var rows = TubeEvent.Instance.GetByDateStartEnd(startDate, endDate, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-get-active-events-all")
            {
                //TubeEvent.Instance.GetActiveEvents(new List<Guid>() {Guid.Parse("9E4D2A48-9B0E-474D-A9D3-47FC246C7107") }, userId);
                var rows = TubeEvent.Instance.GetActiveEventsAll();
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-get-active-events-dateEnd")
            {
                //TubeEvent.Instance.GetActiveEvents(new List<Guid>() {Guid.Parse("9E4D2A48-9B0E-474D-A9D3-47FC246C7107") }, userId);
                var rows = TubeEvent.Instance.GetActiveEventsAllOnlyDateEnd();
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            if (what == "setpoint-quit")
            {
                Guid id = Guid.Parse((string)message.body.id);
                TubeEvent.Instance.UpdateDateQuit(DateTime.Now, id);
                var rows = TubeEvent.Instance.GetById(id, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }
            return Helper.BuildMessage(what);
        }

    }
}

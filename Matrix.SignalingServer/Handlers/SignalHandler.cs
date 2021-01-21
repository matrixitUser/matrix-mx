
using log4net;
using Matrix.SignalingServer.Data;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Matrix.SignalingServer.Handlers
{
    class SignalHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SignalHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("signal");
        }

        private readonly Bus bus;

        public SignalHandler()
        {
            //bus = ServiceLocator.Current.GetInstance<Bus>();
        }
        
        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;            
            if(what == "signal-get-active-events-all")
            {
                var ans = Helper.BuildMessage(what);
                try
                {
                    List<dynamic> activeEvents = GetActiveEventsAll();
                    //List<dynamic> activeEventsReplay = activeEvents.FindAll(x => x.replay == true || x.replay == null);
                    List<dynamic> rows = new List<dynamic>();
                    foreach (var activeEvent in activeEvents)
                    {
                        dynamic row = new ExpandoObject();
                        row.id = activeEvent.id;
                        row.objName = activeEvent.name;
                        row.parameter = activeEvent.tag;
                        row.parameterSetPoint = activeEvent.parameter;
                        row.value = activeEvent.value;
                        row.message = activeEvent.message;
                        row.dateStart = activeEvent.dateStart; 
                        row.dateEnd = activeEvent.dateEnd;
                        row.dateQuit = activeEvent.dateQuit;
                        row.dateNormalize = activeEvent.dateEnd;
                        row.setPoint = activeEvent.setPoint;
                        row.replay = activeEvent.replay;
                        rows.Add(row);
                    }
                    ans.body.rows = rows;
                    ans.body.IsDbOk = true;
                }
                catch(Exception ex)
                {
                    ans.body.IsDbOk = false;
                }
                ans.body.date = DateTime.Now;

                return ans;
            }
            if (what == "signal-get-active-events-not-replay")
            {
                var ans = Helper.BuildMessage(what);
                try
                {
                    List<dynamic> activeEvents = GetActiveEventsAll();
                    List<dynamic> activeEventsReplay = activeEvents.FindAll(x => x.replay == false);
                    List<dynamic> rows = new List<dynamic>();
                    foreach (var activeEvent in activeEventsReplay)
                    {
                        dynamic row = new ExpandoObject();
                        row.id = activeEvent.id;
                        row.objName = activeEvent.name;
                        row.parameter = activeEvent.tag;
                        row.parameterSetPoint = activeEvent.parameter;
                        row.value = activeEvent.value;
                        row.message = activeEvent.message;
                        row.dateStart = activeEvent.dateStart;
                        row.dateEnd = activeEvent.dateEnd;
                        row.dateQuit = activeEvent.dateQuit;
                        row.dateNormalize = activeEvent.dateEnd;
                        row.setPoint = activeEvent.setPoint;
                        row.replay = activeEvent.replay;
                        rows.Add(row);
                    }
                    ans.body.rows = rows;
                    ans.body.IsDbOk = true;
                }
                catch (Exception ex)
                {
                    ans.body.IsDbOk = false;
                }
                ans.body.date = DateTime.Now;

                return ans;
            }
            if (what == "signal-get-by-date-startend")
            {
                var ans = Helper.BuildMessage(what);
                try
                {
                    DateTime startDate = DateTime.Parse(message.body.startDate.ToString());
                    DateTime endDate = DateTime.Parse(message.body.endDate.ToString());
                    var eventsArchive = GetByDateStartEnd(startDate, endDate);
                    List<dynamic> rows = new List<dynamic>();
                    foreach (var eventArchive in eventsArchive)
                    {
                        dynamic row = new ExpandoObject();
                        row.id = eventArchive.id;
                        row.objName = eventArchive.name;
                        row.parameter = eventArchive.tag;
                        row.parameterSetPoint = eventArchive.parameter;
                        row.value = eventArchive.value;
                        row.message = eventArchive.message;
                        row.dateStart = eventArchive.dateStart;
                        row.dateEnd = eventArchive.dateEnd;
                        row.dateQuit = eventArchive.dateQuit;
                        row.dateNormalize = eventArchive.dateEnd;
                        row.setPoint = eventArchive.setPoint;
                        row.replay = eventArchive.replay;
                        rows.Add(row);
                    }
                    ans.body.rows = rows;
                    ans.body.IsDbOk = true;
                }
                catch (Exception ex)
                {
                    ans.body.IsDbOk = false;
                }
                ans.body.date = DateTime.Now;
                return ans;
            }
            if (what == "signal-quit")
            {
                var ans = Helper.BuildMessage(what);
                Guid id = Guid.Parse((string)message.body.id);
                var rows = Quit(id);
                ans.body.rows = rows;
                return ans;
            }
            return Helper.BuildMessage("unhandled");
        }
        public List<dynamic> GetActiveEvents(List<Guid> objectIds)
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-active-events");
            msg.body.objectIds = objectIds;
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetActiveEventsAll()
        {
            List<dynamic> rows = new List<dynamic>();
            if (IsDb)
            {
                rows = TubeEvent.Instance.GetActiveEventsAll();
            }
            else
            {
                dynamic msg = Helper.BuildMessage("setpoint-get-active-events-all");
                var answer = ApiConnector.Instance.SendMessage(msg);
                rows = answer.body.rows;
            }
            
            return rows;
        }
        public bool IsDb
        {
            get
            {
                var strDbOrApi = ConfigurationManager.AppSettings.Get("db-or-api");
                return (strDbOrApi == "db")? true: false;
            }
        }
        public List<dynamic> Quit(Guid id)
        {
            List<dynamic> rows;
            if (IsDb)
            {
                TubeEvent.Instance.UpdateDateQuit(DateTime.Now, id);
                rows = TubeEvent.Instance.GetById(id);
            }
            else
            {
                dynamic msg = Helper.BuildMessage("setpoint-quit");
                msg.body.id = id;
                var answer = ApiConnector.Instance.SendMessage(msg);
                rows = answer.body.rows;
            }
            return rows;
        }
        public List<dynamic> GetByObjectIdAndDateStart(List<Guid> objectIds, DateTime dateStart)
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-ids-datestart");
            msg.body.objectIds = objectIds;
            msg.body.dateStart = dateStart;
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetByDateStart(DateTime dateStart)
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-datestart");
            msg.body.dateStart = dateStart;
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetByDateStartEnd(DateTime startDate, DateTime endDate)
        {
            List<dynamic> rows;
            if (IsDb)
            {
                rows = TubeEvent.Instance.GetByDateStartEnd(startDate, endDate);
            }
            else
            {
                dynamic msg = Helper.BuildMessage("setpoint-get-by-date-startend");
                msg.body.startDate = startDate;
                msg.body.endDate = endDate;
                var answer = ApiConnector.Instance.SendMessage(msg);
                rows = answer.body.rows;
            }
            return rows;
        }
    }
}

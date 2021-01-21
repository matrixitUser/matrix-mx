
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Microsoft.Office.Interop.Excel;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Handlers
{
    class RecordsHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RecordsHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("record");
        }

        private readonly List<IRecordHandler> handlers = new List<IRecordHandler>();

        public void AddHandler(IRecordHandler handler)
        {
            handlers.Add(handler);
        }

        private readonly Bus bus;

        public RecordsHandler()
        {
            bus = ServiceLocator.Current.GetInstance<Bus>();
        }

        private void PostProccess(IEnumerable<DataRecord> records)
        {

        }

        private IEnumerable<dynamic> AdaptRecords(IEnumerable<DataRecord> original)
        {
            foreach (var rec in original)
            {
                dynamic record = new ExpandoObject();
                record.id = rec.Id;
                record.date = rec.Date;
                record.objectId = rec.ObjectId;
                record.type = rec.Type;
                switch (rec.Type)
                {
                    case "Hour":
                    case "Day":
                        record.parameter = rec.S1;
                        record.unit = rec.S2;
                        record.value = rec.D1;
                        record.dateReceive = DateTime.Now;
                        yield return record;
                        break;
                    case "Current":
                        record.parameter = rec.S1;
                        record.unit = rec.S2;
                        record.value = rec.D1;
                        record.dateReceive = DateTime.Now;
                        yield return record;
                        break;
                    case "Constant":
                        record.name = rec.S1;
                        record.value = rec.S2;
                        yield return record;
                        break;
                    case "LogMessage":
                        record.message = rec.S1;
                        yield return record;
                        break;
                    case "MatrixSignal":
                        record.level = rec.D1;
                        yield return record;
                        break;
                    default: continue;
                }
            }
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse((string)session.user.id);
            if (what == "records-save")
            {
                //var sw = new Stopwatch();
                //sw.Start();
                var records = new List<DataRecord>();
                foreach (var raw in message.body.records)
                {
                    records.Add(EntityExtensions.ToRecord(raw));
                }
                
                //RecordsBackgroundProccessor.Instance.AddPart(records);
                RecordAcceptor.Instance.Save(records);

                //хук для паралельной обработки записей в микросервисе
                bus.SendRecords(AdaptRecords(records)); 

                return Helper.BuildMessage(what);
            }
            if (what == "records-get-load-pretty")
            {
                var objectIds = new List<Guid>();
                foreach (string target in message.body.targets)
                {
                    objectIds.Add(Guid.Parse(target));
                }

                DateTime start = message.body.start;
                DateTime end = message.body.end;

                string type = message.body.type;
                var records = RecordsDecorator.Decorate(objectIds.ToArray(), start, end, type, userId).ToDynamic();
                var ans = Helper.BuildMessage(what);
                ans.body.records = records;
                return ans;
            }
            if (what == "records-get")
            {
                var objectIds = new List<Guid>();
                foreach (string target in message.body.targets)
                {
                    var id = Guid.Parse(target);
                    if (StructureGraph.Instance.CanSee(id, Guid.Parse((string)session.userId)))
                        objectIds.Add(id);
                }

                DateTime start = message.body.start;
                DateTime end = message.body.end;

                string type = message.body.type;

                var records = Cache.Instance.GetRecords(start, end, type, objectIds.ToArray()).ToDynamic();
                var ans = Helper.BuildMessage(what);
                ans.body.records = records;
                return ans;
            }
            if (what == "records-get1")
            {
                var objectIds = new List<Guid>();
                foreach (string target in message.body.targets)
                {

                    var id = Guid.Parse(target);
                    //if (StructureGraph.Instance.CanSee(id, Guid.Parse((string)session.userId)))
                    objectIds.Add(id);
                }

                DateTime start = DateTime.Parse(message.body.start);
                DateTime end = DateTime.Parse(message.body.end);

                string type = message.body.type;

                var records = Cache.Instance.GetRecords(start, end, type, objectIds.ToArray()).ToDynamic();
                var rec = records.OrderBy(x => x.date);
                var ans = Helper.BuildMessage(what);
                ans.body.records = rec;
                return ans;
            }
            if (what == "records-get-with-ids-and-s1")
            {
                List<Guid> listIds = new List<Guid>();
                Guid objectId = Guid.Parse((string)message.body.objectId);
                listIds.Add(objectId);
                if(message.body.cmd == "findAnotherTubes")
                {
                    var objectIds = StructureGraph.Instance.GetIdsByTubeId(objectId);
                    listIds.AddRange(objectIds);
                }
                else if(((string)message.body.cmd).Contains("ids:"))
                {
                    listIds.Clear();
                    string[] strIds = ((string)message.body.cmd).Substring(4).Split(',');
                    for(int i = 0; i < strIds.Length; i++)
                    {
                        if(Guid.TryParse(strIds[i], out Guid guidId)){
                            listIds.Add(guidId);
                        }
                    }
                }
                DateTime start = message.body.start;
                DateTime end = message.body.end;

                string type = message.body.type;
                string s1 = message.body.s1;
                var records = Cache.Instance.GetWithIdAndS1Records(listIds, start, end, type, s1).ToDynamic();
                var ans = Helper.BuildMessage(what);
                ans.body.records = records;
                return ans;
            }
            if (what == "records-get-only-with-type")
            {
                DateTime start = message.body.start;
                DateTime end = message.body.end;

                string type = message.body.type;
                var records = Cache.Instance.GetDataOnlyWithTypeRecords(start, end, type).ToDynamic();
                var ans = Helper.BuildMessage(what);
                ans.body.records = records;
                return ans;
            }
            if (what == "records-get-dates")
            {
                Guid id = Guid.Parse(message.body.id.ToString());
                DateTime start = message.body.start;
                DateTime end = message.body.end;
                string type = message.body.type;

                DateTime[] records = Cache.Instance.GetDateSet(start, end, type, id).ToArray();
                var ans = Helper.BuildMessage(what);
                ans.body.records = records;
                return ans;
            }
            if (what == "records-save-count")
            {
                Guid objectId = Guid.Parse(message.body.objectId.ToString());
                DateTime date = DateTime.Now;
                if (DateTime.TryParse(Convert.ToString(message.body.date), out DateTime tmpDate))
                {
                    date = tmpDate;
                }
                double count = Convert.ToDouble((string)message.body.count);
                string comment = message.body.comment.ToString();
                var records = new List<DataRecord>();
                records.Add(EntityExtensions.ToRecord(CountRecord(objectId, date, count, "Оплата",comment)));
                
                RecordAcceptor.Instance.Save(records);
                
                return Helper.BuildMessage(what);
            }
            if (what == "records-delete")
            {
                var records = new List<DataRecord>();
                List<Guid> listIds = new List<Guid>();
                foreach (var id in message.body.ids)
                {
                    listIds.Add(Guid.Parse((string)id));
                }
                string type = (string)message.body.type;
                Cache.Instance.DeleteRecords(listIds, type);
                var ans = Helper.BuildMessage(what);
                return ans;
            }
            return Helper.BuildMessage("unhandled");
        }
        private dynamic CountRecord(Guid objectId, DateTime date, double count, string s1, string comment)
        {
            dynamic countData = new ExpandoObject();
            countData.id = Guid.NewGuid().ToString();
            countData.date = DateTime.Now;
            countData.objectId = objectId.ToString();
            countData.type = "Count";
            countData.d1 = count;
            countData.s1 = s1;
            countData.s2 = comment;
            countData.dt1 = date;
            return countData;
        }
    }
}

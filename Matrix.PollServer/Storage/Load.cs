using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Storage
{
    class Load
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Saver));

        public List<RecordLoad> records = new List<RecordLoad>();
        /*
        public List<dynamic> LoadRecords(DateTime start, DateTime end, Guid[] ids, string type)
        {
            RecordLoad rec = new RecordLoad();
            dynamic message = Helper.BuildMessage("records-get");
            message.body.targets = new Guid[] { GetId() };//Guid.Parse("758b55b6-3853-4624-834a-652bbf8fcc54") };
            message.body.start = start;
            message.body.end = end;
            message.body.type = "Current";// type;
            records.Clear();
            List<dynamic> recordList = new List<dynamic>();
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            foreach (var fileRec in file.body.records)
            {
                rec = new RecordLoad();
                rec.id = fileRec.id;
                rec.type = fileRec.type;
                rec.date = fileRec.date;
                rec.objectId = Guid.Parse(fileRec.objectId);
                rec.d1 = fileRec.d1;
                if(fileRec.d2 !=null)
                    rec.d2 = fileRec.d2;
                if (fileRec.d3 != null)
                    rec.d3 = fileRec.d3;
                if (fileRec.s1 != null)
                    rec.s1 = fileRec.s1;
                if (fileRec.s2 != null)
                    rec.s2 = fileRec.s2;
                rec.dt1 = fileRec.dt1;
                records.Add(rec);
            }
            recordList.AddRange(records);
            return recordList;
        }
        */
    }
    public class RecordLoad
    {
        public string id { get; set; }            
        public string type { get; set; }          
        public DateTime date { get; set; }  
        public Guid objectId { get; set; }  
        public double d1 { get; set; }       
        public double d2 { get; set; }
        public double d3 { get; set; }       
        public string s1 { get; set; }
        public string s2 { get; set; }     
        public DateTime dt1 { get; set; }      
    }
}

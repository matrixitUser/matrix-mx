//using log4net;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Matrix.PollServer.Storage;

//namespace Matrix.PollServer
//{
//    public class RecordsFilter
//    {
//        private readonly static ILog log = LogManager.GetLogger(typeof(RecordsFilter));

//        private readonly List<dynamic> notFilteredRecords = new List<dynamic>();

//        public void Filter(IEnumerable<dynamic> records)
//        {
//            foreach (var filter in listFilters)
//            {
//                if (!records.Any()) break;
//                records = filter(records);
//            }

//            if (records.Any())
//                Task.Factory.StartNew(() => Saver.Save(records));
//        }

//        private RecordsFilter()
//        {
//            listFilters = new List<Func<IEnumerable<dynamic>, IEnumerable<dynamic>>>() 
//            { 
//                BaseFilter, 
//                DayFilter,
//                ConstantFilter 
//            };
//        }

//        static RecordsFilter() { }

//        private static readonly RecordsFilter instance = new RecordsFilter();
//        public static RecordsFilter Instance
//        {
//            get
//            {
//                return instance;
//            }
//        }

//        private List<Func<IEnumerable<dynamic>, IEnumerable<dynamic>>> listFilters;

//        private IEnumerable<dynamic> BaseFilter(IEnumerable<dynamic> records)
//        {
//            List<dynamic> normal = new List<dynamic>();
//            foreach (var record in records)
//            {
//                if (record == null) continue;

//                var recordAsDict = (IDictionary<string, object>)record;
//                if (!recordAsDict.ContainsKey("date") ||
//                    !recordAsDict.ContainsKey("objectId") ||
//                    !recordAsDict.ContainsKey("type")) continue;

//                record.id = Guid.NewGuid();
//                normal.Add(record);
//            }

//            return normal;
//        }

//        private IEnumerable<dynamic> ConstantFilter(IEnumerable<dynamic> records)
//        {
//            var constants = records.Where(r => r.type == "Constant");

//            if (!constants.Any()) return records;

//            var oldConstants = new List<dynamic>();
//            var groups = constants.GroupBy(c => c.objectId);
//            foreach (var group in groups)
//            {
//                var oldRecords = RecordsRepository.Instance.GetLastRecords("Constant", group.Key);

//                foreach (var record in group)
//                {
//                    var oldRecord = (oldRecords as IEnumerable<dynamic>).FirstOrDefault(r => r.s1 == record.s1);
            
//                    if (oldRecord == null) continue;
//                    if (oldRecord.s2 == record.s2)
//                    {
//                        oldConstants.Add(record);
//                        continue;
//                    }
//                }
//            }            
//            return records.Except(oldConstants);
//        }

//        private IEnumerable<dynamic> DayFilter(IEnumerable<dynamic> records)
//        {
//            var days = records.Where(r => r.type == "Day");
//            foreach (var day in days)
//                day.date = ((DateTime)day.date).Date;            
//            return records;
//        }
//    }
//}

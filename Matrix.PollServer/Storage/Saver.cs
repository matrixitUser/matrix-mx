using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Dynamic;

namespace Matrix.PollServer.Storage
{
    static class Saver
    {
        const int PART_SIZE = 5000;

        private static readonly ILog log = LogManager.GetLogger(typeof(Saver));

        public static void Save(IEnumerable<dynamic> records)
        {
            var success = SaveRecords(records);

            /*
            if (success)
            {
                foreach (var group in records.GroupBy(r => r.objectId))
                {
                    dynamic cache = RecordsRepository.Instance.Get(Guid.Parse(group.Key.ToString()));// new ExpandoObject();
                    if (cache == null) cache = new ExpandoObject();
                    var dcache = cache as IDictionary<string, object>;
                    //1. last date
                    if (!dcache.ContainsKey("lastDate"))
                    {
                        cache.lastDate = new ExpandoObject();
                    }
                    var dlast = cache.lastDate as IDictionary<string, object>;

                    if (!dcache.ContainsKey("records"))
                    {
                        cache.records = new ExpandoObject();
                    }
                    var drecords = cache.records as IDictionary<string, object>;
                    foreach (var type in group.GroupBy(r => r.type))
                    {
                        var date = type.Max(r => r.date);
                        if (!dlast.ContainsKey(type.Key))
                        {
                            dlast.Add(type.Key, date);
                        }
                        else
                        {
                            var old = (DateTime)dlast[type.Key];

                            switch((string)type.Key)
                            {
                                case "Day":
                                    date = date.AddDays(1);
                                    break;                                    
                            }
                            
                            dlast[type.Key] = date > old ? date : old;
                        }

                        //if (type.Key == "Hour")
                        //{
                        //    if (!drecords.ContainsKey(type.Key))
                        //    {
                        //        drecords.Add(type.Key, type.ToArray());
                        //    }
                        //    else
                        //    {
                        //        var recs = new List<dynamic>(type);
                        //        foreach (var rec in drecords[type.Key])
                        //        {
                        //            if (!(recs.Any(r => r.date == rec.date && r.s1 == rec.s1)))
                        //            {
                        //                recs.Add(rec);
                        //            }
                        //        }
                        //        var dates = recs.Select(r => r.date).Distinct().OrderBy(d => d);
                        //        if (dates.Count() > 30)
                        //        {
                        //            var border = dates.ElementAt(30);
                        //            drecords[type.Key] = recs.TakeWhile(r => r.date > border);
                        //        }
                        //    }
                        //}

                        //if (type.Key == "Constant")
                        //{
                        //    if (!drecords.ContainsKey(type.Key))
                        //    {
                        //        drecords.Add(type.Key, type.ToArray());
                        //    }
                        //    else
                        //    {
                        //        drecords[type.Key] = type.ToArray();
                        //    }
                        //}
                    }
                    RecordsRepository.Instance.Set(group.Key, cache);
                }               
            }
             */
        }

        private static bool SaveRecords(IEnumerable<dynamic> records)
        {
            var sw = new Stopwatch();
            sw.Start();
            dynamic message = Helper.BuildMessage("records-save");
            message.body.records = records;
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            sw.Stop();
            if ((file is IDictionary<string, object>) && (file as IDictionary<string, object>).ContainsKey("head") 
                && (file.head is IDictionary<string, object>) && (file.head as IDictionary<string, object>).ContainsKey("what") 
                && (file.head.what == "records-save"))
            {
                log.Debug(string.Format("порция из {0} записей отправлена за {1} мс", records.Count(), sw.ElapsedMilliseconds));
            }
            else
            {
                log.Error(string.Format("не получилось отправить порцию из {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
            }
            return true;
        }
    }
}

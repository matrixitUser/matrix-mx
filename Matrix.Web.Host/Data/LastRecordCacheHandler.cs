using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Newtonsoft.Json;

namespace Matrix.Web.Host.Data
{
    class LastRecordCacheHandler : IRecordHandler
    {
        const int DAY_DEPT = 2;
        const int ABNORMAL_LIMIT = 30;

        private static readonly ILog log = LogManager.GetLogger(typeof(LastRecordCacheHandler));

        private readonly object locker = new object();
       
        public void Handle(IEnumerable<Domain.Entities.DataRecord> records, Guid userId)
        {
            //var userId = StructureGraph.Instance.GetRootUser();

            //группируем по объектам
            var objGroups = records.GroupBy(r => r.ObjectId);
            foreach (var objGroup in objGroups)
            {
                var cache = CacheRepository.Instance.GetCache(objGroup.Key);
                if (cache == null) cache = new ExpandoObject();
                var dcache = cache as IDictionary<string, object>;

                //группируем по типам
                var typeGroups = objGroup.GroupBy(r => r.Type);
                foreach (var typeGroup in typeGroups)
                {
                    if (typeGroup.Key != "LogMessage")
                    {
                        ;
                    }
                    foreach (var record in typeGroup)
                    {
                        switch (typeGroup.Key)
                        {
                            case "Day":
                                {
                                    record.Date = record.Date.Date;
                                    dynamic dayCurrent = null;
                                    if (dcache.ContainsKey("dayCurrent"))
                                        dayCurrent = cache.dayCurrent;

                                    dynamic dayPreview = null;
                                    if (dcache.ContainsKey("dayPreview"))
                                        dayPreview = cache.dayPreview;

                                    //DateTime dt = ((DateTime)dayCurrent.date).ToLocalTime();
                                    //long ticks = dt.Date.Ticks;

                                    if (dayCurrent == null || record.Date > dayCurrent.date.ToLocalTime().Date)
                                    {
                                        dayPreview = dayCurrent;
                                        dayCurrent = new ExpandoObject();
                                        dayCurrent.date = record.Date;
                                        dayCurrent.records = new List<dynamic>() { record.ToDynamic() };
                                        cache.Day = new List<dynamic>();
                                    }

                                    if (dayPreview != null && record.Date < dayPreview.date)
                                        continue;

                                    if (dayCurrent != null && dayCurrent.date == record.Date)
                                    {
                                        var duplicate = (dayCurrent.records as IEnumerable<dynamic>).FirstOrDefault(r => r.s1 == record.S1);
                                        if (duplicate != null) dayCurrent.records.Remove(duplicate);

                                        dayCurrent.records.Add(record.ToDynamic());
                                    }

                                    if (dayPreview != null && dayPreview.date == record.Date)
                                    {
                                        var duplicate = (dayPreview.records as IEnumerable<dynamic>).FirstOrDefault(r => r.s1 == record.S1);
                                        if (duplicate != null) dayPreview.records.Remove(duplicate);

                                        dayPreview.records.Add(record.ToDynamic());
                                    }
                                    cache.dayCurrent = dayCurrent;
                                    cache.dayPreview = dayPreview;



                                    var parameters = CacheRepository.Instance.GetParameters(record.ObjectId);

                                    foreach (var parameter in parameters)
                                    {
                                        var rec = (dayCurrent.records as IEnumerable<dynamic>).FirstOrDefault(r => r.s1 == (string)parameter.name);
                                        if (rec == null)
                                            continue;


                                        var dparameter = parameter as IDictionary<string, object>;
                                        if (!dparameter.ContainsKey("tag")) continue;

                                        dynamic day = new ExpandoObject();

                                        var calc = "normal";
                                        if (dparameter.ContainsKey("calc"))
                                        {
                                            calc = (string)parameter.calc;
                                        }

                                        if (calc == "total")
                                        {
                                            if (dayPreview == null) continue;
                                            var recPrv = (dayPreview.records as IEnumerable<dynamic>).FirstOrDefault(r => r.s1 == rec.s1);
                                            if (recPrv == null) continue;
                                            day.d1 = rec.d1 - recPrv.d1;
                                        }
                                        else
                                            day.d1 = rec.d1;

                                        day.s1 = parameter.tag;
                                        day.s2 = rec.s2;
                                        day.date = rec.date;

                                        var duplicates = (cache.Day as IEnumerable<dynamic>).Where(r => r.s1 == day.s1).ToArray();
                                        foreach (var dublicate in duplicates)
                                        {
                                            cache.Day.Remove(dublicate);
                                        }
                                        cache.Day.Add(day);
                                    }
                                    break;
                                }
                            case "Current":
                                {
                                    if (!dcache.ContainsKey("Current") || !(cache.Current is List<dynamic>)) cache.Current = new List<dynamic>();
                                    cache.Current.Add(record.ToDynamic());
                                    var dates = (cache.Current as IEnumerable<dynamic>).Select(d => (DateTime)d.date).Distinct().OrderBy(d => d);
                                    if (dates.Count() > 1)
                                    {
                                        var border = dates.ElementAt(1);
                                        cache.Current = (cache.Current as IEnumerable<dynamic>).Where(r => r.date > border).ToArray();
                                    }

                                    break;
                                }
                            case "Constant":
                                {
                                    if (!dcache.ContainsKey("Constant") || !(cache.Constant is List<dynamic>)) cache.Constant = new List<dynamic>();
                                    var drec = record.ToDynamic();
                                    if (!(cache.Constant as List<dynamic>).Any(c => c.s1 == drec.s1))
                                    {
                                        cache.Constant.Add(drec);
                                    }

                                    break;
                                }
                            case "Abnormal":
                                {
                                    if (!dcache.ContainsKey("Abnormal") || !(cache.Abnormal is List<dynamic>)) cache.Abnormal = new List<dynamic>();
                                    cache.Abnormal.Add(record.ToDynamic());
                                    var dates = (cache.Abnormal as IEnumerable<dynamic>).Select(d => (DateTime)d.date).Distinct().OrderBy(d => d);
                                    if (dates.Count() > ABNORMAL_LIMIT)
                                    {
                                        var border = dates.ElementAt(ABNORMAL_LIMIT);
                                        cache.Abnormal = (cache.Abnormal as IEnumerable<dynamic>).Where(r => r.date > border).ToArray();
                                    }
                                    break;
                                }
                            case "MatrixSignal":
                                {
                                    if (!dcache.ContainsKey("Signal")) cache.Signal = new ExpandoObject();
                                    cache.Signal.date = record.Date;
                                    cache.Signal.value = record.D1;
                                    //related tube?
                                    break;
                                }
                            default:
                                break;
                        }
                    }
                }

                CacheRepository.Instance.SaveCache(objGroup.Key, cache);
                Carantine.Instance.Push(objGroup.Key);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;

namespace Matrix.Web.Host.Data
{
    static class RecordsDecorator
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RecordsDecorator));

        private static IEnumerable<DataRecord> Wrap(Guid[] objectIds, DateTime start, IEnumerable<DataRecord> records, string type)
        {
            var wraped = new List<DataRecord>();

#if ORENBURG            
            //По-новому "ТЕГ-ПАРАМЕТР"
            foreach (var objectId in objectIds)
            {
                var tags = CacheRepository.Instance.GetTags(objectId);
                if (tags == null) continue;
                foreach (var tag in tags.Where(t => t.dataType == type))
                {
                    var dtag = tag as IDictionary<string, object>;
                    if (!dtag.ContainsKey("parameter") || string.IsNullOrEmpty(tag.parameter) || tag.parameter == "<нет>") continue;

                    var calc = "normal";
                    if (dtag.ContainsKey("calc"))
                    {
                        calc = (string)tag.calc;
                    }

                    double init = 0.0;
                    if (!dtag.ContainsKey("init") || !Double.TryParse(tag.init.ToString(), out init))
                    {
                        init = 0.0;
                    }

                    double k = 1.0;
                    if (!dtag.ContainsKey("k") || !Double.TryParse(tag.k.ToString(), out k))
                    {
                        k = 1.0;
                    }

                    var parameterRecords = records.Where(r => r.S1 == (string)tag.parameter && r.ObjectId == objectId).OrderBy(r => r.Date).ToArray();

                    double prev = 0;
                    bool isFirst = true;
                    foreach (var record in parameterRecords)
                    {
                        var rec = (DataRecord)record.Clone();
                        var value = rec.D1 == null ? null : init + rec.D1 * k;
                        rec.D1 = value;

                        if (calc == "total")
                        {
                            var cur = (double)value;
                            rec.D1 = cur - prev;
                            prev = cur;
                        }

                        rec.S1 = tag.name;
                        if (calc == "total" && isFirst)
                        {
                            isFirst = false;
                            continue;
                        }
                        if (rec.Date >= start)
                        {
                            wraped.Add(rec);
                        }
                    }
                }
            }
#else
            //По-старому "ПАРАМЕТР-ТЕГ"
            foreach (var objectId in objectIds)
            {
                var parameters = CacheRepository.Instance.GetParameters(objectId);// StructureGraph.Instance.GetParameters(objectId, userId);

                //sw.Restart();
                foreach (var parameter in parameters)
                {
                    var dparameter = parameter as IDictionary<string, object>;
                    if (!dparameter.ContainsKey("tag")) continue;

                    var calc = "normal";
                    if (dparameter.ContainsKey("calc"))
                    {
                        calc = (string)parameter.calc;
                    }

                    #region Obsolete
                    double init = 0.0;
                    if (!dparameter.ContainsKey("init") || !Double.TryParse(parameter.init.ToString(), out init))
                    {
                        init = 0.0;
                    }

                    double k = 1.0;
                    if (!dparameter.ContainsKey("k") || !Double.TryParse(parameter.k.ToString(), out k))
                    {
                        k = 1.0;
                    }
                    #endregion

                    var parameterRecords = records.Where(r => r.S1 == (string)parameter.name && r.ObjectId == objectId).OrderBy(r => r.Date);

                    double prev = 0;
                    bool isFirst = true;
                    foreach (var record in parameterRecords)
                    {
                        var rec = record;
                        #region Obsolete
                        var value = rec.D1 == null ? null : init + rec.D1 * k;
                        rec.D1 = value;
                        #endregion

                        if (calc == "total")
                        {
                            var cur = (double)rec.D1;
                            rec.D1 = cur - prev;
                            prev = cur;
                        }

                        rec.S1 = parameter.tag;
                        if (calc == "total" && isFirst)
                        {
                            isFirst = false;
                            continue;
                        }
                        if (rec.Date >= start)
                        {
                            wraped.Add(rec);
                        }
                    }
                }
            }
#endif
            return wraped;
        }
        
        //        private static DataRecord Wrap(Guid objectId, DateTime date, DataRecord record, string type)
        //        {
        //            DataRecord wraped = null;

        //#if ORENBURG
        //            //По-новому "ТЕГ-ПАРАМЕТР"
        //            foreach (var objectId in objectIds)
        //            {
        //                var tags = CacheRepository.Instance.GetTags(objectId);
        //                if (tags == null) continue;
        //                foreach (var tag in tags.Where(t => t.dataType == type))
        //                {
        //                    var dtag = tag as IDictionary<string, object>;
        //                    if (!dtag.ContainsKey("parameter") || string.IsNullOrEmpty(tag.parameter) || tag.parameter == "<нет>") continue;

        //                    var calc = "normal";
        //                    if (dtag.ContainsKey("calc"))
        //                    {
        //                        calc = (string)tag.calc;
        //                    }

        //                    double init = 0.0;
        //                    if (!dtag.ContainsKey("init") || !Double.TryParse(tag.init.ToString(), out init))
        //                    {
        //                        init = 0.0;
        //                    }

        //                    double k = 1.0;
        //                    if (!dtag.ContainsKey("k") || !Double.TryParse(tag.k.ToString(), out k))
        //                    {
        //                        k = 1.0;
        //                    }

        //                    var parameterRecords = records.Where(r => r.S1 == (string)tag.parameter && r.ObjectId == objectId).OrderBy(r => r.Date).ToArray();

        //                    double prev = 0;
        //                    bool isFirst = true;
        //                    foreach (var record in parameterRecords)
        //                    {
        //                        var rec = (DataRecord)record.Clone();
        //                        var value = rec.D1 == null ? null : init + rec.D1 * k;
        //                        rec.D1 = value;

        //                        if (calc == "total")
        //                        {
        //                            var cur = (double)value;
        //                            rec.D1 = cur - prev;
        //                            prev = cur;
        //                        }

        //                        rec.S1 = tag.name;
        //                        if (calc == "total" && isFirst)
        //                        {
        //                            isFirst = false;
        //                            continue;
        //                        }
        //                        if (rec.Date >= start)
        //                        {
        //                            wraped.Add(rec);
        //                        }
        //                    }
        //                }
        //            }
        //#else
        //            //По-старому "ПАРАМЕТР-ТЕГ"
        //            var parameters = CacheRepository.Instance.GetParameters(objectId);// StructureGraph.Instance.GetParameters(objectId, userId);

        //            //sw.Restart();
        //            foreach (var parameter in parameters)
        //            {
        //                var dparameter = parameter as IDictionary<string, object>;
        //                if (!dparameter.ContainsKey("tag")) continue;

        //                var rec = record;

        //                if (calc == "total")
        //                {
        //                    var cur = (double)rec.D1;
        //                    rec.D1 = cur - prev;
        //                    prev = cur;
        //                }

        //                rec.S1 = parameter.tag;
        //                if (calc == "total" && isFirst)
        //                {
        //                    isFirst = false;
        //                    continue;
        //                }
        //                if (rec.Date == date)
        //                {
        //                    wraped = rec;
        //                }
        //            }
        //#endif
        //            return wraped;
        //        }

        public static IEnumerable<DataRecord> Decorate(Guid[] objectIds, DateTime start, DateTime end, string type, Guid userId)
        {
            Func<string, DateTime, DateTime> captureLast = (t, s) =>
            {
                if (s == DateTime.MinValue || s == DateTime.MaxValue) return s;
                switch (t)
                {
                    case "Day": return s.AddDays(-1);
                    case "Hour": return s.AddHours(-1);
                    default: return s;
                }
            };

            var records = Cache.Instance.GetRecords(captureLast(type, start), end, type, objectIds);
            return Wrap(objectIds, start, records, type);
        }

        //public static IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Decorate3D(Guid[] objectIds, DateTime start, DateTime end, string type, Guid userId)
        //{
        //    Func<string, DateTime, DateTime> captureLast = (t, s) =>
        //    {
        //        if (s == DateTime.MinValue || s == DateTime.MaxValue) return s;
        //        switch (t)
        //        {
        //            case "Day": return s.AddDays(-1);
        //            case "Hour": return s.AddHours(-1);
        //            default: return s;
        //        }
        //    };

        //    var dict = Cache.Instance.GetRecords3D(captureLast(type, start), end, type, objectIds);
        //    return Wrap3D(objectIds, start, dict, type);
        //}

        //public static IDictionary<DateTime, DataRecord> DecorateByDate(Guid objectId, DateTime start, DateTime end, string type, Guid userId)
        //{
        //    Func<string, DateTime, DateTime> captureLast = (t, s) =>
        //    {
        //        if (s == DateTime.MinValue || s == DateTime.MaxValue) return s;
        //        switch (t)
        //        {
        //            case "Day": return s.AddDays(-1);
        //            case "Hour": return s.AddHours(-1);
        //            default: return s;
        //        }
        //    };

        //    var records = Cache.Instance.GetRecordsByDate(captureLast(type, start), end, type, objectId);
        //    return Wrap(objectId, start, records, type);
        //}

        //public static DataRecord Decorate(Guid objectId, DateTime date, string type, Guid userId)
        //{
        //    var record = Cache.Instance.GetRecord(date, type, objectId);
        //    return Wrap(objectId, date, record, type);
        //}

        public static IEnumerable<DataRecord> DecorateLast(Guid[] objectIds, string type, Guid userId)
        {
            var records = Cache.Instance.GetLastRecords(type, objectIds);
            return Wrap(objectIds, DateTime.MinValue, records, type);
        }

        //[Obsolete("используйте метод с передачей массива объектов")]
        //public static IEnumerable<DataRecord> Decorate(Guid objectId, DateTime start, DateTime end, string type, Guid userId)
        //{
        //    var parameters = CacheRepository.Instance.GetParameters(objectId);// StructureGraph.Instance.GetParameters(objectId, userId);

        //    Func<string, DateTime, DateTime> captureLast = (t, s) =>
        //    {
        //        switch (t)
        //        {
        //            case "Day": return s.AddDays(-1);
        //            case "Hour": return s.AddHours(-1);
        //            default: return s;
        //        }
        //    };

        //    //var sw = new Stopwatch();
        //    //sw.Start();
        //    var records = Cache.Instance.GetRecords(captureLast(type, start), end, type, new Guid[] { objectId });
        //    //sw.Stop();
        //    //log.Debug(string.Format("получение из бд {0} мс", sw.ElapsedMilliseconds));

        //    var wraped = new List<DataRecord>();

        //    //sw.Restart();
        //    foreach (var parameter in parameters)
        //    {
        //        var dparameter = parameter as IDictionary<string, object>;
        //        if (!dparameter.ContainsKey("tag")) continue;

        //        var calc = "normal";
        //        if (dparameter.ContainsKey("calc"))
        //        {
        //            calc = (string)parameter.calc;
        //        }

        //        double init = 0.0;
        //        if (!dparameter.ContainsKey("init") || !Double.TryParse(parameter.init.ToString(), out init))
        //        {
        //            init = 0.0;
        //        }

        //        double k = 1.0;
        //        if (!dparameter.ContainsKey("k") || !Double.TryParse(parameter.k.ToString(), out k))
        //        {
        //            k = 1.0;
        //        }

        //        var parameterRecords = records.Where(r => r.S1 == (string)parameter.name).OrderBy(r => r.Date);

        //        double prev = 0;
        //        bool isFirst = true;
        //        foreach (var record in parameterRecords)
        //        {
        //            var rec = record;
        //            var value = rec.D1 == null ? null : init + rec.D1 * k;
        //            rec.D1 = value;

        //            if (calc == "total")
        //            {
        //                var cur = (double)value;
        //                rec.D1 = cur - prev;
        //                prev = cur;
        //            }
        //            rec.S1 = parameter.tag;
        //            if (calc == "total" && isFirst)
        //            {
        //                isFirst = false;
        //                continue;
        //            }
        //            wraped.Add(rec);
        //        }
        //    }
        //    //sw.Stop();
        //    //log.Debug(string.Format("скрутка тоталов {0} мс", sw.ElapsedMilliseconds));

        //    return wraped;
        //}
    }
}

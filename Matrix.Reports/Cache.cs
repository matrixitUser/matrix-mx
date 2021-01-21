using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    class Cache
    {
        public const string VIEW = "DataRecordView";

        public IEnumerable<dynamic> Get(string type, DateTime start, DateTime end, IEnumerable<Guid> objects)
        {
            var cs = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;
            using (var con = new SqlConnection(cs))
            {
                con.Open();
                using (var com = new SqlCommand())
                {
                    com.Connection = con;
                    com.CommandText = string.Format("select * from [{0}] where date>=@start and date<=@end and type=@type and objectId in ({1})", VIEW, string.Join(",", objects.Select(o => string.Format("'{0}'", o))));
                    com.Parameters.AddWithValue("@start", start);
                    com.Parameters.AddWithValue("@end", end);
                    com.Parameters.AddWithValue("@type", type);

                    var rows = new List<dynamic>();
                    var reader = com.ExecuteReader();
                    while (reader.Read())
                    {
                        dynamic row = new ExpandoObject();
                        var drow = row as IDictionary<string, object>;
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            drow.Add(reader.GetName(i).ToLower(), reader.GetValue(i));
                        }
                        rows.Add(row);
                    }

                    return rows;
                }
            }
        }

        public dynamic Clone(dynamic original)
        {
            var clone = new ExpandoObject();
            var doriginal = original as IDictionary<string, object>;
            var dclone = clone as IDictionary<string, object>;

            foreach (var key in doriginal.Keys)
            {
                dclone.Add(key, doriginal[key]);
            }

            return clone;
        }


        public IEnumerable<dynamic> Decorate(Guid[] objectIds, DateTime start, DateTime end, string type, Guid userId)
        {
            Func<string, DateTime, DateTime> captureLast = (t, s) =>
            {
                switch (t)
                {
                    case "Day": return s.AddDays(-1);
                    case "Hour": return s.AddHours(-1);
                    default: return s;
                }
            };
            
            var records = Get(type, captureLast(type, start), end, objectIds);

            var wraped = new List<dynamic>();

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

                    var parameterRecords = records.Where(r => r.s1 == (string)tag.parameter && r.objectid == objectId).OrderBy(r => r.date).ToArray();

                    double prev = 0;
                    bool isFirst = true;
                    foreach (var record in parameterRecords)
                    {
                        var rec = (DataRecord)record.Clone();
                        var value = rec.d1 == null ? null : init + rec.d1 * k;
                        rec.d1 = value;

                        if (calc == "total")
                        {
                            var cur = (double)value;
                            rec.d1 = cur - prev;
                            prev = cur;
                        }

                        rec.s1 = tag.name;
                        if (calc == "total" && isFirst)
                        {
                            isFirst = false;
                            continue;
                        }
                        if (rec.date >= start)
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

                    var parameterRecords = records.Where(r => r.s1 == (string)parameter.name && r.objectid == objectId).OrderBy(r => r.date);

                    double prev = 0;
                    bool isFirst = true;
                    foreach (var record in parameterRecords)
                    {
                        var rec = record;
                        #region Obsolete
                        var value = rec.d1 == null ? null : init + rec.d1 * k;
                        rec.d1 = value;
                        #endregion

                        if (calc == "total")
                        {
                            var cur = (double)rec.d1;
                            rec.d1 = cur - prev;
                            prev = cur;
                        }

                        rec.s1 = parameter.tag;
                        if (calc == "total" && isFirst)
                        {
                            isFirst = false;
                            continue;
                        }
                        if (rec.date >= start)
                        {
                            wraped.Add(rec);
                        }
                    }
                }
            }
#endif

            return wraped;
        }

        //public IEnumerable<dynamic> Decorate(Guid[] objectIds, DateTime start, DateTime end, string type, Guid userId)
        //{
        //    Func<string, DateTime, DateTime> captureLast = (t, s) =>
        //    {
        //        switch (t)
        //        {
        //            case "Day": return s.AddDays(-1);
        //            case "Hour": return s.AddHours(-1);
        //            default: return s;
        //        }
        //    };

        //    var records = Get(type, captureLast(type, start), end, objectIds);

        //    var wraped = new List<dynamic>();

        //    var allTags = CacheRepository.Instance.GetTags(objectIds);

        //    foreach (var objectId in objectIds)
        //    {
        //        var tags = allTags.Where(t => t.tubeId == objectId.ToString());
        //        if (tags == null) continue;
        //        foreach (var tag in tags.Where(t => t.dataType == type))
        //        {
        //            var dtag = tag as IDictionary<string, object>;
        //            if (!dtag.ContainsKey("parameter") || string.IsNullOrEmpty(tag.parameter) || tag.parameter == "<нет>") continue;

        //            var calc = "normal";
        //            if (dtag.ContainsKey("calc"))
        //            {
        //                calc = (string)tag.calc;
        //            }

        //            double init = 0.0;
        //            if (!dtag.ContainsKey("init") || !Double.TryParse(tag.init.ToString(), out init))
        //            {
        //                init = 0.0;
        //            }

        //            double k = 1.0;
        //            if (!dtag.ContainsKey("k") || !Double.TryParse(tag.k.ToString(), out k))
        //            {
        //                k = 1.0;
        //            }

        //            var parameterRecords = records.Where(r => r.s1 == (string)tag.parameter && r.objectid == objectId).OrderBy(r => r.date).ToArray();

        //            double prev = 0;
        //            bool isFirst = true;
        //            foreach (var record in parameterRecords)
        //            {
        //                var rec = Clone(record);//(DataRecord)record.Clone();
        //                var value = rec.d1 == null ? null : init + rec.d1 * k;
        //                rec.d1 = value;

        //                if (calc == "total")
        //                {
        //                    var cur = (double)value;
        //                    rec.d1 = cur - prev;
        //                    prev = cur;
        //                }

        //                rec.s1 = tag.name;
        //                if (calc == "total" && isFirst)
        //                {
        //                    isFirst = false;
        //                    continue;
        //                }
        //                if (rec.date >= start)
        //                {
        //                    wraped.Add(rec);
        //                }
        //            }
        //        }
        //    }
        //    return wraped;
        //}
    }
}

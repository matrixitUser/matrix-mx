using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotLiquid;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using CoordinateSharp;

namespace Matrix.Web.Host.Reports
{
    class Mapper
    {
        private Mapper()
        {
            Template.RegisterFilter(typeof(Formatter));
        }

        public dynamic Map(dynamic model, string tmpl, dynamic session)
        {
            dynamic ret = new ExpandoObject();
            var renderResult = "";

            dynamic buildResult = new ExpandoObject();

            //Переменные, которые используются в рассылках
            buildResult.nullCount = 0;  // количество пустых записей (используется, если надо отправлять "только полные данные")
            buildResult.nullReport = 0; // флаг "пустого" отчёта (используется, чтобы отсекать отчёты без полезной информации; например, отчёт по НС без сработок)
            buildResult.success = true;
            buildResult.errorText = "";
            buildResult.warningText = "";

            try
            {
                Template template = Template.Parse(tmpl);
                renderResult = template.Render(Hash.FromAnonymousObject(new { root = new Reports.DynamicDrop(model), cache = new List<dynamic>(), session = new Reports.DynamicDrop(session), buildResult }));
                if (renderResult == null || renderResult.Trim() == "")
                {
                    throw new Exception("Отчёт не загружен (пустой шаблон отчёта)");
                }
            }
            catch (Exception ex)
            {
                renderResult = ex.Message;
                buildResult.success = false;
                buildResult.errorText = ex.Message;
            }

            ret.render = renderResult;
            ret.build = buildResult;

            return ret;
        }

        private static Mapper instance = new Mapper();
        public static Mapper Instance
        {
            get
            {
                return instance;
            }
        }
    }

    public static class Formatter
    {
        static Formatter() { }

        private static Stopwatch sw = null;

        private static Random rnd = new Random();

        #region работа с данными

        public static string[] Tablerange(string type, DateTime start, DateTime end)
        {
            return Data.Cache.Instance.GetTableRange(start, end, type);
        }

        /// <summary>
        /// выгрузка данных из кэша
        /// </summary>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static string Unload(List<dynamic> cache)
        {
            cache.Clear();
            return "";
        }

        /// <summary>
        /// загрузка в кэш по многим объектам
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static string Load(List<dynamic> cache, string type, DateTime start, DateTime end, object[] objects)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            var data = Data.Cache.Instance.GetRecords(start, end, type, ids);
            cache.AddRange(data.ToDynamic());
            return "";
        }

        public static IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Load3d(IDictionary<Tuple<Guid, DateTime, string>, DataRecord> dict, string type, DateTime start, DateTime end, object[] objects)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            var data = Data.Cache.Instance.GetRecords3D(start, end, type, ids);

            if (dict == null)
            {
                dict = data;
            }
            else
            {
                foreach (var record in data)
                {
                    dict[record.Key] = record.Value;
                }
            }
            return dict;
        }

        public static IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Loadtables3d(IDictionary<Tuple<Guid, DateTime, string>, DataRecord> dict, string type, DateTime start, DateTime end, object[] objects, string[] tableRange)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            var data = Data.Cache.Instance.GetRecords3D(start, end, type, ids, tableRange);

            if (dict == null)
            {
                dict = data;
            }
            else
            {
                foreach (var record in data)
                {
                    dict[record.Key] = record.Value;
                }
            }
            return dict;
        }


        /// <summary>
        /// загрузка в кэш по одному объекту
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static string Load(List<dynamic> cache, string type, DateTime start, DateTime end, string objects)
        {
            var id = System.Guid.Parse(objects);
            return Load(cache, type, start, end, new object[] { id });
            //var data = Data.Cache.Instance.GetRecords(start, end, type, new Guid[] { id });
            //cache.AddRange(data.ToDynamic());
            //return "";
        }

        /// <summary>
        /// загрузка в кэш по последней дате по объектам
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static string Loadlast(List<dynamic> cache, string type, DateTime start, object[] objects)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            foreach (var id in ids)
            {
                //var date = Data.Cache.Instance.GetLastDate(type, id);
                //var data = Data.Cache.Instance.GetRecords(date, date, type, new System.Guid[] { id });
                var data = Data.Cache.Instance.GetLastRecords(type, new Guid[] { id }, start);
                cache.AddRange(data.ToDynamic());
            }
            return "";
        }
        /// <summary>
        /// 
        /// загрузка в кэш по последней дате по объектам
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static string Loadlast1(List<dynamic> cache, string type, object[] objects)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            foreach (var id in ids)
            {
                var date = Data.Cache.Instance.GetLastDate1(type, id);
                var data = Data.Cache.Instance.GetRecords(date, date, type, new System.Guid[] { id });
                cache.AddRange(data.ToDynamic());
            }
            return "";
        }

        /// <summary>
        /// загрузка в кэш по многим объектам по тегам
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="objects"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string Loadpretty(List<dynamic> cache, string type, DateTime start, DateTime end, object[] objects, string userId)
        {
            var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
            var usrId = System.Guid.Parse(userId);
            var data = Data.RecordsDecorator.Decorate(ids, start, end, type, usrId);
            cache.AddRange(data.ToDynamic());
            return "";
        }

        //public static string Loadpretty3d(IDictionary<DateTime, Dictionary<string, DataRecord>> dict, string type, DateTime start, DateTime end, object[] objects, string userId)
        //{
        //    var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
        //    var usrId = System.Guid.Parse(userId);
        //    var data = Data.RecordsDecorator.Decorate3d(ids, start, end, type, usrId);
        //    //cache.AddRange(data.ToDynamic());
        //    return "";
        //}

        /// <summary>
        /// загрузка в кэш по одному объекту по тегам
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="objects"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string Loadpretty(List<dynamic> cache, string type, DateTime start, DateTime end, string objects, string userId)
        {
            var id = System.Guid.Parse(objects);
            var usrId = System.Guid.Parse(userId);
            var data = Data.RecordsDecorator.Decorate(new Guid[] { id }, start, end, type, usrId);
            cache.AddRange(data.ToDynamic());
            return "";
        }


        /// <summary>
        /// получение списка дат из кэша по типу
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="objectId"></param>
        /// <returns></returns>
        public static DateTime[] Cachedates(List<dynamic> cache, string type, string objectId)
        {
            var id = System.Guid.Parse(objectId);
            var dates = cache.Where(r => r.objectId == id && r.type == type).Select(r => (DateTime)r.date).Distinct().OrderBy(d => d).ToArray();
            return dates;
        }

        public static DateTime[] Dates3d(Dictionary<Tuple<Guid, DateTime, string>, DataRecord> dict, string objectId)
        {
            var id = System.Guid.Parse(objectId);
            var dates = dict.Values.Where(r => r.ObjectId == id).Select(r => r.Date).Distinct().OrderBy(d => d).ToArray();
            return dates;
        }

        public static DateTime[] Dates4d(Dictionary<Tuple<string, Guid, DateTime, string>, DataRecord> dict, string type, string objectId)
        {
            var id = System.Guid.Parse(objectId);
            var dates = dict.Values.Where(r => r.ObjectId == id && r.Type == type).Select(r => (DateTime)r.Date).Distinct().OrderBy(d => d).ToArray();
            return dates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="objectId"></param>
        /// <param name="type"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic[] Values(List<dynamic> cache, string objectId, string type, string property)
        {
            var id = System.Guid.Parse(objectId);
            var dates = cache.Where(r => r.objectId == id && r.type == type).Select(r =>
            {
                var dr = r as IDictionary<string, object>;
                if (dr.ContainsKey(property))
                    return dr[property];
                return null;
            }).
            Where(r => r != null).
            Distinct().ToArray();
            return dates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="date"></param>
        /// <param name="objectId"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static object[] Cachekeys(List<dynamic> cache, DateTime date, string objectId, string property)
        {
            var id = System.Guid.Parse(objectId);
            var dates = cache.Where(r => r.objectId == id && r.date == date).Select(r =>
            {
                var dr = r as IDictionary<string, object>;
                if (dr.ContainsKey(property))
                {
                    return dr[property];
                }
                return "";
            }).Distinct().ToArray();
            return dates;
        }

        /// <summary>
        /// получение минимума по параметру из кэша
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="objectId"></param>
        /// <param name="parameter"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Getmin(List<dynamic> cache, string type, string objectId, string parameter, string property)
        {
            var id = System.Guid.Parse(objectId);
            var max = cache.Where(r => r.type == type && r.s1 == parameter && r.objectId == id).Select(r =>
            {
                var dr = r as IDictionary<string, object>;
                if (dr.ContainsKey(property))
                {
                    return dr[property];
                }
                return null;
            }).Min(r => r);

            return max;
        }

        /// <summary>
        /// получение максимума по параметру из кэша
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="objectId"></param>
        /// <param name="parameter"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Getmax(List<dynamic> cache, string type, string objectId, string parameter, string property)
        {
            var id = System.Guid.Parse(objectId);
            var max = cache.Where(r => r.type == type && r.s1 == parameter && r.objectId == id).Select(r =>
            {
                var dr = r as IDictionary<string, object>;
                if (dr.ContainsKey(property))
                {
                    return dr[property];
                }
                return null;
            }).Max(r => r);

            return max;
        }

        /// <summary>
        /// получение записи из кэша
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="date"></param>
        /// <param name="objectId"></param>
        /// <param name="parameter"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Get(List<dynamic> cache, string type, DateTime date, string objectId, string parameter, string property)
        {
            var id = System.Guid.Parse(objectId);
            var record = cache.FirstOrDefault(r => r.type == type && r.date == date && r.s1 == parameter && r.objectId == id);
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property)) return "";
            return drecord[property];
        }

        public static object Get3d(IDictionary<Tuple<Guid, DateTime, string>, DataRecord> dict, DateTime date, string objectId, string parameter, string property)
        {
            var id = System.Guid.Parse(objectId);
            var key = new Tuple<Guid, DateTime, string>(id, date, parameter);
            if (dict == null || !dict.ContainsKey(key)) return null;
            DataRecord value = dict[key];
            object ret = value.GetProperty(property);
            if (ret != null) return ret;
            return "";
        }

        public static int Size3d(IDictionary<Tuple<Guid, DateTime, string>, DataRecord> dict)
        {
            if(dict != null)
            {
                return dict.Count;
            }
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="date"></param>
        /// <param name="objectId"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Get(List<dynamic> cache, string type, DateTime date, string objectId, string property)
        {
            var id = System.Guid.Parse(objectId);
            var record = cache.OrderByDescending(r => r.dt1).FirstOrDefault(r => r.type == type && r.date == date && r.objectId == id);
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property)) return "";
            return drecord[property];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double Koeff(DateTime start, DateTime end)
        {
            int deltaHour = (DateTime.Now - DateTime.UtcNow).Hours;
            TimeSpan timeWorkForPeriodMinusHour = TimeSpan.Zero, timeWorkForPeriod = TimeSpan.Zero;
            for (int i = 0; start.AddDays(i) < end; i++)
            {
                Celestial cel = Celestial.CalculateCelestialTimes(55.9077000, 53.9355000, start.AddDays(i));
                DateTime sunRiseUTC = cel.SunRise.Value;
                DateTime sunSetUTC = cel.SunSet.Value;
                DateTime sunRise = sunRiseUTC.AddHours(deltaHour);
                DateTime sunSet = sunSetUTC.AddHours(deltaHour);
                TimeSpan tmpsum = (sunRise - start.AddDays(i)) + (start.AddDays(i + 1) - sunSet);
                timeWorkForPeriod += tmpsum;
                timeWorkForPeriodMinusHour += tmpsum - TimeSpan.FromHours(1);
            }
            double tmp = timeWorkForPeriod.TotalMinutes / timeWorkForPeriodMinusHour.TotalMinutes;
            return tmp;
        }
        #region old

        //public static IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> Load4d(IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> dict, string type, DateTime start, DateTime end, object[] objects)
        //{
        //    var ids = objects.Select(o => System.Guid.Parse(o.ToString())).ToArray();
        //    var data = Data.Cache.Instance.GetRecords4D(start, end, type, ids);

        //    if (dict == null)
        //    {
        //        dict = data;
        //    }
        //    else
        //    {
        //        foreach (var record in data)
        //        {
        //            dict[record.Key] = record.Value;
        //        }
        //    }

        //    return dict;
        //}

        //public static IDictionary<DateTime, DataRecord> Loadbydate(IDictionary<DateTime, DataRecord> dict, string type, DateTime start, DateTime end, object objectId)
        //{
        //    var id = System.Guid.Parse(objectId.ToString());
        //    var data = Data.Cache.Instance.GetRecordsByDate(start, end, type, id);
        //    if (dict == null)
        //    {
        //        dict = new Dictionary<DateTime, DataRecord>();
        //    }
        //    foreach (var rec in data)
        //    {
        //        dict[rec.Key] = rec.Value;
        //    }
        //    return dict;
        //}

        //public static IDictionary<string, DataRecord> Loadbyparameter(IDictionary<string, DataRecord> dict, string[] tableRange, string type, DateTime date, object objectId)
        //{
        //    var id = System.Guid.Parse(objectId.ToString());
        //    var data = Data.Cache.Instance.GetRecordsByParameter(tableRange, date, id, type);
        //    if (dict == null)
        //    {
        //        dict = new Dictionary<string, DataRecord>();
        //    }
        //    foreach (var rec in data)
        //    {
        //        dict[rec.Key] = rec.Value;
        //    }
        //    return dict;
        //}

        //public static string Loadbyparametertest(IDictionary<string, DataRecord> dict)
        //{
        //    string result = "";
        //    if (dict != null)
        //    {
        //        result = string.Join(", ", dict.Select(d => $"{d.Key}={{S1={d.Value.S1}, D1={d.Value.D1}, S2={d.Value.S2}, OID={d.Value.ObjectId}}}"));
        //    }
        //    return "[" + result + "]";
        //}

        //public static IDictionary<DateTime, Dictionary<string, DataRecord>> Loadbydateparameter(IDictionary<DateTime, Dictionary<string, DataRecord>> dict, string type, DateTime start, DateTime end, object objectId)
        //{
        //    var id = System.Guid.Parse(objectId.ToString());
        //    var data = Data.Cache.Instance.GetRecordsByDateParameter(start, end, id, type);
        //    if (dict == null)
        //    {
        //        dict = data;
        //    }
        //    else
        //    {
        //        foreach (var rec in data)
        //        {
        //            if (!dict.ContainsKey(rec.Key))
        //            {
        //                dict[rec.Key] = new Dictionary<string, DataRecord>();
        //            }
        //            foreach (var rec2 in rec.Value)
        //            {
        //                dict[rec.Key][rec2.Key] = rec2.Value;
        //            }
        //        }
        //    }
        //    return dict;
        //}

        //public static IDictionary<DateTime, Dictionary<string, DataRecord>> Loadbydateparametertables(IDictionary<DateTime, Dictionary<string, DataRecord>> dict, string[] tableRange, string type, DateTime start, DateTime end, object objectId)
        //{
        //    var id = System.Guid.Parse(objectId.ToString());
        //    var data = Data.Cache.Instance.GetRecordsByDateParameter(tableRange, start, end, id, type);
        //    if (dict == null)
        //    {
        //        dict = new Dictionary<DateTime, Dictionary<string, DataRecord>>();
        //    }
        //    foreach (var rec in data)
        //    {
        //        dict[rec.Key] = rec.Value;
        //    }
        //    return dict;
        //}

        //public static string Loadbydateparametertest(IDictionary<DateTime, Dictionary<string, DataRecord>> dict)
        //{
        //    string result = "";
        //    if (dict != null)
        //    {
        //        result = string.Join(", ", dict.Select(d => $"{d.Key.Item1:dd.MM.yyyy HH:mm},{d.Key.Item2}={{D1={d.Value.D1}, S2={d.Value.S2}, OID={d.Value.ObjectId}}}"));
        //    }
        //    return "[" + result + "]";
        //}
        //public static IDictionary<DateTime, DataRecord> Loadprettybydate(IDictionary<DateTime, DataRecord> dict, string type, DateTime start, DateTime end, object objectId, string userId)
        //{
        //    var id = System.Guid.Parse(objectId.ToString());
        //    var usrId = System.Guid.Parse(userId);

        //    var data = Data.RecordsDecorator.DecorateByDate(id, start, end, type, usrId);
        //    //cache.AddRange(data.ToDynamic());            
        //    if (dict == null)
        //    {
        //        dict = new Dictionary<DateTime, DataRecord>();
        //    }
        //    foreach (var rec in data)
        //    {
        //        dict[rec.Key] = rec.Value;
        //    }
        //    return dict;
        //}

        //// загрузка в кэш по многим объектам по гуидам
        //[Obsolete]
        //public static string Fill(List<DataRecord> cache, IEnumerable<object> nodes, string type, DateTime start, DateTime end)
        //{
        //    var ids = nodes.Select(n => (Guid)n);
        //    var records = Data.Cache.Instance.GetRecords(start, end, type, ids.ToArray());
        //    cache.AddRange(records);
        //    return "";
        //}
        //public static object Get4d(IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> dict, string type, DateTime date, string objectId, string parameter, string property)
        //{
        //    var id = System.Guid.Parse(objectId);
        //    var key = new Tuple<string, Guid, DateTime, string>(type, id, date, parameter);
        //    if (dict == null || !dict.ContainsKey(key)) return null;
        //    DataRecord value = dict[key];
        //    switch (property)
        //    {
        //        case "date":
        //            return value.Date;
        //        case "id":
        //            return value.Id;
        //        case "objectId":
        //            return value.ObjectId;

        //        case "g1":
        //            return value.G1;
        //        case "g2":
        //            return value.G2;
        //        case "g3":
        //            return value.G3;

        //        case "s1":
        //            return value.S1;
        //        case "s2":
        //            return value.S2;
        //        case "s3":
        //            return value.S3;

        //        case "i1":
        //            return value.I1;
        //        case "i2":
        //            return value.I2;
        //        case "i3":
        //            return value.I3;

        //        case "d1":
        //            return value.D1;
        //        case "d2":
        //            return value.D2;
        //        case "d3":
        //            return value.D3;

        //        case "dt1":
        //            return value.Dt1;
        //        case "dt2":
        //            return value.Dt2;
        //        case "dt3":
        //            return value.Dt3;
        //    }
        //    return "";
        //}
        //public static object Getbyparameter(Dictionary<string, DataRecord> dict, string parameter, string property)
        //{
        //    if (dict == null || !dict.ContainsKey(parameter)) return null;
        //    switch (property)
        //    {
        //        case "Date":
        //            return dict[parameter].Date;
        //        case "Id":
        //            return dict[parameter].Id;
        //        case "ObjectId":
        //            return dict[parameter].ObjectId;

        //        case "G1":
        //            return dict[parameter].G1;
        //        case "G2":
        //            return dict[parameter].G2;
        //        case "G3":
        //            return dict[parameter].G3;

        //        case "S1":
        //            return dict[parameter].S1;
        //        case "S2":
        //            return dict[parameter].S2;
        //        case "S3":
        //            return dict[parameter].S3;

        //        case "I1":
        //            return dict[parameter].I1;
        //        case "I2":
        //            return dict[parameter].I2;
        //        case "I3":
        //            return dict[parameter].I3;

        //        case "D1":
        //            return dict[parameter].D1;
        //        case "D2":
        //            return dict[parameter].D2;
        //        case "D3":
        //            return dict[parameter].D3;

        //        case "Dt1":
        //            return dict[parameter].Dt1;
        //        case "Dt2":
        //            return dict[parameter].Dt2;
        //        case "Dt3":
        //            return dict[parameter].Dt3;
        //    }
        //    return null;
        //}

        //public static object Getbydateparameter(Dictionary<DateTime, Dictionary<string, DataRecord>> dict, DateTime date, string parameter, string property)
        //{
        //    if (dict == null || !dict.ContainsKey(date) || !dict[date].ContainsKey(parameter)) return null;
        //    DataRecord value = dict[date][parameter];

        //    switch (property)
        //    {
        //        case "date":
        //            return value.Date;
        //        case "id":
        //            return value.Id;
        //        case "objectId":
        //            return value.ObjectId;

        //        case "g1":
        //            return value.G1;
        //        case "g2":
        //            return value.G2;
        //        case "g3":
        //            return value.G3;

        //        case "s1":
        //            return value.S1;
        //        case "s2":
        //            return value.S2;
        //        case "s3":
        //            return value.S3;

        //        case "i1":
        //            return value.I1;
        //        case "i2":
        //            return value.I2;
        //        case "i3":
        //            return value.I3;

        //        case "d1":
        //            return value.D1;
        //        case "d2":
        //            return value.D2;
        //        case "d3":
        //            return value.D3;

        //        case "dt1":
        //            return value.Dt1;
        //        case "dt2":
        //            return value.Dt2;
        //        case "dt3":
        //            return value.Dt3;
        //    }
        //    return null;
        //}

        //public static dynamic Getbydate(Dictionary<DateTime, DataRecord> cache, DateTime date, string property)
        //{
        //    var record = cache[date];
        //    if (record == null) return null;
        //    var drecord = record as IDictionary<string, object>;
        //    if (!drecord.ContainsKey(property)) return "";
        //    return drecord[property];
        //}

        //public static dynamic Loadget(string type, DateTime date, string objectId, string property, string userId)
        //{
        //    var id = System.Guid.Parse(objectId);
        //    var usrId = System.Guid.Parse(userId);
        //    var data = Data.RecordsDecorator.Decorate(id, date, type, usrId);
        //    var ddata = data.ToDynamic() as IDictionary<string, object>;
        //    if (ddata != null && ddata.ContainsKey(property))
        //        return ddata[property];
        //    return "";
        //}
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="date"></param>
        /// <param name="objectId"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Getparam(List<dynamic> cache, string type, DateTime date, string parameter, string property)
        {
            var record = cache.OrderByDescending(r => r.dt1).FirstOrDefault(r => r.type == type && r.date == date && r.s1 == parameter);
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property)) return "";
            return drecord[property];
        }

        /// <summary>
        ///  получение записей из кэша по гуиду
        /// </summary>
        /// <param name="node"></param>
        /// <param name="cache"></param>
        /// <param name="type"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [Obsolete]
        public static IEnumerable<DataRecord> Records(Guid node, List<DataRecord> cache, string type, DateTime start, DateTime end)
        {
            //var repo = new DataRecordRepository2();
            //var records = repo.Get(start, end, new string[] { type }, new Guid[] { node });
            var records = cache.Where(d => d.Type == type &&
                d.ObjectId == node && d.Date >= start && d.Date <= end);
            return records;
        }
        #endregion

        #region работа с примитивами и объектами

        /// <summary>
        /// антье, целая часть числа
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double Entier(double value)
        {
            return (int)value;
        }

        /// <summary>
        /// сложение вещественных чисел
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public static double Plus1(double obj, double increment)
        {
            return obj + increment;
        }

        /// <summary>
        /// парсинг guid
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static Guid Guid(object raw)
        {
            var id = System.Guid.Parse(raw.ToString());
            return id;
        }

        /// <summary>
        /// парсинг вещественного числа
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        [Obsolete]
        public static double Double(object raw)
        {
            var id = double.Parse(raw.ToString());
            return id;
        }

        /// <summary>
        /// парсинг вещественного числа
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static double Number(string obj)
        {
            double val = 0.0;
            double.TryParse(obj, out val);
            return val;
        }

        public static int Int32(object obj)
        {
            int val = 0;
            if (obj is string)
            {
                int.TryParse(obj as string, out val);
            }
            else
            {
                val = Convert.ToInt32(obj);
            }
            return val;
        }

        public static UInt32 UInt32(object obj)
        {
            UInt32 val = 0;
            if (obj is string)
            {
                uint.TryParse(obj as string, out val);
            }
            else
            {
                val = Convert.ToUInt32(obj);
            }
            return val;
        }



        /// <summary>
        /// текущее время
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static DateTime Now(object obj)
        {
            return DateTime.Now;
        }

        /// <summary>
        /// взятие даты без времени
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime Dateclear(DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// парсинг даты по формату
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static DateTime Dat(string value, string format)
        {
            return DateTime.ParseExact(value, format, null);
        }

        /// <summary>
        /// добавить к дате период
        /// </summary>
        /// <param name="value">дата-источник</param>
        /// <param name="countString">количество</param>
        /// <param name="part">размерность - часы, дни или месяцы</param>
        /// <returns></returns>
        public static object Adddate(object value, object countString, string part)
        {
            if (!(value is DateTime)) return value;
            var date = (DateTime)value;

            int count = 0;
            int.TryParse(countString.ToString(), out count);

            switch (part.ToLower())
            {
                case "second":
                    return date.AddSeconds(count);
                case "minute":
                    return date.AddMinutes(count);
                case "hour":
                    return date.AddHours(count);
                case "day":
                    return date.AddDays(count);
                case "month":
                    return date.AddMonths(count);
                case "year":
                    return date.AddMonths(count);
            }
            return date;
        }

        /// <summary>
        /// случайное число в диапазоне
        /// </summary>
        /// <param name="start">от ...</param>
        /// <param name="end">до ...</param>
        /// <returns></returns>
        public static int Rnd(int start, int end)
        {
            return rnd.Next(start, end);
        }

        /// <summary>
        /// диапазон дат, для обхода архива например
        /// </summary>
        /// <param name="any"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="part"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static DateTime[] Range(DateTime start, DateTime end, string part, int step)
        {
            Func<DateTime, int, DateTime> increment = (d, i) => d.AddDays(i);
            switch (part)
            {
                case "hour": increment = (d, i) => d.AddHours(i); break;
                case "day": increment = (d, i) => d.AddDays(i); break;
                case "month": increment = (d, i) => d.AddMonths(i); break;
            }
            var result = new List<DateTime>();
            for (DateTime date = start; date < end; date = increment(date, step))
            {
                result.Add(date);
            }
            return result.ToArray();
        }

        /// <summary>
        /// стандартное форматирование
        /// </summary>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Format(object value, string format)
        {
            try
            {
                var formatString = string.Format("{{0:{0}}}", format);
                return string.Format(formatString, value);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// создание даты-времени
        /// </summary>
        /// <param name="value"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static DateTime Todate(object value, string year, string month, string day, string hour, string minute, string second)
        {
            int y = DateTime.Now.Year; int.TryParse(year, out y);
            int M = DateTime.Now.Month; int.TryParse(month, out M);
            int d = DateTime.Now.Day; int.TryParse(day, out d);
            int H = DateTime.Now.Hour; int.TryParse(hour, out H);
            int m = DateTime.Now.Minute; int.TryParse(minute, out m);
            int s = DateTime.Now.Second; int.TryParse(second, out s);

            return new DateTime(y, M, d, H, m, s);
        }

        /// <summary>
        /// создание даты
        /// </summary>
        /// <param name="value"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <returns></returns>
        public static DateTime Todate(object value, string year, string month, string day)
        {
            return Todate(value, year, month, day, "0", "0", "0");
        }

        /// <summary>
        /// получение параметра у динамика
        /// </summary>
        /// <param name="record"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static dynamic Getparam(dynamic record, string property)
        {
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property)) return "";
            return drecord[property];
        }

        /// <summary>
        /// установка параметра у динамика
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("используйте Dyn")]
        public static string Set(dynamic obj, string key, object value)
        {
            var dobj = obj as IDictionary<string, object>;
            dobj[key] = value;
            return "";
        }

        /// <summary>
        /// получение параметра у динамика
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Obsolete("используйте точку")]
        public static object Get(dynamic obj, string key)
        {
            if (obj is DynamicDrop)
            {
                return (obj as DynamicDrop).BeforeMethod(key);
            }
            return ((IDictionary<string, object>)obj)[key];
        }

        /// <summary>
        /// создание динамика и установка параметра
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("используйте Dyn")]
        public static dynamic Obj(dynamic obj, string key, object value)
        {
            if (obj == null)
            {
                obj = new ExpandoObject();
            }
            ((IDictionary<string, object>)obj)[key] = value;
            return obj;
        }

        /// <summary>
        /// создание и установка параметров у объектов dynamicDrop
        /// </summary>
        /// <param name="dyn"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DynamicDrop Dyn(DynamicDrop dyn, string key, object value)
        {
            dynamic model;
            if (dyn == null)
            {
                model = new ExpandoObject();
            }
            else
            {
                model = dyn.GetViewModel();
            }

            if (value is DynamicDrop)
            {
                value = (value as DynamicDrop).GetViewModel();
            }
            else if (value is IEnumerable<DynamicDrop>)
            {
                value = (value as IEnumerable<DynamicDrop>).Select(i => (i as DynamicDrop).GetViewModel()).ToArray();
            }

            ((IDictionary<string, object>)model)[key] = value;

            return new DynamicDrop(model);
        }

        /// <summary>
        /// сортировка с поддержкой expandoObject и dynamicDrop
        /// </summary>
        /// <param name="array"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> Sortdyn(IEnumerable<dynamic> array, string key)
        {
            if (array == null) return null;
            try
            {
                var list = array.ToList().OrderBy(p =>
                {
                    if (p is DynamicDrop)
                    {
                        p = (p as DynamicDrop).GetViewModel();
                    }

                    if (p is IDictionary<string, object>)
                    {
                        p = ((IDictionary<string, object>)p)[key];
                    }
                    else
                    {
                        var property = p.GetType().GetProperty(key);
                        p = (property == null) ?
                            null :
                            property.GetValue(p, null);
                    }
                    return p;
                });
                //
                //OrderBy(c => c[key]);
                array = list.ToArray();
            }
            catch (Exception e)
            {

            }
            return array;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static string Tag(string name, dynamic[] tags)
        {
            var tag = tags.FirstOrDefault(t => t.BeforeMethod("tag") == name);
            if (tag == null) return name;
            return tag.BeforeMethod("name");
        }

        /// <summary>
        /// (создать) и добавить к массиву элемент
        /// </summary>
        /// <param name="array"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IEnumerable<dynamic> Cons(IEnumerable<dynamic> array, dynamic item)
        {
            if (item is DynamicDrop)
            {
                item = (item as DynamicDrop).GetViewModel();
            }

            if (array == null)
            {
                array = new dynamic[] { item };
                return array;
            }
            var lst = array.ToList();
            lst.Add(item);
            array = lst.ToArray();
            return array;
        }

        public static dynamic View(dynamic input)
        {
            if (input is DynamicDrop)
            {
                return (input as DynamicDrop).GetViewModel();
            }
            return input;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Cons(object value)
        {
            var list = new List<object>();

            if (value != null)
            {
                list.Add(value);
            }

            return list.ToArray();
        }

        #endregion
        
        public static string Hrmin(object tm)
        {
            int totalMinutes = Int32(tm);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}:{minutes:00}";
        }

        #region работа с графовой базой
        /// <summary>
        /// получение списка рассылок 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static object[] Getmailers(object obj, string userId)
        {
            var list = new List<dynamic>();

            var usrId = System.Guid.Parse(userId);
            var mailers = StructureGraph.Instance.GetMailers(usrId);

            foreach (var m in mailers)
            {
                list.Add(m);
            }

            return list.ToArray();
        }

        //// получение параметра юзера
        //public static object User(Guid id, string property)
        //{
        //    //var repo = new DataRecordRepository2();
        //    //var records = repo.Get(start, end, new string[] { type }, new Guid[] { node });
        //    var user = StructureGraph.Instance.GetUser(id);
        //    var duser = user as IDictionary<string, object>;
        //    if (duser.ContainsKey(property))
        //    {
        //        return duser[property];
        //    }
        //    return "";
        //}
        #endregion

        /// <summary>
        /// замер производительности
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="action">1 - запуск; 0 - останов, результат в мс</param>
        /// <returns></returns>
        public static string Stopwatch(object obj, int action)
        {
            string result = "";
            if (action == 0)
            {
                if (sw != null)
                {
                    sw.Stop();
                    result = sw.ElapsedMilliseconds.ToString();
                    sw = null;
                }
            }
            else if (action == 1)
            {
                if (sw != null)
                {
                    sw.Stop();
                }
                sw = new Stopwatch();
                sw.Start();
            }

            return result;
        }

        public static string[] Bits(object number, int size, bool isAbc)
        {
            UInt32 integer = UInt32(number);
            List<string> result = new List<string>();
            for (int bit = 0, mask = 1; (bit < size) && (mask > 0); bit++, mask <<= 1)
            {
                if ((integer & mask) > 0)
                {
                    if (!isAbc || (bit < 10))
                    {
                        result.Add($"{bit}");
                    }
                    else
                    {
                        result.Add($"{(char)('A' + bit - 10)}");
                    }
                }
            }
            return result.ToArray();
        }

        public static UInt32 Logor(object oper1, object oper2)
        {
            UInt32 val1 = UInt32(oper1);
            UInt32 val2 = UInt32(oper2);
            return val1 | val2;
        }

        public static UInt32 Logand(object oper1, object oper2)
        {
            UInt32 val1 = UInt32(oper1);
            UInt32 val2 = UInt32(oper2);
            return val1 & val2;
        }
    }

    public static class DataRecordExtension
    {
        // оптимизация скорости, уже устарело
        public static object GetProperty(this DataRecord value, string property)
        {
            switch (property)
            {
                case "date":
                    return value.Date;
                case "id":
                    return value.Id;
                case "objectId":
                    return value.ObjectId;

                case "g1":
                    return value.G1;
                case "g2":
                    return value.G2;
                case "g3":
                    return value.G3;

                case "s1":
                    return value.S1;
                case "s2":
                    return value.S2;
                case "s3":
                    return value.S3;

                case "i1":
                    return value.I1;
                case "i2":
                    return value.I2;
                case "i3":
                    return value.I3;

                case "d1":
                    return value.D1;
                case "d2":
                    return value.D2;
                case "d3":
                    return value.D3;

                case "dt1":
                    return value.Dt1;
                case "dt2":
                    return value.Dt2;
                case "dt3":
                    return value.Dt3;
            }
            return null;
        }
    }
}

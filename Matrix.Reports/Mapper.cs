using DotLiquid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
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

            try
            {
                Template template = Template.Parse(tmpl);
                renderResult = template.Render(Hash.FromAnonymousObject(new { root = new Reports.DynamicDrop(model), cache = new List<dynamic>(), session = new Reports.DynamicDrop(session), buildResult }));
            }
            catch (Exception ex)
            {
                renderResult = ex.Message;
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
            var c = new Cache();
            var data = c.Get(type, start, end, ids);
            //var data = Data.Cache.Instance.GetRecords(start, end, type, ids);
            cache.AddRange(data);
            return "";
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
            var c = new Cache();
            var data = c.Decorate(ids, start, end, type, usrId);
            cache.AddRange(data);
            return "";
        }

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
            return Loadpretty(cache, type, start, end, new object[] { id }, userId);
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
            var dates = cache.Where(r => r.objectid == id && r.type == type).Select(r => (DateTime)r.date).Distinct().OrderBy(d => d).ToArray();
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
            var dates = cache.Where(r => r.objectid == id && r.type == type).Select(r =>
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
            var dates = cache.Where(r => r.objectid == id && r.date == date).Select(r =>
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
            var max = cache.Where(r => r.type == type && r.s1 == parameter && r.objectid == id).Select(r =>
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
            var max = cache.Where(r => r.type == type && r.s1 == parameter && r.objectid == id).Select(r =>
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
            var record = cache.FirstOrDefault(r => r.type == type && r.date == date && r.s1 == parameter && r.objectid == id);
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property.ToLower())) return "";
            return drecord[property];
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
            var record = cache.OrderByDescending(r => r.dt1).FirstOrDefault(r => r.type == type && r.date == date && r.objectid == id);
            if (record == null) return null;
            var drecord = record as IDictionary<string, object>;
            if (!drecord.ContainsKey(property)) return "";
            return drecord[property];
        }

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
        public static IEnumerable<dynamic> Records(Guid node, List<dynamic> cache, string type, DateTime start, DateTime end)
        {
            //var repo = new DataRecordRepository2();
            //var records = repo.Get(start, end, new string[] { type }, new Guid[] { node });
            var records = cache.Where(d => d.type == type &&
                d.objectid == node && d.date >= start && d.date <= end);
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
                case "hour":
                    return date.AddHours(count);
                case "day":
                    return date.AddDays(count);
                case "month":
                    return date.AddMonths(count);
                case "year":
                    return date.AddYears(count);
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
        public static object[] Cons(dynamic[] array, dynamic item)
        {
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// 
        public static object Cons(object value)
        {
            var list = new List<object>();

            if (value != null)
            {
                list.Add(value);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// 
        public static object Cons(IEnumerable<object> value1, object value2)
        {
            var list = new List<object>();

            if (value1 != null)
            {
                list.AddRange(value1);
            }

            if (value2 != null)
            {
                list.Add(value2);
            }

            return list.ToArray();
        }

        #endregion


        #region работа с графовой базой (НЕДОСТУПНА!)
        public static object[] Getmailers(object obj, string userId)
        {
            var list = new List<dynamic>();

            var usrId = System.Guid.Parse(userId);
            //var mailers = StructureGraph.Instance.GetMailers(usrId);

            //foreach (var m in mailers)
            //{
            //    list.Add(m);
            //}

            return list.ToArray();
        }
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
        
    }

    //static class ReportExtensions
    //{
    //    public static IEnumerable<dynamic> Pivot<TSource>(this IEnumerable<TSource> data,
    //        string pointField,
    //        string propertyField,
    //        IEnumerable<string> valueFields)
    //    {
    //        if (data == null) return null;

    //        var result = new List<dynamic>();

    //        var point = typeof(TSource).GetProperty(pointField);
    //        var property = typeof(TSource).GetProperty(propertyField);
    //        var values = new List<PropertyInfo>();
    //        foreach (var valueField in valueFields)
    //        {
    //            var value = typeof(TSource).GetProperty(valueField);
    //            values.Add(value);
    //        }

    //        foreach (var rowData in data.GroupBy(d => point.GetValue(d, null)))
    //        {
    //            dynamic obj = new ExpandoObject();
    //            var dobj = obj as IDictionary<string, object>;
    //            foreach (var record in rowData)
    //            {
    //                var fixedGroup = point.GetValue(record, null);

    //                var name = pointField;
    //                if (!dobj.ContainsKey(name))
    //                    dobj.Add(name, fixedGroup);

    //                var propName = property.GetValue(record, null).ToString();
    //                dynamic propValue = new ExpandoObject();
    //                if (values.Count() > 1)
    //                {
    //                    foreach (var value in values)
    //                    {
    //                        (propValue as IDictionary<string, object>).Add(value.Name, value.GetValue(record, null));
    //                    }
    //                }
    //                else
    //                {
    //                    var value = values.FirstOrDefault();
    //                    propValue = value.GetValue(record, null);
    //                }
    //                if (obj.ContainsKey(propName)) continue;
    //                obj.Add(propName, propValue);
    //            }

    //            result.Add(obj);
    //        }
    //        return result;
    //    }
    //}
}

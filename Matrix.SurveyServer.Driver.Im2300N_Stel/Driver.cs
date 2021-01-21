
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        /// <summary>
        /// число повторных опросов в случае неуспешной попытки
        /// </summary>
        private const int TRY_COUNT = 4;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            try
            {

                dynamic result = new ExpandoObject();

                var param = (IDictionary<string, object>)arg;

                string version = "K";
                if (!param.ContainsKey("version"))
                {
                    log(string.Format("не указана версия, принята по-умолчанию {0}", version));
                }
                else
                {
                    version = arg.version.ToUpper();
                    log(string.Format("используется версия {0}", version));
                }

                byte na = 1;
                if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
                {
                    log(string.Format("не указан сетевой адрес, принят по-умолчанию {0}", na));
                }

                if (param.ContainsKey("start") && arg.start is DateTime)
                {
                    getStartDate = (type) => (DateTime)arg.start;
                    log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
                }
                else
                {
                    getStartDate = (type) => getLastTime(type);
                    log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
                }

                if (param.ContainsKey("end") && arg.end is DateTime)
                {
                    getEndDate = (type) => (DateTime)arg.end;
                    log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
                }
                else
                {
                    getEndDate = null;
                    log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
                }

                var components = "Hour;Day;Constant;Abnormal;Current";
                if (param.ContainsKey("components"))
                {
                    components = arg.components;
                    log(string.Format("указаны архивы {0}", components));
                }
                else
                {
                    log(string.Format("архивы не указаны, будут опрошены все"));
                }


                switch (what.ToLower())
                {
                    case "all": return All(na, version, components);
                }

                log(string.Format("неопознаная команда {0}", what));
                result.success = false;
                result.description = string.Format("неопознаная команда {0}", what);

            }catch(Exception ex)
            {
                log("ошибка: " + ex.Message + "; " + ex.StackTrace);
            }
            return MakeResult(0);
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        private dynamic All(byte na, string version, string components)
        {
            if (version.Contains("A"))
                return AllA(na, components);

            if (version.Contains("X") || version.Contains("Y"))
                return MakeResult(1, "для серии X драйвер не реализован");

            return AllK(na, version, components);
        }

        private dynamic AllA(byte na, string components)
        {
            var adapter = IdentifyAdapter();


            #region Паспорт
            dynamic passport = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (fullCancel()) return MakeResult(200);

                passport = GetPassportA(na);
                if (passport.success) break;
                log(string.Format("паспорт не прочитан: {0}", passport.error));
            }
            if (!passport.success) return MakeResult(101);
            log(string.Format("паспорт прочитан"));

            #endregion

            #region Текущие

            dynamic current = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (fullCancel()) return MakeResult(200);

                current = GetCurrentA(na, passport);
                if (current.success) break;
                log(string.Format("текушие не получены: {0}", current.error));
            }

            if (!current.success) return MakeResult(102);

            log(string.Format("текушие получены: дата {0:dd.MM.yyyy HH:mm:ss}", current.date));
            records(current.records);

            if (getEndDate == null)
                getEndDate = (type) => current.date;

            DateTime currentDate = current.date;
            setTimeDifference(DateTime.Now - currentDate);

            #endregion

            log(string.Format("время начала опроса {0:dd.MM.yyyy HH:mm}", getStartDate("Day")));

            if (getStartDate("Day") > currentDate)
            {
                log("время начала опроса превышает текущее время вычислителя");
                return MakeResult(104);
            }

            #region Константы
            ///необходимо заново прочесть константы
            //var needRead = false;
            //int contractHour = getContractHour();
            //if (contractHour == -1) needRead = true;
            int contractHour = 0;
            if (true)//needRead || components.Contains("Constant"))
            {
                dynamic constants = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (fullCancel()) return MakeResult(200);

                    constants = GetConstantsA(na, current.date);
                    if (constants.success) break;

                    log(string.Format("константы не получены: {0}", constants.error));
                }
                if (!constants.success) return MakeResult(103);

                if (components.Contains("Constant")) records(constants.records);
                contractHour = constants.contractHour;
                setContractHour(contractHour);
                log(string.Format("константы получены, контрактный час: {0}", contractHour));
            }
            else
            {
                log(string.Format("контрактный час был прочитан из локальной БД: {0}", contractHour));
            }

            #endregion

            #region Сутки

            if (components.Contains("Day"))
            {
                var startDay = getStartDate("Day").Date;
                var endDay = getEndDate("Day").Date;

                if (startDay > currentDate)
                    startDay = currentDate.AddHours(-contractHour).AddDays(-1).Date;

                var limitDay = currentDate.AddHours(-contractHour).AddDays(-1).Date;
                if (endDay > limitDay)
                    endDay = limitDay;

                var day = GetDayA(na, passport, startDay, endDay, currentDate, adapter.is2318);
                if (!day.success)
                {
                    log(string.Format("суточные записи не получены, {0}", day.error));
                    return MakeResult(104);
                }
                var dmin = (day.records as IEnumerable<dynamic>).Min(r => r.date);
                var dmax = (day.records as IEnumerable<dynamic>).Max(r => r.date);
                log(string.Format("суточные записи получены, за период {0:dd.MM.yy}-{1:dd.MM.yy}", dmin, dmax));
                records(day.records);
            }

            #endregion

            #region Часы

            if (components.Contains("Hour"))
            {
                var startHour = getStartDate("Hour");
                var endHour = getEndDate("Hour");

                if (startHour > currentDate)
                    startHour = currentDate.Date.AddDays(-1).AddHours(currentDate.Hour);

                if (endHour > currentDate)
                    endHour = currentDate.Date.AddHours(currentDate.Hour);

                var hours = new List<dynamic>();

                if (adapter.is2318)
                {
                    dynamic hour = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (fullCancel()) return MakeResult(200);

                        hour = GetHourA(na, passport, startHour, endHour, currentDate);
                        if (hour.success) break;

                        log(string.Format("часовые записи не получены, {0}", hour.error));
                    }
                    if (!hour.success) return MakeResult(105);

                    var minDate = (hour.records as IEnumerable<dynamic>).Min(r => r.date);
                    var maxDate = (hour.records as IEnumerable<dynamic>).Max(r => r.date);
                    log(string.Format("часовые записи получены, за период {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", minDate, maxDate));
                    records(hour.records);
                    hours.AddRange(hour.records);
                }
                else
                {
                    //попытка прочитать сразу много блоков не успешна, видимо мал буффер, читаем с разбивкой по суткам
                    for (var date = startHour; date <= endHour; date = date.AddDays(1))
                    {
                        if (fullCancel()) return MakeResult(200);


                        dynamic hour = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            hour = GetHourA(na, passport, date, date.AddDays(1), current.date);
                            if (hour.success) break;
                            log(string.Format("часовые записи не получены, {0}", hour.error));
                        }
                        if (!hour.success) return MakeResult(105);

                        var maxDate = (hour.records as IEnumerable<dynamic>).Max(r => r.date);
                        var minDate = (hour.records as IEnumerable<dynamic>).Min(r => r.date);
                        log(string.Format("часовые записи получены, за период {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", minDate, maxDate));
                        records(hour.records);
                        hours.AddRange(hour.records);
                    }
                }
            }

            #endregion

            #region НС
            if (components.Contains("Abnormal"))
            {
                var startEvent = getStartDate("Abnormal");
                var endEvent = getEndDate("Abnormal");
                var events = GetEventsA(na);

                if (!events.success)
                {
                    log(string.Format("события не получены, {0}", events.error));
                    return MakeResult(106);
                }
                log(string.Format("события получены"));
                var evs = (events.records as IEnumerable<dynamic>).Where(r => r.date > startEvent && r.date < endEvent);
                records(evs);
            }
            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic AllK(byte na, string version, string components)
        {
            var adapter = IdentifyAdapter();

            #region Паспорт
            dynamic passport = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (fullCancel()) return MakeResult(200);

                passport = GetPassport(na, version);
                if (passport.success) break;
                log(string.Format("паспорт не получен, ошибка: {0}", passport.error));
            }
            if (!passport.success) return MakeResult(101);
            log(string.Format("паспорт прочитан, число каналов {0}", passport.channels.Count));

            #endregion

            #region Текущие

            dynamic current = new ExpandoObject();
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (fullCancel()) return MakeResult(200);

                current = GetCurrent(na, version, passport);
                if (current.success) break;
                log(string.Format("текущие не получены: {0}", current.error));
            }
            if (!current.success) return MakeResult(102);

            log(string.Format("текущие получены, время вычислителя: {0:dd.MM.yyyy HH:mm:ss}", current.date));
            records(current.records);

            //if (getStartDate("Hour") > current.date)
            //{
            //    log("время начала опроса превышает текущее время вычислителя");
            //    return MakeResult(999);
            //}

            if (getEndDate == null)
                getEndDate = (type) => current.date;

            DateTime currentDate = current.date;
            setTimeDifference(DateTime.Now - currentDate);

            #endregion

            #region Константы

            ///необходимо заново прочесть константы
            //var needRead = false;

            //int contractHour = getContractHour();

            //if (contractHour == -1) needRead = true;
            
            var cnsts = new List<dynamic>();
            //меняем дату констант из паспорта
            foreach (var cnst in passport.constants)
            {
                cnsts.Add(MakeConstRecord(cnst.s1, cnst.s2, current.date));
            }
            cnsts.Add(MakeConstRecord("задача", passport.task, current.date));

            dynamic constants = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (fullCancel()) return MakeResult(200);

                constants = GetConstants(na, version, current.date, passport.task);
                if (constants.success) break;
                log(string.Format("константы не получены: {0}", constants.error));
            }
            if (!constants.success)
                return MakeResult(103);

            int contractHour = constants.contractHour;
            setContractHour(contractHour);

            log(string.Format("константы получены: контрактный час {0}", contractHour));
            cnsts.AddRange(constants.records);
            

            if (components.Contains("Constant"))
            {
                records(cnsts);
            }
            else
            {
                //log(string.Format("контрактный час был прочитан из локальной БД: {0}", contractHour));
            }

            #endregion

            #region Часы

            var startHour = getStartDate("Day");

            log(string.Format("время начала опроса {0:dd.MM.yy HH:mm:ss}; текущее время на приборе {1:dd.MM.yy HH:mm:ss}", startHour, current.date));

            startHour = startHour.Date.AddHours(startHour.Hour);
            var endHour = getEndDate("Hour");
            endHour = endHour.Date.AddHours(endHour.Hour);
            var hours = new List<dynamic>();

            ///читаем все разом
            if (adapter.is2318 || adapter.isMatrix)
            {

                endHour = currentDate.Date.AddHours(currentDate.Hour);

                //в случае адаптера, читаем все до текущей даты
                log(string.Format("начато чтение часовых {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", startHour, endHour));
                dynamic hour = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (fullCancel()) return MakeResult(200);

                    log(string.Format("параметры запроса start {0:dd.MM.yy HH:mm} end {1:dd.MM.yy HH:mm} curr {2:dd.MM.yy HH:mm}", startHour, endHour, current.date));
                    hour = GetHour(na, version, passport, startHour, endHour, current.date);
                    if (hour.success) break;
                    log(string.Format("часовые записи не получены, {0}", hour.error));
                }

                if (!hour.success)
                    log(string.Format("чтение часовых прервалось. причина: {0}", hour.error));
                else
                {
                    hour.records = (hour.records as IEnumerable<dynamic>).OrderBy(r => r.date).ToList();
                    log(string.Format("количество прочтенных архивов {0}", hour.records.Count));
                    DateTime minDate = (hour.records as IEnumerable<dynamic>).Min(r => r.date);
                    DateTime maxDate = (hour.records as IEnumerable<dynamic>).Max(r => r.date);
                    int count = (hour.records as IEnumerable<dynamic>).Where(r => r.s1.StartsWith(Glossary.Qn)).Count();
                    log(string.Format("прочитано {0}шт", count));
                    log(string.Format("часовые записи получены, за период {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", minDate, maxDate));
                    hours.AddRange(hour.records);
                    records(hour.records);
                }
            }
            else
            {
                log(string.Format("начато чтение часовых {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", startHour, endHour));
                //попытка прочитать сразу много блоков не успешна, видимо мал буффер, читаем с разбивкой по суткам
                for (DateTime date = startHour; date <= endHour; date = date.AddDays(1))
                {
                    if (fullCancel()) return MakeResult(200);

                    if (date > currentDate)
                    {
                        log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                        break;
                    }

                    log(string.Format("чтение {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", date, date.AddDays(1)));
                    dynamic hour = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (fullCancel()) return MakeResult(200);

                        hour = GetHour(na, version, passport, date, date.AddDays(1), current.date);
                        if (hour.success) break;

                        log(string.Format("часовые записи не получены, {0}", hour.error));
                    }

                    if (!hour.success)
                    {
                        log(string.Format("чтение часовых прервалось. причина: {0}", hour.error));
                        break;
                    }

                    hour.records = (hour.records as IEnumerable<dynamic>).OrderBy(r => r.date).ToList();
                    var minDate = (hour.records as IEnumerable<dynamic>).Min(r => r.date);
                    var maxDate = (hour.records as IEnumerable<dynamic>).Max(r => r.date);
                    int count = (hour.records as IEnumerable<dynamic>).Where(r => r.s1.StartsWith(Glossary.Qn)).Count();
                    log(string.Format("часовые записи получены, за период {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", minDate, maxDate));

                    hours.AddRange(hour.records);
                    records(hour.records);
                }
            }

            if (hours == null || hours.Count == 0)
            {
                return MakeResult(105);
            }

            #endregion

            #region Сутки

            var startDay = getStartDate("Day");
            var endDate = getEndDate("Day");

            var readyEnd = endDate;
            if (endDate < endDate.Date.AddHours(contractHour))
            {
                readyEnd = endDate.AddDays(-1);
                log(string.Format("суточные за {0:dd.MM.yyyy HH:mm} еще не сформированы, будут расчитаны до {1:dd.MM.yyyy}", endDate, readyEnd));
            }

            hours = hours.OrderBy(r => r.date).ToList();

            for (var date = startDay.Date; date < readyEnd.Date; date = date.AddDays(1))
            {
                //log("дата суточных ");
                if (fullCancel()) return MakeResult(200);

                if (date > current.date.AddHours(-contractHour).AddDays(-1).Date)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", date));
                    break;
                }
                records(CalcDay(hours, contractHour, date));
            }

            #endregion

            #region НС
            if (components.Contains("Abnormal"))
            {
                var events = GetEvents(na);
                if (!events.success)
                    return MakeResult(106);

                var lastEvent = getLastTime("Abnormal");
                var evs = (events.records as IEnumerable<dynamic>).Where(e => e.date > lastEvent).ToArray();
                log(string.Format("события прочитаны, {0} новых", evs.Length));
                records(events.records);
            }
            #endregion

            return MakeResult(0);
        }

        private dynamic IdentifyAdapter()
        {
            dynamic adapter = new ExpandoObject();
            adapter.is2318 = false;
            adapter.isStel = false;
            adapter.isMatrix = false;

            adapter.is2318 = Is2318();
            if (adapter.is2318)
            {
                GetBlocks = GetBlocks2318;
                return adapter;
            }

            adapter.isStel = IsStel();
            if (adapter.isStel)
            {
                GetBlocks = GetBlocksStel;
                return adapter;
            }
            log(string.Format("используется Matrix"));
            adapter.isMatrix = true;
            GetBlocks = GetBlocksMatrix;
            return adapter;
        }

        private bool Is2318()
        {
            var timeout = 5000;
            byte[] okBytes = new byte[] { };
            while (timeout > 0 && !okBytes.Any())
            {
                Thread.Sleep(100);
                timeout = timeout - 100;
                okBytes = response();
            }
            if (okBytes == null || !okBytes.Any()) return false;
            var msg = Encoding.ASCII.GetString(okBytes);
            log(string.Format("от адаптера получено {0}", msg));
            var result = msg.ToUpper().Contains("OK");
            if (result) log(string.Format("используется адаптер 2318"));
            return result;
        }

        private bool IsStel()
        {
            var stelVersion = GetStelVersion();
            if (!stelVersion.success)
            {
                return false;
            }
            log(string.Format("версия СТЕЛ получена: {0}", stelVersion.version));
            return true;
        }

        private IEnumerable<dynamic> CalcDay(IEnumerable<dynamic> hours, int contractHour, DateTime day)
        {
            List<dynamic> result = new List<dynamic>();
            DateTime start = day.Date.AddHours(contractHour);
            DateTime end = day.Date.AddDays(1).AddHours(contractHour - 1);
            hours = hours.Where(h => h.date >= start && h.date <= end);
            var count = hours.Where(h => h.s1.StartsWith(Glossary.Gn)).Count();

            if (count == 0) return result;

            if (count < 24)
            {
                log(string.Format("недостаточное количество часовых архивов ({0} из 24) за сутки {1:dd.MM.yy}. Попытка использовать архивы локальной БД", count, day));

                var dates = hours.Where(h => (h as IDictionary<string, object>).ContainsKey("date") && h.date != null).Select(h => (DateTime)h.date).ToArray();
                var localHour = getRange("Hour", start, end).Where(h => !dates.Contains((DateTime)h.date));
                log(string.Format("из локальной БД прочитано {0} часовых архивов за сутки {1:dd.MM.yy}", localHour.Where(h => h.s1.StartsWith(Glossary.Gn)).Count(), day));
                hours = hours.Union(localHour);
            }

            if (count > 24)
            {
                //  log(string.Format("избыточное количество часовых архивов ({0} из 24) за сутки {1:dd.MM.yy}", count, day));
                hours = hours.Distinct();
            }

            foreach (var x in hours.GroupBy(g => g.s1))
            {
                if (x.Count() != 24)
                {
                    log(string.Format("записи по параметру {0} за {1:dd.MM.yyyy} !=24 ({2} шт)", x.Key, day.Date, x.Count()));
                    //string sdate1 = SerializeObject(x.ToList());

                    //string path = @"D:\Im2300\";
                    //string fileName1 = string.Format("{0}Hour{1:MMdd}_{2}.txt", path, day, x.Key);
                    //System.IO.File.WriteAllText(fileName1, sdate1);

                    //string sdate2 = SerializeObject(hours);

                    //string fileName2 = string.Format("{0}Hours_{1:dd.HH_mm_ss}.txt", path, DateTime.Now);
                    //System.IO.File.WriteAllText(fileName2, sdate2);
                }

                if (x.Key.StartsWith(Glossary.T) ||
                   x.Key.StartsWith(Glossary.P) ||
                   x.Key.StartsWith(Glossary.Pa) ||
                   x.Key.StartsWith(Glossary.Pb) ||
                   x.Key.StartsWith(Glossary.dP))
                {
                    var avg = x.Average(y => (double)y.d1);
                    result.Add(MakeDayRecord(x.Key, avg, x.First().s2, day));
                    // log(string.Format("{0}: {1}", x.Key, string.Join("; ", x.Select(y => (double)y.d1))));
                    //  if (x.Key.StartsWith(Glossary.T)) log(string.Format("температуры за {0:dd.MM.yy} [{1}] (среднее {2})", day, string.Join(",", x.Select(r => r.d1)), avg));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.Gm) ||
                    x.Key.StartsWith(Glossary.Go) ||
                    x.Key.StartsWith(Glossary.Gn) ||
                    x.Key.StartsWith(Glossary.Gr) ||
                    x.Key.StartsWith(Glossary.Gw) ||
                    x.Key.StartsWith(Glossary.ts) ||
                    x.Key.StartsWith(Glossary.tm))
                {
                    result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day));
                    // log(string.Format("{0}: {1}", x.Key, string.Join("; ", x.Select(y => (double)y.d1))));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.Qn) ||
                    x.Key.StartsWith(Glossary.Qw) ||
                    x.Key.StartsWith(Glossary.Qo))
                {
                    result.Add(MakeDayRecord(x.Key, x.Sum(y => (double)y.d1), x.First().s2, day));
                    // log(string.Format("{0}: {1}", x.Key, string.Join("; ", x.Select(y => (double)y.d1))));
                    continue;
                }
            }

            log(string.Format("рассчитана суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm}", day, start, end));
            return result;
        }

        private string SerializeObject(IEnumerable<dynamic> objects)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var obj in objects)
            {
                var dobj = obj as IDictionary<string, object>;
                foreach (var key in dobj.Keys)
                {
                    sb.Append(string.Format("{0}: {1}; ", key, dobj[key]));
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
    }
}

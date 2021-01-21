// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private readonly DateTime RECORDDT_EMPTY_MIN = DateTime.MinValue;
        private readonly DateTime RECORDDT_ERROR_MAX = DateTime.MaxValue;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private void log(string message, int level = 2)
        {
#if OLD_DRIVER
            if ((level < 3) || ((level == 3) && debugMode))
            {
                logger(message);
            }
#else
            logger(message, level);
#endif
        }

        /// <summary>
        /// число попыток опроса в случае неуспеха
        /// </summary>
        private const int TRY_COUNT = 3;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            try
            {
                var parameters = (IDictionary<string, object>)arg;

                int sn = 0;

                if (!parameters.ContainsKey("networkAddress") || !int.TryParse(arg.networkAddress.ToString(), out sn))
                {
                    log(string.Format("отсутствуют сведения о серийном номере, принят по умолчанию {0:0000}", sn));
                }
                else
                    log(string.Format("используется серийный номер {0}", sn));

#if OLD_DRIVER
                byte debug = 0;
                if (parameters.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
                {
                    if (debug > 0)
                    {
                        debugMode = true;
                    }
                }
#endif

                int isMatrix = 0;
                if (!parameters.ContainsKey("isMatrix") || !int.TryParse(arg.isMatrix.ToString(), out isMatrix))
                {
                    log(string.Format("отсутствуют сведения о использовании Матрикс, по-умолчанию {0} используется", isMatrix == 1 ? "" : "НЕ"));
                }
                else
                    log(string.Format("Матрикс {0} используется", isMatrix == 1 ? "" : "НЕ"));


                if (parameters.ContainsKey("start") && arg.start is DateTime)
                {
                    getStartDate = (type) => (DateTime)arg.start;
                    log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
                }
                else
                {
                    getStartDate = (type) => getLastTime(type);
                    log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
                }

                if (parameters.ContainsKey("end") && arg.end is DateTime)
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
                if (parameters.ContainsKey("components"))
                {
                    components = arg.components;
                    log(string.Format("указаны архивы {0}", components));
                }
                else
                {
                    log(string.Format("архивы не указаны, будут опрошены все"));
                }

                #region hourRanges
                List<dynamic> hourRanges;
                if (parameters.ContainsKey("hourRanges") && arg.hourRanges is IEnumerable<dynamic>)
                {
                    hourRanges = arg.hourRanges;
                    foreach (var range in hourRanges)
                    {
                        log(string.Format("принят часовой диапазон {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", range.start, range.end));
                    }
                }
                else
                {
                    hourRanges = new List<dynamic>();
                    dynamic defaultrange = new ExpandoObject();
                    defaultrange.start = getStartDate("Hour");
                    defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Hour");
                    hourRanges.Add(defaultrange);
                }
                #endregion

                #region dayRanges
                List<dynamic> dayRanges;
                if (parameters.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
                {
                    dayRanges = arg.dayRanges;
                    foreach (var range in dayRanges)
                    {
                        log(string.Format("принят суточный диапазон {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", range.start, range.end));
                    }
                }
                else
                {
                    dayRanges = new List<dynamic>();
                    dynamic defaultrange = new ExpandoObject();
                    defaultrange.start = getStartDate("Day");
                    defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Day");
                    dayRanges.Add(defaultrange);
                }
                #endregion

                switch (what.ToLower())
                {
                    case "all": return All(hourRanges, dayRanges, sn, isMatrix == 1, components);
                }
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка: {0} {1}", ex.Message, ex.StackTrace), level: 1);
                return MakeResult(999, ex.Message);
            }

            log(string.Format("неопознаная команда {0}", what), level: 1);
            return MakeResult(201, what);
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        private string VersionToString(byte ver)
        {
            return $"{(ver >> 4):0}.{(ver & 0x0F):0}";
        }

        private struct ArchiveInfo
        {
            public int archiveCapacity;
            public string archiveType;
            public TimeSpan recordPeriod;
            public string recordFormatDt;
            public Func<int, int, dynamic> TryGetArchiveRecord;
            public Func<DateTime, DateTime, int> GetRecordDateOffset;
            //public Func<DateTime, DateTime> GetCurrentRecordDateFromCurrentDt;
            public Func<DateTime, DateTime> RoundDateTime;
        }



        private dynamic ReadCircleArchive(int sn, DateTime currentDate, List<dynamic> ranges, ArchiveInfo info)
        {
            setArchiveDepth(info.archiveType, info.archiveCapacity);

            dynamic ret = new ExpandoObject();
            ret.success = false;
            ret.error = "";
            ret.isCancel = false;

            List<dynamic> archiveRecords = new List<dynamic>();

            //Получение записи #0 - точка отсчета
            DateTime date0;
            {
                if (cancel())
                {
                    ret.isCancel = true;
                    ret.error = "задача отменена";
                    return ret;
                }
                dynamic arecord = info.TryGetArchiveRecord(sn, 0);
                if (!arecord.success)
                {
                    ret.error = $"ошибка при получении записи #0: {arecord.error}";
                    return ret;
                }
                log($"индекс записи #0 дата {arecord.date:dd.MM.yyyy HH:mm}", level: 3);
                date0 = arecord.date;
            }

            if (date0 == RECORDDT_EMPTY_MIN)
            {
                ret.error = "архив пуст";
                return ret;
            }

            //Предполагаемая дата последней записи
            DateTime currentRecordDate = info.RoundDateTime(currentDate).Add(-info.recordPeriod); //info.GetCurrentRecordDateFromCurrentDt(currentDate);//.Date.AddDays(-1);

            //Поиск границ архива - начало и конец
            DateTime firstRecordDate = RECORDDT_ERROR_MAX;
            int firstInx = -1;
            DateTime lastRecordDate = RECORDDT_ERROR_MAX;
            int lastInx = -1;

            int lastRecordOffset = info.GetRecordDateOffset(currentRecordDate, date0) + 1;
            if (lastRecordOffset >= info.archiveCapacity) lastRecordOffset = info.archiveCapacity - 1;
            for (int i = 0; i < info.archiveCapacity; i++) // поиск по всему архиву "вниз" до получения начального и конечного дней
            {
                if (cancel())
                {
                    ret.isCancel = true;
                    ret.error = "задача отменена";
                    return ret;
                }
                int circleInx = lastRecordOffset >= i ? lastRecordOffset - i : info.archiveCapacity - (i - lastRecordOffset);
                dynamic arecord = info.TryGetArchiveRecord(sn, circleInx);
                if (!arecord.success)
                {
                    ret.error = $"не удалось получить архивную запись: {arecord.error}";
                    return ret;
                }
                DateTime recDt = (DateTime)arecord.date;

                log($"индекс записи #{circleInx} дата {recDt:dd.MM.yyyy HH:mm}", level: 3);
                if (firstRecordDate == RECORDDT_ERROR_MAX)
                {
                    firstRecordDate = (recDt == RECORDDT_EMPTY_MIN) ? RECORDDT_EMPTY_MIN : info.RoundDateTime(recDt); // если запись пуста, то все равно записываем 
                    firstInx = circleInx;
                }
                else if (recDt <= firstRecordDate)
                {
                    firstRecordDate = info.RoundDateTime(recDt);
                    firstInx = circleInx;
                }
                else
                {
                    lastRecordDate = info.RoundDateTime(recDt);
                    lastInx = circleInx;
                    break;
                }
            }

            log($"начало буфера #{firstInx} {firstRecordDate:dd.MM.yyyy} конец #{lastInx} {lastRecordDate:dd.MM.yyyy}", level: 3);
            if ((firstInx == -1) || (lastInx == -1))
            {
                ret.error = "ошибка при определении границ архива";
                return ret;
            }


            foreach (var range in ranges)
            {
                DateTime start = range.start;
                DateTime end = range.end;

                for (DateTime date = start; date <= end; date = date.Add(info.recordPeriod))
                {
                    if (cancel())
                    {
                        ret.isCancel = true;
                        ret.error = "задача отменена";
                        return ret;
                    }
                    //log($"дата {date:dd.MM.yy HH} проверка на пределы архива {firstDay:dd.MM.yy HH}-{lastDay:dd.MM.yy HH} ");
                    if (date < firstRecordDate)
                    {
                        log(string.Format($"{info.recordFormatDt} за пределами архива", date), level: 2);
                        continue;
                    }
                    if (date > lastRecordDate)
                    {
                        log(string.Format($"{info.recordFormatDt} ещё не сформирована, остановка опроса", date), level: 2);
                        break;
                    }

                    //поиск в архиве от смещения (lastDay - date) "вверх" до lastDay
                    for (int i = info.GetRecordDateOffset(lastRecordDate, date); i >= 0; i--)
                    {
                        if (cancel())
                        {
                            ret.isCancel = true;
                            ret.error = "задача отменена";
                            return ret;
                        }
                        int circleInx = lastInx >= i ? lastInx - i : info.archiveCapacity - (i - lastInx);
                        dynamic arecord = info.TryGetArchiveRecord(sn, circleInx);
                        if (!arecord.success)
                        {
                            ret.error = $"не удалось получить архивную запись: {arecord.error}";
                            return ret;
                        }
                        DateTime recordDate = info.RoundDateTime(arecord.date);
                        log($"индекс записи #{circleInx} дата {recordDate:dd.MM.yyyy HH:mm}", level: 3);

                        //log($"дата {date:dd.MM.yy HH} прочитана #{circleInx} {day.date:dd.MM.yy HH} (сут.{dayDate:dd.MM.yy HH}) ");
                        if (recordDate == date)
                        {
                            log(string.Format($"{info.recordFormatDt} #{circleInx} получена", date));
                            records(arecord.records);
                            archiveRecords.AddRange(arecord.records);
                            break;
                        }
                        else if (recordDate > date)
                        {
                            log(string.Format($"{info.recordFormatDt} НЕ получена (дыра в архиве?)", date));
                            break;
                        }
                        else
                        {
                            //log($"запрошен {date} получен {dayDate}, пропуск");
                        }
                    }
                }
            }

            ret.records = archiveRecords;
            ret.success = true;
            return ret;
        }

        private dynamic All(List<dynamic> hourRanges, List<dynamic> dayRanges, int sn, bool isMatrix, string components)
        {
            DoRaccord(isMatrix);

            var current = GetCurrents(sn);
            if (!current.success)
            {
                log(string.Format("текущие не получены, {0}", current.error), level: 1);
                return MakeResult(102);
            }
            if (getEndDate == null) getEndDate = (type) => current.date;
            records(current.records);
            log(string.Format("текущие получены, дата на приборе {0:dd.MM.yyyy HH:mm:ss}", current.date), level: 1);

            DateTime currentDate = current.date;
            setTimeDifference(DateTime.Now - currentDate);

            int contractHour = getContractHour();

            if (contractHour == -1)
            {
                // констант у вычислителя нет, но нужно придерживаться установленным нормам 
                contractHour = 0;
                setContractHour(contractHour);
            }

            log(string.Format("контрактный час — {0}", contractHour));

            if (components.Contains("Constant"))
            {
                dynamic rsp = ParseResponse(Send(MakeMemoryRequest(sn, 0x0002, 12)));
                if (!rsp.success)
                {
                    log(string.Format("константы не получены, {0}", rsp.error), level: 1);
                    return MakeResult(103);
                }
                byte[] body = rsp.body;
                if (body.Length == 12)
                {
                    string snumber = $"{BitConverter.ToUInt32(body, 0):00000000}";
                    string verHw = VersionToString(body[4]);
                    string verSw = VersionToString(body[5]);

                    List<dynamic> recs = new List<dynamic>();
                    recs.Add(MakeConstRecord("Заводской номер прибора", snumber, currentDate));
                    recs.Add(MakeConstRecord("Версия аппаратной части прибора", verHw, currentDate));
                    recs.Add(MakeConstRecord("Версия программной части прибора", verSw, currentDate));
                    recs.Add(MakeConstRecord("Дата запуска счетчика", $"{new DateTime(year: 2000 + body[11], month: body[10], day: body[9], hour: body[8], minute: body[7], second: body[6]):dd.MM.yyyy HH:mm:ss}", currentDate));
                    records(recs);
                    log($"константы получены, версия прибора {verHw}, ПО {verSw}, заводской номер {snumber}", level: 1);
                }
            }

            if (components.Contains("Day"))
            {
                #region Сутки

                ArchiveInfo info = new ArchiveInfo();
                info.archiveCapacity = 300;
                info.archiveType = "Day";
                //info.GetCurrentRecordDateFromCurrentDt = (DateTime a) => { return a.Date.AddDays(-1); };
                info.GetRecordDateOffset = (DateTime a, DateTime b) => { return (int)(a.Date - b.Date).TotalDays; };
                info.recordFormatDt = "суточная запись за {0:dd.MM.yyyy}";
                info.recordPeriod = new TimeSpan(days: 1, hours: 0, minutes: 0, seconds: 0);
                info.RoundDateTime = (DateTime a) => { return a.Date; };
                info.TryGetArchiveRecord = (s, index) =>
                {
                    dynamic day = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        day = GetDayCached(s, index);
                        if (day.success) break;
                    }
                    if (!day.success) return day;
                    return day;
                };

                var startDay = getStartDate("Day").AddHours(-contractHour).Date;
                var endDay = getEndDate("Day").AddHours(-contractHour).Date;
                dynamic res = ReadCircleArchive(sn, currentDate, dayRanges, info);
                if (!res.success)
                {
                    log($"Не удалось прочитать суточный архив: {res.error}");
                    return MakeResult(res.isCancel ? 200 : 104, res.error);
                }


                //// Информация. Архив кольцевой, записей 300 штук - от 0 до 299. 
                //// Пустых записей не обнаруживал, но скорее всего там FFFFFFFF или ещё какая-нибудь хрень
                ////1. Читаем первую запись (#0)
                ////2. Находим последнюю запись на текущий момент смещением (сразу читаем первую?)
                ////3. Ищем нужные данные по диапазонам

                //{
                //    DateTime date0;
                //    {
                //        if (cancel()) return MakeResult(200);
                //        dynamic day = TryGetDay(sn, 0);
                //        if (!day.success) return MakeResult(104);
                //        log($"индекс записи #0 дата {day.date:dd.MM.yyyy}", level: 3);
                //        date0 = day.date;
                //    }

                //    DateTime currentDay = currentDate.Date.AddDays(-1);
                //    DateTime firstDay = DateTime.MinValue;
                //    int firstInx = -1;
                //    DateTime lastDay = DateTime.MinValue;
                //    int lastInx = -1;

                //    int lastDayOffset = (int)(currentDay - date0.Date).TotalDays + 1;
                //    if (lastDayOffset >= 300) lastDayOffset = 299;
                //    for (int i = 0; i < 300; i++) // поиск по всему архиву "вниз" до получения начального и конечного дней
                //    {
                //        if (cancel()) return MakeResult(200);
                //        int circleInx = lastDayOffset >= i ? lastDayOffset - i : 300 - (i - lastDayOffset);
                //        dynamic day = TryGetDay(sn, circleInx);
                //        if (!day.success) return MakeResult(104);
                //        log($"индекс записи #{circleInx} дата {day.date:dd.MM.yyyy}", level: 3);
                //        if (firstDay == DateTime.MinValue)
                //        {
                //            firstDay = day.date.Date;
                //            firstInx = circleInx;
                //        }
                //        else if (day.date < firstDay)
                //        {
                //            firstDay = day.date.Date;
                //            firstInx = circleInx;
                //        }
                //        else
                //        {
                //            lastDay = day.date.Date;
                //            lastInx = circleInx;
                //            break;
                //        }
                //    }

                //    log($"начало буфера #{firstInx} {firstDay:dd.MM.yyyy} конец #{lastInx} {lastDay:dd.MM.yyyy}", level: 3);

                //    var startDay = getStartDate("Day").AddHours(-contractHour).Date;
                //    var endDay = getEndDate("Day").AddHours(-contractHour).Date;
                //    //if(startDay < lastDay)
                //    //{
                //    //    log($"Запрошенная начальная дата {startDay:dd.MM.yyyy} вне архива, изменена на {firstDay:dd.MM.yyyy}", level: 1);
                //    //    startDay = firstDay;
                //    //}

                //    for (DateTime date = startDay.Date; date <= endDay; date = date.AddDays(1))
                //    {
                //        if (cancel()) return MakeResult(200);
                //        //log($"дата {date:dd.MM.yy HH} проверка на пределы архива {firstDay:dd.MM.yy HH}-{lastDay:dd.MM.yy HH} ");
                //        if (date < firstDay)
                //        {
                //            log($"суточная запись за {date:dd.MM.yyyy} за пределами архива", level: 2);
                //            continue;
                //        }
                //        if (date > lastDay)
                //        {
                //            log($"суточная запись за {date:dd.MM.yyyy} ещё не сформирована, остановка опроса", level: 2);
                //            break;
                //        }

                //        //поиск в архиве от смещения (lastDay - date) "вверх" до lastDay
                //        for (int i = (int)(lastDay - date).TotalDays; i >= 0; i--)
                //        {
                //            if (cancel()) return MakeResult(200);
                //            int circleInx = lastInx >= i ? lastInx - i : 300 - (i - lastInx);
                //            dynamic day = TryGetDay(sn, circleInx);
                //            if (!day.success) return MakeResult(104);
                //            DateTime dayDate = day.date.Date;
                //            log($"индекс записи #{circleInx} дата {dayDate:dd.MM.yyyy}", level: 3);

                //            //log($"дата {date:dd.MM.yy HH} прочитана #{circleInx} {day.date:dd.MM.yy HH} (сут.{dayDate:dd.MM.yy HH}) ");

                //            if (dayDate == date)
                //            {
                //                log($"суточная запись #{circleInx} за {date:dd.MM.yy} получена");
                //                records(day.records);
                //                break;
                //            }
                //            else if (dayDate > date)
                //            {
                //                log($"суточная запись {date:dd.MM.yy} НЕ получена (дыра в архиве?)");
                //                break;
                //            }
                //            else
                //            {
                //                log($"запрошен {date} получен {dayDate}, пропуск");
                //            }
                //        }
                //    }
                //}
                #endregion
            }

            if (components.Contains("Hour"))
            {
                #region Часы

                ArchiveInfo info = new ArchiveInfo();
                info.archiveCapacity = 1125;
                info.archiveType = "Hour";
                //info.GetCurrentRecordDateFromCurrentDt = (DateTime a) => { return a.Date.AddHours(a.Hour).AddHours(-1); };
                info.GetRecordDateOffset = (DateTime a, DateTime b) => { return (int)(a.Date.AddHours(a.Hour) - b.Date.AddHours(b.Hour)).TotalHours; };
                info.recordFormatDt = "часовая запись за {0:dd.MM.yyyy HH:mm}";
                info.recordPeriod = new TimeSpan(days: 0, hours: 1, minutes: 0, seconds: 0);
                info.RoundDateTime = (DateTime a) => { return a.Date.AddHours(a.Hour); };
                info.TryGetArchiveRecord = (s, index) =>
                {
                    dynamic hour = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        hour = GetHourCached(s, index);
                        if (hour.success) break;
                    }
                    if (!hour.success) return hour;
                    return hour;
                };

                var startHour = getStartDate("Hour");
                var endHour = getEndDate("Hour");

                dynamic res = ReadCircleArchive(sn, currentDate, hourRanges, info);
                if (!res.success)
                {
                    log($"Не удалось прочитать часовой архив: {res.error}");
                    return MakeResult(res.isCancel ? 200 : 104, res.error);
                }

                //var firstHour = GetStartRecord(sn, 0x0e);
                //if (!firstHour.success)
                //{
                //    log(string.Format("дата начала ведения часового архива не получена, {0}", firstHour.error), level: 1);
                //    return MakeResult(105);
                //}
                //log(string.Format("дата начала ведения часового архива получена, {0:dd.MM.yyyy HH:mm}", firstHour.date));

                //var currentHour = current.date.Date.AddHours(current.date.Hour);
                //var startHour = getStartDate("Hour");
                //if (startHour < firstHour.date) startHour = firstHour.date;
                //var endHour = getEndDate("Hour");
                //if (endHour > currentHour) endHour = currentHour;

                //int lastHourIndex = (int)(currentHour - firstHour.date).TotalHours;
                //int startHourIndex = lastHourIndex - (int)(currentHour - startHour).TotalHours + 1;
                //int endHourIndex = lastHourIndex - (int)(currentHour - endHour).TotalHours;

                //if (endHourIndex - startHourIndex > 0)
                //    log(string.Format("начат опрос часовых архивов {0:dd.MM.yy HH:mm}(#{2}) — {1:dd.MM.yy HH:mm}(#{3})", startHour, endHour, startHourIndex, endHourIndex));

                //for (int index = startHourIndex; index <= endHourIndex; index++)
                //{
                //    dynamic hour = null;
                //    for (int i = 0; i < TRY_COUNT; i++)
                //    {
                //        if (cancel()) return MakeResult(200);

                //        hour = GetHour(sn, index);
                //        if (hour.success) break;
                //        log(string.Format("часовая запись #{1} не получена, ошибка: {0}", hour.error, index), level: 1);
                //    }
                //    if (!hour.success) return MakeResult(105);

                //    log(string.Format("часовая запись (#{1}) {0:dd.MM.yy HH:mm} получена", hour.date, index));

                //    records(hour.records);
                //}

                #endregion
            }

            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic TryGetDay(int sn, int index)
        {
            dynamic day = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                day = GetDayCached(sn, index);
                if (day.success) break;
            }
            if (!day.success) return day;
            return day;
        }
    }
}

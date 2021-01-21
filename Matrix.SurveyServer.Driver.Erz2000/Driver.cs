using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        /// <summary>
        /// число попыток опроса в случае неуспеха
        /// </summary>
        private const int TRY_COUNT = 4;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            byte na = 1;
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
            {
                log(string.Format("не указан сетевой адрес, принят по-умолчанию {0}", na));
            }
            else
            {
                log(string.Format("сетевой адрес: {0}", na));
            }

            string mode = "1";
            if (!param.ContainsKey("mode"))
            {
                log(string.Format("не указан вариант (1-группы 0,1,8; 2-группа 8), принят по-умолчанию {0}", mode));
            }
            else
            {
                mode = arg.mode.ToString();
                log(string.Format("режим: {0} (1-группы 0,1,8; 2-группа 8)", mode));
            }

            byte channel = 1;
            if (!param.ContainsKey("channel") || !byte.TryParse(arg.channel.ToString(), out channel))
            {
                log(string.Format("не указан канал, принят по-умолчанию {0}", channel));
            }
            else
            {
                log(string.Format("канал: {0}", channel));
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
                log(string.Format("дата начала опроса не указана, опрос продолжится до последней записи в вычислителе"));
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
                case "all": return All(na, mode, components);
                default:
                    {
                        log(string.Format("неопознаная команда {0}", what));
                        return MakeResult(201, what);
                    }
            }
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        //private dynamic MakeAns(bool success, string description)
        //{
        //    dynamic ans = new ExpandoObject();
        //    ans.success = success;
        //    ans.description = description;
        //    return ans;
        //}

        private dynamic All(byte na, string mode, string components)
        {
            log("1");

            #region Текущие

            dynamic current = new ExpandoObject();
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel()) return MakeResult(200);
                current = GetCurrent(na);
                if (current.success) break;
                log(string.Format("текущие параметры не получены, ошибка: {0}", current.error));
            }
            if (!current.success) return MakeResult(102);
            log(string.Format("текущие параметры получены, время вычислителя: {0:dd.MM.yy HH:mm:ss}", current.date));
            if (getEndDate == null)
                getEndDate = (type) => current.date;

            records(current.records);

            DateTime currentDate = current.date;
            setTimeDifference(DateTime.Now - currentDate);

            #endregion

            #region Константы

            ///необходимо заново прочесть константы
            var needRead = false;
            int contractHour = getContractHour();

            if (contractHour == -1) needRead = true;
            if (needRead || components.Contains("Constant"))
            {
                log("начато чтение констант");
                dynamic constants = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    constants = GetConstants(na, DateTime.Now);
                    if (constants.success) break;

                    log(string.Format("константы не были прочитаны, ошибка: {0}", constants.error));
                }
                if (!constants.success) return MakeResult(103);

                records(constants.records);
                contractHour = constants.contractHour;
                setContractHour(contractHour);

                records(constants.records);
                log(string.Format("константы получены, отчетный час: {0}", contractHour));
            }
            else
            {
                log(string.Format("константы получены из БД: расчетный час={0}", contractHour));
            }
            #endregion

            //для "старых" ерзетов
            if (mode == "1")
            {
                #region mode 1

                #region Сутки
                //сутки

                //1. определяем индекс последней записи
                //2. читаем по индексу от последней до текущей даты (-12?), при этом НС записи непонятно, посмотрим...

                if (components.Contains("Day"))
                {
                    var startDay = getStartDate("Day");
                    var endDay = getEndDate("Day");

                    //начальный индекс делаем бесконечным
                    var supposeIndex = int.MaxValue;
                    var lstIdx = -1;
                    do
                    {
                        var supposeDay = GetDay(na, supposeIndex);
                        if (lstIdx == -1)
                        {
                            lstIdx = supposeDay.number;
                            log(string.Format("индекс последней записи {0}", lstIdx));
                        }
                        supposeIndex = supposeDay.number;

                        var delta = (supposeDay.date - startDay).TotalDays;
                        var offset = (int)delta;
                        if (delta > (double)(int)delta)
                        {
                            offset++;
                        }

                        if (offset == 0) break;
                        if (offset < 0) break;
                        if (offset > 0) supposeIndex = supposeDay.number - offset;

                    } while (true);

                    log(string.Format("опрос от #{0} до #{1}", supposeIndex, lstIdx));

                    //читаем с указанного индекса                    
                    for (var index = supposeIndex; index <= lstIdx; index++)
                    {
                        if (cancel()) return MakeResult(200);
                        var days = GetDay(na, index);

                        if (!days.success)
                        {
                            log(string.Format("сутки не прочитаны, {0}", days.error));
                            break;
                        }
                        log(string.Format("сутки #{1} за {0:dd.MM.yyyy} прочитаны", days.date, index));

                        records(days.records);

                        if (days.date <= startDay) continue;
                        if (days.date > endDay.AddDays(1))
                        {
                            break;
                        }
                    }

                    //var lastDayInDev = GetDay(na, int.MaxValue);
                    //if (!lastDayInDev.success)
                    //{
                    //    log(string.Format("не удалось прочитать последние сутки, {0}", lastDayInDev.error));
                    //    return;
                    //}

                    //int end = lastDayInDev.number - (int)((DateTime)currents.date - endDay).TotalDays;
                    //int start = lastDayInDev.number - (int)((DateTime)currents.date - startDay).TotalDays;

                    //log(string.Format("последняя суточная запись в приборе {0:dd.MM.yyyy}, #{1}", lastDayInDev.date, end));
                    //for (var number = start; number < end; number++)
                    //{
                    //    if (number < 0 || number > lastDayInDev.number) break;
                    //    if (cancel()) return;

                    //    var days = GetDay(na, number);
                    //    if (!days.success)
                    //    {
                    //        log(string.Format("сутки не прочитаны, {0}", days.error));
                    //        break;
                    //    }
                    //    log(string.Format("сутки #{1} за {0:dd.MM.yyyy} прочитаны", days.date, number));

                    //    if (days.date <= startDay) continue;

                    //    records(days.records);
                    //}
                    //records(lastDayInDev.records);
                }
                #endregion

                #region Часы

                if (components.Contains("Hour"))
                {
                    //часы
                    var startHour = getStartDate("Hour");
                    var endHour = getEndDate("Hour");
                    var lastHourInDev = GetHour(na, int.MaxValue);

                    if (!lastHourInDev.success)
                    {
                        log(string.Format("не удалось прочитать последнюю часовую запись, {0}", lastHourInDev.error));
                        return MakeResult(104);
                    }

                    var end = lastHourInDev.number - (int)((DateTime)currentDate - endHour).TotalHours; ;
                    var start = lastHourInDev.number - (int)((DateTime)currentDate - startHour).TotalHours;

                    log(string.Format("последняя часовая запись в приборе {0:dd.MM.yyyy HH:mm}, #{1}", lastHourInDev.date, end));
                    for (var number = start; number < end; number++)
                    {
                        if (number < 0 || number > lastHourInDev.number) break;
                        if (cancel()) return MakeResult(200);
                        var hours = GetHour(na, number);
                        if (!hours.success)
                        {
                            log(string.Format("часы не прочитаны, {0}", hours.error));
                            break;
                        }
                        log(string.Format("часы #{1} за {0:dd.MM.yyyy HH:mm} прочитаны", hours.date, number));

                        if (hours.date <= startHour) continue;

                        records(hours.records);
                    }
                    records(lastHourInDev.records);


                    var lastHourAbnInDev = GetHourAbnormal(na, int.MaxValue);
                    if (!lastHourAbnInDev.success)
                    {
                        log(string.Format("не удалось прочитать последнюю часовую НС запись, {0}", lastHourAbnInDev.error));
                        return MakeResult(105);
                    }

                    end = lastHourAbnInDev.number - (int)((DateTime)currentDate - endHour).TotalHours;
                    start = lastHourAbnInDev.number - (int)((DateTime)currentDate - startHour).TotalHours;

                    for (var number = start; number < end; number++)
                    {
                        if (number < 0 || number > lastHourAbnInDev.number) break;
                        if (cancel()) return MakeResult(200);
                        var hours = GetHourAbnormal(na, number);
                        if (!hours.success)
                        {
                            log(string.Format("часы НС не прочитаны, {0}", hours.error));
                            break;
                        }
                        log(string.Format("часы НС #{1} за {0:dd.MM.yyyy HH:mm} прочитаны", hours.date, number));

                        if (hours.date <= startHour) continue;

                        records(hours.records);
                    }
                    records(lastHourAbnInDev.records);
                }

                #endregion

                #endregion
            }
            else
            {
                #region mode 2

                #region Сутки
                if (components.Contains("Day"))
                {
                    //сутки
                    var startDay = getStartDate("Day");
                    var endDay = getEndDate("Day");
                    var lastDayInDev = GetDay2(na, int.MaxValue, contractHour);

                    if (!lastDayInDev.success)
                    {
                        log(string.Format("не удалось прочитать последние сутки, {0}", lastDayInDev.error));
                        return MakeResult(104);
                    }

                    int dayOffset = 1;
                    if (currentDate.Hour < contractHour)
                    {
                        dayOffset = 2;
                    }

                    int end = lastDayInDev.number - ((int)((DateTime)currentDate - endDay).TotalDays - dayOffset) * 24;
                    int start = lastDayInDev.number - ((int)((DateTime)currentDate - startDay).TotalDays - dayOffset) * 24;

                    log(string.Format("индексы в от {0} до {1}", start, end));

                    log(string.Format("последняя суточная запись в приборе {0:dd.MM.yyyy}, #{1}", lastDayInDev.date, end));
                    for (var number = start; number < end; number += 24)
                    {
                        if (number < 0 || number > lastDayInDev.number) break;
                        if (cancel()) return MakeResult(200);
                        var days = GetDay2(na, number, contractHour);
                        if (!days.success)
                        {
                            log(string.Format("сутки не прочитаны, {0}", days.error));
                            break;
                        }
                        log(string.Format("сутки #{1} за {0:dd.MM.yyyy} прочитаны", days.date, number));

                        if (days.date <= startDay && currentDate.AddHours(-12) < days.date) continue;

                        records(days.records);
                    }
                    records(lastDayInDev.records);
                }
                #endregion

                #region Часы
                if (components.Contains("Hour"))
                {
                    //hours
                    var startHour = getStartDate("Hour");
                    var endHour = getEndDate("Hour");
                    var lastHourInDev = GetHour2(na, int.MaxValue);

                    if (!lastHourInDev.success)
                    {
                        log(string.Format("не удалось прочитать последнюю часовую запись, {0}", lastHourInDev.error));
                        return MakeResult(105);
                    }

                    var end = lastHourInDev.number - (int)((DateTime)currentDate - endHour).TotalHours;
                    var start = lastHourInDev.number - (int)((DateTime)currentDate - startHour).TotalHours;

                    log(string.Format("последняя часовая запись в приборе {0:dd.MM.yyyy HH:mm}, #{1}", lastHourInDev.date, end));
                    for (var number = start; number < end; number++)
                    {
                        if (number < 0 || number > lastHourInDev.number) break;
                        if (cancel()) return MakeResult(200);
                        var hours = GetHour2(na, number);
                        if (!hours.success)
                        {
                            log(string.Format("часы не прочитаны, {0}", hours.error));
                            break;
                        }
                        log(string.Format("часы #{1} за {0:dd.MM.yyyy HH:mm} прочитаны", hours.date, number));

                        if (hours.date <= startHour) continue;

                        records(hours.records);
                    }
                    records(lastHourInDev.records);
                }
                #endregion

                #endregion
            }

            #region НС
            if (components.Contains("Abnormal"))
            {
                //var lastAbnormal = new DateTime[] { getLastTime("Abnormal"), lastHour, lastDay }.Min();
                var lastAbnormal = getLastTime("Abnormal");

                var number1 = int.MaxValue;
                while (true)
                {
                    if (cancel()) return MakeResult(200);
                    var abnormals = GetAbnormal(na, number1);
                    if (!abnormals.success)
                    {
                        log(string.Format("НС не прочитаны, {0}", abnormals.error));
                        break;
                    }

                    log(string.Format("НС за {0:dd.MM.yyyy HH:mm:ss} прочитаны, {0}", abnormals.date));
                    if (abnormals.date <= lastAbnormal) break;
                    records(abnormals.records);
                    number1 = abnormals.number - 1;
                }
            }
            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic ParseFloat(byte[] bytes)
        {
            var parse = ParseModbus(bytes);
            if (!parse.success)
            {
                return parse;
            }

            parse.value = ToSingle(parse.body, 1);
            return parse;
        }

        private dynamic ParseInt32(byte[] bytes)
        {
            var parse = ParseModbus(bytes);
            if (!parse.success)
            {
                return parse;
            }

            parse.value = ToInt32(parse.body, 1);
            return parse;
        }

        private dynamic ParseShort(byte[] bytes)
        {
            var parse = ParseModbus(bytes);
            if (!parse.success)
            {
                return parse;
            }

            parse.value = BitConverter.ToInt16(parse.body, 1);
            return parse;
        }

        private dynamic ParseDate(byte[] bytes)
        {
            var parse = ParseModbus(bytes);
            if (!parse.success)
            {
                return parse;
            }

            var year = ToInt16(parse.body, 1);
            var month = ToInt16(parse.body, 3);
            var day = ToInt16(parse.body, 5);
            var hour = ToInt16(parse.body, 7);
            var minute = ToInt16(parse.body, 9);
            var seconds = ToInt16(parse.body, 11);

            parse.date = new DateTime(year, month, day, hour, minute, seconds);
            return parse;
        }
    }
}

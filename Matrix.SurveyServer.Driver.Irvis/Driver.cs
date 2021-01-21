using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Threading;
using System.Diagnostics;

namespace Matrix.SurveyServer.Driver.Irvis
{
    /// <summary>
    /// Драйвер для регистраторов РИ-3|4|5
    /// </summary>
    public partial class Driver
    {
        /// <summary>
        /// Регистр выбора страницы
        /// </summary>
        private const short RVS = 0x0084;

        /// <summary>
        /// число попыток опроса в случае неуспеха
        /// </summary>
        private const int TRY_COUNT = 4;

        private bool debugMode = false;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var parameters = (IDictionary<string, object>)arg;

            byte na = 0x00;
            //byte ch = 0x01;
            byte pass = 0x00;

            if (!parameters.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
            {
                log(string.Format("отсутствутют сведения о сетевом адресе"));
                return MakeResult(202, "сетевой адрес");
            }
            else
                log(string.Format("используется сетевой адрес {0}", na));



            byte debug = 0;
            if (parameters.ContainsKey("debug") && byte.TryParse(parameters["debug"].ToString(), out debug))
            {
                if (debug == 1)
                {
                    debugMode = true;
                }
            }


            //if (!parameters.ContainsKey("channel") || !byte.TryParse(arg.channel.ToString(), out ch))
            //{
            //    ch = 0x01;
            //    log(string.Format("отсутствутют сведения о канале, принят по-умолчанию {0}", ch));
            //}
            //else
            //    log(string.Format("используется канал {0}", ch));

            var channels = new List<byte>();
            if (parameters.ContainsKey("channel")) //каналов может быть несколько, разделяются запятыми: 1,2
            {
                var chs = parameters["channel"].ToString().Split(',');
                foreach (var chstr in chs)
                {
                    var chpar = chstr.Split(':');
                    byte chb = 0;
                    if (byte.TryParse(chpar[0], out chb))
                    {
                        channels.Add(chb);
                    }
                }
            }

            if (channels.Count == 0)
            {
                channels.Add(1);
                log(string.Format("отсутствуют сведения о каналах, принят по-умолчанию {0}", channels[0]));
            }



            if (!parameters.ContainsKey("password") || !byte.TryParse(arg.password.ToString(), out pass))
                log(string.Format("отсутствутют сведения о пароле, принят по-умолчанию '{0}'", pass));
            else
                log(string.Format("используется пароль '{0}'", pass));

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
                log(string.Format("архивы не указаны, будут опрошены все"));

            switch (what.ToLower())
            {
                case "all": return All(na, channels, pass, components);
            }

            log(string.Format("неопознаная команда {0}", what));
            return MakeResult(201, what);
        }

        private dynamic All(byte na, List<byte> channels, byte pass, string components)
        {
            #region Паспорт

            dynamic passport = new ExpandoObject();
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel()) return MakeResult(200);
                passport = GetPassport(na, pass);
                if (passport.success) break;
                log(string.Format("паспорт не получен, ошибка: {0}", passport.error));
            }

            if (!passport.success) return MakeResult(101);
            log(string.Format("паспорт получен, версия прошивки регистратора: {0}, заводской номер {1}", passport.version, passport.factoryNumber));
            var version = passport.version;

            #endregion

            #region Текущие

            List<dynamic> currents = null;// new ExpandoObject();
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel()) return MakeResult(200);
                currents = GetCurrent(na, channels, pass);
                if ((currents != null) && (currents.Count > 0)) break;
                log(string.Format("текущие параметры не получены, ошибка: не получен ответ на запрос"));
            }

            foreach (var current in currents)
            {
                if (!current.success) return MakeResult(102);
                log(string.Format("канал {0} - текущие параметры получены, время вычислителя: {1:dd.MM.yy HH:mm:ss}", current.channel, current.date));
                if (getEndDate == null)
                    getEndDate = (type) => current.date;

                records(current.records);
            }

            DateTime currentDate = currents.First().date;
            setTimeDifference(DateTime.Now - currentDate);
            log(string.Format("setTimeDifference: {0}", DateTime.Now - currentDate));

            #endregion

            #region Константы

            ///необходимо заново прочесть константы
            var needRead = false;

            int contractHour = getContractHour();

            if (contractHour == -1) needRead = true;

            if (needRead || components.Contains("Constant"))
            {
                #region
                log("начато чтение констант");
                dynamic constants = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);

                    constants = GetConstant(na, channels, pass, version, currentDate);
                    if (constants.success) break;

                    log(string.Format("константы не были прочитаны, ошибка: {0}", constants.error));
                }
                if (constants.success)
                {
                    log(string.Format("константы были прочитаны, {0} шт", constants.records.Count));
                    records(constants.records);
                }

                dynamic bod = null;
                if ((version >= 850 && version <= 899) ||
                    (version >= 970 && version <= 999) ||
                    (version >= 609 && version <= 629))
                {
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);
                        bod = GetBODFlash7(na, pass, currentDate);
                        if (bod.success) break;
                        log(string.Format("блок общих данных не получен, ошибка: {0}", bod.error));
                    }
                }
                if ((version >= 300 && version <= 399) ||
                    (version >= 400 && version <= 449) ||
                    (version >= 450 && version <= 499))
                {
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);
                        bod = GetBODFlash3(na, pass, currentDate);
                        if (bod.success) break;
                        log(string.Format("блок общих данных не получен, ошибка: {0}", bod.error));
                    }
                }

                if (version >= 950 && version <= 969)
                {
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);
                        bod = GetBOD53(na, pass, currentDate);
                        if (bod.success) break;
                        log(string.Format("блок общих данных не получен, ошибка: {0}", bod.error));
                    }
                }

                if (bod == null)
                {
                    log(string.Format("блок общих данных не получен, версия {0} не поддерживается", version));
                    return MakeResult(103, "блок общих данных не получен");
                }
                if (!bod.success) return MakeResult(103, "блок общих данных не получен");
                contractHour = bod.contractHour;

                setContractHour(contractHour);
                log(string.Format("блок общих данных получен, контрактный час: {0}", bod.contractHour));

                records(bod.records);
                #endregion
            }
            else
            {
                //log(string.Format("константы были прочитаны из локальной БД, контрактный час: {0}", contractHour));
                log(string.Format("контрактный час был прочитан из локальной БД: {0}", contractHour));
            }

            #endregion

            if (components.Contains("Day") || components.Contains("Hour"))
            {
                if ((version >= 610 && version <= 619) ||
                    (version >= 621 && version <= 629) ||
                    (version >= 858 && version <= 899) ||
                    (version >= 900 && version <= 999))
                {
                    #region новые Ирвисы

                    var lastDay = getStartDate("Day");
                    var startDay = lastDay.AddHours(-contractHour).Date;
                    var endDay = getEndDate("Day");

                    log(string.Format("начат опрос суточных в интервале {0:dd.MM.yy} — {1:dd.MM.yy}", startDay, endDay));

                    for (var date = startDay.Date; date < endDay; date = date.AddDays(1))
                    {
                        if (date >= currentDate.AddHours(-contractHour).Date)
                        {
                            log(string.Format("суточные архивы за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }

                        dynamic days = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);

                            days = GetDay09(na, channels, pass, date);
                            if ((days != null) && (days.Count > 0)) break;

                            log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: не получен ответ на запрос", date));
                        }

                        if (days == null)
                        {
                            continue;
                        }

                        foreach (var day in days)
                        {
                            if (!day.success) return MakeResult(104);
                            if (day.isEmpty)
                                log(string.Format("канал {0} - суточная запись {1:dd.MM.yy} отсутствует", day.channel, date));
                            else
                                log(string.Format("канал {0} - суточная запись {1:dd.MM.yy} получена", day.channel, day.date));

                            records(day.records);
                        }
                    }

                    var lastHour = getStartDate("Hour");
                    var startHour = lastHour.Date.AddHours(lastHour.Hour);
                    var endHour = getEndDate("Hour");
                    endHour = endHour.Date.AddHours(endHour.Hour);

                    log(string.Format("начат опрос часовых архивов в интервале {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", startHour, endHour));
                    for (var date = startHour; date < endHour; date = date.AddHours(1))
                    {
                        if (date > currentDate)
                        {
                            log(string.Format("часовые архивы за {0:dd.MM.yyyy HH:mm} еще не сформированы", date));
                            break;
                        }

                        List<dynamic> hours = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);
                            // hour = GetHour09(na, ch, pass, date);
                            hours = GetHour09(na, channels, pass, HourConvert(date, contractHour));

                            //  log(string.Format("читаем часовую запись {0:dd.MM.yy HH:00}, но на вычислитель шлем {1:dd.MM.yy HH:00}", date, HourConvert(date, contractHour)));
                            if ((hours != null) && (hours.Count > 0)) break;
                            log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, не получен ответ на запрос", date));
                        }
                        if (hours == null) continue;

                        foreach (var hour in hours)
                        {
                            if (!hour.success) return MakeResult(105);
                            if (hour.isEmpty)
                                log(string.Format("канал {0} - часовая запись {1:dd.MM.yy HH:mm} отсутствует", hour.channel, date));
                            else
                                log(string.Format("канал {0} - часовая запись {1:dd.MM.yy HH:mm} получена", hour.channel, hour.date));

                            records(hour.records);
                        }
                    }
                    #endregion
                }
                else
                {
                    #region Старые Ирвисы

                    //учалытеплосервис
                    if (version == 412)
                    {
                        log(string.Format("версия вычислителя 412 - полный опрос часов, потом суток"));

                        var startHour = getStartDate("Hour").Date;
                        var endHour = getEndDate("Hour");

                        var archiveHour = startHour.AddHours(contractHour);
                        List<dynamic> hours = new List<dynamic>();

                        foreach (var ch in channels)
                        {
                            log(string.Format("начат опрос часовых {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm} по каналу {2}", startHour, endHour, ch));
                            for (var date = startHour; date < endHour; date = date.AddDays(1))
                            {
                                byte mode = 0; // режим чтения сначала
                                do
                                {
                                    dynamic hour = null;
                                    for (int i = 0; i < TRY_COUNT; i++)
                                    {
                                        if (cancel()) return MakeResult(200);
                                        hour = GetHour01(na, ch, pass, date, mode, version);
                                        if (hour.success)
                                            break;

                                        if (hour.n == 0)
                                        {
                                            log(string.Format("опрос часовых архивов за сутки {0:dd.MM.yy} завершен", date));
                                            break;
                                        }

                                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", archiveHour, hour.error));
                                        mode = 2;   // режим повторного чтения архива
                                    }

                                    if (!hour.success)
                                    {
                                        if (hour.n == 0) break;
                                        else return MakeResult(105);
                                    }

                                    mode = 1; // режим чтения следующего архива
                                    records(hour.records);
                                    hours.AddRange(hour.records);

                                    if (hour.n == 1)
                                    {
                                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} получена", (hour.dates as List<DateTime>).First()));
                                    }
                                    else
                                    {
                                        log(string.Format("часовые записи {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm} получены", (hour.dates as List<DateTime>).Min(), (hour.dates as List<DateTime>).Max()));
                                    }

                                    archiveHour = (hour.dates as List<DateTime>).Last().AddHours(1);

                                } while (true);

                                if (date >= currentDate.AddHours(-contractHour).Date)
                                {
                                    log(string.Format("суточные архивы за {0:dd.MM.yyyy} еще не сформированы", date));
                                    break;
                                }
                            }
                                                        
                            for (var date = startHour; date < endHour; date = date.AddDays(1))
                            {
                                if (cancel()) return MakeResult(200);

                                if (date >= currentDate)
                                {
                                    log(string.Format("данные по НС за {0:dd.MM.yyyy} еще не собраны", date));
                                    break;
                                }

                                var daily = CalcDay(hours, contractHour, date).ToList();

                                byte mode = 0; // режим чтения сначала

                                byte fl_a = 0;
                                UInt16 fl_b = 0;

                                do
                                {
                                    dynamic abnormal = null;
                                    for (int i = 0; i < TRY_COUNT; i++)
                                    {
                                        if (cancel()) return MakeResult(200);

                                        abnormal = GetAbnormalDay(na, ch, pass, mode, date);
                                        if (abnormal.success)
                                        {
                                            // режим чтения следующего архива
                                            mode = 1; break;
                                        }

                                        if (abnormal.n == 0)
                                        {
                                            log(string.Format("завершено чтение архивов суточных НС за {0:dd.MM.yy HH:00}", date)); break;
                                        }

                                        //log(string.Format("записи НС за {1:dd.MM.yy} не получены, причина: {0}", abnormal.error, date));
                                        mode = 2;   // режим повторного чтения архива
                                    }

                                    if (!abnormal.success) break;

                                    foreach (var record in abnormal.records)
                                    {
                                        log(string.Format("на дату {0:dd.MM.yy} получен абнормал {1:dd.MM.yy HH:mm} - fl_a={2:X}h fl_b={3:X}h", date, record.date, record.fl_a, record.fl_b));
                                        fl_a |= record.fl_a;
                                        fl_b |= record.fl_b;
                                    }

                                } while (true);
                                
                                log(string.Format("получены суточные НС {0:dd.MM.yy}, флаги fl_a={1:X}h fl_b={2:X}h", date, fl_a, fl_b));

                                daily.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_a, ch), fl_a, "", date.Date));
                                daily.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_b, ch), fl_b, "", date.Date));

                                records(daily);
                            }
                        }
                    }
                    else
                    {

                        var startHour = getStartDate("Hour").AddHours(-contractHour).Date;
                        var endHour = getEndDate("Hour");

                        var archiveHour = startHour.AddHours(contractHour);
                        List<dynamic> hours = new List<dynamic>();


                        foreach (var ch in channels)
                        {
                            log(string.Format("начат опрос часовых {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm} по каналу {2}", archiveHour, endHour, ch));

                            for (var date = startHour; date < endHour; date = date.AddDays(1))
                            {
                                byte mode = 0; // режим чтения сначала
                                do
                                {
                                    dynamic hour = null;
                                    for (int i = 0; i < TRY_COUNT; i++)
                                    {
                                        if (cancel()) return MakeResult(200);
                                        hour = GetHour01(na, ch, pass, date, mode, version);
                                        if (hour.success)
                                            break;

                                        if (hour.n == 0)
                                        {
                                            log(string.Format("опрос часовых архивов за сутки {0:dd.MM.yy} завершен", date));
                                            break;
                                        }

                                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", archiveHour, hour.error));
                                        mode = 2;   // режим повторного чтения архива
                                    }

                                    if (!hour.success)
                                    {
                                        if (hour.n == 0) break;
                                        else return MakeResult(105);
                                    }

                                    mode = 1; // режим чтения следующего архива
                                    records(hour.records);
                                    hours.AddRange(hour.records);

                                    if (hour.n == 1)
                                    {
                                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} получена", (hour.dates as List<DateTime>).First()));
                                    }
                                    else
                                    {
                                        log(string.Format("часовые записи {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm} получены", (hour.dates as List<DateTime>).Min(), (hour.dates as List<DateTime>).Max()));
                                    }

                                    archiveHour = (hour.dates as List<DateTime>).Last().AddHours(1);

                                } while (true);

                                if (date >= currentDate.AddHours(-contractHour).Date)
                                {
                                    log(string.Format("суточные архивы за {0:dd.MM.yyyy} еще не сформированы", date));
                                    break;
                                }
                            }
                            
                            for (var date = startHour; date < endHour; date = date.AddDays(1))
                            {
                                if (cancel()) return MakeResult(200);

                                if (date >= currentDate)
                                {
                                    log(string.Format("данные по НС за {0:dd.MM.yyyy} еще не собраны", date));
                                    break;
                                }

                                var daily = CalcDay(hours, contractHour, date).ToList();

                                byte mode = 0; // режим чтения сначала

                                byte fl_a = 0;
                                UInt16 fl_b = 0;

                                do
                                {
                                    dynamic abnormal = null;
                                    for (int i = 0; i < TRY_COUNT; i++)
                                    {
                                        if (cancel()) return MakeResult(200);

                                        abnormal = GetAbnormalDay(na, ch, pass, mode, date);
                                        if (abnormal.success)
                                        {
                                            // режим чтения следующего архива
                                            mode = 1; break;
                                        }

                                        if (abnormal.n == 0)
                                        {
                                            log(string.Format("завершено чтение архивов суточных НС за {0:dd.MM.yy HH:00}", date)); break;
                                        }

                                        //log(string.Format("записи НС за {1:dd.MM.yy} не получены, причина: {0}", abnormal.error, date));
                                        mode = 2;   // режим повторного чтения архива
                                    }

                                    if (!abnormal.success) break;

                                    foreach (var record in abnormal.records)
                                    {
                                        log(string.Format("на дату {0:dd.MM.yy} получен абнормал {1:dd.MM.yy HH:mm} - fl_a={2:X}h fl_b={3:X}h", date, record.date, record.fl_a, record.fl_b));
                                        fl_a |= record.fl_a;
                                        fl_b |= record.fl_b;
                                    }

                                } while (true);

                                log(string.Format("получены суточные НС {0:dd.MM.yy}, флаги fl_a={1:X}h fl_b={2:X}h", date, fl_a, fl_b));

                                daily.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_a, ch), fl_a, "", date.Date));
                                daily.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_b, ch), fl_b, "", date.Date));

                                records(daily);
                            }
                        }
                    }
                    #endregion
                }
            }

            #region НС
            if (components.Contains("Abnormal"))
            {

                var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
                var startAbnormal = lastAbnormal.AddHours(-contractHour).Date;

                var endAbnormal = getEndDate("Abnormal");
                byte[] codes = new byte[] { };


                foreach (var ch in channels)
                {
                    log(string.Format("начато чтение архивов НС с {0:dd.MM.yy HH:mm} по каналу {1}", startAbnormal, ch));

                    List<dynamic> abnormals = new List<dynamic>();

                    for (var date = startAbnormal; date < endAbnormal; date = date.AddDays(1))
                    {
                        if (cancel()) return MakeResult(200);

                        if (date >= currentDate)
                        {
                            log(string.Format("данные по НС за {0:dd.MM.yyyy} еще не собраны", date));
                            break;
                        }
                        abnormals.Clear();
                        byte mode = 0; // режим чтения сначала
                        do
                        {
                            dynamic abnormal = null;
                            for (int i = 0; i < TRY_COUNT; i++)
                            {
                                if (cancel()) return MakeResult(200);

                                abnormal = GetAbnormal(na, ch, pass, mode, date, codes);
                                if (abnormal.success)
                                {
                                    // режим чтения следующего архива
                                    mode = 1; break;
                                }

                                if (abnormal.n == 0)
                                {
                                    log(string.Format("завершено чтение архивов НС за {0:dd.MM.yy HH:00}", date)); break;
                                }

                                log(string.Format("записи НС за {1:dd.MM.yy} не получены, причина: {0}", abnormal.error, date));
                                mode = 2;   // режим повторного чтения архива
                            }

                            if (!abnormal.success)
                                if (abnormal.n == 0) break;
                                else return MakeResult(106);

                            codes = abnormal.codes;

                            if (abnormal.records.Count > 0)
                            {
                                abnormals.AddRange(abnormal.records);
                                //var rec = (abnormal.records as IEnumerable<dynamic>).Where(r => r.date > lastAbnormal).ToArray();
                                //if (rec.Length > 0)
                                //{
                                //    log(string.Format("получено {0} записей НС за {1:dd.MM.yy}", rec.Length, date));
                                //    records(rec);
                                //}
                            }

                        } while (true);
                        //if (abnormals.Count > 0)
                        //{
                        log(string.Format("получено {0} записей НС за {1:dd.MM.yy}", abnormals.Count, date));
                        records(abnormals);
                    }
                    //}
                }
            }
            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        private DateTime HourConvert(DateTime hour, int contractHour)
        {
            if (hour.Hour <= contractHour) return hour.AddDays(-1);
            return hour;
        }

        private IEnumerable<dynamic> CalcDay(IEnumerable<dynamic> hours, int contractHour, DateTime day)
        {
            List<dynamic> result = new List<dynamic>();
            DateTime start = day.Date.AddHours(contractHour);
            DateTime end = day.Date.AddDays(1).AddHours(contractHour - 1);
            hours = hours.Where(h => h.date >= start && h.date <= end);
            var count = hours.Where(h => h.s1.StartsWith(Glossary.Qn)).Count();

            if (count == 0)
            {
                log(string.Format("суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm} НЕ расчитана", day, start, end));
                return result;
            }
            if (count < 24)
            {
                log(string.Format("недостаточное количество часовых архивов ({0} из 24) за сутки {1:dd.MM.yy}. Попытка использовать архивы локальной БД", count, day));

                var dates = hours.Select(h => (DateTime)h.date).ToArray();
                var localHour = getRange("Hour", start, end).Where(h => !dates.Contains((DateTime)h.date));
                log(string.Format("из локальной БД прочитано {0} часовых архивов за сутки {1:dd.MM.yy}", localHour.Where(h => h.s1.StartsWith(Glossary.Qn)).Count(), day));
                hours = hours.Union(localHour);
            }
            if (count > 24)
            {
                log(string.Format("количество часовых архивов {0} из 24 за сутки {1:dd.MM.yy}", count, day));
            }

            if (count == 24)
            {
                log(string.Format("количество часовых архивов {0} из 24 за сутки {1:dd.MM.yy}", count, day));
            }

            foreach (var x in hours.GroupBy(g => g.s1))
            {
                if (x.Key.StartsWith(Glossary.Qn) ||
                    x.Key.StartsWith(Glossary.Qw))
                {
                    result.Add(MakeDayRecord(x.Key,
                        x.GroupBy(y => y.date).Select(y => y.Max(z => (double)z.d1)).Sum(y => y),
                        x.First().s2,
                        day));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.Vn) ||
                    x.Key.StartsWith(Glossary.Vw) ||
                    x.Key.StartsWith(Glossary.Twork))
                {
                    result.Add(MakeDayRecord(x.Key, x.Select(y => (double)y.d1).Max(y => y), x.First().s2, day));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.T) ||
                  x.Key.StartsWith(Glossary.P))
                {
                    result.Add(MakeDayRecord(x.Key, x.GroupBy(y => y.date).Select(y => y.Max(z => (double)z.d1)).Average(y => y), x.First().s2, day));
                    continue;
                }
            }

            log(string.Format("рассчитана суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm}", day, start, end));
            return result;
        }

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date.Date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

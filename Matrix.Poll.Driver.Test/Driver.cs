// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
//using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.Poll.Driver.Test
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        private enum DeviceError
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION
        };

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

        private byte[] SendSimple(byte[] data)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var timeout = 7500;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
            {
                Thread.Sleep(sleep);

                var buf = response();
                if (buf.Any())
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0;
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == 6)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
            {
                buffer = SendSimple(data);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 4)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 4 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    //else if (buffer[0] != NetworkAddress)
                    //{
                    //    answer.error = "Несовпадение сетевого адреса";
                    //    answer.errorcode = DeviceError.ADDRESS_ERROR;
                    //}

                    //else if (!Crc.Check(buffer, new Crc16Modbus()))
                    //{
                    //    answer.error = "контрольная сумма кадра не сошлась";
                    //    answer.errorcode = DeviceError.CRC_ERROR;
                    //}
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                    }
                }
            }

            if (answer.success)
            {
                answer.Body = buffer.Skip(1).Take(buffer.Count() - 3).ToArray();
                answer.NetworkAddress = buffer[0];

                //modbus error
                if (buffer.Length == 4)
                {
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                    answer.success = false;
                    switch (buffer[1])
                    {
                        case 0x00:
                            answer.errorcode = DeviceError.NO_ERROR;
                            answer.success = true;
                            answer.error = "все нормально";
                            break;
                        case 0x01:
                            answer.error = "недопустимая команда или параметр";
                            break;
                        case 0x02:
                            answer.error = "внутренняя ошибка счетчика";
                            break;
                        case 0x03:
                            answer.error = "не достаточен уровень доступа для удовлетворения запроса";
                            break;
                        case 0x04:
                            answer.error = "внутренние часы счетчика уже корректировались в течении текущих суток";
                            break;
                        case 0x05:
                            answer.error = "не открыт канал связи";
                            break;
                        default:
                            answer.error = "неизвестная ошибка";
                            break;
                    }
                }
            }

            return answer;
        }


        private dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
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
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeResult(int code, DeviceError errorcode, string description)
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion

        #region ImportExport
        /// <summary>
        /// Регистр выбора стрраницы
        /// </summary>
        private const short RVS = 0x0084;

#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        private Func<byte[]> response;

        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        [Import("cancel")]
        private Func<bool> cancel;

        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;

        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;

        [Import("setContractHour")]
        private Action<int> setContractHour;

        [Import("setContractDay")]
        private Action<int> setContractDay;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            //setArchiveDepth("Day", 2);
            setContractDay(25);

            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            //#region networkAddress
            //if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            //{
            //    log("Отсутствуют сведения о сетевом адресе", level: 1);
            //    return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            //}
            //#endregion

            //#region KTr
            //if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            //{
            //    log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            //}
            //#endregion

            //#region password
            //if (!param.ContainsKey("password"))
            //{
            //    log("Отсутствуют сведения о пароле, принят по-умолчанию");
            //}
            //else
            //{
            //    password = arg.password;
            //}
            //#endregion

#if OLD_DRIVER
            #region debug
            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
            }
            #endregion
#endif

            #region components
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
            #endregion

            #region start
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
            #endregion

            #region end
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
            #endregion

            #region hourRanges
            List<dynamic> hourRanges;
            if (param.ContainsKey("hourRanges") && arg.hourRanges is IEnumerable<dynamic>)
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
            if (param.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges), password);
                        }
                        break;
                    //case "ping":
                    //    {
                    //        result = Wrap(() => Ping(), password);
                    //    }
                    //    break;
                    //case "current":
                    //    {
                    //        result = Wrap(() => Current(), password);
                    //    }
                    //    break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            ////PREPARE
            //var response = ParseTestResponse(Send(MakeTestRequest()));

            //if (!response.success)
            //{
            //    log("ответ не получен: " + response.error, level: 1);
            //    return MakeResult(100, response.errorcode, response.error);
            //}

            //var open = ParseTestResponse(Send(MakeOpenChannelRequest(Level.Slave, password)));
            //if (!open.success)
            //{
            //    log("не удалось открыть канал связи (возможно пароль не верный): " + open.error, level: 1);
            //    return MakeResult(100, open.errorcode, open.error);
            //}

            //log("канал связи открыт");

            //ACTION
            return func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion


        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            var currentDate = DateTime.Now;

            //var curDate = ParseReadCurrentDateResponse(Send(MakeReadCurrentDateRequest()), info.Version);
            //if (!curDate.success)
            //{
            //    log(string.Format("Ошибка при считывании текущей даты на вычислителе: {0}", curDate.error), level: 1);
            //    return MakeResult(102, curDate.errorcode, curDate.error);
            //}

            //date = curDate.Date;
            setTimeDifference(DateTime.Now - currentDate);
            setArchiveDepth("Hour", 9999);

            log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", currentDate));

            if (getEndDate == null)
            {
                getEndDate = (type) => currentDate;
            }

            if (components.Contains("Constant"))
            {
                var constants = new List<dynamic>();
                constants.Add(MakeConstRecord("Тест константы", currentDate.Month.ToString(), currentDate));
                log(string.Format("Константы прочитаны: всего {0}", constants.Count));
                records(constants);
            }

            if (components.Contains("Current"))
            {
                var currents = new List<dynamic>();

                //    var current = GetCurrents(properties, date);
                //    if (!current.success)
                //    {
                //        log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
                //        return MakeResult(102, current.errorcode, current.error);
                //    }

                //    currents = current.records;

                currents.Add(MakeCurrentRecord("Текущий час", currentDate.Hour, "ч", currentDate));
                currents.Add(MakeCurrentRecord("Текущая минута", currentDate.Minute, "мин", currentDate));
                currents.Add(MakeCurrentRecord("Текущая секунда", currentDate.Second, "сек", currentDate));
                currents.Add(MakeCurrentRecord("Текущий день", currentDate.Day, "", currentDate));
                currents.Add(MakeCurrentRecord("Текущий месяц", currentDate.Month, "", currentDate));
                currents.Add(MakeCurrentRecord("Текущий год", currentDate.Year, "год", currentDate));
                currents.Add(MakeCurrentRecord("UNIX-секунд прошло", (currentDate - new DateTime(1970, 1, 1)).TotalSeconds, "сек", currentDate));

                log(string.Format("Текущие на {0} прочитаны: всего {1}", currentDate, currents.Count), level: 1);
                records(currents);
            }

            if (components.Contains("Hour"))
            {
                List<dynamic> hours = new List<dynamic>();
                if (hourRanges != null)
                {
                    foreach (var range in hourRanges)
                    {
                        var startH = range.start;
                        var endH = range.end;

                        //if (startH > currentDate) continue;
                        //if (endH > currentDate) endH = currentDate;

                        //            var hour = GetHours(startH, endH, date, properties);
                        //            if (!hour.success)
                        //            {
                        //                log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        //                return MakeResult(105, hour.errorcode, hour.error);
                        //            }
                        //            hours = hour.records;


                        var date = startH.Date.AddHours(startH.Hour);

                        while (date <= endH)
                        {
                            var hour = new List<dynamic>();

                            if (cancel())
                            {
                                log("Ошибка при считывании часовых: опрос отменен", level: 1);
                                return MakeResult(105, DeviceError.NO_ERROR, "опрос отменен");
                            }

                            //if (DateTime.Compare(date.AddHours(1), currentDate) > 0)
                            //{
                            //    log(string.Format("Часовой записи за {0:dd.MM.yyyy HH:00} ещё нет", date));
                            //    break;
                            //}

                            hour.Add(MakeHourRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));
                            hour.Add(MakeHourRecord("Big string", (date - new DateTime(1970, 1, 1)).TotalSeconds,
                                "012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789", date));

                            hours.AddRange(hour);
                            log(string.Format("Прочитана часовая запись за {0:dd.MM.yyyy HH:00}", date, hour.Count));
                            records(hour);
                            date = date.AddHours(1);
                        }

                        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                    }
                }
                else
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");

                    //        var hour = GetHours(startH, endH, date, properties);
                    //        if (!hour.success)
                    //        {
                    //            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                    //            return MakeResult(105, hour.errorcode, hour.error);
                    //        }
                    //        hours = hour.records;

                    var date = startH.Date.AddHours(startH.Hour);

                    while (date <= endH)
                    {
                        var hour = new List<dynamic>();

                        if (cancel())
                        {
                            log("Ошибка при считывании часовых: опрос отменен", level: 1);
                            return MakeResult(105, DeviceError.NO_ERROR, "опрос отменен");
                        }

                        if (DateTime.Compare(date.AddHours(1), currentDate) > 0)
                        {
                            log(string.Format("Часовой записи за {0:dd.MM.yyyy HH:00} ещё нет", date));
                            break;
                        }

                        hour.Add(MakeHourRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));

                        hours.AddRange(hour);
                        log(string.Format("Прочитана часовая запись за {0:dd.MM.yyyy HH:00}", date, hour.Count));
                        records(hour);
                        date = date.AddHours(1);
                    }

                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                }
            }

            if (components.Contains("Day"))
            {
                List<dynamic> days = new List<dynamic>();
                if (dayRanges != null)
                {
                    foreach (var range in dayRanges)
                    {
                        var startD = range.start;
                        var endD = range.end;

                        if (startD > currentDate) continue;
                        if (endD > currentDate) endD = currentDate;

                        //            var day = GetDays(startD, endD, date, properties, info.TotalDay);
                        //            if (!day.success)
                        //            {
                        //                log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        //                return MakeResult(104, day.errorcode, day.error);
                        //            }
                        //            days = day.records;

                        var date = startD.Date;

                        while (date <= endD)
                        {
                            var day = new List<dynamic>();

                            if (cancel())
                            {
                                log("Ошибка при считывании суточных: опрос отменен", level: 1);
                                return MakeResult(105, DeviceError.NO_ERROR, "опрос отменен");
                            }

                            if (DateTime.Compare(date, currentDate) > 0)
                            {
                                log(string.Format("Суточных данных за {0:dd.MM.yyyy} ещё нет", date));
                                break;
                            }

                            day.Add(MakeDayRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));

                            days.AddRange(day);
                            log(string.Format("Прочитана суточная запись за {0:dd.MM.yyyy}", date, day.Count));
                            records(day);
                            date = date.AddDays(1);
                        }


                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                else
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    //        var day = GetDays(startD, endD, date, properties, info.TotalDay);
                    //        if (!day.success)
                    //        {
                    //            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                    //            return MakeResult(104, day.errorcode, day.error);
                    //        }
                    //        days = day.records;


                    var date = startD.Date;

                    while (date <= endD)
                    {
                        var day = new List<dynamic>();

                        if (cancel())
                        {
                            log("Ошибка при считывании суточных: опрос отменен", level: 1);
                            return MakeResult(105, DeviceError.NO_ERROR, "опрос отменен");
                        }

                        if (DateTime.Compare(date, currentDate) > 0)
                        {
                            log(string.Format("Суточных данных за {0:dd.MM.yyyy} ещё нет", date));
                            break;
                        }

                        day.Add(MakeDayRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));

                        days.AddRange(day);
                        log(string.Format("Прочитана суточная запись за {0:dd.MM.yyyy}", date, day.Count));
                        records(day);
                        date = date.AddDays(1);
                    }


                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }



                /// Нештатные ситуации ///
                if (components.Contains("Abnormal"))
                {
                    var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
                    var startAbnormal = lastAbnormal.Date;

                    var endAbnormal = getEndDate("Abnormal");
                    byte[] codes = new byte[] { };

                    List<dynamic> abnormals = new List<dynamic>();

                    var fakeStart = currentDate.Date.AddDays(-1).AddHours(15).AddMinutes(38).AddSeconds(0);
                    //var fakeEnd = now.Date.AddDays(-1).AddHours(23).AddMinutes(0).AddSeconds(14);
                    //var fakeStartOld = now.Date.AddDays(-3).AddHours(15).AddMinutes(17).AddSeconds(19);
                    //var fakeEndOld = now.Date.AddDays(-2).AddHours(1).AddMinutes(2).AddSeconds(3);

                    //var fakeStart = new DateTime(2016, 10, 26, 0, 1, 15);
                    //var fakeEnd = new DateTime(2016, 10, 26, 10, 20, 1);

                    //if ((endAbnormal >= fakeStart) && (fakeStart >= startAbnormal))
                    {
                        abnormals.Add(MakeAbnormalRecord("Критическая ситуация: начало", 0, fakeStart, 1000));
                        abnormals.Add(MakeAbnormalRecord("Некритичное событие 1", 0, fakeStart.AddSeconds(1), 1));
                        abnormals.Add(MakeAbnormalRecord("Некритичное событие 2", 0, fakeStart.AddSeconds(2), 2));
                    }
                    /*
                    //if ((endAbnormal >= fakeEnd) && (fakeEnd >= startAbnormal))
                    {
                        abnormals.Add(MakeAbnormalRecord("Критическая ситуация: окончание", 0, fakeEnd));
                    }

                    //if ((endAbnormal >= fakeStart) && (fakeStart >= startAbnormal))
                    {
                        abnormals.Add(MakeAbnormalRecord("Критическая ситуация: начало", 0, fakeStartOld));
                    }

                    //if ((endAbnormal >= fakeEnd) && (fakeEnd >= startAbnormal))
                    {
                        abnormals.Add(MakeAbnormalRecord("Критическая ситуация: окончание", 0, fakeEndOld));
                    }*/

                    log(string.Format("получено {0} записей НС за период", abnormals.Count));//{1:dd.MM.yy}, date));
                    records(abnormals);

                    /*
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
                    }*/
                }
            }
            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV034
{
    /// <summary>
    /// драйвер для теплосчетчиков ТСРВ024
    /// счетчики имеют три теплосистемы, по 4 трубы в каждой
    /// нумерация каналов сквозная (тс1-1,2,3,4; тс2-5,6,7,8; тс3-9,10,11,12)
    /// </summary>
    public partial class Driver
    {
        byte NetworkAddress = 1;
        bool debugMode = false;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common

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

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
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

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic result = new ExpandoObject();
            result.code = code;
            result.success = code == 0 ? true : false;
            result.description = description;
            return result;
        }
        #endregion

        #region Do
        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            if (!param.ContainsKey("networkAddress") || !byte.TryParse(param["networkAddress"].ToString(), out NetworkAddress))
            {
                log(string.Format("отсутствуют сведения о сетевом адресе, принят по-умолчанию {0}", NetworkAddress));
            }

            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    //case "all":
                    //    {
                    //        var password = (string)arg.password;
                    //        Wrap(() => All(), password);
                    //    }
                    //    break;
                    case "all":
                        {
                            result = Wrap(() => All(components));
                        }
                        break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "current": Current(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description);
                            result = MakeResult(201, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> act)
        {
            ////PREPARE
            //var response = ParseTestResponse(Send(MakeTestRequest()));

            //if (!response.success)
            //{
            //    log("ответ не получен: " + response.error);
            //    return;
            //}

            //var open = ParseTestResponse(Send(MakeOpenChannelRequest(Level.Slave, password)));
            //if (!open.success)
            //{
            //    log("не удалось открыть канал связи (возможно пароль не верный): " + open.error);
            //    return;
            //}

            //log("канал связи открыт");

            //ACTION
            return act();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion

        /// <summary>
        ///Задание формулы расчёта W1, W2, W3:
        ///0: W = 0
        ///1: W = m[m1]*h[h1]
        ///2: W = m[m1]*h[h1]-m[m2]*h[h2]
        ///3: W = m[m1]*(h[h1]-h[h2])
        ///4: W = (m[m1]-m[m2])*h[h1]
        /// </summary>
        /// <returns></returns>
        private string GetFuncW(byte n)
        {
            switch(n)
            {
                case 0:
                    return "W = 0";
                case 1:
                    return "W = m1*h1";
                case 2:
                    return "W = m1*h1-m2*h2";
                case 3:
                    return "W = m1*(h1-h2)";
                case 4:
                    return "W = (m1-m2)*h1";
            }
            return "";
        }

        #region Интерфейс

        private dynamic All(string components)
        {
            var ping = ParseResponse17(Send(MakeRequest(17)));
            if (!ping.success)
            {
                log(string.Format("Не удалось прочесть версию прибора: {0}", ping.error));
                return MakeResult(101, "не удалось прочесть версию прибора");
            }

            log(string.Format("Версия прибора = {0}", ping.Version));

            //vzljot 82.01.91.11 => ИВК-102

            dynamic consumptionProperties = new ExpandoObject();

            if (!ping.isIvk)
            {
                if (components.Contains("Constant"))
                {
                    var now = DateTime.Now;
                    var constants = new List<dynamic>();
                    //рег. хран. целое 1байт
                    //400028 Формула вычислений для W1, б/р
                    //400029 Формула вычислений для W2, б/р
                    //400065 Формула вычислений для W3, б/р
                    var funcW12 = ParseResponseRegisterAsByte(Send(MakeRegisterRequest(400028, 2)));
                    if (!funcW12.success || funcW12.Body.Length != 4) throw new Exception(funcW12.error + " при чтении параметров \"Формула вычислений для W1,W2\"");

                    var funcW3 = ParseResponseRegisterAsByte(Send(MakeRegisterRequest(400065, 1)));
                    if (!funcW3.success || funcW3.Body.Length != 2) throw new Exception(funcW3.error + " при чтении параметров \"Формула вычислений для W3\"");

                    constants.Add(MakeConstRecord("Формула вычислений для W1", GetFuncW(funcW12.Body[0]), now));
                    constants.Add(MakeConstRecord("Формула вычислений для W2", GetFuncW(funcW12.Body[2]), now));
                    constants.Add(MakeConstRecord("Формула вычислений для W3", GetFuncW(funcW3.Body[0]), now));

                    //Регистры ввода типа целое значение 4 байта
                    //432769 Заводской номер ТВ, б/р
                    //432771 Код объекта, б/р

                    records(constants);
                    log(string.Format("Прочитаны константы: {0} записей", constants.Count));
                }


                if (components.Contains("Hour") || components.Contains("Day"))
                {
                    //читаем регистры и узнаем, а что же сохраняется: масса или объем? по умолчанию - масса
                    consumptionProperties.IsMassByChannel1 = true;
                    consumptionProperties.IsMassByChannel2 = true;
                    consumptionProperties.IsMassByChannel3 = true;

                    //с версии 63.01.03.07, до этого - только масса(!)
                    try
                    {
                        log("чтение настроек");

                        var sets = ParseResponseRegisterAsByte(Send(MakeRegisterRequest(400084, 3)));
                        if (!sets.success || sets.Body.Length != 6) throw new Exception(sets.error + " при чтении параметров \"Расход в архиве\"");

                        consumptionProperties.IsMassByChannel1 = (sets.Body[0] == 0);
                        consumptionProperties.IsMassByChannel2 = (sets.Body[2] == 0);
                        consumptionProperties.IsMassByChannel3 = (sets.Body[4] == 0);
                    }
                    catch (Exception ex)
                    {
                        log(string.Format("не удалось определить что сохраняется как расход, по умолчанию - масса, причина: {0}", ex.Message));
                        return MakeResult(101, "не удалось прочитать настройки");
                    }
                }
            }


            ////приборное время
            //var time = ParseResponseTime(Send(MakeRequestTime()));
            //if (!time.success)
            //{
            //    return MakeResult(101, time.error);
            //}


            //log(string.Format("Время на приборе: {0:dd.MM.yyyy HH:mm}", date));

            //var dtbytes = ParseResponseRegisterAsByte(Send(MakeRegisterRequest(432785, 1)));
            //if(!dtbytes.success)
            //{
            //    log(string.Format("Ошибка при считывании текущего времени: {0}", dtbytes.error));
            //    return MakeResult(102, dtbytes.error);
            //}

            var date = DateTime.Now.AddHours(3); //new DateTime(1970, 1, 1).AddSeconds(Helper.ToUInt32(dtbytes.Body, 0));

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            //if (components.Contains("Current"))
            //{
            //    var current = GetCurrent(date);
            //    if (!current.success)
            //    {
            //        log(string.Format("Ошибка при считывании текущих: {0}", current.error));
            //        return MakeResult(102, current.error);
            //    }

            //    records(current.records);
            //    List<dynamic> currents = current.records;
            //    log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count));

            //}


            ////чтение часовых
            if (components.Contains("Hour"))
            {
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");
                var hours = new List<dynamic>();

                log(string.Format("Запрос часовых с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}", startH, endH, hours.Count));

                var hour = GetHours(startH, endH, consumptionProperties, ping.isIvk);
                if (!hour.success)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", hour.error));
                    return MakeResult(105, hour.error);
                }
                else
                {
                    hours = hour.records;
                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count));
                }
            }

            ////чтение суточных
            if (components.Contains("Day"))
            {
                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                var day = GetDays(startD, endD, consumptionProperties, ping.isIvk);
                if (!day.success)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", day.error));
                    return MakeResult(104, day.error);
                }
                List<dynamic> days = day.records;
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count));
            }

            ///// Нештатные ситуации ///
            //var lastAbnormal = getLastTime("Abnormal");
            //DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
            //DateTime endAbnormal = current.date;


            //var resp = ParseResponse17(Send(MakeRequest17()));
            //if (resp.success == false)
            //{
            //    var strerr = string.Format("Попытка пинга завершилась неудачей: {0}", resp.error);
            //    log(strerr);
            //    return MakeResult(101, strerr);
            //}

            //log(string.Format("Проверка связи завершилась успешно: версия={0}", resp.Version));
            return MakeResult(0);
        }

        #endregion

    }
}
//        private static readonly ILog Log = LogManager.GetLogger(typeof(Driver));

//        public static byte ByteLow(int getLow)
//        {
//            return (byte)(getLow & 0xFF);
//        }
//        public static byte ByteHigh(int getHigh)
//        {
//            return (byte)((getHigh >> 8) & 0xFF);
//        }

//        public override SurveyResult Ping()
//        {
//            try
//            {
//                var req = new Request17(NetworkAddress);
//                var resp = new Response17(SendMessageToDevice(req));
//                if (resp == null) return new SurveyResult { State = SurveyResultState.NoResponse };
//                OnSendMessage(resp.ToString());
//                return new SurveyResult { State = SurveyResultState.Success };
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }
//            return new SurveyResult { State = SurveyResultState.NotRecognized };
//        }

//        public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
//        {
//            var data = new List<Data>();
//            try
//            {
//                //каналы для суточного архива
//                Dictionary<int, ArchiveType> channels = new Dictionary<int, ArchiveType>
//                {
//                    {1,ArchiveType.DailySystem1},
//                    {2,ArchiveType.DailySystem2},
//                    {3,ArchiveType.DailySystem3},
//                };

//                byte[] bytes = null;

//                foreach (var channel in channels)
//                {
//                    foreach (var date in dates)
//                    {
//                        try
//                        {
//                            OnSendMessage(string.Format("чтение суточных данных за {0:dd.MM.yyyy} по теплосистеме {1}", date, channel.Key));

//                            bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, channel.Value));
//                            var dataResponse = new Response65(bytes, channel.Key);
//                            foreach (var d in dataResponse.Data)
//                            {
//                                //убираем лишние 23:59:59
//                                d.Date = d.Date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
//                                data.Add(d);
//                            }
//                        }
//                        catch (Exception ex1)
//                        {
//                            OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} по ТС {1} будет пропущена", date, channel.Key));
//                        }
//                    }
//                }

//                foreach (var date in dates)
//                {
//                    try
//                    {
//                        OnSendMessage(string.Format("чтение суточных данных нарастающим итогом за {0:dd.MM.yyyy} ", date));
//                        bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.DailyGrowing));
//                        var responseTotals = new Response65Totals(bytes);
//                        foreach (var d in responseTotals.Data)
//                        {
//                            //убираем лишние 23:59:59
//                            d.Date = d.Date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
//                            data.Add(d);
//                        }
//                    }
//                    catch (Exception ex2)
//                    {
//                        OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} нарастающим итогом", date));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//                return new SurveyResultData { Records = data, State = SurveyResultState.PartialyRead };
//            }
//            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
//        }

//        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
//        {
//            var data = new List<Data>();
//            try
//            {
//                //каналы для часового архива
//                Dictionary<int, ArchiveType> channels = new Dictionary<int, ArchiveType>
//                {
//                    {1,ArchiveType.HourlySystem1},
//                    {2,ArchiveType.HourlySystem2},
//                    {3,ArchiveType.HourlySystem3},
//                };

//                foreach (var channel in channels)
//                {
//                    foreach (var date in dates)
//                    {
//                        OnSendMessage(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm} по теплосистеме {1}", date, channel.Key));

//                        var bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, channel.Value));
//                        var dataResponse = new Response65(bytes, channel.Key);
//                        //Response65.Channel = channel.Key;
//                        //var dataResponse = SendMessageToDevice<Response65>(new Request65ByDate(NetworkAddress, date, channel.Value));
//                        foreach (var d in dataResponse.Data)
//                        {
//                            //убираем лишние 59:59
//                            d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
//                            data.Add(d);
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//                return new SurveyResultData { Records = data, State = SurveyResultState.PartialyRead };
//            }
//            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
//        }

//        /// <summary>
//        /// читает регистр текущих показаний
//        /// </summary>
//        /// <param name="register"></param>
//        /// <param name="name"></param>
//        /// <param name="measuringUnit"></param>
//        /// <param name="channel"></param>
//        /// <param name="calculationType"></param>
//        /// <param name="date"></param>
//        /// <returns></returns>
//        private Data ReadCurrent(int register, string name, MeasuringUnitType measuringUnit, int channel, CalculationType calculationType, DateTime date)
//        {
//            Data data = null;
//            try
//            {
//                var result = new ResponseFloat(SendMessageToDevice(new Request4(NetworkAddress, register, 2)));
//                data = new Data(name, measuringUnit, date, result.Values.ElementAt(0), calculationType, channel);
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("не удалось прочитать регистр 0x{0:X}", register));
//            }
//            return data;
//        }

//        public override SurveyResultData ReadCurrentValues()
//        {
//            var data = new List<Data>();
//            try
//            {
//                OnSendMessage(string.Format("чтение мгновенных данных"));

//                var dateResponse = new ResponseDateTime(SendMessageToDevice(new Request4(NetworkAddress, 0x8000, 2)));

//                Data current = null;
//                current = ReadCurrent(0xC6AC, "Eтс(0)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6AE, "Eгв(0)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6B0, "Gтс(0)", MeasuringUnitType.tonn_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6B2, "Eтс(1)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6B4, "Eгв(1)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6B6, "Gтс(1)", MeasuringUnitType.tonn_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6B8, "Eтс(2)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6BA, "Eгв(2)", MeasuringUnitType.Gkal_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC6BC, "Gтс(2)", MeasuringUnitType.tonn_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);


//                current = ReadCurrent(0xC048, "t(0)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC04A, "t(1)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC04C, "t(2)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC04E, "t(3)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC050, "t(4)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC052, "t(5)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC054, "t(6)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC056, "t(7)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC058, "t(8)", MeasuringUnitType.C, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC05A, "Q(0)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC05C, "Q(1)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC05E, "Q(2)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC060, "Q(3)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC062, "Q(4)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC064, "Q(5)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC066, "Q(6)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC068, "Q(7)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC06A, "Q(8)", MeasuringUnitType.m3_h, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                current = ReadCurrent(0xC03C, "P(0)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC03E, "P(1)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC040, "P(2)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC042, "P(3)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC044, "P(4)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//                current = ReadCurrent(0xC046, "P(5)", MeasuringUnitType.MPa, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);

//                OnSendMessage("попытка прочиталь Mтс(1)");
//                current = ReadCurrent(0xC0C6, "Mтс(1)", MeasuringUnitType.tonn, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null)
//                {
//                    OnSendMessage("удалось прочиталь Mтс(1)");
//                    data.Add(current);
//                }
//                else
//                {
//                    OnSendMessage("НЕ удалось прочиталь Mтс(1)");
//                }

//                current = ReadCurrent(0xC0DE, "Mтс(2)", MeasuringUnitType.tonn, 1, CalculationType.Average, dateResponse.Date);
//                if (current != null) data.Add(current);
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
//        }

//        /// <summary>
//        /// отправка сообщения прибору
//        /// </summary>		
//        /// <param name="request">запрос</param>		
//        /// <returns>ответ</returns>	
//        private byte[] SendMessageToDevice(Request request)
//        {
//            byte[] response = null;

//            bool success = false;
//            int attemtingCount = 0;

//            while (!success && attemtingCount < 5)
//            {
//                attemtingCount++;
//                isDataReceived = false;
//                receivedBuffer = null;
//                var bytes = request.GetBytes();
//                RaiseDataSended(bytes);
//                Wait(7000);
//                if (isDataReceived)
//                {
//                    response = receivedBuffer;
//                    success = true;
//                }
//            }
//            return response;
//        }
//    }
//}
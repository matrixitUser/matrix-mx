using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV024
{
    /// <summary>
    /// драйвер для теплосчетчиков ТСРВ024
    /// счетчики имеют три теплосистемы, по 4 трубы в каждой
    /// нумерация каналов сквозная (тс1-1,2,3,4; тс2-5,6,7,8; тс3-9,10,11,12)
    /// </summary>
    public partial class Driver
    {
        byte NetworkAddress = 0;
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        public enum DeviceError
        {
            NO_ERROR = 0, //нет ошибки вычислителя, хотя может быть логическая ошибка (неизвестная команда ping вместо all)
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION,
            UNEXPECTED_RESPONSE
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


        public dynamic MakeResult(int code, DeviceError errorcode = DeviceError.NO_ERROR, string description = "")
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

            result.success = code == 0 ? true : false;
            result.description = description;

            return result;
        }
        #endregion

        #region Do
        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            double KTr = 1.0;

            var param = (IDictionary<string, object>)arg;
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
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
                            log(description, 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
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

        #region Интерфейс

        private dynamic All(string components)
        {
            //приборное время
            var time = ParseResponseTime(Send(MakeRequestTime()));
            if (!time.success)
            {
                log(string.Format("Ошибка получения времени: {0}", time.error), 1);
                return MakeResult(101, time.errorcode, time.error);
            }
            DateTime date = time.date;

            log(string.Format("Время на приборе: {0:dd.MM.yyyy HH:mm}", date));

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            dynamic contract = ParseResponseUInt16(Send(MakeRequestRegister(0x0272, 2, func: 3)));
            if(!contract.success)
            {
                log(string.Format("Ошибка получения контрактного дня: {0}", contract.error), 1);
                return MakeResult(101, contract.errorcode, contract.error);
            }

            int contractHour = contract.values[0];
            int contractDay = contract.values[1];
            log($"Контрактный час={contractHour} контрактный день={contractDay}", 1);

            if (components.Contains("Signal"))
            {
                var current = GetCurrentEvent();
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих для события: {0}", current.error));
                    return MakeResult(102, current.errorcode, current.error);
                }
                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Текущие для события на {0} прочитаны: всего {1}", current.date, currents.Count), 1);
            }

            if (components.Contains("Current"))
            {
                var current = GetCurrent(date);
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error));
                    return MakeResult(102, current.errorcode, current.error);
                }

                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count), 1);
            }


            ////чтение часовых
            if (components.Contains("Hour"))
            {
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");
                var hours = new List<dynamic>();

                var hour = GetHours(startH, endH, date);
                if (!hour.success)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", hour.error));
                }
                else
                {
                    hours = hour.records;
                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), 1);
                }
            }

            ////чтение суточных
            if (components.Contains("Day"))
            {
                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                var day = GetDays(startD, endD, date);
                if (!day.success)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", day.error));
                    return MakeResult(104, day.errorcode, day.error);
                }
                List<dynamic> days = day.records;
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), 1);
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
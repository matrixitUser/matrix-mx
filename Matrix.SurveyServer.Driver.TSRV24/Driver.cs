using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Timers;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.TSRV24
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
        #endregion

        #region Do
        [Export("do")]
        public void Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;
            if (!param.ContainsKey("networkAddress"))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return;
            }

            NetworkAddress = (byte)arg.networkAddress;

            if (!param.ContainsKey("KTr"))
            {
                arg.KTr = 1;
                log("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию 1");
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
                    case "ping":
                        {
                            Wrap(() => Ping());
                        }
                        break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "current": Current(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
                    default: log(string.Format("неопознаная команда {0}", what)); break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
            }
        }

        private void Wrap(Action act)
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

            log("канал связи открыт");

            //ACTION
            act();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion

        #region Интерфейс

        private void Ping()
        {
            var resp = ParseResponse17(Send(MakeRequest17()));
            if (resp.success == false)
            {
                log(string.Format("Попытка пинга завершилась неудачей: {0}", resp.error));
                return;
            }

            log(string.Format("Проверка связи завершилась успешно: версия={0}", resp.Version));
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
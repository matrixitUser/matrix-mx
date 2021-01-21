// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Dynamic;
using System.Threading;
using System.ComponentModel.Composition;
using Matrix.SurveyServer.Driver.Common.Crc;
//using System.Timers;


namespace Matrix.SurveyServer.Driver.Mercury230
{
    /// <summary>
    /// Драйвер для электросчетчика Меркурий 230
    /// 26.01.2017 добавлен DeviceError
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        byte NetworkAddress = 0;
        int firstStart = 0;
        bool gate228 = false;

        private const int TIMEZONE_SERVER = +5;
        private const int TIMEZONE_DEFAULT = +3;

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
            DEVICE_EXCEPTION,
            CHANNEL_CLOSED,
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

            var timeout = 27500;
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
                        if (waitCollected == 2)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, int attempts_total = 8)   //4
        {
            if (gate228)
            {
                dynamic answer = new ExpandoObject();
                answer.success = false;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;

                byte[] buffer = null;
                if(firstStart == 0)
                {
                    byte[] extraBytes1 = new byte[] { 0xDE, 0x65, 0x71, 0x01, 0x00, 0x00, 0x80, 0x7F, 0x20, 0x3F };
                    byte[] extraBytes2 = new byte[] { 0x27, 0xB7, 0xFC, 0x01, 0x00, 0x04, 0x00, 0x00, 0x01, 0x16, 0x22, 0x04, 0x3C };

                    SendSimple(extraBytes1);
                    SendSimple(extraBytes2);

                    firstStart++;
                }
                

                List<byte> dataGate228 = new List<byte>();
                dataGate228.Add(01);
                dataGate228.Add(00);
                if (data.Length > 255) dataGate228.AddRange(BitConverter.GetBytes(data.Length));
                else
                {
                    dataGate228.Add((byte)(data.Length));
                    //dataGate228.Add(4);
                    dataGate228.Add(0);
                }
                // dataGate228.Add(0);
                dataGate228.Add(1);
                UInt32 crc24 = Crc24.Compute(dataGate228.ToArray());

                dataGate228.InsertRange(0, BitConverter.GetBytes(crc24).Take(3));
                byte csLast = (byte)(data.Sum(d => d) - 1);
                dataGate228.AddRange(data);
                dataGate228.Add(csLast);

                for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
                {
                    buffer = SendSimple(dataGate228.ToArray());
                    buffer = buffer.Skip(8).Take(buffer[5]).ToArray();

                    log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

                    if (buffer.Length == 0)
                    {
                        answer.error = "Нет ответа";
                        answer.errorcode = DeviceError.NO_ANSWER;
                    }
                    else
                    {
                        buffer = buffer.SkipWhile(b => b == 0xff).ToArray();
                        if (buffer.Length < 4)
                        {
                            answer.error = "в кадре ответа не может содежаться менее 4 байт";
                            answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                        }
                        else 
                        {
                            UInt32 crc24forcheck = BitConverter.ToUInt32(new byte[] { buffer[0], buffer[1], buffer[2], 0x00 }, 0);
                            byte cs = (byte)(buffer.Skip(8).Take(buffer.Length - 9).Sum(b => b) - 1);
                            //if (crc24forcheck != Crc24.Compute(buffer, 3, 5))
                            //{
                            //    answer.error = "контрольная сумма заголовка не сошлась";
                            //    answer.errorcode = DeviceError.CRC_ERROR;
                            //}
                            //else if (cs != buffer[buffer.Length - 1])
                            //{
                            //    answer.error = "контрольная сумма данных не сошлась";
                            //    answer.errorcode = DeviceError.CRC_ERROR;
                            //}
                            answer.success = true;
                            answer.error = string.Empty;
                            answer.errorcode = DeviceError.NO_ERROR;
                            //else
                            //{
                                
                            //}
                        }
                    }
                }

                if (answer.success)
                {
                    answer.Body = buffer.Skip(1).ToArray();
                    //answer.Payload = buffer.Skip(8).Take(buffer.Length - 9).ToArray();

                    //modbus error
                    if (buffer.Length == 4)
                    {
                        answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                        answer.exceptioncode = buffer[1];
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
            else
            {
                dynamic answer = new ExpandoObject();
                answer.success = false;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;

                byte[] buffer = null;

                for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
                {
                    buffer = SendSimple(data);
                    if (buffer.Length == 0)
                    {
                        answer.error = "Нет ответа";
                        answer.errorcode = DeviceError.NO_ANSWER;
                    }
                    else
                    {
                        buffer = buffer.SkipWhile(b => b == 0xff).ToArray();
                        if (buffer.Length < 4)
                        {
                            answer.error = "в кадре ответа не может содежаться менее 4 байт";
                            answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                        }
                        else if (buffer[0] != NetworkAddress)
                        {
                            answer.error = "Несовпадение сетевого адреса";
                            answer.errorcode = DeviceError.ADDRESS_ERROR;
                        }
                        else if (buffer.Length == 4 && buffer[1] == 0x05)
                        {
                            answer.error = "Закрыт канал связи";
                            answer.errorcode = DeviceError.CHANNEL_CLOSED;
                        }
                        else if (!Crc.Check(buffer, new Crc16Modbus()))
                        {
                            answer.error = "контрольная сумма кадра не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;

                            for (int i = 0; i < buffer.Length - 1; i++)
                            {
                                if ((buffer[i] == 0x0d) && (buffer[i + 1] == 0x0a))
                                {
                                    for (int j = i + 2; j < buffer.Length - 1; j++)
                                    {
                                        if ((buffer[j] == 0x0d) && (buffer[j + 1] == 0x0a))
                                        {
                                            //var tmp = buffer.Take(i);
                                            //buffer = null;
                                            //buffer = tmp.ToArray();
                                            buffer = buffer.Take(i).ToArray();
                                            if (Crc.Check(buffer, new Crc16Modbus()))
                                            {
                                                answer.success = true;
                                                answer.error = string.Empty;
                                                answer.errorcode = DeviceError.NO_ERROR;

                                            }
                                        }
                                    }
                                }
                            }
                        }
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
                        answer.exceptioncode = buffer[1];
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

        private dynamic MakeAbnormalRecord(string name, string message, DateTime date, int output)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.s1 = name;
            record.d1 = output;
            record.s2 = message;
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
            result.error = description;
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

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            setArchiveDepth("Day", 2);

            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion

            #region gate228
            UInt32 gate228On = 0;
            if (!param.ContainsKey("gate228") || !UInt32.TryParse(arg.gate228.ToString(), out gate228On))
            {
            }
            else
            {

                if (gate228On == 1) gate228 = true;
                else gate228 = false;
            }
            #endregion

            #region KTr
            if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            {
                log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            }
            #endregion

            #region password
            if (!param.ContainsKey("password"))
            {
                log("Отсутствуют сведения о пароле, принят по-умолчанию");
            }
            else
            {
                password = arg.password;
            }
            #endregion

           

            #region contractHour - in TubeNode
            //int contractHour = 0;
            //if (param.ContainsKey("contractHour") && int.TryParse(arg.contractHour.ToString(), out contractHour))
            //{
            //    if(contractHour >= 0 && contractHour <= 23)
            //    {
            //        setContractHour(contractHour);
            //    }
            //}
            #endregion

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

            #region isTimeCorrectionEnabled
            bool isTimeCorrectionEnabled;
            int timeCorrectionEnable = 0;
            if (param.ContainsKey("timeCorrectionEnable") && (arg.timeCorrectionEnable is string) && int.TryParse(arg.timeCorrectionEnable, out timeCorrectionEnable) && (timeCorrectionEnable == 1))
            {
                isTimeCorrectionEnabled = true;
            }
            else
            {
                isTimeCorrectionEnabled = false;
            }
            #endregion

            #region timeZone
            int timeZone = 0;
            if(param.ContainsKey("timeZone") && (arg.timeZone is string) && int.TryParse(arg.timeZone, out timeZone))
            {
                if((timeZone < -12) || (timeZone > +14))
                {
                    timeZone = TIMEZONE_DEFAULT;
                }
            }
            else
            {
                timeZone = TIMEZONE_DEFAULT;
            }
            #endregion

            #region cmd
            string cmd = "";
            if (param.ContainsKey("cmd") && arg.cmd != "")
            {
                cmd = arg.cmd;
                log(string.Format("Введена строка команд: \"{0}\"", cmd), level: 1);
            }
            if (param.ContainsKey("cmd") && arg.cmd != "" && components.Contains("Abnormal"))
            {
                cmd = arg.cmd;
                if(byte.TryParse(cmd, out byte byteCmd))
                {
                    byte[] arrParams = new byte[] { 0x01, 0x02, 0x07, 0x08, 0x0A, 0x12 };//, 0x13 };
                    for(int i = 0; i < arrParams.Length; i++)
                    {
                        if(((byte)(byteCmd >> i) & 1) == 1)
                        {
                            log(ParameterName(arrParams[i]));
                            var turnOffOn = ParseJournalEventAndPKEResponse(arrParams[i], Send(MakeJournalEventAndPKERequest(arrParams[i])));
                            if (turnOffOn.success && !turnOffOn.IsEmpty)
                            {
                                records(turnOffOn.records);
                            }
                        }
                    }
                    //log(ParameterName(byteCmd));
                    
                }
                /*
                if (cmd.Contains("1"))
                {
                    log(ParameterName(0x01));
                    var turnOffOn = ParseJournalEventAndPKEResponse(0x01, Send(MakeJournalEventAndPKERequest(0x01)));
                    if(turnOffOn.success && !turnOffOn.IsEmpty)
                    {
                        records(turnOffOn.records);
                    }
                }
                if (cmd.Contains("2"))
                {
                    log(ParameterName(0x02));
                    var currectTime = ParseJournalEventAndPKEResponse(0x02, Send(MakeJournalEventAndPKERequest(0x02)));
                    if (currectTime.success && !currectTime.IsEmpty)
                    {
                        records(currectTime.records);
                    }
                }
                if (cmd.Contains("7"))
                {
                    log(ParameterName(0x07));
                    var currectTime = ParseJournalEventAndPKEResponse(0x07, Send(MakeJournalEventAndPKERequest(0x07)));
                    if (currectTime.success && !currectTime.IsEmpty)
                    {
                        records(currectTime.records);
                    }
                }
                if (cmd.Contains("8"))
                {
                    log(ParameterName(0x08));
                    var currectTime = ParseJournalEventAndPKEResponse(0x08, Send(MakeJournalEventAndPKERequest(0x08)));
                    if (currectTime.success && !currectTime.IsEmpty)
                    {
                        records(currectTime.records);
                    }
                }
                if (cmd.Contains("10"))
                {
                    log(ParameterName(0x0A));
                    var currectTime = ParseJournalEventAndPKEResponse(0x0A, Send(MakeJournalEventAndPKERequest(0x0A)));
                    if (currectTime.success && !currectTime.IsEmpty)
                    {
                        records(currectTime.records);
                    }
                }
                if (cmd.Contains("18"))
                {
                    log(ParameterName(0x12));
                    var currectTime = ParseJournalEventAndPKEResponse(0x12, Send(MakeJournalEventAndPKERequest(0x12)));
                    if (currectTime.success && !currectTime.IsEmpty)
                    {
                        records(currectTime.records);
                    }
                }
                */
            }
            #endregion
            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges, isTimeCorrectionEnabled, timeZone), password);
                        }
                        break;
                    case "ping":
                        {
                            result = Wrap(() => Ping(), password);
                        }
                        break;
                    case "current":
                        {
                            result = Wrap(() => Current(), password);
                        }
                        break;
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

                if((result != null) && !result.success)
                {
                    log(string.Format("при чтении прибора произошла ошибка: {0}", result.error));
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            //PREPARE
            var response = ParseTestResponse(Send(MakeTestRequest()));

            if (!response.success)
            {
                log("ответ не получен: " + response.error, level: 1);
                return MakeResult(100, response.errorcode, response.error);
            }

            var open = ParseTestResponse(Send(MakeOpenChannelRequest(Level.Slave, password)));
            if (!open.success)
            {
                log("не удалось открыть канал связи (возможно пароль не верный): " + open.error, level: 1);
                return MakeResult(100, open.errorcode, open.error);
            }

            log("канал связи открыт");

            //ACTION
            return func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion

        #region Интерфейс

        private dynamic Ping()
        {
            var currDate = ParseTimeResponse(Send(MakeTimeRequest(0x00, 0)));
            if (!currDate.success)
            {
                log("Не удалось прочесть текущее время: " + currDate.error, level: 1);
                return MakeResult(101, currDate.errorcode, "Не удалось прочесть текущее время: " + currDate.error);
            }

            log(string.Format("Текущее время на приборе {0:dd.MM.yyyy HH:mm:ss}", currDate.date), level: 1);
            //GetConst(currDate.date);
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }


        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges, bool isTimeCorrectionEnabled, int timeZone)
        {
            var version = ParseVersionResponse(Send(MakeParametersRequest(0x03)));
            if (!version.success) return version;

            log(string.Format("Версия прибора: {0}", version.Version));

            bool onlyCurrent = false;

            //читаем текущюю дату
            DateTime date;
            var time = ParseTimeResponse(Send(MakeTimeRequest(0x00, 1)));
            if (time.success)
            {
                DateTime now = DateTime.Now;
                if (isTimeCorrectionEnabled)
                {
                    DateTime nowTz = now.AddHours(timeZone - TIMEZONE_SERVER);
                    TimeSpan timeDiff = nowTz - time.date;
                    bool isTimeCorrectable = (timeDiff.TotalMinutes > -4) && (timeDiff.TotalMinutes < 4);
                    bool isTimeNeedToCorrent = (timeDiff.TotalSeconds >= 5) || (timeDiff.TotalSeconds <= -5);

                    //log(string.Format("коррекция времени: {0}, {1} разность {2}", isTimeCorrectable? "осуществима" : "только установка времени", isTimeNeedToCorrent? "необходима" : "нет необходимости", timeDiff.TotalSeconds));

                    if (isTimeCorrectable && isTimeNeedToCorrent)
                    {
                        nowTz.AddMilliseconds(1500);
                        var timeCorrect = Send(MakeTimeCorrectionRequest(nowTz.Hour, nowTz.Minute, nowTz.Second), 1);
                        if (!timeCorrect.success)
                        {
                            log(string.Format("Ошибка при попытке коррекции времени: {0}", timeCorrect.error));
                        }
                        else
                        {
                            log(string.Format("Произведена коррекция времени на {0} секунд", timeDiff.TotalSeconds), 1);
                        }
                    }

                    time = ParseTimeResponse(Send(MakeTimeRequest(0x00, 1)));
                    if (!time.success)
                    {
                        return time;
                    }
                }

                date = time.date;
            }
            else if ((time.errorcode == DeviceError.DEVICE_EXCEPTION) && (time.exceptioncode == 0x01))
            {
                onlyCurrent = true;
                date = DateTime.Now;
            }
            else
            {
                return time;
            }

            setTimeDifference(DateTime.Now - date);
            
            log(string.Format("текущая дата на приборе {0:dd.MM.yyyy HH:mm:ss}", date));

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }


            if (components.Contains("Current"))
            {
                var current = GetCurrent(date);
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
            }

            ////

            if(!onlyCurrent && (components.Contains("Hour") || components.Contains("Day")))
            {
                //if (components.Contains("Constant"))
                //{
                var constant = GetConstant(date);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                    return MakeResult(103, constant.errorcode, constant.error);
                }

                records(constant.records);
                List<dynamic> constants = constant.records;
                log(string.Format("Константы прочитаны: всего {0}", constants.Count), level: 1);
                //}


                ////чтение часовых
                if (!onlyCurrent && components.Contains("Hour"))
                {
                    if (hourRanges != null)
                    {
                        foreach (var range in hourRanges)
                        {
                            var startH = range.start;
                            var endH = range.end;
                            var hours = new List<dynamic>();

                            if (startH > date) continue;
                            if (endH > date) endH = date;

                            var hour = GetHours(startH, endH, date, constant.version, constant.variant);
                            if (!hour.success)
                            {
                                log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                            }
                            else
                            {
                                hours = hour.records;
                                log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                            }
                        }
                    }
                    else
                    {
                        var startH = getStartDate("Hour");
                        var endH = getEndDate("Hour");
                        var hours = new List<dynamic>();

                        var hour = GetHours(startH, endH, date, constant.version, constant.variant);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        }
                        else
                        {
                            hours = hour.records;
                            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                        }
                    }
                }

                //чтение суточных
                if (!onlyCurrent && components.Contains("Day"))
                {
                    if (dayRanges != null)
                    {
                        foreach (var range in dayRanges)
                        {
                            var startD = range.start;
                            var endD = range.end;

                            if (startD > date) continue;
                            if (endD > date) endD = date;

                            var day = GetDays(startD, endD, date, constant.variant);
                            if (!day.success)
                            {
                                log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                                return MakeResult(104, day.errorcode, day.error);
                            }
                            List<dynamic> days = day.records;
                            log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                        }
                    }
                    else
                    {
                        var startD = getStartDate("Day");
                        var endD = getEndDate("Day");

                        var day = GetDays(startD, endD, date, constant.variant);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.errorcode, day.error);
                        }
                        List<dynamic> days = day.records;
                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
            }

            /// Нештатные ситуации ///
            if (components.Contains("Abnormal"))
            {
                byte param = 0x01;
                log(ParameterName(param));
                var ans = ParseJournalEventAndPKEResponse(param, Send(MakeJournalEventAndPKERequest(param)));
                if (ans.success && !ans.IsEmpty)
                {
                    records(ans.records);
                }
            }

            //var lastAbnormal = getLastTime("Abnormal");
            //DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
            //DateTime endAbnormal = current.date;

            //var abnormal = GetAbnormals(startAbnormal, endAbnormal);
            //if (!abnormal.success)
            //{
            //    log(string.Format("ошибка при считывании НС: {0}", abnormal.error));
            //    return;
            //}

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        private dynamic Current()
        {
            //читаем текущюю дату
            var time = ParseTimeResponse(Send(MakeTimeRequest(0x00, 1)));
            if (!time.success) return time;

            var date = time.date;
            log(string.Format("текущая дата на приборе {0:dd.MM.yyyy HH:mm:ss}", date));

            var current = GetCurrent(date);
            if (!current.success)
            {
                log(string.Format("Ошибка при считывании текущих: {0}", current.error));
                return MakeResult(102, current.errorcode, current.error);
            }

            records(current.records);
            List<dynamic> currents = current.records;
            log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count));
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        #endregion
    }
}

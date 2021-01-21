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


namespace Matrix.SurveyServer.Driver.TV7
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
            PACKET_ERROR,
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

            var dataBytes = Helper.ASCIItoBytes(data.Skip(1).Take(data.Count() - 3).ToArray());
            log(string.Format("> {0}", string.Join(",", dataBytes.Select(b => b.ToString("X2")))), level: 3);
            //log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 1);
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
            var bufferBytes = Helper.ASCIItoBytes(buffer.Skip(1).Take(buffer.Count() - 3).ToArray());
            string tmp = string.Format("< {0}", string.Join(",", bufferBytes.Select(b => b.ToString("X2"))));
            int i = 0;
            while (200 * i < tmp.Length)
            {
                if(200 * (i+1) > tmp.Length)
                    log(string.Format("{0}: ", i) + tmp.Substring(200 * i), level: 3);
                else
                    log(string.Format("{0}: ", i) + tmp.Substring(200 * i, 200) + "...", level: 3);
                i++;
            } 
            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, int attempts_total = 4)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            try
            {
                answer.errorcode = DeviceError.NO_ERROR;

                byte[] bufferASCII = null;
                byte[] buffer = null;

                for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
                {
                    /*buffer = new byte[] {0x01,0x48,00,0xDA,00,00,0x0B,0x0C,04,0x12,0x88,06,0x42,0xA4,0xA1,0x2D,0x3F,0x16,0x9F,0xBE,0x40,04,0xC0,0x6F,0x40,00,0x2F,0xB1,0x42,0x60,0xD6,0xE6,0x3E,0xC8,0xDF,0x3B,0x40,03,0xF2,0xE1,0x40,01,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,
                            00,00,00,00,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,0xB3,0x70,0x41,0xD9,00,00,0x7F,0xF0,0xB1,06,0x3E,0x6E,00,00,0x7F,
                            0xF0,00,00,0x7F,0xF0,00,01,00,00,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,00,00,00,00,0x7F,0xF0,00,00,0x7F,0xF0,00,00,0x7F, 0xF0,00,00,0x7F,0xF0,00,01,00,00,00,00,0x7F,0xF0,00,00,00,00,00,00,00,00,00,00,00,00,00,0x80,00,00,00,00,
                            00,00,00,00,06,00,00,00,0x0C,00,0x0B,00,00,00,0xEE,0x38,00,00,00,00,00,00,00,00,0xCD};
                    
                    if (buffer[0] != NetworkAddress)
                    {
                        answer.error = "Несовпадение сетевого адреса";
                        answer.errorcode = DeviceError.ADDRESS_ERROR;
                    }
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                    }*/
                    bufferASCII = SendSimple(data);

                    if (bufferASCII.Length == 0)
                    {
                        answer.error = "Нет ответа";
                        answer.errorcode = DeviceError.NO_ANSWER;
                    }
                    else
                    {
                        if (bufferASCII.Length < 4)
                        {
                            answer.error = "В кадре ответа не может содежаться менее 4 байт";
                            answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                        }
                        else if (bufferASCII[0] != 0x3A || (bufferASCII[bufferASCII.Length - 2] != 0x0d && bufferASCII[bufferASCII.Length-1] != 0x0a))
                        {
                            answer.error = "Отсутствуют начало и конец пакета";
                            answer.errorcode = DeviceError.PACKET_ERROR;
                        }
                        buffer = Helper.ASCIItoBytes(bufferASCII.Skip(1).Take(bufferASCII.Count() - 3).ToArray());
                        if (!LRC.Check(buffer))
                        {
                            answer.error = "Контрольная сумма пакета не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;
                        }
                        else if (buffer[0] != NetworkAddress)
                        {
                            answer.error = "Несовпадение сетевого адреса";
                            answer.errorcode = DeviceError.ADDRESS_ERROR;
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
                    if (buffer[1] == 0x48)
                        answer.Body = buffer.Skip(6).ToArray();
                    else if (buffer[1] == 0x03)
                        answer.Body = buffer.Skip(3).ToArray();
                    else
                    {
                        answer.success = false;
                        answer.error = "Нет такой команды";
                    }
                    answer.NetworkAddress = buffer[0] ;
                }
                return answer;
            }
            catch
            {
                answer.error = "Send exception";
                answer.errorcode = DeviceError.DEVICE_EXCEPTION;
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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = All(components, hourRanges, dayRanges, isTimeCorrectionEnabled, timeZone);
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
            //var response = ParseTestResponse(Send(MakeBaseRequest0X48(0, 19, 0, 0, 0, 0, 0)));
            var response = Send(MakeBaseRequest0X48(0, 19, 0, 0, 0, 0));
            log(string.Format("response.success in wrap== {0}", response.success), level: 1);
            if (!response.success)
            {
                log("ответ не получен: " + response.error, level: 1);
                return MakeResult(100, response.errorcode, response.error);
            }
            var responseData = response.Body;
            
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
            bool onlyCurrent = false;

            //читаем текущюю дату
            DateTime date = DateTime.Now;

            var response = Send(MakeBaseRequest0X48(0, 19, 0, 0, 0, 0));
            if (!response.success)
            {
                log("Ответ не получен: " + response.error, level: 1);
                return MakeResult(100, response.errorcode, response.error);
            }
            IEnumerable<byte> snData = response.Body;
            log("Канал связи открыт");
            
            byte version = snData.ToArray()[2];
            
           
            

            
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
            


            if (components.Contains("Constant"))
            {
                var constant = GetConstant(date);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                    return MakeResult(103, constant.errorcode, constant.error);
                }

                records(constant.records);
                List<dynamic> constants = constant.records;
                log(string.Format("Константы прочитаны: всего {0}", constants.Count), level: 1);
            }

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

                        var hour = GetHours(startH, endH, date, version);//, constant.version,);
                        //, constant.version,);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        }
                        else
                        {
                            //hours = hour.records;
                            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                        }
                    }
                }
                else
                {
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");
                    var hours = new List<dynamic>();

                    //var hour = GetHours(startH, endH, date, version);//, constant.version,);
                    var hour = GetHours(startH, endH, date, version);
                    if (!hour.success)
                    {
                        log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                    }
                    else
                    {
                        //hours = hour.records;
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

                        var day = GetDays(startD, endD, date, version);
                        GetFinalArchive(startD, endD, date, version, "Day");
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

                    var day = GetDays(startD, endD, date, version);
                    GetFinalArchive(startD, endD, date, version, "Day");
                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        return MakeResult(104, day.errorcode, day.error);
                    }
                    List<dynamic> days = day.records;
                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }
            }

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        private dynamic Current()
        {
            DateTime date = DateTime.Now;
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
        public string NCToTUBE(byte[] bits)
        {
            string strBits = "";
            foreach (var bit in bits)
            {
                switch (bit)
                {
                    case 0:
                        strBits += "HC t<min; ";
                        break;
                    case 1:
                        strBits += "HC t>max; ";
                        break;
                    case 2:
                        strBits += "HC неиспр. датчика t; ";
                        break;
                    case 3:
                        strBits += "HC P<min; ";
                        break;
                    case 4:
                        strBits += "HC P>max ";
                        break;
                    case 5:
                        strBits += "HC V<min; ";
                        break;
                    case 6:
                        strBits += "HC V>max; ";
                        break;
                    case 7:
                        strBits += "НС неиспр. или отсут. питания ВС;";
                        break;
                }
            }
            return strBits;
        }
        public string NCToTV(byte[] bits)
        {
            string strBits = "";
            foreach (var bit in bits)
            {
                switch (bit)
                {
                    case 0:
                        strBits += "НС по dt; ";
                        break;
                    case 1:
                        strBits += "HC по dM; ";
                        break;
                    case 2:
                        strBits += "HC по Qтв; ";
                        break;
                    case 3:
                        strBits += "HC tx<min; ";
                        break;
                    case 4:
                        strBits += "HC tx>max; ";
                        break;
                    case 5:
                        strBits += "HC неиспр. датчика tx; ";
                        break;
                    case 6:
                        strBits += "HC tнв<min; ";
                        break;
                    case 7:
                        strBits += "НС tнв>max; ";
                        break;
                    case 8:
                        strBits += "HC неиспр. датчика tнв; ";
                        break;
                    case 9:
                        strBits += "HC по Q12; ";
                        break;
                    case 10:
                        strBits += "HC по Qг; ";
                        break;
                    case 11:
                        strBits += "HC по Px<min; ";
                        break;
                    case 12:
                        strBits += "HC по Px>max;";
                        break;
                }
            }
            return strBits;
        }
        #endregion
    }
}

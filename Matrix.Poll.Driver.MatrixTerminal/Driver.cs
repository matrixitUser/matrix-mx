// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Matrix.SurveyServer.Driver.MatrixTerminal
{
    public partial class Driver
    {
        bool isRtcEnabled = true;
        int hourlyStart = 30;

        private const int SEND_ATTEMPTS_COUNT = 3;
        private const int TIMEOUT_TIME_MIN = 5000;//2200;  //1500
        private const int SLEEP_TIME = 10;
        private const int COLLECT_MUL = 25;
        
        private void log(string message, int level = 2)
        {
            logger(message, level);
        }
        
        private dynamic GetOldRegisterSet()
        {
            dynamic regs = new ExpandoObject();
            regs.name = "old";

            regs.counters = 4;

            regs.Channels = 0x0;
            regs.Counters = 0x0;
            regs.Digital = 0x0;

            regs.Timestamp = 0x34001;
            regs.Chipid = 0x34A01;
            regs.Flashver = 0x34A11;

            regs.Counter = 0x4C101;
            regs.State = 0x4C141;

            return regs;
        }

        private dynamic GetNewRegisterSet()
        {
            dynamic regs = new ExpandoObject();
            regs.name = "new";

            regs.Flashver = 0x30000;
            regs.NA = 0x30002;
            regs.mode = 0x30003;

            regs.Password = 0x30400;

            regs.Timestamp = 0x32000;
            regs.Counter = 0x32004;

            regs.counters = null;

            regs.Channels = 0x44000;
            regs.Counters = 0x44001;
            regs.Digital = 0x44002;

            regs.State = 0x45000;
            regs.Chipid = 0x46000;

            regs.AdcTemp = 0x45004;
            regs.AdcBat = 0x45006;
            regs.AdcVbat = 0x45008;
            regs.AdcMains = 0x4500A;

            return regs;
        }

        List<byte> NetworkAddress = null;

        private byte mid = 0;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        private byte[] SendSimple(byte[] data, int timeout)
        {
            var buffer = new List<byte>();

            log(string.Format("{1}>{0}", string.Join(",", data.Select(b => b.ToString("X2"))), mid & 0x0F, DateTime.Now), level: 3);

            response();
            request(data);

            //var timeout = TIMEOUT_TIME;

            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while (((timeout -= SLEEP_TIME) >= 0) && !isCollected)
            {
                Thread.Sleep(SLEEP_TIME);

                var buf = response();
                if (buf.Any())  //если есть данные, то
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0; 
                }
                else //иначе
                {
                    if (isCollecting) // если хоть раз были данные, то
                    {
                        waitCollected++;  
                        if (waitCollected == COLLECT_MUL) // ждет 250 мс, те 25 доп итерации, при ожидании до 25 итерации есть данные все начинается сначала
                        {
                            isCollected = true; // после этого выход из цикла 
                        }
                    }
                }
            }

            log(string.Format("{1:X}< {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), mid & 0x0F), level: 3);

            return buffer.ToArray();
        }

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

        private Dictionary<int, string> deviceExceptionMessage = new Dictionary<int, string>()
        {
            { 0, "нет ошибки" },
            { 1, "неверный код функции" },
            { 2, "адрес не существует" },
            { 3, "неверное значение" },
            { 4, "исключительное состояние" },
            { 5, "начало процесса" },
            { 6, "в процессе" },
            { 7, "функция не может быть выполнена" },
            { 8, "ошибка памяти устройства" },
            { 9, "ошибка доступа к памяти (выравнивание байт)" },

            { 32, "недостаточно памяти" },
            { 33, "данный тип архива не поддерживается" },
            { 34, "нет записи в архиве" },
            { 35, "архивная запись пуста" },

            { 60, "ошибка доступа (нет доступа)" },

            { 180, "функция не реализована" },
        };

        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            //var attempts_total = (packagesSent > 0) ? SEND_ATTEMPTS_COUNT : 1; // непонятный оператор 04.03.2019
            var attempts_total = SEND_ATTEMPTS_COUNT;

            for (var attempts = 0; (attempts < attempts_total) && (answer.success == false); attempts++)
            {
                var timeout = TIMEOUT_TIME_MIN * (attempts + 1);
                buffer = SendSimple(data, timeout);

                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 5)
                    {
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                        answer.error = "в кадре ответа не может содержаться менее 5 байт";
                    }
                    else if (!Crc.CheckReverse(buffer, new Crc16Modbus()))
                    {
                        answer.errorcode = DeviceError.CRC_ERROR;
                        answer.error = "контрольная сумма кадра не сошлась";
                        answer = xOAxOD(buffer);
                        if (answer.success)
                        {
                            buffer = answer.buffer;
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
                var offset = 0;
                answer.NetworkAddress = buffer[0];
                if (answer.NetworkAddress == 251)
                {
                    offset = 12;
                }
                answer.Function = buffer.Skip(offset + 1).FirstOrDefault();
                answer.Body = buffer.Skip(offset + 2).Take(buffer.Length - (offset + 2 + 2)).ToArray();
                //modbus error
                if (answer.Function > 0x80)//0xc1)
                {
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                    answer.success = false;
                    answer.exceptionCode = buffer.Skip(offset + 2).FirstOrDefault();
                    string exceptionMessage = deviceExceptionMessage.ContainsKey(answer.exceptionCode) ? deviceExceptionMessage[answer.exceptionCode] : "";
                    answer.error = $"устройство вернуло ошибку: {answer.exceptionCode} {exceptionMessage}";
                }
            }

            mid++;

            return answer;
        }
        public dynamic xOAxOD(byte[] buffer)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            if ((buffer[0] == 0x0A && buffer[1] == 0x0D)|| (buffer[buffer.Length - 2] == 0x0A && buffer[buffer.Length - 1] == 0x0D))
            {
                List<byte> bufferTmp = buffer.ToList();
                while (bufferTmp.Count > 5 && (bufferTmp[0] == 0x0A && bufferTmp[1] == 0x0D))
                {
                    bufferTmp.RemoveRange(0, 2);
                }
                while (bufferTmp.Count > 5 && (bufferTmp[bufferTmp.Count - 2] == 0x0A && bufferTmp[bufferTmp.Count - 1] == 0x0D))
                {
                    bufferTmp.RemoveRange(bufferTmp.Count - 2, 2);
                }
                buffer = null;
                buffer = bufferTmp.ToArray();

                log(string.Format("---{0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);
                if (!Crc.Check(buffer, new Crc16Modbus()))
                {
                    answer.errorcode = DeviceError.CRC_ERROR;
                    answer.error = "контрольная сумма кадра не сошлась";
                }
                else
                {
                    answer.success = true;
                    answer.error = string.Empty;
                    answer.errorcode = DeviceError.NO_ERROR;
                    answer.buffer = buffer;
                }
            }
            return answer;
        }

        public dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
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

        public dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
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

        public dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
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

        public dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date, DateTime dateNow)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = dateNow;
            //record.dt1 = DateTime.Now;
            record.dt1 = date; 
            return record;
        }

        public dynamic MakeResult(int code, DeviceError errorcode, string description)
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
        public dynamic MakeResult(int code, DeviceError errorcode, string description, double lightMk, double lightReal)
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
            result.lightMk = lightMk;
            result.lightReal = lightReal;

            return result;
        }
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
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

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Import("recordLoad")]
        private Func<DateTime, DateTime, string, List<dynamic>> recordLoad;

        [Import("recordLoadWithId")]
        private Func<DateTime, DateTime, string, Guid, List<dynamic>> recordLoadWithId;

        [Import("loadRowsCache")]
        private Func<Guid, List<dynamic>> loadRowsCache;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Import("setModbusControl")]
        private Action<dynamic> setModbusControl;
        
        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            //List<dynamic> recordLoadList = new List<dynamic>();
            //recordLoadList = recordLoad(new DateTime(2018, 12, 1, 1, 0, 0), new DateTime(2019, 1, 1, 1, 0, 0), "Current");

            setArchiveDepth("Day", 2);
            setArchiveDepth("Hour", -1);

            var param = (IDictionary<string, object>)arg;
            #region networkAddress (обычный или расширенный)
            string strNetworkAddress = "243";// arg.networkAddress.ToString().Trim();

            if (!param.ContainsKey("networkAddress"))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }

            byte na;
            if (strNetworkAddress.Length == 24)//001122334455667788990011
            {
                NetworkAddress = Enumerable.Range(0, strNetworkAddress.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(strNetworkAddress.Substring(x, 2), 16))
                    .ToList();
            }
            else if (byte.TryParse(strNetworkAddress, out na))
            {
                NetworkAddress = new List<byte> { na };
            }
            else
            {
                log("Неверные сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion
            
            #region cmd
            string cmd = "";
            if (param.ContainsKey("cmd") && arg.cmd != "")
            {
                cmd = arg.cmd;
                log(string.Format("Введена строка команд: \"{0}\"", cmd), level: 1);
                
                if (cmd.Contains("setConfig"))
                {
                    string str = "setConfig";
                    string textSetLight = cmd.Substring(0, str.Length);
                    log(textSetLight, level: 1);
                    string sBytes = cmd.Substring(str.Length, cmd.Length - str.Length);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
                    List<byte> byteLight = new List<byte>();
                    byteLight.AddRange(bytes);
                    dynamic current = SetConfig(byteLight);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке SoftConfig: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                if (cmd.Contains("getConfig"))
                {
                    dynamic current = GetConfig();
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке SoftConfig: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
            }


            #endregion
            
            #region startDate
            var startDate = DateTime.MinValue;

            //входная строка: начало архивов
            if (!param.ContainsKey("startDate") || !(arg.startDate != "") || !DateTime.TryParse(arg.startDate, out startDate))
            {
                startDate = DateTime.MinValue;
            }
            #endregion

            #region rtcResetDate
            var rtcResetDate = DateTime.MinValue;

            //входная строка: начало архивов
            if (!param.ContainsKey("rtcResetDate") || !(arg.rtcResetDate != "") || !DateTime.TryParse(arg.rtcResetDate, out rtcResetDate))
            {
                rtcResetDate = DateTime.MinValue;
            }
            else
            {
                rtcResetDate = rtcResetDate.Date.AddHours(rtcResetDate.Hour);
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

            #region rtcEnabled
            byte rtcEnabled = 1;
            if (param.ContainsKey("rtcEnabled") && byte.TryParse(arg.rtcEnabled.ToString(), out rtcEnabled))
            {
                if (rtcEnabled == 0)
                {
                    isRtcEnabled = false;
                }
            }
            #endregion

            #region hourlyStart
            // минута, с которой считать начало следующего часа (если равна 10 => текущие 10:09 -> часовые 10:00; но 10:10 -> 11:00)
            if (param.ContainsKey("hourlyStart"))
            {
                int.TryParse(arg.hourlyStart.ToString(), out hourlyStart);
                hourlyStart %= 60;
            }
            #endregion

            dynamic result = new ExpandoObject();
            try
            {

                switch (what.ToLower())
                {

                    case "all":
                        {
                            result = All(components, cmd, startDate);
                        }
                        break;

                    case "ping":
                        {
                            result = MakeResult(0, DeviceError.NO_ERROR, "");
                        }
                        break;

                    case "current":
                        {
                        }
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
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
        
        #endregion

        #region Интерфейс
       
        private dynamic Current(string components, dynamic flashver, bool isRtcEnabled)
        {
            var devid = (UInt16)flashver.devid;

            int res;
            if (int.TryParse(components, out res))
            {
                if (res != NetworkAddress.FirstOrDefault())
                {
                    return MakeResult(207, DeviceError.NO_ERROR, "опрос проигнорирован");
                }
            }
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
        
        private dynamic All(string components, string cmd, DateTime startDate)
        {
            CorrectTime();
            //string kkk = "setConfigB0-41-F3-00-00-00-00-00-37-37-2E-37-39-2E-31-38-36-2E-39-31-3A-31-30-31-31-34-00-00-00-00-00-00-37-37-2E-37-39-2E-31-38-36-2E-38-36-3A-31-30-31-31-35-00-00-00-00-00-00-6C-69-73-74-65-6E-65-72-3A-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-69-6E-74-65-72-6E-65-74-2E-6D-74-73-2E-72-75-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-E1-00-00-00-00-00-00-80-25-00-00-00-00-00-00-80-25-00-00-00-00-00-00-FF-00-01-01-39-00-00-00-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-FF-00-00-00-00";
            //if (kkk.Contains("setConfig"))
            //{
            //    string str = "setConfig";
            //    string sBytes = kkk.Substring(str.Length, kkk.Length - str.Length);
            //    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
            //    List<byte> byteLight = new List<byte>();
            //    byteLight.AddRange(bytes);
            //    dynamic current = SetConfig(byteLight);
            //    if (!current.success)
            //    {
            //        log(string.Format("Ошибка при установке SoftConfig: {0}", current.error), level: 1);
            //        return MakeResult(102, current.errorcode, current.error);
            //    }
            //    records(current.records); //запись в базу данных
            //}
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
        #endregion
    }
}

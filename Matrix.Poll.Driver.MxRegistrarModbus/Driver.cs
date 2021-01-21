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

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    /// <summary>
    /// Драйвер для счётчика импульсов Matrix Pulse Registrar Modbus для версии >= 3.0.0
    /// 
    /// работает как со старыми регистраторами матрикс типа Kur13, 1002(Kpk), так и с новыми 1001R4-P12.01, IC485.03
    /// 
    /// ранее была добавлена возможность синхронизации времени 1-го числа месяца (до +-2 часов)
    /// ранее были добавлены команды debug, setbkp, setmode(work|service)
    /// ранее была добавлена срочная команда setna 
    /// 27.01.2017 добавлен DeviceError
    /// 03.02.2017 добавлен parameterConfiguration - конфигурация параметров (коэффициент, нач.знач., имя параметра, ед.измерения)
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif
        bool isRtcEnabled = true;
        int hourlyStart = 30;

        private const int SEND_ATTEMPTS_COUNT = 3;
        private const int TIMEOUT_TIME_MIN = 5000;//2200;  //1500
        private const int SLEEP_TIME = 10;
        private const int COLLECT_MUL = 25;
        private const int ADC_ATTEMPTS_COUNT = 10;
        private const double ADC_VMIN = 3.28;
        private const double ADC_VMAX = 3.48;

        //версия флеш со времён Ильшата 16 бит
        private const int Register_FlashVerOld = 0x34A11;
        //обновленная версия флеш 16/32 бит
        private const int Register_FlashVerNew = 0x30000;

        private int packagesSent = 0;
        private bool infiniteMode = false;

        public class Parameter
        {
            public int n { get; set; }
            public double start { get; set; }
            public double k { get; set; }
            public string name { get; set; }
            public string unit { get; set; }
            public bool isEnable { get; set; }
            public bool isError { get; set; }
            public SByte point { get; set; }
            public bool PinState { get; set; }
            public bool PinMagState { get; set; }


            public Parameter(int i) {
                n = i;
                start = 0.0;
                k = 1.0;
                name = $"Канал {i}";
                unit = string.Empty;
                isEnable = true;
                isError = false;
                point = 0;
            }

            public double GetValue(double counter)
            {
                return isEnable ?
                    (start + counter * k * Math.Pow(10.0, point))
                    : 0.0;
            }

            public string GetView(double counter)
            {
                return isEnable ?
                    $"{GetValue(counter)}{((unit != null && unit != "") ? " " + unit : "")}"
                    : "---";
            }
        }

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

        private dynamic GetRegisterSet(UInt16 devId)
        {
            switch ((DeviceType)devId)
            {
                case DeviceType.TYPE_MX1001R4_P12_01:
                case DeviceType.TYPE_MX1005R4_P16D16I_01:
                case DeviceType.TYPE_LIGHT_CONTROL:
                case DeviceType.TYPE_PUMP_CONTROL:
                case DeviceType.TYPE_IC485_03:
                    return GetNewRegisterSet();
            }

            return GetOldRegisterSet();
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
                    else if (!Crc.Check(buffer, new Crc16Modbus()))
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
                        packagesSent++;
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
                    packagesSent++;
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
            dynamic flashver = null;
            int ver = 0;

            var param = (IDictionary<string, object>)arg;
            Guid idWls = new Guid();
            #region idWls
            if (param.ContainsKey("idWls")) //1; 2; 1,2; _
            {
                try
                {
                    idWls = Guid.Parse(arg.idWls.ToString());
                }
                catch (Exception ex){}
            }
            #endregion
            #region max min for upp
            float max = 14, min = 12.5f;
            if (param.ContainsKey("max")) //1; 2; 1,2; _
            {
                max = Convert.ToSingle(arg.max.ToString().Replace('.', ','));
                log($"max = {max} arg.max={arg.max}", level: 1);
            }
            if (param.ContainsKey("min")) //1; 2; 1,2; _
            {
                min = Convert.ToSingle(arg.min.ToString().Replace('.', ','));
                log($"min = {min} arg.min={arg.min}", level: 1);
            }
            if (param.ContainsKey("criticalMax")) //1; 2; 1,2; _
            {
                log("criticalMax", level: 1);
            }
            #endregion
            #region networkAddress (обычный или расширенный)
            string strNetworkAddress = arg.networkAddress.ToString().Trim();

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

            #region password
            byte[] password = null;
            if (param.ContainsKey("password"))
            {
                password = Encoding.ASCII.GetBytes((string)arg.password);
            }
            #endregion

            #region objectId
            string objectId = "";
            if (param.ContainsKey("id") && arg.id != "")
            {
                objectId = arg.id;
                log(string.Format("ObjectId: \"{0}\"", objectId), level: 1);
            }
            #endregion

            #region cmd
            string cmd = "";
            if (param.ContainsKey("cmd") && arg.cmd != "")
            {
                cmd = arg.cmd;
                log(string.Format("Введена строка команд: \"{0}\"", cmd), level: 1);
            }

            
           
            if (param.ContainsKey("cmd") && arg.cmd != "")
            {
                cmd = arg.cmd;
                if (cmd.Contains("lightOn"))
                {
                    SetLight(1);
                }
                if (cmd.Contains("lightOff"))
                {
                    SetLight(0);
                }
                if (cmd.Contains("setConfig"))
                {
                    string textSetLight = cmd.Substring(0, 9);
                    string sBytes = cmd.Substring(9, cmd.Length - 9);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));

                    List<byte> byteTmp = new List<byte>();
                    byteTmp.AddRange(bytes);
                    UInt32 registerStart = (UInt32)(byteTmp[0] + byteTmp[1] * 256);

                    List<byte> byteConfig = new List<byte>();
                    byteConfig.AddRange(bytes.Skip(2));
                    SetConfig(byteConfig, registerStart);

                }
                if (cmd.Contains("setSoftConfig"))
                {
                    string textSetLight = cmd.Substring(0, 13);
                    log(textSetLight, level: 1);
                    string sBytes = cmd.Substring(13, cmd.Length - 13);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
                    List<byte> byteLight = new List<byte>();
                    byteLight.AddRange(bytes);
                    dynamic current = SoftConfig(byteLight, flashver, objectId);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке SoftConfig: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                
                if (cmd.Contains("lightSoftConfig")) //20190507
                {
                    //Получение типа устройства и версии !!! Возможно надо ставить вначале обработки команд
                    if (flashver == null)
                    {
                        flashver = GetFlashVer();
                    }

                    if (!flashver.success)
                    {
                        return MakeResult(101, flashver.errorcode, flashver.error);
                    }
                    //

                    string[] arrString = cmd.Split('#');
                    if (arrString.Length == 4)
                    {
                        List<byte> byteLight = new List<byte>();
                        log(arrString[0], level: 1);
                        byteLight.AddRange(lightSetSoftConfig(arrString, 1800, (int)flashver.ver,(int)flashver.devid));
                        dynamic current = SoftConfig(byteLight, flashver, objectId);
                        if (!current.success)
                        {
                            log(string.Format("Ошибка при установке lightSoftConfig: {0}", current.error), level: 1);
                            return MakeResult(102, current.errorcode, current.error);
                        }
                        records(current.records); //запись в базу данных
                    }
                }
                if (cmd.Contains("valveControl#")) //20190517
                {
                    string[] arrString = cmd.Split('#');
                    log(arrString[0], level: 1);
                    dynamic current = valveControlSetConfig(arrString[1], flashver, objectId);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке valveControlSetConfig: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                if (cmd.Contains("abnormal"))
                {
                    if (flashver == null)
                    {
                        flashver = GetFlashVer();
                    }

                    if (!flashver.success)
                    {
                        return MakeResult(101, flashver.errorcode, flashver.error);
                    }
                    ver = (int)flashver.ver;

                    string textSetLight = cmd.Substring(0, 8);
                    log(textSetLight, level: 1);
                    string sBytes = cmd.Substring(8, cmd.Length - 8);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
                    List<byte> byteAbnormal = new List<byte>();
                    byteAbnormal.AddRange(bytes);
                    dynamic current = SetGetAbnormal(byteAbnormal, ver);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке abnormal: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                if (cmd.Contains("setEvents"))
                {
                    if (flashver == null)
                    {
                        flashver = GetFlashVer();
                    }

                    if (!flashver.success)
                    {
                        return MakeResult(101, flashver.errorcode, flashver.error);
                    }
                    ver = (int)flashver.ver;

                    string textSetLight = cmd.Substring(0, 9);
                    string sBytes = cmd.Substring(9, cmd.Length - 9);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
                    List<byte> byteAbnormal = new List<byte>();
                    byteAbnormal.AddRange(bytes);
                    dynamic current = SetGetEvents(byteAbnormal, ver);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке events: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                if (cmd.Contains("setDHT"))
                {
                    string textSetLight = cmd.Substring(0, 6);
                    log(textSetLight, level: 1);
                    dynamic current = DHT();
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке SoftConfig: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
               
                if (cmd.Contains("setAstronTimer"))
                {
                    string textSetLight = cmd.Substring(0, 14);
                    log(string.Format(textSetLight), level: 1);
                    string sBytes = cmd.Substring(14, cmd.Length - 14);
                    var bytes = sBytes.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber));
                    List<byte> byteLight = new List<byte>();

                    byteLight.AddRange(bytes);
                    dynamic current = SetAstronTimer(byteLight, flashver);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при установке астрономического таймера: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }
                #region автоматика водонапорной башни
                if (cmd.Contains("softStartControl#"))
                {
                    var current = softStartControlDistributionByFunction(cmd, flashver, objectId, idWls);
                    records(current.records); //запись в базу данных
                }
                #endregion
                if (cmd.Contains("correcttime"))
                {
                    var time = CorrectTime(flashver);
                }
                if (cmd.Contains("startFlash"))
                {
                    string base64String = cmd.Substring(11);
                    StartFlash(base64String, null, null, null);//
                }
            }


            #endregion

            #region version
            //dynamic flashver = null;
            byte counters = 255;
            byte digitals = 0;
            if (param.ContainsKey("version") && arg.version is string)
            {
                dynamic answer = new ExpandoObject();
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.Function = (byte)3;

                switch (arg.version as string)
                {
                    /*case "kur13":
                        fv.Register = new byte[] { 0x00, 0x80, 0x73, 0x00 };
                        flashver = ParseVersionResponse(fv);
                        break;*/
                    case "stroit1":
                        answer.Body = new byte[] { 0x04, 0x00, 0x80, 0x73, 0x00 };
                        flashver = ParseVersionResponse(answer);
                        counters = 4;
                        break;
                    default:
                        break;
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

            #region parameters
            var parameterConfiguration = new Dictionary<int, Parameter>();
            //входная строка: PARAMETER|PARAMETER, где PARAMETER = CHANNEL;START;END;NAME;UNIT
            if (param.ContainsKey("parameters") && (arg.parameters != ""))
            {
                //log(string.Format("Введена строка параметров: \"{0}\"", arg.parameters));
                var x = ((string)arg.parameters).Split('|');
                foreach (var parameter in x)
                {
                    var spar = parameter.Split(';');

                    int channel = 0;
                    if (spar.Length > 0 && int.TryParse(spar[0], out channel))
                    {
                        double start = 0.0;
                        if ((spar.Length > 1) && (spar[1] != ""))
                        {
                            double.TryParse(spar[1], out start);
                        }

                        double k = 1.0;
                        if ((spar.Length > 2) && (spar[2] != ""))
                        {
                            double.TryParse(spar[2], out k);
                        }

                        string name = "";
                        if ((spar.Length > 3) && (spar[3] != ""))
                        {
                            name = spar[3];
                        }

                        string unit = "";
                        if ((spar.Length > 4) && (spar[4] != ""))
                        {
                            unit = spar[4];
                        }

                        Parameter p = new Parameter(channel);
                        p.start = start;
                        p.k = k;
                        p.name = name;
                        p.unit = unit;

                        parameterConfiguration[channel] = p;
                        //log(string.Format("param[{0}] = {{ name = {3}, start = {1}, k = {2}, unit = {4} }}", channel, start, k, name, unit));
                    }

                }
                log(string.Format("Найдено параметров: {0}", parameterConfiguration.Keys.Count));
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

            //

            /*
             * Ввод в драйвер: начальное значение, коэффициент, название параметра, единица измерения
             * [{start: 0.0, k: 1.0, name: 'Xx0', unit: 'l'}, {start: 0.0, k: 1.0, name: 'Xx1', unit: 'l'}, {start: 0.0, k: 1.0, name: 'Xx2', unit: 'l'}, ... {start: 0.0, k: 1.0, name: 'Xx11', unit: 'l'}]
             * Дата-время пусконаладки: '2017-01-01' (обязательно) 
             */

            //

            #region Ввод пароля
            if (/*(GetRegisterSet(devid).name == "new") && */(password != null) && (password.Length > 0))
            {
                var makePass = password.ToList();
                while (makePass.Count < 24)
                {
                    makePass.Add(0x00);
                }

                var pass = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)0x30400/*GetRegisterSet(devid).Password*/, 24, makePass.Take(24).ToList())));
                if (pass.success)
                {
                    if (pass.Wrote > 0)
                    {
                        log("Введён пароль");
                    }
                    else
                    {
                        log("Пароль ввести не удалось (значение пустое или нет доступа)");
                    }
                }
                else
                {
                    log(string.Format("Пароль НЕ введён: {0}", pass.error));
                }
            }
            #endregion


            #region Срочная команда на изменение сетевого адреса

            if (cmd.Length > 0)
            {

                var cmds = cmd.Split(' ');

                foreach (var command in cmds)
                {
                    SetNaCmd(command);
                }
            }
            #endregion

            //

            dynamic result;
            //int ver = 0;
            try
            {

                switch (what.ToLower())
                {

                    case "all":
                        {
                            if (flashver == null)
                            {
                                flashver = GetFlashVer();
                            }

                            if (!flashver.success)
                            {
                                return MakeResult(101, flashver.errorcode, flashver.error);
                            }
                            ver = (int)flashver.ver;
                            if (((DeviceType)flashver.devid == DeviceType.TYPE_MX1005R4_P16D16I_01) || ((DeviceType)flashver.devid == DeviceType.TYPE_LIGHT_CONTROL) || ((DeviceType)flashver.devid == DeviceType.TYPE_PUMP_CONTROL) || ((DeviceType)flashver.devid == DeviceType.TYPE_CONTROL_UPP))
                            {
                                result = All4(components, flashver, cmd, password, objectId, idWls, max, min);
                            }
                            else
                            {
                                if (counters == 255)
                                {
                                    var cnt = GetCounters((UInt16)flashver.devid);
                                    if (!cnt.success)
                                    {
                                        return MakeResult(101, cnt.errorcode, cnt.error);
                                    }

                                    counters = (byte)cnt.counters;
                                    digitals = (byte)cnt.digitals;
                                }
                                result = All(components, flashver, counters, digitals, cmd, password, parameterConfiguration, startDate, isRtcEnabled, rtcResetDate, rtcEnabled);
                            }
                        }
                        break;

                    case "ping":
                        {
                            if (flashver == null)
                            {
                                flashver = GetFlashVer();
                            }

                            if (!flashver.success)
                            {
                                return MakeResult(101, flashver.errorcode, flashver.error);
                            }
                            result = MakeResult(0, DeviceError.NO_ERROR, "");
                        }
                        break;

                    case "current":
                        {
                            if (flashver == null)
                            {
                                flashver = GetFlashVer();
                            }

                            if (!flashver.success)
                            {
                                return MakeResult(101, flashver.errorcode, flashver.error);
                            }

                            if (counters == 255)
                            {
                                var cnt = GetCounters((UInt16)flashver.devid);
                                if (!cnt.success)
                                {
                                    return MakeResult(101, cnt.errorcode, cnt.error);
                                }

                                counters = cnt.counters;
                            }

                            result = Current(components, flashver, counters, digitals, parameterConfiguration, isRtcEnabled);
                        }
                        break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
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
        //private dynamic Wrap(Func<dynamic, byte, dynamic> func, dynamic flashver)
        //{
        //    //ACTION
        //    return func(flashver, counters);
        //}
        #endregion

        #region Интерфейс

        //private dynamic Ping(dynamic flashver)
        //{
        //    var devid = (UInt16)flashver.devid;

        //    var currDate = ParseTimeResponse(Send(MakeTimeRequest(devid)));
        //    if (!currDate.success)
        //    {
        //        log("Не удалось прочесть текущее время: " + currDate.error, level: 1);
        //        return MakeResult(101, currDate.errorcode, "Не удалось прочесть текущее время: " + currDate.error);
        //    }

        //    log(string.Format("Текущее время на приборе {0:dd.MM.yyyy HH:mm:ss}", currDate.date), level: 1);
        //    //GetConst(currDate.date);
        //    return MakeResult(0, DeviceError.NO_ERROR, "");
        //}

        private dynamic GetFlashVer()
        {
            var flashver = ParseVersionResponse(Send(MakeNewVersionRequest()));
            if (flashver.success == true)
            {
                if (flashver.devid == -1)
                {
                    flashver = ParseVersionResponse(Send(MakeOldVersionRequest()));
                }

                if (flashver.success == true && flashver.devid >= 0)
                {
                    log(string.Format("Тип устройства: \"{0}\" версии {1}{2}", flashver.device, flashver.ver,
                        flashver.flash == null ? "" : string.Format(" размер конфигурации: {0}", flashver.flash)));
                }
            }

            return flashver;
        }

        private dynamic GetCounters(UInt16 devid)
        {
            dynamic answer = new ExpandoObject();
            answer.error = "";
            answer.success = true;
            answer.errorcode = DeviceError.NO_ERROR;

            if (GetRegisterSet(devid).name == "new")
            {
                var reg = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).Channels, 4)));
                if (!reg.success) return reg;

                answer.counters = reg.Register[1];
                answer.digitals = reg.Register[2];
            }
            else
            {
                answer.counters = (byte)GetRegisterSet(devid).counters;
                answer.digitals = 0;
            }
            return answer;
        }

        private dynamic Current(string components, dynamic flashver, byte counters, byte digitals, Dictionary<int, Parameter> parameterConfiguration, bool isRtcEnabled)
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

            //приборное время
            DateTime date;
            if (isRtcEnabled)
            {
                var time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                if (!time.success)
                {
                    return MakeResult(101, time.errorcode, time.error);
                }

                date = time.date;
            }
            else
            {
                date = DateTime.Now;
            }
            DateTime dateNow = DateTime.Now;
            ////
            var current = GetCurrent(date, dateNow, flashver, counters, digitals, parameterConfiguration);
            if (!current.success)
            {
                log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                return MakeResult(102, current.errorcode, current.error);
            }

            records(current.records);
            List<dynamic> currents = current.records;
            log(string.Format("Текущие на {0} прочитаны: {1}; {2}; {3}",
                current.date,
                current.values != "" ? string.Format("показания - {0}", current.values) : string.Format("количество импульсов - {0}", current.counters),
                current.inputs,
                current.adc != "" ? string.Format("значения АЦП - {0}", current.adc) : ""), level: 1);

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }


  
        private dynamic All(string components, dynamic flashver, byte counters, byte digitals, string cmd, byte[] password, Dictionary<int, Parameter> parameterConfiguration, DateTime startDate, bool isRtcEnabled, DateTime rtcResetDate, byte rtcEnabled)
        {

            var devid = (UInt16)flashver.devid;
            var ver = (int)flashver.ver;
            //приборное время
            //if(ver != 7)  //Закоментировал Зимфир 10/10/2018  14:44 Почему не корректируется время?
            {
                DateTime date;
                if (isRtcEnabled)
                {
                    var time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                    if (!time.success)
                    {
                        return MakeResult(101, time.errorcode, time.error);
                    }

                    date = time.date;

                    #region Установка времени прибора
                    DateTime now = DateTime.Now;
                    var timeOffsetSign = (date > now);
                    var timeOffset = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
                    // установка времени (если время на приборе значительно отличается от серверного)
                    var isSetTime = (timeOffset > (3600 * 2)) || cmd.Contains("settime");
                    // коррекция времени (если отличается больше, чем на 60 секунд и если время опроса соответствует HH:04-HH:56)
                    var isDoCorrectTime = (timeOffset >= 60) && (now.Minute > 8) && (now.Minute < (60 - 8));
                    var isMinCorrectTime = (timeOffset >= 5) && (timeOffset <= 2000) && (now.Minute > 8) && (now.Minute < (60 - 8));
                    //log(isSetTime ? "cmd.Contains(settime)" : string.Format("cmd={0}",cmd), level: 1);
                    if (isSetTime || isDoCorrectTime || isMinCorrectTime)
                    {
                        if ((now.Day == 1) || isSetTime || isMinCorrectTime) //корректировка часов производится только 1го числа месяца ЛИБО если очень большая разница во времени
                        {
                            var bkp = Send(MakeWriteBkpRequest(DateTime.Now, devid));
                            if (bkp.success)
                            {
                                time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                                if (!time.success) return time;
                                //log(string.Format("время на счётчике {0} на сервере {1}", time.date, DateTime.Now));
                            }
                            var timeOffsetNew = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
                            if (bkp.success && time.success && (timeOffsetNew < 5))
                            {
                                date = time.date;
                                log(string.Format(isSetTime ? "Время установлено" : "Произведена корректировка времени на {0:0.###} секунд", timeOffset), level: 1);
                            }
                            else
                            {
                                log(string.Format("Время НЕ {0}: {1}", isSetTime ? "установлено" : "скорректировано", bkp.success ? time.error : bkp.error));
                            }
                        }
                        else
                        {
                            log(string.Format("Время на приборе {1} на {0:0.###} секунд, корректировка будет произведена 1го числа следующего месяца", timeOffset, timeOffsetSign ? "спешит" : "отстаёт"));
                        }
                    }
                    else
                    {
                        if (timeOffset < 5)
                        {
                            log("Корректировка времени не требуется");
                            //log(string.Format("время на счётчике {0} на сервере {1}", time.date, DateTime.Now));
                        }
                        else
                        {
                            if ((now.Minute < 8) || (now.Minute > (60 - 8)))
                            {
                                log("Корректировка времени  в начале часа 8 минут и за 8 минут до конца часа не производится");
                            }
                        }
                    }
                    #endregion

                    setTimeDifference(DateTime.Now - time.date);
                }
                else
                {
                    log(string.Format("Время серверное, начало часа в {0:2} минут", hourlyStart));
                    date = DateTime.Now;
                    setTimeDifference(new TimeSpan(0));
                    setArchiveDepth("hour", 1);
                }

                #region Коррекция времени прибора
                if ((isRtcEnabled) && (cmd.Contains("correcttime")))
                {

                    var time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                    if (!time.success)
                    {
                        return MakeResult(101, time.errorcode, time.error);
                    }

                    date = time.date;

                    DateTime now = DateTime.Now;
                    var timeOffset = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
                    bool isSetTime = timeOffset > 5;
                    // коррекция времени (елси отличается больше, чем на 5 секунд и если время опроса соответствует HH:04-HH:56)
                    if (isSetTime)
                    {
                        var bkp = Send(MakeWriteBkpRequest(DateTime.Now, devid));
                        if (bkp.success)
                        {
                            time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                            if (!time.success) return time;
                            //log(string.Format("время на счётчике {0} на сервере {1}", time.date, DateTime.Now));
                        }
                        var timeOffsetNew = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
                        if (bkp.success && time.success && (timeOffsetNew < 5))
                        {
                            date = time.date;
                            log(isSetTime ? "Время установлено" : string.Format("Произведена корректировка времени на {0:0.###} секунд", timeOffset), level: 1);
                        }
                        else
                        {
                            log(string.Format("Время НЕ {0}: {1}", isSetTime ? "установлено" : "скорректировано", bkp.success ? time.error : bkp.error), level: 1);
                        }

                    }
                    else
                    {
                        log(string.Format("Корректировка времени не требуется"), level: 1);
                    }
                }
                #endregion

                #region Команды прибору и драйверу
                if (cmd.Length > 0)
                {
                    var cmds = cmd.Split(' ');

                    foreach (var command in cmds)
                    {
                        SetBkpCmd(command, counters, devid);

                        //#region infinite
                        //if (command.StartsWith("infinite"))
                        //{
                        //    infiniteMode = true;
                        //}
                        //#endregion

                        //КОМАНДЫ ОБНОВЛЁННОМУ СЕТУ РЕГИСТРОВ
                        if (GetRegisterSet(devid).name == "new")
                        {
                            #region setna (закомментирован)
                            //if (command.StartsWith("setna="))
                            //{
                            //    var strValue = command.Substring(6);  //30, 500, 100, 200, 110
                            //    byte val = 0;
                            //    if (Byte.TryParse(strValue, out val) && (val > 0) && (val < 250))
                            //    {
                            //        var getna = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).NA, 1)));
                            //        if (getna.success && getna.Register[0] != val)
                            //        {
                            //            var setna = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).NA, 1, new List<byte>() { val })));
                            //            if (setna.success)
                            //            {
                            //                if (setna.Wrote > 0)
                            //                {
                            //                    log(string.Format("Новый сетевой адрес успешно изменён: {0}->{1}", getna.Register[0], val));
                            //                }
                            //                else
                            //                {
                            //                    log("Не удалось установить сетевой адрес (нет доступа?)");
                            //                }
                            //            }
                            //            else
                            //            {
                            //                log(string.Format("Не удалось установить сетевой адрес: {0}", setna.error));
                            //            }
                            //        }
                            //    }
                            //    else
                            //    {
                            //        log(string.Format("Новый сетевой адрес не распознан: должно быть число от 1 до 249, введено {0}", strValue));
                            //    }
                            //}
                            #endregion

                            #region setmodework
                            if (command.StartsWith("setmodework"))
                            {
                                var getmode = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).mode, 1)));
                                if (getmode.success && getmode.Register[0] == 0)
                                {
                                    var setmode = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).mode, 1, new List<byte>() { 1 })));
                                    if (setmode.success)
                                    {
                                        if (setmode.Wrote > 0)
                                        {
                                            log("Режим \"Работа\" успешно установлен!", level: 1);
                                        }
                                        else
                                        {
                                            log("Не удалось установить режим \"Работа\" (нет доступа?)", level: 1);
                                        }
                                    }
                                    else
                                    {
                                        log(string.Format("Не удалось установить режим \"Работа\": {0}", setmode.error), level: 1);
                                    }
                                }
                            }
                            #endregion

                            #region setmodeservice
                            else if (command.StartsWith("setmodeservice"))
                            {
                                var getmode = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).mode, 1)));
                                if (getmode.success && getmode.Register[0] == 1)
                                {
                                    var setmode = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).mode, 1, new List<byte>() { 0 })));
                                    if (setmode.success)
                                    {
                                        if (setmode.Wrote > 0)
                                        {
                                            log("Режим \"Сервис\" успешно установлен!", level: 1);
                                        }
                                        else
                                        {
                                            log("Не удалось установить режим \"Сервис\" (нет доступа?)", level: 1);
                                        }
                                    }
                                    else
                                    {
                                        log(string.Format("Не удалось установить режим \"Сервис\": {0}", setmode.error), level: 1);
                                    }
                                }
                            }
                            #endregion
                        }
                        else //СТАРЫЙ СЕТ РЕГИСТРОВ
                        {
                            #region setbkp (закомментирован)
                            //if (command.StartsWith("setbkp="))
                            //{
                            //    var newValues = new List<UInt32>();
                            //    var newStringValues = command.Substring(7).Split(',');  //30, 500, 100, 200, 110

                            //    //newValues = 30, 500, 100, 200, 110
                            //    foreach (var strvalue in newStringValues)
                            //    {
                            //        UInt32 val;
                            //        if (UInt32.TryParse(strvalue, out val))
                            //        {
                            //            newValues.Add(val);
                            //        }
                            //    }

                            //    //newValues = 30, 500, 100, 200, 110 OR newValues = 30, 500, 100, 200, 110, 0, 0, 0
                            //    while (newValues.Count < counters)
                            //    {
                            //        newValues.Add(0);
                            //    }

                            //    //newData = 0,0,0,0x30, 0,0,0x1,0xf4, 0,0,0,0x64, 0,0,0,0xc8 OR 0,0,0,0x30, 0,0,0x1,0xf4, 0,0,0,0x64, 0,0,0,0xc8, 0,0,0,0x6e, 0,0,0,0 0,0,0,0, 0,0,0,0
                            //    var newData = new List<byte>();
                            //    for (var i = 0; i < counters; i++)
                            //    {
                            //        newData.AddRange(BitConverter.GetBytes(newValues[0]).Reverse());
                            //    }

                            //    if (GetRegisterSet(devid).name == "new")
                            //    {
                            //        var setbkp = ParseWriteHoldingRegisterResponse(Send(MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).Counter, (UInt16)(counters * 4), newData)));
                            //        if (setbkp.success)
                            //        {
                            //            if (setbkp.Wrote > 0)
                            //            {
                            //                log("Счётные регистры успешно установлены!");
                            //            }
                            //            else
                            //            {
                            //                log("Не удалось установить счётные регистры (нет доступа?)");
                            //            }
                            //        }
                            //        else
                            //        {
                            //            log(string.Format("Не удалось установить счётные регистры: {0}", setbkp.error));
                            //        }
                            //    }
                            //    else
                            //    {
                            //        var setbkp = Send(MakeWriteBkpRequest(DateTime.MinValue, devid, newValues.ToArray()));
                            //        if (setbkp.success)
                            //        {
                            //            log("Счётные регистры успешно установлены!");
                            //        }
                            //        else
                            //        {
                            //            log(string.Format("Не удалось установить счётные регистры: {0}", setbkp.error));
                            //        }

                            //    }
                            //}
                            #endregion
                        }
                    }
                }
                #endregion

                ////

                do
                {
                    if (getEndDate == null)
                    {
                        getEndDate = (type) => date;
                    }

                    if (components.Contains("Current"))
                    {
                        DateTime dateNow = DateTime.Now;
                        var current = GetCurrent(date, dateNow, devid, counters, digitals, parameterConfiguration);
                        if (!current.success)
                        {
                            log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                            return MakeResult(102, current.errorcode, current.error);
                        }
                        log(string.Format("All:"), level: 1);
                        records(current.records);
                        List<dynamic> currents = current.records;
                        log(string.Format("Текущие на {0} прочитаны: {1}; {2}; {3}",
                            current.date,
                            current.values != "" ? string.Format("показания - {0}", current.values) : string.Format("количество импульсов - {0}", current.counters),
                            current.inputs,
                            current.adc != "" ? string.Format("значения АЦП - {0}", current.adc) : ""), level: 1);
                        
                        //log(string.Format("Текущие на {0} прочитаны: {1}; состояния входов: {2}", current.date, current.counters, current.inputs));
                        //log(string.Format("Текущие на {0} прочитаны: {1}; состояния входов: {2}; значения АЦП: {3}", current.date, current.counters, current.inputs, current.adc));

                    }
                    if (components.Contains("Day"))
                    {
                        DateTime dtNow = DateTime.Now;
                        DateTime dtDay = new DateTime(dtNow.Year, dtNow.Month, (dtNow.Hour > 5) ? dtNow.AddDays(1).Day : dtNow.Day, 0, 0, 0);
                        var current = GetDay(dtDay, devid, counters, digitals, parameterConfiguration);
                        if (!current.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", current.error), level: 1);
                            return MakeResult(102, current.errorcode, current.error);
                        }
                        records(current.records);
                        List<dynamic> currents = current.records;

                        log(string.Format("Суточные на {0} прочитаны: {1}; {2}; {3}",
                            current.date,
                            current.values != "" ? string.Format("показания - {0}", current.values) : string.Format("количество импульсов - {0}", current.counters),
                            current.inputs,
                            current.adc != "" ? string.Format("значения АЦП - {0}", current.adc) : ""), level: 1);

                        //log(string.Format("Текущие на {0} прочитаны: {1}; состояния входов: {2}", current.date, current.counters, current.inputs));
                        //log(string.Format("Текущие на {0} прочитаны: {1}; состояния входов: {2}; значения АЦП: {3}", current.date, current.counters, current.inputs, current.adc));

                    }
                    //////

                    if (components.Contains("Constant"))
                    {
                        var constant = GetConstant(date, flashver);
                        if (!constant.success)
                        {
                            log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                            return MakeResult(103, constant.errorcode, constant.error);
                        }

                        records(constant.records);
                        List<dynamic> constants = constant.records;
                        log(string.Format("Константы прочитаны: всего {0}; {1}", constants.Count, constant.text), level: 1);
                    }


                    ////чтение часовых
                    if (components.Contains("Hour"))
                    {
                        if (isRtcEnabled)
                        {
                            var endH = getEndDate("Hour");
                            var startH = getStartDate("Hour");

                            if (DateTime.Compare(endH, startDate) < 0)
                            {
                                log(string.Format("Внимание: дата пусконаладки установлена {0:dd.MM.yyyy}, часовые за период {1:dd.MM.yyyy HH:mm}-{2:dd.MM.yyyy HH:mm} опрошены не будут", startDate, startH, endH), level: 1);
                            }
                            else
                            {
                                if (DateTime.Compare(startH, startDate) < 0)
                                {
                                    startH = startDate;
                                    log(string.Format("Внимание: дата пусконаладки установлена {0:dd.MM.yyyy}, новый период опроса часовых {1:dd.MM.yyyy HH:mm}-{2:dd.MM.yyyy HH:mm}", startDate, startH, endH), level: 1);
                                }

                                var hours = new List<dynamic>();

                                var hour = GetHours(startH, endH, date, counters, devid, parameterConfiguration, rtcResetDate);
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
                            DateTime hourlyDate = date.Date.AddHours(date.Hour + (date.Minute >= hourlyStart ? 1 : 0));
                            DateTime dateNow = DateTime.Now;
                            var hourly = GetCurrent(hourlyDate, dateNow, devid, counters, digitals, parameterConfiguration);
                            if (!hourly.success)
                            {
                                log(string.Format("Ошибка при считывании часовых: {0}", hourly.error), level: 1);
                                return MakeResult(102, hourly.errorcode, hourly.error);
                            }

                            List<dynamic> recs = hourly.records;
                            foreach (dynamic rec in recs)
                            {
                                rec.type = "Hour";
                                rec.dt2 = date;
                                rec.d2 = 102;
                            }
                            records(recs);
                            log(string.Format("Прочитаны часовые за {0:dd.MM.yyyy HH:mm}: {1} записей", hourlyDate, recs.Count), level: 1);
                        }


                    }

                    ////чтение суточных
                    //var startD = getStartDate("Day");
                    //var endD = getEndDate("Day");

                    //var day = GetDays(startD, endD, current.date, constant.variant);
                    //if (!day.success)
                    //{
                    //    log(string.Format("Ошибка при считывании суточных: {0}", day.error));
                    //    return MakeResult(104, day.error);
                    //}
                    //List<dynamic> days = day.records;
                    //log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count));

                    ///// Нештатные ситуации ///
                    //var lastAbnormal = getLastTime("Abnormal");
                    //DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
                    //DateTime endAbnormal = current.date;

                    if (components.Contains("Abnormal"))
                    {
                        var startAe = DateTime.Compare(getStartDate("Abnormal"), startDate) > 0 ? getStartDate("Abnormal") : startDate;
                        var endAe = getEndDate("Abnormal");
                        var abnormal = GetAbnormals(10, startAe);//startAbnormal, endAbnormal);
                        if (!abnormal.success)
                        {
                            log(string.Format("ошибка при считывании НС: {0}", abnormal.error), level: 1);
                            return MakeResult(106, abnormal.errorcode, abnormal.error);
                        }
                    }

                    if (cancel())
                    {
                        infiniteMode = false;
                    }

                    if (infiniteMode && isRtcEnabled)
                    {
                        HourlyFlush();
                        dynamic time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                        if (!time.success)
                        {
                            return MakeResult(101, time.errorcode, time.error);
                        }
                    }
                }
                while (infiniteMode);
            }
            

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        //private dynamic Current()
        //{
        //    var current = GetCurrent();
        //    if (!current.success)
        //    {
        //        log(string.Format("Ошибка при считывании текущих: {0}", current.error));
        //        return MakeResult(102, current.error);
        //    }

        //    records(current.records);
        //    List<dynamic> currents = current.records;
        //    log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count));
        //    return MakeResult(0);
        //}

        #endregion

        private const ushort AEID_PINRST = 0x01;
        private const ushort AEID_PORRST = 0x02;
        private const ushort AEID_SFTRST = 0x04;
        private const ushort AEID_IWDGRST = 0x08;
        private const ushort AEID_WWDGRST = 0x10;
        private const ushort AEID_LPWRRST = 0x20;

        private string Abnormal_GetDescriptionById(UInt32 id)
        {
            string description;

            var type = (UInt16)(id >> 16);
            var kind = (UInt16)(id & 0xFFFF);

            if (type == 0x0000)
            {
                description = "Перезагрузка: ";

                if ((kind & AEID_PINRST) > 0)
                {
                    description += "Нажатие RESET; ";
                }
                if ((kind & AEID_PORRST) > 0)
                {
                    description += "Срабатывание POR/PDR; ";
                }
                if ((kind & AEID_SFTRST) > 0)
                {
                    description += "Программная перезагрузка; ";
                }
                if ((kind & AEID_IWDGRST) > 0)
                {
                    description += "Независимый WatchDog; ";
                }
                if ((kind & AEID_WWDGRST) > 0)
                {
                    description += "Оконный WatchDog; ";
                }
                if ((kind & AEID_LPWRRST) > 0)
                {
                    description += "Сброс при пониженном питании; ";
                }
                if (kind == 0)
                {
                    description += "Старт программы";
                }
            }
            else if (type == 0x0001)
            {
                if (kind == 0)
                {
                    description = "Изменение даты и установка счётных регистров";
                }
                else
                {
                    description = "Изменение даты: ";
                    if ((kind & 0x4000) > 0)
                    {
                        description += "Установка счётных регистров; ";
                    }
                    if ((kind & 0x1FFF) == 0x1FFF)
                    {
                        description += "Установка времени";
                    }
                    else
                    {
                        description += string.Format("Корректировка времени на {0}{1} сек.", (kind & 0x2000) > 0 ? "-" : "+", (kind & 0x1FFF));
                    }
                }
            }
            else if (type == 0x0002)
            {
                description = "Период отключения: ";
                if (kind == 0xFFFF)
                {
                    description += "часы были сброшены";
                }
                else
                {
                    description += string.Format("{0} минут", kind);
                }
            }
            else if (type == 0x0003)
            {
                description = "Режим изменён на ";
                if (kind == 0)
                {
                    description += "сервисный";
                }
                else
                {
                    description += "рабочий";
                }
            }
            else if (type == 0x8000)
            {
                description = "Системная ошибка: ";
                switch (kind)
                {
                    case 0: //AEID_HARD_FAULT
                        description += "HARD_FAULT";
                        break;
                    case 1: //AEID_MEM_MANAGE
                        description += "MEM_MANAGE";
                        break;
                    case 2: //AEID_BUS_FAULT
                        description += "BUS_FAULT";
                        break;
                    case 3: //AEID_USAGE_FAULT
                        description += "USAGE_FAULT";
                        break;
                    default:
                        description += string.Format("Неизвестно (#{0})", kind);
                        break;
                }
            }
            else
            {
                description = string.Format("Нераспознанное событие 0x{0:X8}", id);
            }

            return description;
        }
    }
}

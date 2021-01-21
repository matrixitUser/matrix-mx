// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Threading;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.EK270
{
    /// <summary>
    /// Драйвер для ЕК270
    /// </summary>
    public partial class Driver
    {
        /// <summary>
        /// число попыток опроса в случае неуспешного запроса
        /// </summary>
        private const int TRY_COUNT = 3;

        //private const int SEND_ATTEMPTS_COUNT = 6;
        private const int TIMEOUT_TIME = 15000;
        private const int SLEEP_TIME = 100;
        private const int COLLECT_MUL = 40;

#if OLD_DRIVER
        bool debugMode = false;
#endif

        private bool testMode = false;
        private bool nezhinka = false;
        private byte networkAddress = 0;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            var initMessage = false;

            #region Команды драйверу
            if (param.ContainsKey("command") && (arg.command is string))
            {
                string commandText = (string)(arg.command).Trim();
                if (commandText != "")
                {
                    log(string.Format("Введена команда: {0}", commandText));
                    var commands = commandText.Split(' ');

                    foreach (var command in commands)
                    {
                        var c = command.Split('=');
                        switch (c[0])
                        {
#if OLD_DRIVER
                        case "debug":
                            if ((c.Length == 2) && (c[1] == "1"))
                            {
                                debugMode = true;
                            }
                            break;
#endif

                            case "init":
                                if ((c.Length == 2) && (c[1] == "1"))
                                {
                                    initMessage = true;
                                }
                                break;

                            case "nezhinka":
                                if (c.Length == 1 || ((c.Length == 2) && (c[1] == "1")))
                                {
                                    nezhinka = true;
                                }
                                break;

                            case "test":
                                if ((c.Length == 2) && (c[1] == "1"))
                                {
                                    testMode = true;
                                }
                                break;
                        }
                    }
                }
            }
            #endregion
            #region Входные параметры
            byte na = 0x00;
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(param["networkAddress"].ToString(), out networkAddress))
            {
                log(string.Format("отсутствуют сведения о сетевом адресе, принят по-умолчанию {0}", networkAddress));
            }

            string sna = networkAddress == 0 ? "" : na.ToString();


            byte channel = 0x01;
            if (!param.ContainsKey("channel") || !byte.TryParse(param["channel"].ToString(), out channel))
                log(string.Format("отсутствуют сведения о канале, принят по-умолчанию {0}", channel));

            string password = "";
            if (!param.ContainsKey("password"))
            {
                log("отсутствуют сведения о пароле, принят по-умолчанию, в зависимости от устройства");
            }
            else
            {
                password = param["password"].ToString();
            }

            bool? isConsumer = null;
            byte passType;
            if (param.ContainsKey("passType") && byte.TryParse(param["passType"].ToString(), out passType))
            {
                if (passType == 0)
                {
                    log("авторизация в качестве потребителя");
                    isConsumer = true;
                }
                else if (passType == 1)
                {
                    log("авторизация в качестве поставщика");
                    isConsumer = false;
                }
            }

            int speed = -1;
            if (param.ContainsKey("speed") && int.TryParse(arg.speed, out speed))
            {
                log(string.Format("скорость {0}", speed));
            }
            else
            {
                log(string.Format("скорость не указана, будет выбрана автоматически"));
            }
            #endregion
            #region Параметры опроса
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

            switch (what.ToLower())
            {
                case "all": return Wrap(() => All(channel, components, isConsumer, hourRanges, dayRanges), sna, password, isConsumer, initMessage, speed);
                default:
                    {
                        log(string.Format("неопознаная команда {0}", what), level: 1);
                        return MakeResult(201, what);
                    }
            }
        }

        private dynamic MakeResult(int code, string description = "", dynamic info = null)
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            if (info != null && (info is IDictionary<string, object>) && (info as IDictionary<string, object>).ContainsKey("badChannel") && (info.badChannel is bool) && ((bool)info.badChannel == true))
            {
                res.code = 310;
            }

            if (((description == null) || (description == "")) && (info != null) && (info is IDictionary<string, object>) && (info as IDictionary<string, object>).ContainsKey("error") && (info.error is string))
            {
                res.description = info.error;
            }
            else
            {
                res.description = description;
            }
            return res;
        }

        // Modbus

        enum ModbusExceptionCode : byte
        {
            ILLEGAL_FUNCTION = 0x01,
            ILLEGAL_DATA_ADDRESS = 0x02,
            ILLEGAL_DATA_VALUE = 0x03,
            SLAVE_DEVICE_FAILURE = 0x04,
            ACKNOWLEDGE = 0x05,
            SLAVE_DEVICE_BUSY = 0x06,
            MEMORY_PARITY_ERROR = 0x07,
            GATEWAY_PATH_UNAVAILABLE = 0x0a,
            GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
        }

        byte[] MakeModbusRequest3(UInt16 register, UInt16 count)
        {

            var data = new List<byte>();
            data.Add(networkAddress);
            data.Add(0x03);

            data.Add(GetHighByte(register));
            data.Add(GetLowByte(register));
            data.Add(GetHighByte(count));
            data.Add(GetLowByte(count));

            var crc = Crc.Calc(data.ToArray(), new Crc16Modbus());
            data.Add(crc.CrcData[0]);
            data.Add(crc.CrcData[1]);

            return data.ToArray();
        }

        dynamic ParseModbusDateResponse(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "";

            if (data.Length < 5)
            {
                answer.error = "в кадре ответа не может содежаться менее 5 байт";
                return answer;
            }

            if (!Crc.Check(data, new Crc16Modbus()))
            {
                answer.error = "контрольная сумма кадра не сошлась";
                return answer;
            }

            answer.NetworkAddress = data[0];
            answer.Function = data[1];

            //modbus error
            if (answer.Function > 0x80)
            {
                var exceptionCode = (ModbusExceptionCode)data[2];
                answer.error = string.Format("устройство вернуло ошибку: {0}", exceptionCode);
                return answer;
            }

            answer.success = true;

            answer.Body = data.Skip(2).Take(2 + 2).ToArray();

            var year1 = ToBCD(answer.Body[1]);
            var year2 = ToBCD(answer.Body[2]);
            var year = year1 * 100 + year2;

            var month = ToBCD(answer.Body[3]);
            var day = ToBCD(answer.Body[4]);
            var hour = ToBCD(answer.Body[5]);
            var minute = ToBCD(answer.Body[6]);

            answer.Date = new DateTime(year, month, day, hour, 0, 0);
            return answer;
        }

        static byte GetLowByte(UInt16 b)
        {
            return (byte)(b & 0xFF);
        }

        static byte GetHighByte(UInt16 b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        static byte ToBCD(byte sourceByte)
        {
            byte left = (byte)(sourceByte >> 4 & 0xf);
            byte right = (byte)(sourceByte & 0xf);
            return (byte)(left * 10 + right);
        }

        //// УРОВЕНЬ 1 ////

        private dynamic Wrap(Func<dynamic> act, string na, string password, bool? isConsumer, bool initMessage, int speed)
        {
            dynamic result = MakeResult(0);
            try
            {
                var session = GetSession(na, password, isConsumer, speed);
                if (!session.success)
                {
                    log(string.Format("сессия не получена, ошибка: {0}", session.error), level: 1);
                    SendInstant(MakeSessionByeRequest(), true);

                    var dateResponse = ParseModbusDateResponse(Send(MakeModbusRequest3(0x032c, 4)));
                    if (dateResponse.success == true)
                    {
                        log(string.Format("обнаружен Modbus-протокол, дата/время на устройстве: {0}", dateResponse.Date));
                    }

                    return MakeResult(201, info: session);
                }

                result = act();
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(999);
            }
            SendInstant(MakeSessionByeRequest(), true);

            log("опрос завершен");
            return result;
        }

        private dynamic All(byte ch, string components, bool? isConsumer, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            #region текущие

            var currDate = DateTime.Now;
            if (components.Contains("Current"))
            {
                dynamic current = null;
                log("начато чтение текущих значений");
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    current = GetCurrent(isConsumer);
                    if (current.success) break;
                }

                if (!current.success)
                {
                    log(string.Format("ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, info: current);
                }
                log(string.Format("текущие параметры получены, текущая дата регистратора: {0:dd.MM.yy HH:mm:ss}", current.date), level: 1);
                currDate = current.date;
                records(current.records);
            }
            else
            {
                //short version (datetime only)
                var time = ParseResponse(Send(MakeRequest(RequestType.Read, "1:0400.0", "1")));


                if (!time.success)
                {
                    if ((time as IDictionary<string, object>).ContainsKey("errorCode") && time.errorCode == 18)
                    {
                        OpenConsumerCastle();
                    }
                }
                currDate = ParseDate(time.Values[0]);
                log(string.Format("дата на приборе {0:dd.MM.yyyy HH:mm:ss}", currDate), level: 1);
            }

            if (getEndDate == null)
            {
                getEndDate = (type) => currDate;
            }

            DateTime currentDate = currDate;
            setTimeDifference(DateTime.Now - currentDate);

            #endregion

            #region Константы

            ///необходимо заново прочесть константы
            var needRead = true;
            dynamic constant = new ExpandoObject();
            int contractHour = getContractHour();
            constant.period = "";

            if (components.Contains("Constant"))
            {
                log("начато чтение констант");
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    constant = GetConstant(currDate, true);
                    if (constant.success) break;

                }
                if (!constant.success)
                {
                    log(string.Format("ошибка при считывании констант: {0}", constant.error), level: 1);
                    return MakeResult(103, info: constant);
                }

                contractHour = constant.contractHour;
                setContractHour(contractHour);

                log(string.Format("константы прочитаны: тип прибора={0}; версия ПО={1}; серийный номер: {2}; расчетный час={3}",
                    constant.devType,
                    constant.version,
                    constant.serial,
                    contractHour), level: 1);

                records(constant.records);


                //contractHour = constant.contractHour;
                //        setContractHour(contractHour);

            }
            else
            {
                constant = SmallConst();
            }

            contractHour = constant.contractHour;
            log("CH=" + contractHour + "; period=" + constant.period);

            #endregion

            #region Архивы

            bool supportDays = false;
            bool supportHours = false;

            switch ((DevType)constant.devType)
            {
                case DevType.EK260:
                    {
                        if (constant.version < 3) supportHours = true;
                        else { supportDays = true; supportHours = true; }
                        break;
                    }
                case DevType.EK270: { supportDays = true; supportHours = true; break; }
                case DevType.TC210: { supportHours = true; break; }
                case DevType.TC215:
                    {
                        if (constant.period == "1D") supportDays = true;
                        else supportHours = true;
                        break;
                    }
                case DevType.TC220: { supportHours = true; break; }
                default: break;
            }

            if (!supportHours)
            {
                setArchiveDepth("Hour", 0);
            }

            if (!supportHours && !supportDays)
            {
                log(string.Format("данный тип устройства ({0}) не поддерживает ни часовые, ни суточные архивы", constant.devType), level: 1);
                return MakeResult(104);
            }

            dynamic day = new ExpandoObject();
            day.emptyDays = new List<DateTime>();
            if (supportDays && components.Contains("Day"))
            {
                if (dayRanges == null)
                {
                    dayRanges = new List<dynamic>();
                    dynamic defaultRange = new ExpandoObject();
                    defaultRange.start = getStartDate("Day").Date;
                    defaultRange.end = getEndDate("Day").Date;
                }

                foreach (var range in dayRanges)
                {
                    DateTime startDay = range.start;
                    DateTime endDay = range.end;

                    bool flg = true;
                    if (constant.devType == DevType.TC215) flg = false;

                    if (endDay > currDate.AddHours(-constant.contractHour).Date)
                    {
                        endDay = currDate.AddHours(-constant.contractHour).Date;
                    }

                    if (nezhinka)
                    {
                        day = GetDaysNezhinka(startDay, endDay, constant.contractHour, constant.devType, constant.version, flg);
                    }
                    else
                    {
                        day = GetDays(startDay, endDay, constant.contractHour, constant.devType, constant.version, flg);
                    }

                    if (!day.success)
                    {
                        log(string.Format("ошибка при считывании суточных: {0}", day.error), level: 1);
                        return MakeResult(104, info: day);
                    }
                    log(string.Format("прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startDay, endDay, day.records.Count), level: 1);
                }
            }

            if ((supportHours && components.Contains("Hour")) || (!supportDays && components.Contains("Day")))
            {
                var hstart = getStartDate("Hour");
                var hend = getEndDate("Hour");

                var startHour = new DateTime(hstart.Year, hstart.Month, hstart.Day, hstart.Hour, 0, 0);
                var endHour = new DateTime(hend.Year, hend.Month, hend.Day, hend.Hour, 0, 0);

                if (endHour > currDate)
                    endHour = new DateTime(currDate.Year, currDate.Month, currDate.Day, currDate.Hour, 0, 0);
                
                if (!supportDays && components.Contains("Day"))
                {
                    var startHourContract = startHour.Date.AddHours(contractHour);
                    if (startHourContract > startHour)
                    {
                        startHourContract = startHourContract.AddDays(-1);
                    }
                    startHour = startHourContract;

                    var endHourContract = endHour.Date.AddHours(contractHour);
                    if (endHourContract < endHour)
                    {
                        endHourContract = endHourContract.AddDays(1);
                    }
                    endHour = endHourContract;
                    if (endHour > currDate)
                    {
                        endHour = new DateTime(currDate.Year, currDate.Month, currDate.Day, currDate.Hour, 0, 0);
                    }

                    log(string.Format("суточный архив не поддерживается, для формирования записей будут опрошены часы с {0:dd.MM.yyyy HH:mm} до {1:dd.MM.yyyy HH:mm}", startHour, endHour));
                }

                List<dynamic> hours = new List<dynamic>();
                for (DateTime date = startHour; date < endHour; date = date.AddHours(1))
                {
                    dynamic hour = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);

                        var dt = date.AddHours(1);
                        hour = GetArchiveRecord(dt, constant.devType, constant.version);
                        if (hour.success) break;
                    }

                    if (!hour.success)
                    {
                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", date, hour.error), level: 1);
                        return MakeResult(105, info: hour);
                    }

                    log(string.Format("часовая запись {0:dd.MM.yy HH:mm} получена", date));
                    records(hour.records);
                    hours.AddRange(hour.records);

                    if (!supportDays && date.AddHours(1).Hour == contractHour)
                    {
                        records(CalcDay(hours, contractHour, date.AddDays(-1)));
                        //if (!supportDays || day.emptyDays.Contains(date.AddDays(-1)))
                        //    records(CalcDay(hours, constant.contractHour, date.AddDays(-1)));
                    }
                }
            }

            #endregion

            #region НС
            if (components.Contains("Abnormal"))
            {
                DateTime startAbnormal = getStartDate("Abnormal");
                DateTime endAbnormal = getEndDate("Abnormal");
                if (endAbnormal > currDate)
                {
                    endAbnormal = currDate;
                }

                var abnormal = GetAbnormals(startAbnormal, endAbnormal);
                if (!abnormal.success)
                {
                    log(string.Format("ошибка при считывании НС: {0}", abnormal.error));
                    return MakeResult(106, info: abnormal);
                }
            }
            #endregion

            return MakeResult(0);
        }

        /// <summary>
        /// попытка чтения необходимых констант из локальной базы
        /// </summary>
        /// <returns></returns>
        private dynamic ReadConstantsFromDB()
        {
            dynamic constant = new ExpandoObject();
            constant.needRead = true;
            constant.period = "";

            var constants = getLastRecords("Constant");

            if (constants == null || !constants.Any()) return constant;


            constant.devType = DevType.Unknown;

            var c1 = constants.FirstOrDefault(c => c.s1 == "Тип устройства");
            if (c1 != null)
            {
                var value = c1.s2;
                if (value.Contains("EK260")) constant.devType = DevType.EK260;
                if (value.Contains("EK270")) constant.devType = DevType.EK270;
                if (value.Contains("TC210")) constant.devType = DevType.TC210;
                if (value.Contains("TC215")) constant.devType = DevType.TC215;
                if (value.Contains("TC220")) constant.devType = DevType.TC220;
            }
            else
                return constant;

            var c2 = constants.FirstOrDefault(c => c.s1 == "Версия ПО");
            if (c2 != null)
            {
                var value = c2.s2;
                float f;
                float.TryParse(value.Replace('.', ','), out f);
                constant.version = f;
            }
            else
                return constant;

            var c3 = constants.FirstOrDefault(c => c.s1 == "Граница дня (начало газового дня) 2 (час)");
            if (c3 != null)
            {
                var value = constants.First(c => c.s1 == "Граница дня (начало газового дня) 2 (час)").s2;
                int i;
                int.TryParse(value, out i);
                constant.contractHour = i;
            }
            else
                return constant;

            var c4 = constants.FirstOrDefault(c => c.s1 == "Период архивации");
            if (c4 != null)
            {
                constant.period = c4.s2;
            }
            else
            {
                if (constant.devType == DevType.TC215)
                    return constant;
            }
            constant.needRead = false;
            return constant;
        }

        public static byte Hex2Dec(string input)
        {
            byte result = 0;
            result = byte.Parse(input, System.Globalization.NumberStyles.HexNumber);
            return result;
        }

        public static long Hex2DecL(string input)
        {
            long result = 0;
            result = long.Parse(input, System.Globalization.NumberStyles.HexNumber);
            return result;
        }
    }
}

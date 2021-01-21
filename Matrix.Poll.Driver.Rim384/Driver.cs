using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Threading;


namespace Matrix.Poll.Driver.Rim384
{
    /// <summary>
    /// Драйвер для электросчетчика Milur107 и Milur307
    /// </summary>
    public partial class Driver
    {
        #region GlobalField
        string ModelVersion = string.Empty;
        string SoftwareVersion = string.Empty;
        string SerialNumber = string.Empty;
        UInt32 currentTarif = 0;
        UInt32 maxNumberOfTarifs = 0;
        UInt32? NetworkAddress = null;
        bool electricalLoad = true;
        bool electricalLoadSoft = true;
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;
        List<dynamic> listJournalOnOff = new List<dynamic>();

        const string hourP = "Активная энергия";
        const string hourQ = "Реактивная энергия";

        const string dayP = "EnergyP+ (сумма тарифов)";
        const string dayPexp = "EnergyP- (сумма тарифов)";
        const string dayQ = "EnergyQ+ (сумма тарифов)";
        const string dayQexp = "EnergyQ- (сумма тарифов)";

        string[] dayArrP = new string[] { "EnergyP+ (тариф 1)", "EnergyP+ (тариф 2)", "EnergyP+ (тариф 3)", "EnergyP+ (тариф 4)"};
        string[] dayArrPexp = new string[] { "EnergyP- (тариф 1)", "EnergyP- (тариф 2)", "EnergyP- (тариф 3)", "EnergyP- (тариф 4)" };
        string[] dayArrQ = new string[] { "EnergyQ+ (тариф 1)", "EnergyQ+ (тариф 2)", "EnergyQ+ (тариф 3)", "EnergyQ+ (тариф 4)" };
        string[] dayArrQexp = new string[] { "EnergyQ- (тариф 1)", "EnergyQ- (тариф 2)", "EnergyQ- (тариф 3)", "EnergyQ- (тариф 4)" };
        #endregion

        #region GetNaPacket, MakePackage, AOpen
        private byte[] GetNaPacket()
        {
            if (NetworkAddress == null)
            {
                return new byte[0];
            }
           
            return BitConverter.GetBytes(NetworkAddress.Value);
        }
        private byte[] MakePackage(byte cmd1, byte cmd2, byte index, byte[] data = null)
        {
            var result = new List<byte>();

            result.AddRange(GetNaPacket());
            result.Add(cmd1);
            if (cmd2 != 0xff)
            {
                result.Add(cmd2);
            }
            if (index != 0xff)
            {
                result.Add(index);
            }
            if (data != null)
            {
                result.AddRange(data);
            }
            var crc = new Crc16Modbus();
            result.AddRange(crc.Calculate(result.ToArray(), 0, result.Count).CrcData);

            return result.ToArray();
        }

        private bool AOpen()
        {
            byte[] psw = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            dynamic dt = Send(MakePackage(0x08, 0x01, 0xff, psw), 0x08, 0x02);
            if (!dt.success)
            {
                log("Сеанс связи не открылся!");
                return false;
            }
            
            log("Сеанс связи открыт!");
            return true;
        }
        #endregion

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
            logger(message, level);
        }

        private byte[] SendSimple(byte[] data, int timeout = 10000, int waitCollectedMax = 2)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);
            
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) >= 0 && !isCollected)
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
                        if (waitCollected == waitCollectedMax)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, byte cmd, int attempts = 3)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempt = 0; (attempt < attempts) && (answer.success == false); attempt++)
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
                        answer.error = "в кадре ответа не может содежаться менее 6 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if ((buffer[0] != data[0]) || (buffer[1] != data[1]) || (buffer[2] != data[2]) || (buffer[3] != data[3]))
                    {
                        log("Несовпадение сетевого адреса", level: 1);
                        answer.error = "Несовпадение сетевого адреса";
                        answer.errorcode = DeviceError.ADDRESS_ERROR;
                    }
                    else if (cmd != buffer[4])
                    {
                        answer.error = "Несовпадение команды";
                        answer.errorcode = DeviceError.ADDRESS_ERROR;
                    }
                    else
                    {
                        do
                        {
                            if (Crc.Check(buffer, new Crc16Modbus())) break;
                            buffer = buffer.Take(buffer.Length - 1).ToArray();
                        }
                        while (buffer.Length > 3);

                        if (!Crc.Check(buffer, new Crc16Modbus()))
                        {
                            answer.error = "контрольная сумма кадра не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;
                        }
                        else
                        {
                            answer.success = true;
                            answer.error = string.Empty;
                            answer.errorcode = DeviceError.NO_ERROR;
                        }
                    }
                }
            }

            if (answer.success)
            {
                answer.Body = buffer.Take(buffer.Length - 2).Skip(7).ToArray();
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
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion

        #region ImportExport

        [Import("logger")]
        private Action<string, int> logger;

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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            ulong na = 0;
            if (!param.ContainsKey("networkAddress") || !ulong.TryParse(arg.networkAddress.ToString(), out na))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            else
            {
                string netWorkAddressS = na.ToString();

                int startIndex = netWorkAddressS.Length > 6 ? netWorkAddressS.Length - 6 : 0;
                netWorkAddressS = netWorkAddressS.Substring(startIndex);

                NetworkAddress = UInt32.Parse(netWorkAddressS);
            }

            if (!param.ContainsKey("electricalLoad") || !UInt32.TryParse(arg.electricalLoad.ToString(), out UInt32 el))
            {
                log("Отсутствуют сведения о подключении нагрузки");
            }
            else
            {
                if(na == 0)
                {
                    electricalLoad = true;
                }
                else
                {
                    electricalLoad = false;
                }
            }

            //if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            //{
            //    log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            //}

            //if (!param.ContainsKey("password"))
            //{
            //    log("Отсутствуют сведения о пароле, принят по-умолчанию");
            //}
            //else
            //{
            //    password = arg.password;
            //}
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


            //if (param.ContainsKey("start") && arg.start is DateTime)
            //{
            //    getStartDate = (type) => (DateTime)arg.start;
            //    log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            //}
            //else
            //{
            //    getStartDate = (type) => getLastTime(type);
            //    log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            //}

            //if (param.ContainsKey("end") && arg.end is DateTime)
            //{
            //    getEndDate = (type) => (DateTime)arg.end;
            //    log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            //}
            //else
            //{
            //    getEndDate = null;
            //    log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            //}

            dynamic result = new ExpandoObject();

            if (!AOpen())
            {
                result.code = 201;
                result.description = DeviceError.NO_ANSWER;
                result.success = false;
                return result;
            }
            GetModelVersion();
            GetSoftwareVersion();
            
            GetSerialNumber();
            //GetMaxNumberOfTarifs();
            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = All(components, hourRanges, dayRanges);
                        }
                        break;
                 
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }
        #endregion

        #region Интерфейс

        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            DateTime date  = GetCurrentTime();

            if (components.Contains("Current"))
            {
                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");
                var current = GetCurrent();
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", DateTime.Now, currents.Count), level: 1);
            }

            ////

            if (components.Contains("Constant"))
            {
                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");
                var current = GetConstant(DateTime.Now); 
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Константы на {0} прочитаны: всего {1}", DateTime.Now, currents.Count), level: 1);
                
            }

            //if (getStartDate == null)
            //{
            //    DateTime dateStart = DateTime.Now.AddDays(-2);
            //    getStartDate = (type) => dateStart;
            //}

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }


            //var startD = getStartDate("Day");
            //var endD = getEndDate("Day");

            //////чтение часовых
            if (components.Contains("Hour"))
            {
                //GetJournal();

                if (hourRanges != null)
                {
                    foreach (var range in hourRanges)
                    {
                        var startH = range.start;
                        var endH = range.end;
                        var hours = new List<dynamic>();

                        if (startH > date) continue;
                        if (endH > date) endH = date;

                        var hour = GetHours(startH, endH);
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
                    var hours = GetHours(startH, endH);

                    if (!hours.success)
                    {
                        log(string.Format("Ошибка при считывании часовых: {0}", hours.error), level: 1);
                        //return MakeResult(102, hours.errorcode, hours.error);
                    }
                    else
                    {
                        records(hours.records);
                        List<dynamic> currents = hours.records;
                        log(string.Format("часовые прочитаны: всего {0}", currents.Count), level: 1);
                    }
                    
                }
            }

            ////чтение суточных
            if (components.Contains("Day"))
            {
                if(dayRanges != null)
                {
                    foreach (var range in dayRanges)
                    {
                        var startD = range.start;
                        var endD = range.end;

                        if (startD > date) continue;
                        if (endD > date) endD = date;

                        var day = GetDays(startD, endD);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.errorcode, day.error);
                        }
                        records(day.records);
                        List<dynamic> days = day.records;
                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                else
                {
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    var days = GetDays(startD, endD);
                    if (!days.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", days.error), level: 1);
                        return MakeResult(102, days.errorcode, days.error);
                    }
                    records(days.records);
                    List<dynamic> currents = days.records;
                    log(string.Format($"суточные прочитаны: всего {currents.Count}"), level: 1);
                }

               

                
            }

            //НС
            if (components.Contains("abnormal"))
            {
            }

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

    
        #endregion

        #region Convert
        private static int ConvertFromBcd(byte bcd)
        {
            return ConvertFromBcd(new byte[] { (byte)((bcd >> 4) | ((bcd & 0x0F) << 4))}, 0, 1);
        }
        private static int ConvertFromBcd(byte[] bcd, int startIndex, int length)
        {
            if (bcd == null || startIndex < 0 || length <= 0 || startIndex >= bcd.Length || startIndex + length > bcd.Length)
                return 0;

            string str = string.Empty;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                str += bcd[i].ToString("X");
            }
            int result = 0;
            if (int.TryParse(str, out result))
            {
                return result;
            }
            return 0;
        }
        private byte ConvertToBcd(int value)
        {
            var valStr = value.ToString();
            return Convert.ToByte(valStr, 16);
            //byte result;
            //byte.TryParse("0x" + valStr, out result);
            //return result;
        }
        #endregion
    }
}

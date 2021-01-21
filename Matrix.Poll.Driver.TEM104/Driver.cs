using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.TEM104
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        byte NetworkAddress = 0;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;


        private T2K t2k;

        public T2K T2k
        {
            get { return t2k ?? (t2k = T2KRead()); }
        }

        private const string DriverVersion = "TEM-104";
        private const string DriverVersion1 = "TSM-104";

        public enum ChecksumType { Normal = 0, Complement1, Complement2 }

        enum ArchiveType
        {
            Hourly = 0,
            Daily,
            Monthly,
        }

        private readonly List<ChecksumType> _getCheckSumType = new List<ChecksumType>() { ChecksumType.Complement1 };

        private byte CheckSum(byte[] buff, int length, ChecksumType type)
        {
            byte CRC1 = 0;
            for (var i = 0; i < length; i++)
            {
                CRC1 += buff[i];
            }
            if (type != ChecksumType.Normal) CRC1 = (byte)(~CRC1);
            if (type == ChecksumType.Complement2) CRC1++;
            return CRC1;
        }

        public static byte IntToBCD(int toBCD)
        {
            byte result = 0xFF;
            if (toBCD < 100)
            {
                result = 0;
                result |= (byte)(toBCD % 10);
                toBCD /= 10;
                result |= (byte)((toBCD % 10) << 4);
            }
            return result;
        }


        private T2K T2KRead()
        {
            var answer = new byte[2048];
            for (int i = 0; i < 32; i++)
            {
                var curAddr = i * 0x40;
                var cmd = 0x0f01;
                var curanswer = Send(MakeBaseRequest(cmd, new byte[] { ConvertHelper.ByteHigh(curAddr), ConvertHelper.ByteLow(curAddr), 0x40 }), cmd);
                if (!curanswer.success || (curanswer.Body as byte[]).Length != 65)
                {
                    return null;
                }
                Array.Copy((curanswer.Body as byte[]), 1, answer, 64 * i, 64);
            }
            return T2K.Parse(answer, 0);
        }




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

        private byte[] SendSimple(byte[] data, int timeoutMaximum = 7500)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var timeout = timeoutMaximum;
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

        private dynamic Send(byte[] data, int timeOut = 3333, int attemptsMaximum = 5)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < attemptsMaximum && answer.success == false; attempts++)
            {
                buffer = SendSimple(data, timeOut);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 7)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 7 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (buffer[0] != 0xAA)
                    {
                        answer.error = "Начало кадра не найдено";
                        answer.errorcode = DeviceError.ADDRESS_ERROR;
                    }
                    else if (buffer[5] != (buffer.Length - 7))
                    {
                        answer.error = "Ожидаемая длина кадра не совпадает с фактической";
                        answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                    }
                    else if (buffer[3] != data[3] || buffer[4] != data[4])
                    {
                        answer.error = "Получен неизвестный ответ";
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

            if (answer.success)
            {
                answer.Body = buffer.Skip(5).Take(1 + (buffer.Length - 7)).ToArray();
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

        private byte[] MakeBaseRequest(int cmd, byte[] data = null)
        {
            var bytes = new List<byte>() { 0x55, NetworkAddress, (byte)(~NetworkAddress), ConvertHelper.ByteHigh(cmd), ConvertHelper.ByteLow(cmd), 0 };

            if (data != null && data.Length > 0)
            {
                bytes.Add((byte)data.Length);
                bytes.AddRange(data);
            }

            bytes.Add(CheckSum(bytes.ToArray(), bytes.Count, ChecksumType.Complement1));
            return bytes.ToArray();
        }

        private byte[] MakeVersionRequest()
        {
            return MakeBaseRequest(0x0000);
        }


        dynamic ParseVersionResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            answer.Version = null;
            byte[] body = answer.Body as byte[];
            if (body.Length > 0)
            {
                answer.Version = Encoding.ASCII.GetString(body).Substring(1);
            }
            return answer;
        }

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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges));
                        }
                        break;

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

        private dynamic Wrap(Func<dynamic> func)
        {
            var version = ParseVersionResponse(Send(MakeVersionRequest()));
            if (!version.success || version.Version == null)
            {
                log("Прибор НЕ обнаружен", level: 1);
                return MakeResult(100, version.errorcode, version.error);
            }

            log(version.Version.Equals(DriverVersion) ? "Работает!" : $"Прибор '{version.Version}' обнаружен");
            return func();
        }
        #endregion


        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            var currentDate = DateTime.Now;
            setTimeDifference(DateTime.Now - currentDate);

            log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", currentDate));

            if (getEndDate == null)
            {
                getEndDate = (type) => currentDate;
            }

            if (components.Contains("Constant"))
            {
                var constants = new List<dynamic>();

                var constant = GetConstants(currentDate);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                    return MakeResult(103, constant.errorcode, constant.error);
                }

                constants = constant.records as List<dynamic>;

                //    var writeDb1 = ParseWriteResponse(Send(MakeWriteRequest(0x3ff7, 2, new byte[] { 0x28, 0x05 })));
                //    if (!writeDb1.success)
                //    {
                //        log(string.Format("Ошибка при считывании констант: {0}", writeDb1.error));
                //        return MakeResult(103, writeDb1.errorcode, writeDb1.error);
                //    }

                //    var readDb1 = ParseReadResponse(Send(MakeReadRequest(0x3ff8, 0x80)));
                //    if (!readDb1.success)
                //    {
                //        log(string.Format("Ошибка при считывании констант: {0}", readDb1.error));
                //        return MakeResult(103, readDb1.errorcode, readDb1.error);
                //    }

                //    byte[] data = (readDb1.Body as IEnumerable<byte>).Skip(18).Take(2).ToArray();
                //    constants.Add(MakeConstRecord("tх, град. C", (double)BitConverter.ToInt16(data, 0) / 100.0, date));

                log(string.Format("Константы прочитаны: всего {0}", constants.Count));
                records(constants);
            }

            //if (components.Contains("Current"))
            //{
            //    var currents = new List<dynamic>();

            //    //    var current = GetCurrents(properties, date);
            //    //    if (!current.success)
            //    //    {
            //    //        log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
            //    //        return MakeResult(102, current.errorcode, current.error);
            //    //    }

            //    //    currents = current.records;

            //    log(string.Format("Текущие на {0} прочитаны: всего {1}", currentDate, currents.Count), level: 1);
            //    records(currents);
            //}

            if (components.Contains("Hour"))
            {
                List<dynamic> hours = new List<dynamic>();
                if (hourRanges != null)
                {
                    foreach (var range in hourRanges)
                    {
                        var startH = range.start;
                        var endH = range.end;

                        if (startH > currentDate) continue;
                        if (endH > currentDate) endH = currentDate;

                        var hour = ReadArchive(ArchiveType.Hourly, new TimeSpan(1, 0, 0), startH, endH, currentDate);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                            return MakeResult(105, hour.errorcode, hour.error);
                        }
                        hours.AddRange(hour.records);

                        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                    }
                }
                else
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");

                    var hour = ReadArchive(ArchiveType.Hourly, new TimeSpan(1, 0, 0), startH, endH, currentDate);
                    if (!hour.success)
                    {
                        log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        return MakeResult(105, hour.errorcode, hour.error);
                    }
                    hours.AddRange(hour.records);

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

                        var day = ReadArchive(ArchiveType.Daily, new TimeSpan(1, 0, 0), startD.Date, endD, currentDate);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.errorcode, day.error);
                        }
                        days.AddRange(day.records);

                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                else
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    var day = ReadArchive(ArchiveType.Daily, new TimeSpan(1, 0, 0), startD.Date, endD, currentDate);
                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        return MakeResult(104, day.errorcode, day.error);
                    }
                    days.AddRange(day.records);

                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }
            }

            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }

        private dynamic GetConstants(DateTime currentDt)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var allRecs = new List<dynamic>();

            if (T2k == null)
            {
                archive.error = "не удалось прочесть память таймера 2к";
                archive.success = false;
                return archive;
            }

            allRecs.Add(MakeConstRecord("число систем", $"{T2k.Systems:0}", currentDt));
            for (int i = 0; i < 6; i++)
            {
                int n = i + 1;
                allRecs.Add(MakeConstRecord($"тип датчиков расхода {n}", $"{T2k.Type_g[i].GetDescription():0}", currentDt));
                //allRecs.Add(MakeConstRecord($"тип единиц энергии {sysN}", $"{T2k.Type_q[i]:0}", currentDt));
                allRecs.Add(MakeConstRecord($"тип температур в статистике {n}", $"{T2k.Type_t[i].GetDescription():0}", currentDt));
            }

            allRecs.Add(MakeConstRecord("номер прибора в сети", $"{T2k.Net_num}", currentDt));
            allRecs.Add(MakeConstRecord("заводской номер прибора", $"{T2k.Number}", currentDt));

            for (int i = 0; i < 4; i++)
            {
                int n = i + 1;
                allRecs.Add(MakeConstRecord($"Диаметр условного прохода по каналам {n}, мм", $"{T2k.Diam[i]}", currentDt));
                allRecs.Add(MakeConstRecord($"Максимальное значение расхода по системам(Gmax1) {n}, т/ч", $"{T2k.G_max[i]}", currentDt));
                allRecs.Add(MakeConstRecord($"Установленное значение Gуmax в процентах от (*) {n}, т/ч", $"{T2k.G_pcnt_max[i]}", currentDt));
                allRecs.Add(MakeConstRecord($"Установленное значение Gуmin в процентах от (*) {n}, т/ч", $"{T2k.G_pcnt_min[i]}", currentDt));
            }

            archive.records = allRecs;
            return archive;
        }

        private dynamic ReadArchive(ArchiveType archiveType, TimeSpan d, DateTime start, DateTime end, DateTime currentDt)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var allRecs = new List<dynamic>();
            string type = archiveType == ArchiveType.Hourly ? "Hour" : "Day";

            if (T2k == null)
            {
                archive.error = "не удалось прочесть память таймера 2к";
                archive.success = false;
                return archive;
            }

            log($"Чтение {archiveType} архива");

            for (var date = start.Date.AddHours(start.Hour); date <= end; date.Add(d))
            {
                List<dynamic> recs = new List<dynamic>();

                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= currentDt)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    break;
                }

                log($"Запрос даты {date:HH:mm dd.MM.yyyy}");

                var answer = Send(MakeBaseRequest(0x0d11, new byte[] {
                    (byte) archiveType,
                    archiveType == ArchiveType.Hourly? IntToBCD(date.Hour):(byte)0x00,
                    archiveType != ArchiveType.Monthly? IntToBCD(date.Day):(byte)0x01,
                    IntToBCD(date.Month),
                    IntToBCD(date.Year-2000)
                }));

                if (!answer.success)
                {
                    return answer;
                }

                byte[] body = answer.Body as byte[];

                if (body.Length == 3)
                {
                    var num = body[1] << 8 | body[2];
                    if (num == 0xFFFF)
                    {
                        log("запись не обнаружена");
                    }
                    else
                    {
                        log($"номер записи: {num} (0x{num:X4})");
                        //Int64 addr = 0;
                        //var answer0 = SendRequest(0x0f03, new byte[] { 64, (byte)(addr >> 24), (byte)(addr >> 16), (byte)(addr >> 08), (byte)(addr) });
                        //answer = new byte[256];

                        List<byte> bytes = new List<byte>();
                        for (int i = 0; i < 4; i++)
                        {
                            var curanswer = Send(MakeBaseRequest(0x0f03, new byte[] { 64, 0x00, ConvertHelper.ByteHigh(num), ConvertHelper.ByteLow(num) /*answer[1], answer[2]*/, (byte)(i * 0x40) }), 3333, 5);
                            if (!curanswer.success || (curanswer.Body as byte[]).Length != 65)
                            {
                                log("не удалось запросить запись, пропуск");
                                return curanswer;
                            }
                            bytes.AddRange((curanswer.Body as byte[]).Skip(1));
                        }


                        var sysInt = SysInt.Parse(bytes.ToArray(), 0);

                        if (T2k.Systems < 1 || T2k.Systems > 4)
                        {
                            log($"Некорректное число систем: {T2k.Systems}");
                            answer.error = "";
                            answer.success = false;
                            return answer;
                        }

                        recs.Add(MakeDayOrHourRecord(archiveType == ArchiveType.Hourly ? "Hour" : "Day", sysInt.Trab.Parameter, sysInt.Trab.Value[0], sysInt.Trab.MeasuringUnit, sysInt.date));

                        for (int sys = 0; sys < T2k.Systems; sys++)
                        {
                            var systype = T2k.SysConN[sys].sysType;
                            recs.Add(MakeDayOrHourRecord(type, sysInt.IntV.Parameter, sysInt.IntV.Value[sys], sysInt.IntV.MeasuringUnit, sysInt.date));
                            recs.Add(MakeDayOrHourRecord(type, sysInt.IntM.Parameter, sysInt.IntM.Value[sys], sysInt.IntM.MeasuringUnit, sysInt.date));
                            recs.Add(MakeDayOrHourRecord(type, sysInt.IntQ.Parameter, sysInt.IntQ.Value[sys], sysInt.IntQ.MeasuringUnit, sysInt.date));
                            recs.Add(MakeDayOrHourRecord(type, sysInt.Tnar.Parameter, sysInt.Tnar.Value[sys], sysInt.Tnar.MeasuringUnit, sysInt.date));

                            for (int i = 0; i < SysCon.GetChannelsPorT(systype); i++)
                            {
                                recs.Add(MakeDayOrHourRecord(type, sysInt.T.Parameter, sysInt.T.Value[sys * 3 + i], sysInt.T.MeasuringUnit, sysInt.date));
                                recs.Add(MakeDayOrHourRecord(type, sysInt.P.Parameter, sysInt.P.Value[sys * 3 + i], sysInt.P.MeasuringUnit, sysInt.date));
                            }

                            recs.Add(MakeDayOrHourRecord(type, sysInt.Rshv.Parameter, sysInt.Rshv.Value[sys], sysInt.Rshv.MeasuringUnit, sysInt.date));
                        }

                        allRecs.AddRange(recs);
                        records(recs);
                    }
                }
                else
                {
                    log("ответ не получен");
                }
            }

            archive.records = allRecs;
            return archive;
        }
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                           Attribute.GetCustomAttribute(field,
                             typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}

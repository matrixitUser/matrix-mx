// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;
using System.Threading;
using Matrix.SurveyServer.Driver.Common.Crc;
//using System.Timers;


namespace Matrix.Poll.Driver.Mercury206
{
    /// <summary>
    /// Драйвер для электросчетчика Меркурий 206
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        int hourlyStart = 30;

        private class Block
        {
            public DateTime Date { get; set; }
            public byte Number { get; set; }
        }

        UInt32? NetworkAddress = null;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private byte[] GetNaPacket()
        {
            if (NetworkAddress == null)
            {
                return new byte[0];
            }
            var result = BitConverter.GetBytes(NetworkAddress.Value);
            return result.Reverse().ToArray();
        }

        private byte[] MakePackage(byte cmd, byte[] data = null)
        {
            var result = new List<byte>();

            result.AddRange(GetNaPacket());
            result.Add(cmd);
            if (data != null)
            {
                result.AddRange(data);
            }
            var crc = new Crc16Modbus();
            result.AddRange(crc.Calculate(result.ToArray(), 0, result.Count).CrcData);

            return result.ToArray();
        }
       
        private dynamic ReadActiveEnergy(DateTime date)
        {
            dynamic dt = Send(MakePackage(0x27), 0x27);
            if (!dt.success)
            {
                return dt;
            }

            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            var result = new double[dt.Body.Length / 4];
            if (result.Length > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    //result[i] = BitConverter.ToUInt32(dt.Body, i * 4) * 0.1;
                    result[i] = ConvertFromBcd(dt.Body[i * 4]) * 10000 + ConvertFromBcd(dt.Body[i * 4 + 1]) * 100 + ConvertFromBcd(dt.Body[i * 4 + 2]) * 1 + ConvertFromBcd(dt.Body[i * 4 + 3]) * 0.01;
                }
            }

            double indication = 0;
            for(int i =0; i <result.Length; i++)
            {
                indication += result[i];
            }
            setIndicationForRowCache(indication, "Вт", date);
            int j = 0;
            answer.records = result.Select(r => MakeCurrentRecord("Активная мощность по тарифу " + (++j), r, "Вт", date));

            return answer;
        }

        private dynamic GetHours(DateTime startDate, DateTime endDate, DateTime currentDate)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.records = new List<dynamic>();

            if (cancel())
            {
                answer.success = false;
                answer.error = "опрос отменен";
                answer.errorcode = DeviceError.NO_ERROR;
                return answer;
            }

            if (startDate >= currentDate)
            {
                log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", startDate));
                answer.success = false;
                answer.error = "данные еще не собраны";
                answer.errorcode = DeviceError.NO_ERROR;
                return answer;
            }

            dynamic record = new ExpandoObject();
            List<dynamic> result = new List<dynamic>();

            while (startDate < endDate)
            {
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    break;
                }

                DateTime hourTime = startDate;

                int sDay = startDate.Day;
                int sMonth = startDate.Month;
                int sYear = startDate.Year - 2000;

                int hour = startDate.Hour;
                int group = 0;
                if (hour >= 0 && hour <= 3) group = 0;
                else if (hour >= 4 && hour <= 7) group = 1;
                else if (hour >= 8 && hour <= 11) group = 2;
                else if (hour >= 12 && hour <= 15) group = 3;
                else if (hour >= 16 && hour <= 19) group = 4;
                else if (hour >= 20 && hour <= 23) group = 5;

                group += 10;

                byte[] bytes = new byte[] { Convert.ToByte(group.ToString(), 16), Convert.ToByte(sDay.ToString(), 16), Convert.ToByte(sMonth.ToString(), 16), Convert.ToByte(sYear.ToString(), 16) };

                dynamic dt = Send(MakePackage(0x37, bytes), 0x37);
                if (!dt.success)
                {
                    return dt;
                }

                byte[] bytess = dt.Body;

                for(int i = 0; i < 4; i++)
                {
                    if (dt.Body[0 + i*8] != 0xff && dt.Body[1+i*8] != 0xff && dt.Body[2 + i * 8] != 0xff && dt.Body[3 + i * 8] != 0xff && dt.Body[4 + i * 8] != 0xff && dt.Body[5 + i * 8] != 0xff && dt.Body[6 + i * 8] != 0xff && dt.Body[7 + i * 8] != 0xff)
                    {
                        record.status = 0;
                    }
                    else record.status = 2;

                    record.value = ((dt.Body[0 + i*8] + dt.Body[1 + i * 8] * 256) + (dt.Body[4 + i * 8] + dt.Body[5 + i * 8] * 256)) * 0.2;
                    hourTime = hourTime.AddHours(1);

                    if (record.status == 0)
                    {
                        answer.records.Add(MakeHourRecord("Q+", record.value, "Вт*ч", hourTime));
                        answer.records.Add(MakeHourRecord("Статус", record.status, "", hourTime));
                        log($"Часовые на {hourTime}: {record.value}");
                    }
                    else
                    {
                        answer.records.Add(MakeHourRecord("Статус", record.status, "", hourTime));
                        log($"Часовые на {hourTime}: Несоответствие");
                    }
                    record.date = hourTime;
                    result.Add(record);
                }
                
                startDate = startDate.AddHours(4);
            }
            return answer;
        }

        //private dynamic ReadReactiveEnergy(DateTime date)
        //{
        //    dynamic dt = Send(MakePackage(0x85), 0x85);
        //    if (!dt.success)
        //    {
        //        return dt;
        //    }

        //    dynamic answer = MakeBlankDynamic();

        //    var result = new double[dt.Body.Length / 4];

        //    if (result.Length > 0)
        //    {
        //        for (int i = 0; i < 4; i++)
        //        {
        //            result[i] = BitConverter.ToUInt32(dt.Body, i * 4) * 0.1;
        //        }
        //    }

        //    int j = 0;
        //    answer.records = result.Select(r => MakeCurrentRecord("Реактивная мощность по тарифу " + (++j), r, "Вт", date));
        //    return answer;
        //}

        private dynamic ReadUIP(DateTime date)
        {
            dynamic dt = Send(MakePackage(0x63), 0x63);
            if (!dt.success)
            {
                return dt;
            }

            dynamic answer = MakeBlankDynamic();

            var records = new List<dynamic>();
            if (dt.Body.Length == 7)
            {
                records.Add(MakeCurrentRecord("U", ConvertFromBcd(dt.Body, 0, 2) / 10.0, "В", date));
                records.Add(MakeCurrentRecord("I", ConvertFromBcd(dt.Body, 2, 2) / 100.0, "А", date));
                records.Add(MakeCurrentRecord("P", ConvertFromBcd(dt.Body, 4, 3), "Вт", date));
            }

            answer.records = records;
            return answer;
        }

        private static dynamic MakeBlankDynamic()
        {
            dynamic dynb = new ExpandoObject();
            SetDynamicError(ref dynb, DeviceError.NO_ERROR);
            return dynb;
        }
        private static double[] ParseCount4(byte[] data)
        {
            if (data.Length < 16) return null;
            double[] result = new double[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = ConvertFromBcd(data[i * 4]) * 10000 + ConvertFromBcd(data[i * 4 + 1]) * 100 + ConvertFromBcd(data[i * 4 + 2]) * 1 + ConvertFromBcd(data[i * 4 + 3]) * 0.01;
            }

            return result;
        }
        private static void SetDynamicError(ref dynamic dyn, DeviceError devError, string errorText = "")
        {
            dyn.success = ((errorText == "") && (devError == DeviceError.NO_ERROR));
            dyn.error = errorText;
            dyn.errorcode = devError;
        }

        private dynamic ReadMonthlyData(DateTime date)
        {
            dynamic dt = Send(MakePackage(0x32, new byte[] { (byte)(date.Month - 1) }), 0x32);
            if (!dt.success)
            {
                return dt;
            }

            double[] result = ParseCount4(dt.Body);
            if (result == null)
            {
                SetDynamicError(ref dt, DeviceError.NO_ERROR, $"Ошибка парсинга месячного среза {date:dd.MM.yyyy}");
                return dt;
            }

            int j = 0;
            dt.records = result.Select(r => MakeDayRecord("Активная мощность по тарифу " + (++j), r, "Вт*ч", date));
            return dt;
        }

        private dynamic ReadCurrentTime()
        {
            dynamic dt = Send(MakePackage(0x21), 0x21);
            if (!dt.success)
            {
                return dt;
            }

            dt.date = DateTime.Now;

            if (dt.Body.Length == 7)
            {
                var hour = ConvertFromBcd(dt.Body[1]);
                var min = ConvertFromBcd(dt.Body[2]);
                var sec = ConvertFromBcd(dt.Body[3]);
                var day = ConvertFromBcd(dt.Body[4]);
                var mon = ConvertFromBcd(dt.Body[5]);
                var year = ConvertFromBcd(dt.Body[6]);

                try
                {
                    dt.date = new DateTime(year + 2000, mon, day, hour, min, sec);
                    return dt;
                }
                catch
                {
                }
            }

            log("Не удалось прочитать текущую дату");
            return dt;
        }

        private dynamic ReadSerialNumber()
        {
            dynamic sn = Send(MakePackage(0x2F), 0x2F);
            if (!sn.success)
            {
                return sn;
            }

            if (sn.Body.Length != 4)
            {
                sn.success = false;
                sn.error = "Не удалось прочитать серийный номер счётчика";
                sn.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
            }

            sn.serial = BitConverter.ToUInt32((sn.Body as IEnumerable<byte>).Reverse().ToArray(), 0);
            return sn;
        }



        dynamic GetCurrent(DateTime date)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            dynamic active = ReadActiveEnergy(date);
            if (active.success)
            {
                records.AddRange(active.records);
            }
            else
            {
                return active;
            }
            
            /*dynamic reactive = ReadReactiveEnergy(date);
            if (reactive.success)
            {
                records.AddRange(reactive.records);
            }
            */
            dynamic uip = ReadUIP(date);
            if (uip.success)
            {
                records.AddRange(uip.records);
            }

            current.records = records;
            return current;
        }

        dynamic GetDays(DateTime start, DateTime end, DateTime currentDate)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var recs = new List<dynamic>();

            if (cancel())
            {
                archive.success = false;
                archive.error = "опрос отменен";
                archive.errorcode = DeviceError.NO_ERROR;
                return archive;
            }


            DateTime date = start.Date.AddDays(-1);

            while (date < end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    archive.errorcode = DeviceError.NO_ERROR;
                    break;
                }

                if (date >= currentDate.Date)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", date));
                    break;
                }

                if (date.Day == 1)
                {
                    dynamic mon = ReadMonthlyData(date);
                    if (!mon.success)
                    {
                        return mon;
                    }

                    log(string.Format("прочитан месячный срез за {0:dd.MM.yyyy}", date));
                    recs.AddRange(mon.records);
                }

                //recs.Add(MakeDayRecord("Статус", 0, "", date));
                date = date.AddDays(1);
            }


            records(recs);

            archive.records = recs;
            return archive;
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

        private byte[] SendSimple(byte[] data, int timeout = 4000, int waitCollectedMax = 2)
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
                    //buffer = buffer.SkipWhile(b => b == 0xff).ToArray();
                    var na = GetNaPacket();

                    if (buffer.Length < 6)
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
                    else if (cmd != data[4])
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
                        while (buffer.Length > 6);

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
                answer.Body = buffer.Take(buffer.Length - 2).Skip(5).ToArray();
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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            uint na = 0;
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out na))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            else
            {
                NetworkAddress = na;
            }

            #region hourlyStart
            // минута, с которой считать начало следующего часа (если равна 10 => текущие 10:09 -> часовые 10:00; но 10:10 -> 11:00)
            if (param.ContainsKey("hourlyStart"))
            {
                int.TryParse(arg.hourlyStart.ToString(), out hourlyStart);
                hourlyStart %= 60;
            }
            #endregion

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

#if OLD_DRIVER
            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
            }
#endif

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
                    case "all":
                        {
                            result = All(components);
                        }
                        break;
                    //case "ping":
                    //    {
                    //        result = Wrap(() => Ping(), password);
                    //    }
                    //    break;
                    //case "current":
                    //    {
                    //        result = Wrap(() => Current(), password);
                    //    }
                    //    break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
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
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        //private dynamic Wrap(Func<dynamic> func, string password)
        //{
        //    //PREPARE
        //    var response = ParseTestResponse(Send(MakeTestRequest()));

        //    if (!response.success)
        //    {
        //        log("ответ не получен: " + response.error);
        //        return MakeResult(100, response.errorcode, response.error);
        //    }

        //    var open = ParseTestResponse(Send(MakeOpenChannelRequest(Level.Slave, password)));
        //    if (!open.success)
        //    {
        //        log("не удалось открыть канал связи (возможно пароль не верный): " + open.error);
        //        return MakeResult(100, open.errorcode, open.error);
        //    }

        //    log("канал связи открыт");

        //    //ACTION
        //    return func();

        //    //RELEASE
        //    //log(cancel() ? "успешно отменено" : "считывание окончено");
        //}
        #endregion

        #region Интерфейс

        //private dynamic Ping()
        //{
        //    var currDate = ParseTimeResponse(Send(MakeTimeRequest(0x00, 0)));
        //    if (!currDate.success)
        //    {
        //        log("Не удалось прочесть текущее время: " + currDate.error);
        //        return MakeResult(101, currDate.errorcode, "Не удалось прочесть текущее время: " + currDate.error);
        //    }

        //    log(string.Format("Текущее время на приборе {0:dd.MM.yyyy HH:mm:ss}", currDate.date));
        //    //GetConst(currDate.date);
        //    return MakeResult(0, DeviceError.NO_ERROR, "");
        //}

        private dynamic All(string components)
        {
            //var version = ParseVersionResponse(Send(MakeParametersRequest(0x03)));
            //if (!version.success) return version;

            //log(string.Format("Версия прибора: {0}", version.Version));

            ////читаем текущюю дату
            //var time = ParseTimeResponse(Send(MakeTimeRequest(0x00, 1)));
            //if (!time.success) return time;

            var time = ReadCurrentTime();
            if (!time.success)
            {
                log(string.Format("Ошибка при считывании времени на вычислителе: {0}", time.error), level: 1);
                return MakeResult(102, time.errorcode, time.error);
            }

            var date = time.date;
            setTimeDifference(DateTime.Now - date);
            //log(string.Format("текущая дата на приборе {0:dd.MM.yyyy HH:mm:ss}", date));

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }
            
            if (components.Contains("Current"))
            {
                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");

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


            //////чтение часовых
            if (components.Contains("Hour"))
            {
                DateTime hourlyDate = date.Date.AddHours(date.Hour + (date.Minute >= hourlyStart ? 1 : 0));

                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                var hourly = GetHours(startD, endD, date);

                if (!hourly.success)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", hourly.error), level: 1);
                    return MakeResult(102, hourly.errorcode, hourly.error);
                }

                //foreach(var item in hourly.records) // для отладки нужны были
                //{
                //    int i = 0;
                //    log($"#{i}: {item.type} : {item.d1} : {item.s1} : {item.s2} : {item.date} : {item.dt1}");
                //    i++;
                //}
                records(hourly.records);
                List<dynamic> hours = hourly.records;
                log(string.Format("Прочитаны часовые за {0:dd.MM.yyyy HH:mm}: {1} записей", hourlyDate, hours.Count), level: 1);
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
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            }
            
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }

        //private dynamic Current()
        //{
        //    //читаем текущюю дату
        //    var time = ParseTimeResponse(Send(MakeTimeRequest(0x00, 1)));
        //    if (!time.success) return time;

        //    var date = time.date;
        //    log(string.Format("текущая дата на приборе {0:dd.MM.yyyy HH:mm:ss}", date));

        //    var current = GetCurrent(date);
        //    if (!current.success)
        //    {
        //        log(string.Format("Ошибка при считывании текущих: {0}", current.error));
        //        return MakeResult(102, current.errorcode, current.error);
        //    }

        //    records(current.records);
        //    List<dynamic> currents = current.records;
        //    log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count));
        //    return MakeResult(0, DeviceError.NO_ERROR, "");
        //}

        #endregion

        #region Convert
        private static int ConvertFromBcd(byte bcd)
        {
            return ConvertFromBcd(new byte[] { bcd }, 0, 1);
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

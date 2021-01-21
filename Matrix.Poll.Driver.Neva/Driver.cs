using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.Neva
{
    public partial class Driver
    {
        private const int TIMEZONE_SERVER = +5;
        private const int TIMEZONE_DEVICE = +3;

        /// <summary>
        /// Максимальное допустимое расхождение, сек.
        /// </summary>
        private const int TIME_NEED_CORRECTION_MODULE = 3;

        /// <summary>
        /// Максимальное допустимое количество секунд коррекции (в сутки)
        /// </summary>
        private const int TIME_CORRECTION_MAXIMUM_MODULE = 30;



        string serial = "";

        private byte mid = 0;

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

        private byte[] SendSimple(byte[] data, int timeout = 7000)
        {
            var isCollecting = false;
            var buffer = new List<byte>();
            var sleep = 250;

            request(data);

            log(string.Format("{1}> {0}", string.Join(",", data.Select(b => b.ToString("X2"))), mid & 0x0F), level: 3);
            for (var i = 0; i < data.Length; i += 200)
            {
                var part = data.Skip(i).Take(200).ToArray();
                log(string.Format("{3:X}>({1};{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, i + part.Length, mid & 0x0F), level: 3);
            }
            
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

                if (isCollecting && (waitCollected++) == 0)
                {
                    isCollected = true;
                }
            }

            buffer = buffer.Select(b => (byte)(b & 0x7F)).ToList();

            log(string.Format("{1:X}< {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), mid & 0x0F), level: 3);
            if (buffer.Any())
            {
                for (var i = 0; i < buffer.Count(); i += 200)
                {
                    var part = buffer.Skip(i).Take(200).ToArray();
                    log(string.Format("{3:X}<({1};{2}) \"{0}\"", Encoding.GetEncoding(1252).GetString(part), i, i + part.Length, mid & 0x0F), level: 3);
                }
            }

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, int attempts_total = 3, int timeout = 7000)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
            {
                buffer = SendSimple(data, timeout);

                if ((buffer == null) || (buffer.Length == 0))
                {
                    answer.error = "нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                    continue;
                }

                if ((buffer.Length == 1) && (buffer[0] == 0x15))
                {
                    answer.error = "отрицательный ответ : NAK";
                    answer.errorcode = DeviceError.NO_ERROR;
                    continue;
                }

                if ((buffer.Length == 1) && (buffer[0] == 0x06))
                {
                    //ACK
                }
                else if (buffer.Length < 5)
                {
                    answer.error = "в кадре ответа не может содежаться менее 5 байт";
                    answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    continue;
                }



                answer.success = true;
            }

            if (answer.success)
            {
                answer.rsp = buffer;
                answer.text = Encoding.Default.GetString(buffer);
            }

            mid++;

            Thread.Sleep(200);

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

            result.success = code == 0 ? true : false;
            result.description = description;

            return result;
        }
        #endregion


        #region import-export
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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;
            if (!param.ContainsKey("networkAddress"))
            {
                log("Отсутствуют сведения о серийном номере", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "серийный номер");
            }

            serial = string.Format("{0}", arg.networkAddress);

            #region password
            string password = null;
            if (param.ContainsKey("password") && (arg.password is string))
            {
                password = (string)arg.password;//Encoding.ASCII.GetBytes()
            }
            #endregion

            #region passType
            int passType = 0;
            bool isAdminPass = false;
            if (param.ContainsKey("passType") && (arg.passType is string) && int.TryParse(arg.passType, out passType))
            {
                if (passType == 1)
                {
                    isAdminPass = true;
                }
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
            if (param.ContainsKey("timeZone") && (arg.timeZone is string) && int.TryParse(arg.timeZone, out timeZone))
            {
                if ((timeZone < -12) || (timeZone > +14))
                {
                    timeZone = TIMEZONE_DEVICE;
                }
            }
            else
            {
                timeZone = TIMEZONE_DEVICE;
            }
            #endregion

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
                log(string.Format("дата начала опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }


            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, password, isAdminPass, isTimeCorrectionEnabled, timeZone), password);
                        }
                        break;
#if PING
                    case "ping":
                        {
                            result = Ping();
                        }
                        break;
#endif
                    default:
                        {
                            log(string.Format("неопознаная команда {0}", what), level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, string.Format("неопознаная команда {0}", what));
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

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            //PREPARE
            //Типовая цепочка авторизации по МЭК61107: Запрос-> Потверждение/опция -> считыввание SNUMB
            //Пинг
            var ping = ParsePingResponse(Send(MakePingRequest()));
            if (!ping.success)
            {
                log(string.Format("Ошибка при установлении соединения: {0}", ping.error), level: 1);
                return MakeResult(101, ping.errorcode, string.Format("Ошибка при установлении соединения: {0}", ping.error));
            }
            log(string.Format("Вычислитель {0}", ping.text));

            //сообщения потверждения/выбор опции (режим программирования)
            var manor = Send(MakeAskNOptionRequest(0x31));
            if (!manor.success)
            {
                log(string.Format("Счетчик не перешел в режим программирования: {0}", manor.error), level: 1);
                return MakeResult(101, manor.errorcode, string.Format("Счетчик не перешел в режим программирования: {0}", manor.error));
            }

            var pass = Send(MakePassRequest(password));
            if(!pass.success)
            {
                log(string.Format("Ошибка открытия сеанса связи: {0}", pass.error), level: 1);
                return MakeResult(101, pass.errorcode, string.Format("Ошибка открытия сеанса связи: {0}", pass.error));
            }

            //Считывание данных (серийного номера счетчика) по протоколу МЭК
            /*var snumb = Send(MakeDataRequest("SNUMB()"));
            if (!snumb.success)
            {
                log(string.Format("Ошибка при запросе параметра SNUMB: {0}", snumb.error), level: 1);
                return MakeResult(101, snumb.errorcode, string.Format("Ошибка при запросе параметра SNUMB: {0}", snumb.error));
            }

            log("Данные со счетчика:" + snumb.text);*/

            //ACTION
            var result = func();

            //RELEASE
            Send(MakeSessionByeRequest(), 1, 2000);

            //Thread.Sleep(1000); //надо подождать

            return result;
        }

        #endregion import-export



        #region interface
#if PING
        private dynamic Ping()
        {
            //Request.CrcType = CrcType.CRC;
            var ping = Send(MakePingRequest());
            if (!ping.success)
            {
                log(string.Format("Ошибка пинга: {0}", ping.error), level: 1);
                return MakeResult(101, ping.errorcode, string.Format("Ошибка пинга: {0}", ping.error));
            }

            log(string.Format("Тест связи со счетчиком прошел удачно: {0}", ping.text), level: 1);
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
#endif

        private dynamic GetCounterDt()
        {
            var cdate = Send(MakeDataRequest("000902FF()")); //Date, rsp=.000902FF(170719).
            if (!cdate.success) return cdate;

            var ctime = Send(MakeDataRequest("000901FF()"));   //Time, rsp=.000901FF(082824).
            if (!ctime.success) return ctime;

            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = "";
            answer.errorcode = DeviceError.NO_ERROR;

            answer.date = DriverHelper.DateTimeFromCounter(cdate.rsp, ctime.rsp);
            return answer;
        }

        private dynamic All(string components, string password, bool isAdminPass, bool isTimeCorrectionEnable, int timeZone)
        {
            dynamic dt = GetCounterDt();
            if(!dt.success)
            {
                log(string.Format("Ошибка при считывании текущего времени: {0}", dt.error), level: 1);
                return MakeResult(101, dt.errorcode, dt.error);
            }

            setTimeDifference(DateTime.Now - dt.date);

            if (getEndDate == null)
            {
                getEndDate = (type) => dt.date;
            }


            if (components.Contains("Current"))
            {
                var current = GetCurrent(dt.date);
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                List<dynamic> currents = current.records;
                log(string.Format("Текущие на {0:dd.MM.yyyy HH:mm:ss} прочитаны: всего {1}", dt.date, currents.Count), level: 1);
                records(currents);
            }

            //

            if (components.Contains("Constant"))
            {
                var addr = ParseValueArray(Send(MakeDataRequest("600100FF()")));
                if (!addr.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", addr.error), level: 1);
                    return MakeResult(103, addr.errorcode, addr.error);
                }

                List<dynamic> recs = new List<dynamic>();
                recs.Add(MakeConstRecord("Серийный номер", addr.texts[0], dt.date));
                log(string.Format("Константы на {0:dd.MM.yyyy HH:mm:ss} прочитаны: всего {1}", dt.date, recs.Count), level: 1);
                records(recs);
            }
            
            //чтение часовых      
            if (components.Contains("Hour"))
            {
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");
                
                var hour = GetHours(startH, endH, dt.date);
                if (!hour.success)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                    return MakeResult(105, hour.errorcode, hour.error);
                }
                List<dynamic> hours = hour.records;
                log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            }
            ///конец чтения часовых
            
            //чтение суточных
            if (components.Contains("Day"))
            {
                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                var day = GetDays(startD, endD, dt.date);
                if (!day.success)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                    return MakeResult(104, day.errorcode, day.error);
                }
                List<dynamic> days = day.records;
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            }

#if TIME_CORRECTION
            if (isTimeCorrectionEnable)
            {
                var cdate = Send(MakeDataRequest("DATE_()"));
                if (!cdate.success) return cdate;

                var ctime = Send(MakeDataRequest("TIME_()"));
                if (!ctime.success) return ctime;

                DateTime date = DriverHelper.DateTimeFromCounter(cdate.rsp, ctime.rsp);
                DateTime now = DateTime.Now;

                DateTime nowTz = now.AddHours(timeZone - TIMEZONE_SERVER);
                TimeSpan timeDiff = nowTz - date;
                bool isTimeCorrectable = (timeDiff.TotalSeconds > -TIME_CORRECTION_MAXIMUM_MODULE) && (timeDiff.TotalSeconds < TIME_CORRECTION_MAXIMUM_MODULE);
                bool isTimeNeedToCorrent = (timeDiff.TotalSeconds >= TIME_NEED_CORRECTION_MODULE) || (timeDiff.TotalSeconds <= -TIME_NEED_CORRECTION_MODULE);

                //log(string.Format("Дата/время {0:dd.MM.yyyy HH:mm:ss}; расхождение {1} сек.; isTimeCorrectable?{2}, isTimeNeedToCorrent?{3}", date, timeDiff.TotalSeconds, isTimeCorrectable, isTimeNeedToCorrent), 3);

                if (isTimeCorrectable && isTimeNeedToCorrent)
                {
                    var timeToSleep = 60000 - (now.Second * 1000 + now.Millisecond) - 500;
                    log(string.Format("Расхождение {1:0.0} сек. - нужна корректировка; спим {0} сек.", timeToSleep / 1000, timeDiff.TotalSeconds), 3);
                    Thread.Sleep(timeToSleep);
                    Send(MakeDataRequest("CTIME()"), 1);

                    log("Произведена корректировка времени");

                    /*cdate = Send(MakeDataRequest("DATE_()"));
                    if (!cdate.success) return cdate;

                    ctime = Send(MakeDataRequest("TIME_()"));
                    if (!ctime.success) return ctime;

                    date = DriverHelper.DateTimeFromCounter(cdate.rsp, ctime.rsp);

                    now = DateTime.Now;
                    nowTz = now.AddHours(timeZone - TIMEZONE_SERVER);
                    log(string.Format("Время установлено, расхождение = {0:0.0} сек.", (date - nowTz).TotalSeconds));*/
                }
            }
#endif

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
        #endregion interface
    }
}

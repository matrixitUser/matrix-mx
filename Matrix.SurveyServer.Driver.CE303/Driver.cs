// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER
// Если счетчик CE102M необходимо конвертировать 8N1 в 7E1


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
//using System.Threading.Tasks;
//using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using System.Globalization;
using System.Dynamic;
using System.Threading;

namespace Matrix.SurveyServer.Driver.CE303
{
    /// <summary>
    /// похоже, что не был скопирован со старой системы! возможно проявление старых глюков, если были
    /// 27.01.2017 добавил DeviceError
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif
        public int gCount = 0;
        private const int TIMEZONE_SERVER = +5;
        private const int TIMEZONE_DEVICE = +3;
        private bool convertTo7E1 = false;

        /// <summary>
        /// Максимальное допустимое расхождение, сек.
        /// </summary>
        private const int TIME_NEED_CORRECTION_MODULE = 3;

        /// <summary>
        /// Максимальное допустимое количество секунд коррекции (в сутки)
        /// </summary>
        private const int TIME_CORRECTION_MAXIMUM_MODULE = 30;

        string serial = "";
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
            UNSUPPORTED_PARAMETER
        };

        private void log(string message, int level = 2)
        {
#if OLD_DRIVER
            if ((level < 3) || ((level == 3) && debugMode))
            {
                logger(message);
            }
#else
            gCount++;
            message = string.Format("{0}:{1}", gCount, message);
            logger(message, level);
            
#endif
        }

        private byte[] SendSimple(byte[] data)
        {
            var isCollecting = false;
            var buffer = new List<byte>();
            var sleep = 4000;

            request(data);

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            var timeout = 16000;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
            {
                Thread.Sleep(sleep);

                var buf = response();
                if (convertTo7E1)
                {
                    buf = convertByteSTo8N1(buf);
                }

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

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private byte convertByteTo7E1(byte byte_)
        {
            int sum = 0;
            byte temp = 0x01;
            for (int i = 0; i < 7; i++)
            {
                if ((temp & byte_) > 0) sum++;
                temp <<= 1;
            }
            if ((sum % 2)==1)
            {
                byte_ |=  0x80;
            }
            else
            {
                byte_ &= 0x7F;
            }
            return byte_;
        }
        private byte[] convertByteSTo7E1(byte[] bytes)
        {
            for (int i=0;i<bytes.Length;i++)
            {
                bytes[i] = convertByteTo7E1(bytes[i]);
            }
            return bytes;
        }


        private byte[] convertByteSTo8N1(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                  bytes[i] &= 0x7F;
            }
            return bytes;

        }

        private dynamic Send( Request request , int attempts_total = 3)
        {
            //attempts_total = 1;
            byte[] data = request.bytes;
            string parameterName = request.Name;
            int i = parameterName.IndexOf("(");
            if (i >= 0) parameterName = parameterName.Substring(0, i - 1);
            if (convertTo7E1)
                data = convertByteSTo7E1(data);


            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;



            for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
            {
                log(string.Format("Отправлено: {0}", Encoding.Default.GetString(data)));
                buffer = SendSimple(data);

                if (buffer == null || buffer.Length == 0)
                {
                    answer.error = "нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                    continue;
                }

                if (buffer.Length == 1 && buffer[0] == 0x15)
                {
                    log("отрицательный ответ : NAK", 1);
                    answer.error = "отрицательный ответ : NAK";
                    answer.errorcode = DeviceError.NO_ERROR;
                    continue;
                }
                if (buffer[0] > 0x2F)
                {
                    log(string.Format("неверный ответ {0}", buffer[0].ToString("X")), 1);
                    answer.error = string.Format("неверный ответ {0}", buffer[0].ToString("X"));
                    answer.errorcode = DeviceError.NO_ERROR;
                    continue;
                }
                if (buffer.Length < 5)
                {
                    answer.error = "в кадре ответа не может содежаться менее 5 байт";
                    answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    continue;
                }

                answer.text = Encoding.Default.GetString(buffer);
                log(string.Format("Получено: {0}", answer.text));
                if (answer.text.Contains("ERROR"))
                {
                    answer.error = "в кадре ответа от счетчика пришел 'ERROR'";
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                    continue;
                }

                if ((parameterName != "") && (answer.text.Contains("ERR12")))  
                {
                    answer.error = "в кадре:" + answer.text + " пришел ответ от счетчика не поддерживаемый параметр '" + parameterName+"'"; 
                    answer.errorcode = DeviceError.UNSUPPORTED_PARAMETER;
                    return answer;
                }

                if ((parameterName !="") && (!answer.text.Contains(parameterName)) )
                {
                    answer.error = "в кадре:" + answer.text+ " пришел ответ не содержащий "+ parameterName;
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                    continue;
                }

                answer.success = true;
            }

            if (answer.success)
            {
                answer.rsp = buffer;
                log(string.Format("Succes:Получено: {0}", answer.text));
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

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

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
                if(passType == 1)
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

            #region CONVERT7E1
            if (param.ContainsKey("to7E1"))
            {
                if (arg.to7E1=="1")
                {
                    convertTo7E1 = true;
                }
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

            if(convertTo7E1) log("Внимание! Производится конвертация из формата 8N1 в 7E1 !!!");

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


            dynamic result = new ExpandoObject();

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            //var makePingRequest = Send(MakePingRequest());
                            result = Wrap(() => All(components, password, isAdminPass, isTimeCorrectionEnabled, timeZone));
                            var sessionStop = Send(MakeSessionStop());
                        }
                        break;
                    case "ping":
                        {
                            result = Ping();
                        }
                        break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "current": Current(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
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

        private dynamic Wrap(Func<dynamic> func)
        {
            var sessionStop = Send(MakeSessionStop(), 1);
            //PREPARE
            //Типовая цепочка авторизации по МЭК61107: Запрос-> Потверждение/опция -> считыввание SNUMB
            //Пинг
            var ping = ParseResponse(Send(MakePingRequest()));
            if (!ping.success)
            {
                log(string.Format("Ошибка при установлении соединения: {0}", ping.error), level: 1);
                return MakeResult(101, ping.errorcode, string.Format("Ошибка при установлении соединения: {0}", ping.error));
            }

            //сообщения потверждения/выбор опции (режим программирования)
            var manor = ParseResponse(Send(MakeAskNOptionRequest(0x31)));
            if (!manor.success)
            {
                log(string.Format("Счетчик не перешел в режим программирования: {0}", manor.error), level: 1);
                return MakeResult(101, manor.errorcode, string.Format("Счетчик не перешел в режим программирования: {0}", manor.error));
            }
            //OnSendMessage("Ответ счетчика на сообщения потверждения/выбор опции программирования:" + Encoding.Default.GetString(response));

            //Считывание данных (серийного номера счетчика) по протоколу МЭК
            var snumb = ParseResponse(Send(MakeDataRequest("SNUMB()")));

            if (!snumb.success)
            {
                log(string.Format("Ошибка при запросе параметра SNUMB: {0}", snumb.error), level: 1);
                return MakeResult(101, snumb.errorcode, string.Format("Ошибка при запросе параметра SNUMB: {0}", snumb.error));
            }
            if (!snumb.text.Contains(serial))
            {
                log(string.Format("Ошибка: несовпадение сетевого адреса при запросе параметра SNUMB: получили {0} и не содержит {1}", snumb.text, serial), level: 3);
                return MakeResult(101, snumb.errorcode, string.Format("Ошибка: несовпадение сетевого адреса при запросе параметра SNUMB: получили {0} и не содержит {1}", snumb.text, serial));
            }

            log("Данные со счетчика:" + snumb.text);
            //ACTION
            var result = func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");

            Thread.Sleep(2400); //надо подождать

            return result;
        }

        #endregion import-export

        #region interface
        private dynamic Ping()
        {
            //Request.CrcType = CrcType.CRC;
            var ping = ParseResponse(Send(MakePingRequest()));
            if (!ping.success)
            {
                log(string.Format("Ошибка пинга: {0}", ping.error), level: 1);
                return MakeResult(101, ping.errorcode, string.Format("Ошибка пинга: {0}", ping.error));
            }

            log(string.Format("Тест связи со счетчиком прошел удачно: {0}", ping.text), level: 1);
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }


        private dynamic All(string components, string password, bool isAdminPass, bool isTimeCorrectionEnable, int timeZone)
        {
            Send(MakeDataRequestFromBytes(new List<byte>() { ASK, 0x30, 0x35, 0x31, CR, LF })); //0x06, 0x30, 0x35, 0x31, 0x0D, 0x0A 
            Send(MakeDataRequestFromBytes(new List<byte>() { SOH, 0x50, 0x31, STX, 0x28, 0x37, 0x37, 0x37, 0x37, 0x37, 0x37, 0x29, ETX, VOSKL })); //0x01, 0x50, 0x31, 0x02, 0x28, 0x37, 0x37, 0x37, 0x37, 0x37, 0x37, 0x29,0x03, 0x21
            var current = GetCurrent();
            if (!current.success)
            {
                log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                return MakeResult(102, current.errorcode, current.error);
            }

            setTimeDifference(DateTime.Now - current.date);

            List<dynamic> currents = current.records;
            log(string.Format("Текущие на {0:dd.MM.yyyy HH:mm:ss} прочитаны: всего {1}, показание счетчика = {2:0.000}", current.date, currents.Count, current.energy), level: 1);
            records(currents);

            //

            if (getEndDate == null)
            {
                getEndDate = (type) => current.date;
            }

            if (components.Contains("Constant"))
            {
                //    var constant = GetConstant();
                //    if (!constant.success)
                //    {
                //        log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                //        return;
                //    }

                //    {
                //List<dynamic> recs = constant.records;
                //        foreach (var rec in recs)
                //        {
                //            rec.date = current.date;
                //        }
                //        log(string.Format("Константы прочитаны: всего {0}", recs.Count));      
                List<dynamic> recs = new List<dynamic>();
                recs.Add(MakeConstRecord("Нет констант", "", current.date));
                log(string.Format("Вычислитель не имеет констант", recs.Count));
                records(recs);
                //    }

                //    //////
            }

            //чтение часовых      
            if (components.Contains("Hour"))
            {
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");

                var hour = GetHours(startH, endH, current.date);
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

                var day = GetDays(startD, endD, current.date);
                if (!day.success)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                    return MakeResult(104, day.errorcode, day.error);
                }
                List<dynamic> days = day.records;
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            }

            //    ///// Нештатные ситуации ///
            //    //var lastAbnormal = getLastTime("Abnormal");
            //    //DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
            //    //DateTime endAbnormal = current.date;

            //    //var abnormal = GetAbnormals(startAbnormal, endAbnormal);
            //    //if (!abnormal.success)
            //    //{
            //    //    log(string.Format("ошибка при считывании НС: {0}", abnormal.error));
            //    //    return;
            //    //}

            if(isTimeCorrectionEnable)
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
                    log(string.Format("Расхождение {1:0.0} сек. - нужна корректировка; спим {0} сек.", timeToSleep/1000, timeDiff.TotalSeconds), 3);
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


            /*            
            if ((password != null) && (password != ""))
            {
                log(string.Format("будут посланы следующие запросы: {0}", string.Format("PASS{0}({1})", isAdminPass ? "W" : "U", password)));


                var pass = Send(MakeDataRequest(string.Format("PASS{0}({1})", isAdminPass ? "W" : "U", password)));
                if (pass.success)
                {
                    log(string.Format("Введён пароль \"{0}\"", password));
                }
                else
                {
                    log(string.Format("Пароль НЕ введён: {0}", pass.error));
                }
            }

            if (isTimeCorrectionEnable)
            {
                DateTime now = DateTime.Now;
                DateTime deviceTime = now.AddHours(timeZone - TIMEZONE_SERVER);
                int weekDay = (int)deviceTime.DayOfWeek;

                //log(string.Format("будут посланы следующие запросы: {0}; {1}; {2}", string.Format("DATE_({0}.{1:dd.MM.yy})", weekDay, deviceTime), string.Format("TIME_({0:HH:mm:ss})", deviceTime), string.Format("TRSUM({0})", 0)));

                var trsum = Send(MakeDataRequest(string.Format("TRSUM({0})", 0)));
                if (!trsum.success) return trsum;

                var ctime = Send(MakeDataRequest(string.Format("TIME_({0:HH:mm:ss})", deviceTime)));
                if (!ctime.success) return ctime;

                var cdate = Send(MakeDataRequest(string.Format("DATE_({0}.{1:dd.MM.yy})", weekDay, deviceTime)));
                if (!cdate.success) return cdate;

                log(string.Format("Ответы: {0}; {1}; {2}", trsum.text, ctime.text, cdate.text), 3);
            }
            */


            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
        #endregion interface
    }
}
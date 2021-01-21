// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Matrix.SurveyServer.Driver.Common;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Text.RegularExpressions;
using System.Threading;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.SET4
{
    /// <summary>
    /// Драйвер для электросчетчика СЭТ4
    /// 27.01.2017 скопирован со старой системы
    /// 27.01.2017 добавил DeviceError
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private const int TIMEOUT_TIME = 10000;
        private const int SLEEP_TIME = 250;
        private const int COLLECT_MUL = 9;
        private const int SEND_ATTEMPTS = 2;
        
        UInt32 NetworkAddress = 0;

        private const int TIMEZONE_SERVER = +5;
        private const int TIMEZONE_DEFAULT = +3;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;
        DateTime currentTime = DateTime.MinValue;

        private byte msg = 0;

        #region Common
        private byte[] SendSimple(byte[] data)
        {
            var buffer = new List<byte>();

            log(string.Format("o{0:X} {1}", msg, string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);
            response();
            request(data);

            var timeout = TIMEOUT_TIME;

            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= SLEEP_TIME) > 0 && !isCollected)
            {
                Thread.Sleep(SLEEP_TIME);

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
                        if (waitCollected == COLLECT_MUL)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("i{0:X} {1}", msg, string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);
            msg++;

            return buffer.ToArray();
        }

        private enum DeviceError
        {
            NO_ERROR = 0, //нет ошибки вычислителя, хотя может быть логическая ошибка (неизвестная команда ping вместо all)
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
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

        private dynamic Send(byte[] datasend, int attempts_total = SEND_ATTEMPTS)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < attempts_total && answer.success == false; attempts++)
            {
                buffer = SendSimple(datasend);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 4)
                    {
                        answer.error = "в кадре ответа не может содержаться менее 4 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (!Crc.Check(buffer, new Crc16Modbus()))
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

            if (answer.success)
            {
                answer.NetworkAddress = buffer[0];
                answer.Body = buffer.Skip(1).Take(buffer.Length - 3).ToArray();
                var tmp = buffer.Skip(1).Take(buffer.Length - 3);
                /*
                log($"buffer, {string.Join(",", buffer.Select(b => b.ToString("X2")))}");
                log($"Body, {string.Join(",", tmp.Select(b => b.ToString("X2")))}");
                
                List<byte> tmpByte = (tmp.Select(b => b)).ToList();
                log($"tmpByte до удаления, {string.Join(",", tmpByte.Select(b => b.ToString("X2")))}");
                try
                {
                    int firstIndexFF = tmpByte.IndexOf(0xFF);
                    log($"Индекс1: {firstIndexFF}");
                    if (firstIndexFF > 40)
                    {
                        tmpByte.RemoveAt(firstIndexFF);
                    }
                }
                catch
                {
                }
                log($"tmpByte после удаления, {string.Join(",", tmpByte.Select(b => b.ToString("X2")))}");
                */
                //answer.Body = tmpByte.ToArray();
                //modbus error
                if (buffer.Length == 4)
                {
                    answer.success = false;
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                    switch (buffer[1] & 0x0F)
                    {
                        case 0x00:
                            answer.success = true;
                            answer.errorcode = DeviceError.NO_ERROR;
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
                        case 0x0F:
                            answer.error = "счётчик не отвечает (коммуникатор)";
                            break;
                        default:
                            answer.error = "неизвестная ошибка";
                            break;
                    }
                }
            }

            return answer;
        }

        //dynamic ParseBaseResponse(byte[] data)
        //{
        //    dynamic answer = new ExpandoObject();
        //    answer.success = false;
        //    answer.error = "";

        //    data = data.SkipWhile(b => b == 0xff).ToArray();
        //    answer.Body = data.Skip(1).Take(data.Count() - 3).ToArray();
        //    if (data.Length < 4)
        //    {
        //        answer.error = "в кадре ответа не может содержаться менее 4 байт";
        //        return answer;
        //    }

        //    if (!Crc.Check(data, new Crc16Modbus()))
        //    {
        //        answer.error = "контрольная сумма кадра не сошлась";
        //        return answer;
        //    }


        //    answer.NetworkAddress = data[0];

        //    //modbus error
        //    if (data.Length == 4)
        //    {
        //        switch (data[1] & 0x0F)
        //        {
        //            case 0x00:
        //                answer.success = true;
        //                answer.error = "все нормально";
        //                break;
        //            case 0x01:
        //                answer.error = "недопустимая команда или параметр";
        //                break;
        //            case 0x02:
        //                answer.error = "внутренняя ошибка счетчика";
        //                break;
        //            case 0x03:
        //                answer.error = "не достаточен уровень доступа для удовлетворения запроса";
        //                break;
        //            case 0x04:
        //                answer.error = "внутренние часы счетчика уже корректировались в течении текущих суток";
        //                break;
        //            case 0x05:
        //                answer.error = "не открыт канал связи";
        //                break;
        //            case 0x0F:
        //                answer.error = "счётчик не отвечает (коммуникатор)";
        //                break;
        //            default:
        //                answer.error = "неизвестная ошибка";
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        answer.success = true;
        //    }

        //    return answer;
        //}



        //private byte[] Send(byte[] data)
        //{
        //    response();
        //    request(data);

        //    if (debugMode)
        //    {
        //        log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
        //    }

        //    var buffer = new List<byte>();
        //    var timeout = 10000;
        //    var sleep = 250;
        //    var isCollecting = false;
        //    var waitCollected = 0;
        //    var isCollected = false;
        //    while ((timeout -= sleep) > 0 && !isCollected)
        //    {
        //        Thread.Sleep(sleep);

        //        var buf = response();
        //        if (buf.Any())
        //        {
        //            isCollecting = true;
        //            buffer.AddRange(buf);
        //            waitCollected = 0;
        //        }
        //        else
        //        {
        //            if (isCollecting)
        //            {
        //                waitCollected++;
        //                if (waitCollected == 9)
        //                {
        //                    isCollected = true;
        //                }
        //            }
        //        }
        //    }

        //    if (debugMode)
        //    {
        //        log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
        //    }

        //    return buffer.ToArray();
        //}


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

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            setArchiveDepth("Day", 30);

            double KTr = 1.0;
            string password = "000000";

            var param = (IDictionary<string, object>)arg;
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }

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
                    timeZone = TIMEZONE_DEFAULT;
                }
            }
            else
            {
                timeZone = TIMEZONE_DEFAULT;
            }
            #endregion



            if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            {
                log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            }
            if (!param.ContainsKey("password"))
            {
                log("Отсутствуют сведения о пароле, принят по-умолчанию");
            }
            else
            {
                password = arg.password.ToString();
            }


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
                            result = Wrap(() => All(components, isTimeCorrectionEnabled, timeZone), password);
                        }
                        break;
                    case "ping":
                        {
                            result = Ping(password);
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

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            //PREPARE
            var test = Send(MakeTestRequest());
            if (!test.success)
            {
                log(string.Format("Ошибка связи со счетчиком: {0}", test.error), level: 1);
                return MakeResult(101, test.errorcode, string.Format("Ошибка связи со счетчиком: {0}", test.error));
            }

            var answer = ParseOpenChannelResponse(Send(MakeOpenChannelRequest(password)));
            if (!answer.success)
            {
                log("ошибка при открытии канала связи: " + answer.error, level: 1);
                return MakeResult(100, answer.errorcode, answer.error);
            }

            log("канал связи открыт");

            //ACTION
            return func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }

        #endregion import-export


        byte[] MakeTimeCorrectionRequest(int hour, int min, int sec)
        {
            return MakeRequestOnWriteParameters(0x0D, new byte[] { Helper.IntToBinDec((byte)(sec % 60)), Helper.IntToBinDec((byte)(min % 60)), Helper.IntToBinDec((byte)(hour % 24)) });
        }


        #region interface
        private dynamic Ping(string password)
        {
            //Request.CrcType = CrcType.CRC;
            var test = Send(MakeTestRequest());
            if (!test.success)
            {
                log(string.Format("Ошибка связи со счетчиком: ", test.error), level: 1);
                return MakeResult(101, test.errorcode, string.Format("Ошибка связи со счетчиком: ", test.error));
            }

            log(string.Format("Тест связи со счетчиком прошел удачно"), level: 1);
            return MakeResult(0, DeviceError.NO_ERROR, "");
        }


        private dynamic All(string components, bool isTimeCorrectionEnabled, int timeZone)
        {
            var current = GetCurrent();
            if (!current.success)
            {
                log(string.Format("Ошибка при считывании времени на приборе: {0}", current.error), level: 1);
                return MakeResult(102, current.errorcode, current.error);
            }
            currentTime = current.date;
            DateTime now = DateTime.Now;

            if (isTimeCorrectionEnabled)
            {
                DateTime nowTz = now.AddHours(timeZone - TIMEZONE_SERVER);
                TimeSpan timeDiff = nowTz - currentTime;
                bool isTimeCorrectable = (timeDiff.TotalSeconds > -120) && (timeDiff.TotalSeconds < 120);
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
                
                var rsp = Send(MakeRequestLogsExt(0x00, 0x00));
                if (rsp.success == false) return rsp;
                currentTime = new DateTime(Helper.BinDecToInt(rsp.Body[6]) + 2000, Helper.BinDecToInt(rsp.Body[5]), Helper.BinDecToInt(rsp.Body[4]), Helper.BinDecToInt(rsp.Body[2]), Helper.BinDecToInt(rsp.Body[1]), Helper.BinDecToInt(rsp.Body[0]));

            }
            
            log(string.Format("Время на приборе: {0}", currentTime));            
            setTimeDifference(DateTime.Now - currentTime);

            //

            if (getEndDate == null)
            {
                getEndDate = (type) => currentTime;// current.date;
            }


            var constant = GetConstant(currentTime);
            if (!constant.success)
            {
                log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                return MakeResult(103, constant.errorcode, constant.error);
            }

            if (components.Contains("Constant"))
            {
                List<dynamic> constants = constant.records;
                log(string.Format("Константы прочитаны: всего {0}", constants.Count), level: 1);
                records(constants);
            }

            ///

            if (components.Contains("Current"))
            {
                var currentEnergy = GetCurrentEnergy(constant.constA, currentTime, constant.aType);
                if (!currentEnergy.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", currentEnergy.error), level: 1);
                    //return MakeResult(102, currentEnergy.error);
                }
                else
                {
                    List<dynamic> currents = currentEnergy.records;
                    currents.AddRange(current.records);

                    log(string.Format("Текущие на {0} прочитаны: всего {1}, показание счетчика = {2:0.000}", current.date, currents.Count, currentEnergy.energy), level: 1);
                    records(currents);
                }
            }
            ///


            if (components.Contains("Day"))
            {
                //чтение суточных            
                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                var day = GetDays(startD, endD, current.date, constant.constA, constant.aType);
                if (!day.success)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                    return MakeResult(104, day.errorcode, day.error);
                }
                List<dynamic> days = day.records;
                log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            }

            //чтение часовых
            if (components.Contains("Hour"))
            {
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");

                var hour = GetHours(startH, endH, currentTime, constant);
                if (!hour.success)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                    return MakeResult(105, hour.errorcode, hour.error);
                }
                List<dynamic> hours = hour.records;
                log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            }
            ///конец чтения часовых



            ///// Нештатные ситуации ///
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

        List<dynamic> _j = null;
        List<dynamic> journal
        {
            get
            {
                if (_j == null)
                {
                    log(string.Format("чтение журнала выключений"));
                    _j = new List<dynamic>();
                    for (byte i = 0; i < 10; i++)
                    {
                        var rsp = Send(MakeRequestLogsExt(0x01, (byte)i));
                        if (!rsp.success)
                        {
                            return null;
                        }

                        var jr = ParseJournalResponse(rsp, currentTime);
                        log(string.Format("запись журнала выкл {0:dd.MM.yy HH:mm} - вкл {1:dd.MM.yy HH:mm}", jr.TurnOff, jr.TurnOn));
                        _j.Add(jr);
                    }
                }
                return _j;
            }
        }

        private bool isInJournal(DateTime date)
        {
            foreach (var jr in journal)
            {
                if ((jr.TurnOff <= date) && (date < jr.TurnOn))
                {
                    return true;
                }
            }
            return false;
        }

        //2.4.3.6.2  Внутреннее представление мощности массива профиля и ее преобразование  
        //2.4.3.6.1 Структура данных массива профиля мощности
        private double PQ(byte[] bytes, int startIndex, int constA, byte TimeInterval)
        {
            if ((bytes[startIndex] & 0x80) != 0x00)
            {
                log("Недостоверные данные");
            }
            bytes[startIndex] &= 0x7F;
            return (60.0 / TimeInterval) * (Helper.ToInt16(bytes, startIndex)) / (2 * constA);
        }

        private double PQEnergy(byte[] bytes, int startIndex, int constA)
        {
            return ((double)Helper.ToInt32(bytes, startIndex)) / (2 * constA);
        }

        private DateTime dtCurrentMemory(byte[] response, byte startIndex)
        {
            return new DateTime(Helper.BinDecToInt(response[startIndex + 4]) + 2000, Helper.BinDecToInt(response[startIndex + 3]), Helper.BinDecToInt(response[startIndex + 2]), Helper.BinDecToInt(response[startIndex + 1]), Helper.BinDecToInt((byte)(0x7F & response[startIndex])), 0);        //Время начала текущего среза  стр.100
        }

        private DateTime dtfromCounter(byte[] response, byte startIndex)
        {
            try
            {
                return new DateTime(Helper.BinDecToInt(response[startIndex + 3]) + 2000, Helper.BinDecToInt(response[startIndex + 2]), Helper.BinDecToInt(response[startIndex + 1]), Helper.BinDecToInt(response[startIndex]), 0, 0);        //Время из профиля мощности
            }
            catch
            {
                return DateTime.MinValue;
            }
            //return new DateTime(Helper.BinDecToInt(response[startIndex + 3]) + 2000, Helper.BinDecToInt(response[startIndex + 2]), Helper.BinDecToInt(response[startIndex + 1]), Helper.BinDecToInt(response[startIndex]), 0, 0);        //Время из профиля мощности
        }

        private dynamic ReadProfiles(byte nArray, DateTime date, byte count, byte TimeInterval)
        {
            byte[] aMemory = new byte[] { 0x02, 0x03, 0x08, 0x09 };

            //2.3.1.23  Поиск адреса заголовка массива профиля мощности
            var request = MakeRequestOnWriteParameters(0x28, new byte[] { nArray, 0xFF, 0xFF, Helper.IntToBinDec(date.Hour), Helper.IntToBinDec(date.Day), Helper.IntToBinDec(date.Month), Helper.IntToBinDec(date.Year - 2000), 0xFF, TimeInterval });
            var rsp = Send(request);
            if (!rsp.success)
            {
                return rsp;
            }

            dynamic rspAddress;

            var inProcess = true;
            do
            {
                rspAddress = Send(MakeRequestParameters(0x18, new byte[] { 0x00 }));
                if (!rspAddress.success) return rspAddress;

                if (rspAddress.Body[0] == 0)
                {
                    inProcess = false;
                }
                else if (rspAddress.Body[0] > 1)
                {
                    rspAddress.errorcode = DeviceError.DEVICE_EXCEPTION;
                    rspAddress.success = false;
                    switch ((byte)rspAddress.Body[0])
                    {
                        case 0x2:
                            rspAddress.error = "Запрошенный заголовок не найден";
                            break;
                        case 0x3:
                            rspAddress.error = "Внутренняя аппаратная ошибка счетчика. Не отвечает память указателя поиска";
                            break;
                        case 0x4:
                            rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы указателя поиска";
                            break;
                        case 0x5:
                            rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы дескриптора поиска";
                            break;
                        case 0x6:
                            rspAddress.error = "Внутренняя аппаратная ошибка счетчика. Не отвечает память массива профиля";
                            break;
                        case 0x7:
                            rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы заголовка в массиве профиля";
                            break;
                        case 0x8:
                            rspAddress.error = "Внутренняя логическая ошибка счетчика. Заголовок находится по адресу, где должна быть запись среза";
                            break;
                        case 0x9:
                            rspAddress.error = "Недопустимый номер массива поиска";
                            break;
                        case 0xA:
                            rspAddress.error = "Недопустимое время интегрирования профиля мощности в дескрипторе запроса (не соответствует времени интегрирования счетчика)";
                            break;
                        default:
                            rspAddress.error = string.Format("Неизвестная ошибка {0:X}h", rspAddress.Body[0]);
                            break;
                    }
                    return rspAddress;
                }
            }
            while (inProcess);
            
            return Send(MakeRequestProfiles(0x00, aMemory[nArray + 1], rspAddress.Body[3], rspAddress.Body[4], count));
        }
        #endregion Интерфейс
    }
}

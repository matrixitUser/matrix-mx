// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
//using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.Poll.Driver.Scylar
{
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

    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        public enum DeviceError
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION
        };

        private static Driver instance = null;

        public static void Log(string message, int level = 2)
        {
            if (instance != null)
            {
                instance.log(message, level);
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

        private byte[] SendSimple(byte[] data, int minWait = 500)
        {
            var buffer = new List<byte>();

            log(string.Format(">({1}) {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), level: 3);

            response();
            request(data);

            var timeout = 7500;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var waitToCollect = minWait / sleep;
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
                        if (waitCollected == waitToCollect)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("<({1}) {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, bool giveAdditionalTime = false)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
            {
                buffer = SendSimple(data, giveAdditionalTime ? 2000 : 500);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    answer.success = true;
                    answer.error = string.Empty;
                    answer.errorcode = DeviceError.NO_ERROR;
                    do
                    {
                        if (buffer.Length == 1)
                        {
                            if (buffer[0] == 0xe5)
                            {
                                break;
                            }
                            answer.error = "ответ не распознан";
                            answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                        }
                        else if (buffer.Length == 5)
                        {
                            if (buffer[0] == 0x10 && buffer[4] == 0x16)
                            {
                                if (CalcCrc8(new[] { buffer[1], buffer[2] }) == buffer[3])
                                {
                                    break;
                                }
                                answer.error = "контрольная сумма кадра не сошлась";
                                answer.errorcode = DeviceError.CRC_ERROR;
                            }
                            else
                            {
                                answer.error = "ответ не распознан";
                                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                            }
                        }
                        else if (buffer.Length == 9)
                        {
                            if (buffer[0] == 0x68 && buffer[3] == 0x68 && buffer[8] == 0x16 && buffer[1] == buffer[2] && buffer[1] == 3)
                            {
                                if (CalcCrc8(new byte[] { buffer[4], buffer[5], buffer[6] }) == buffer[7])
                                {
                                    break;
                                }
                                answer.error = "контрольная сумма кадра не сошлась";
                                answer.errorcode = DeviceError.CRC_ERROR;
                            }
                            else
                            {
                                answer.error = "ответ не распознан";
                                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                            }
                        }
                        else
                        {

                            if ((buffer[0] == 0x68) && (buffer[3] == 0x68) && (buffer[1] == buffer[2]))
                            {
                                if(buffer[buffer.Length - 1] == 0x16)
                                {
                                    if (CalcCrc8(buffer.Skip(4).Take(buffer.Length - 6)) == buffer[buffer.Length - 2])
                                    {
                                        break;
                                    }
                                    answer.error = "контрольная сумма кадра не сошлась";
                                    answer.errorcode = DeviceError.CRC_ERROR;
                                }
                                else
                                {
                                    answer.error = "конец сообщения не обнаружен";
                                    answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                                    //    break;
                                }
                            }
                            else
                            {
                                answer.error = "ответ не распознан";
                                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                            }
                        }
                        answer.success = false;
                    }
                    while (false);
                }
            }

            if (answer.success)
            {
                answer.Body = buffer.Skip(4).Take(buffer.Count() - 6).ToArray();
                answer.buffer = buffer.ToArray();
            }

            return answer;
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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            setArchiveDepth("Day", 2);

            instance = this;

            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            //#region networkAddress
            //if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            //{
            //    log("Отсутствуют сведения о сетевом адресе", level: 1);
            //    return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            //}
            //#endregion

            //#region KTr
            //if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            //{
            //    log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            //}
            //#endregion

            //#region password
            //if (!param.ContainsKey("password"))
            //{
            //    log("Отсутствуют сведения о пароле, принят по-умолчанию");
            //}
            //else
            //{
            //    password = arg.password;
            //}
            //#endregion

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
                            result = Wrap(() => All(components, hourRanges, dayRanges), password);
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
                            log(description, level: 1);
                            result = DriverHelper.Instance().MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = DriverHelper.Instance().MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            ////PREPARE
            var open = SlaveInitialization();
            if (!open.success)
            {
                log("не удалось открыть канал связи: " + open.error, level: 1);
                return DriverHelper.Instance().MakeResult(100, open.errorcode, open.error);
            }

            log("канал связи открыт");

            //ACTION
            return func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion


        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            var currentDate = DateTime.Now;

            //date = curDate.Date;
            setTimeDifference(DateTime.Now - currentDate);

            //log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", currentDate));

            if (getEndDate == null)
            {
                getEndDate = (type) => currentDate;
            }

            if (components.Contains("Constant"))
            {
                var constants = new List<dynamic>();

                var archiveReadInitialization = InitialReadArchive(ArchiveType.Hourly);
                if (!archiveReadInitialization.success)
                {
                    log(string.Format("Ошибка чтения констант (инициализация): {0}", archiveReadInitialization.error), level: 1);
                    return DriverHelper.Instance().MakeResult(103, archiveReadInitialization.errorcode, archiveReadInitialization.error);
                }

                dynamic record = null;
                for (int i = 0; i < 5; i++)
                {
                    if(cancel())
                    {
                        return DriverHelper.Instance().MakeResult(103, DeviceError.NO_ERROR, "опрос отменён");
                    }

                    record = GetNextRecord(0x7B);
                    if (record.success)
                        break;

                    log(string.Format("Попытка чтения констант {0} неуспешна: {1}", i + 1, record.error));
                }

                if (record.success)
                {
                    constants = DriverHelper.ParseConstantData(record.buffer, currentDate);
                }

                log(string.Format("Константы прочитаны: всего {0}", constants.Count));
                records(constants);
            }

            if (components.Contains("Current"))
            {
                var currents = new List<dynamic>();



                log(string.Format("Текущие на {0} прочитаны: всего {1}", currentDate, currents.Count), level: 1);
                records(currents);
            }

            if (components.Contains("Hour"))
            {
                List<dynamic> recs = new List<dynamic>();

                //чтение часовых
                var startH = getStartDate("Hour");
                var endH = getEndDate("Hour");

                dynamic archive = Survey(ArchiveType.Hourly, startH);
                if (!archive.success)
                {
                    log(string.Format("Ошибка при опросе часовых: {0}", archive.error), level: 1);
                    return DriverHelper.Instance().MakeResult(105, archive.errorcode, archive.error);
                }
                else
                {
                    recs = archive.records;
                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, recs.Count), level: 1);
                }
            }

            if (components.Contains("Day"))
            {
                List<dynamic> recs = new List<dynamic>();

                //чтение суточных
                var startD = getStartDate("Day");
                var endD = getEndDate("Day");

                dynamic archive = Survey(ArchiveType.Daily, startD);
                if(!archive.success)
                {
                    log(string.Format("Ошибка при опросе суточных: {0}", archive.error), level: 1);
                    return DriverHelper.Instance().MakeResult(106, archive.errorcode, archive.error);
                }
                else
                {
                    recs = archive.records;
                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, recs.Count), level: 1);
                }
            }

            ///// Нештатные ситуации ///
            //if (components.Contains("Abnormal"))
            //{
            //    var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
            //    var startAbnormal = lastAbnormal.Date;

            //    var endAbnormal = getEndDate("Abnormal");
            //    byte[] codes = new byte[] { };

            //    List<dynamic> abnormals = new List<dynamic>();

            //    log(string.Format("получено {0} записей НС за период", abnormals.Count));//{1:dd.MM.yy}, date));
            //    records(abnormals);
            //}
            return DriverHelper.Instance().MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }

        #region MakeRequest
        private byte CalcCrc8(IEnumerable<byte> buffer)
        {
            byte ret = 0;
            foreach (var b in buffer)
            {
                ret += b;
            }
            return ret;
        }

        private byte[] NestSimplePacket(byte[] packet)
        {
            if (packet == null) return null;
            var result = new List<byte>(packet) { CalcCrc8(packet), 0x16 };
            result.Insert(0, 0x10);
            return result.ToArray();
        }

        private byte[] NestComplexPacket(byte[] packet)
        {
            if (packet == null) return null;
            var length = (byte)packet.Length;
            var result = new List<byte>(packet) { CalcCrc8(packet), 0x16 };
            result.Insert(0, 0x68);
            result.Insert(0, length);//длину надо вставить 2 раза
            result.Insert(0, length);
            result.Insert(0, 0x68);
            return result.ToArray();
        }

        private byte[] MakeSnd_Nke()
        {
            //return new byte[] { 0x10, 0x40, 0xFE, 0x3E, 0x16 };
            var buf = new byte[] { 0x40, 0xFE };
            return NestSimplePacket(buf);
        }

        private byte[] GetNextRecordMessage(byte fcv)
        {
            byte[] buffer = { fcv, 0xFE };
            return NestSimplePacket(buffer);
        }

        private byte[] MakeArchiveReadMessage(ArchiveType archiveType)
        {
            byte[] buf = { 0x73, 0xFE, 0x50, (byte)archiveType };
            return NestComplexPacket(buf);
        }
        #endregion

        private dynamic SlaveInitialization()
        {
            return Send(MakeSnd_Nke());
        }

        private dynamic InitialReadArchive(ArchiveType archiveType)
        {
            return Send(MakeArchiveReadMessage(archiveType));
        }

        private dynamic GetNextRecord(byte fcv)
        {
            return Send(GetNextRecordMessage(fcv), giveAdditionalTime: true);
        }



        private string GetDataFormatString(ArchiveType archiveType)
        {
            switch (archiveType)
            {
                case ArchiveType.Daily:
                case ArchiveType.Monthly:
                    return "dd.MM.yyyy";
                default:
                    return "dd.MM.yyyy HH:mm";
            }
        }

        private dynamic Survey(ArchiveType archiveType, DateTime dateEnd)
        {
            dynamic result = new ExpandoObject();
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;
            result.success = false;

            byte[] fcv = { 0x7B, 0x5B };
            int ifcv = 0;

            var currentDateTime = DateTime.MaxValue;
            int errorTryCounter = 0;

            var recs = new List<dynamic>();

            //инициируем чтение архива
            var archiveReadInitialization = InitialReadArchive(archiveType);
            if (!archiveReadInitialization.success)
            {
                //log(string.Format("Ошибка чтения часовых (инициализация): {0}", archiveReadInitialization.error), level: 1);
                result.error = string.Format("Ошибка чтения часовых (инициализация): {0}", archiveReadInitialization.error);
                result.errorcode = archiveReadInitialization.errorcode;
                return result;
                //return DriverHelper.Instance().MakeResult(105, archiveReadInitialization.errorcode, archiveReadInitialization.error);
            }
            DateTime lastDate = DateTime.MaxValue;

            do
            {
                dynamic record = null;
                for (int i = 0; i < 5; i++)
                {
                    if (cancel())
                    {
                        result.error = "опрос отменён";
                        result.errorcode = DeviceError.NO_ERROR;
                        return result;
                    }

                    record = GetNextRecord(fcv[ifcv]);
                    if (record.success)
                        break;

                    log(string.Format("Попытка чтения часовых {0} неуспешна: {1}", i + 1, record.error));
                }

                Tuple<DateTime, IEnumerable<dynamic>> parsedData = null;
                if (record.success)
                {
                    parsedData = DriverHelper.ParseData(record.buffer, archiveType); // Parser.Parse(record.Body, archiveType); //
                }

                if (parsedData == null)
                {
                    result.error = "Данные не получены";
                    //result.State = SurveyResultState.PartialyRead;
                    errorTryCounter++;
                }
                else
                {
                    if (parsedData.Item1 > lastDate)
                    {
                        break;
                    }

                    lastDate = parsedData.Item1;
                }


                if (!record.success || parsedData == null)
                {
                    result.error = "Данные не получены";
                    errorTryCounter++;
                }
                else if (parsedData.Item1 == default(DateTime))
                {
                    log("Окончание чтения архива");
                    break;
                }
                else
                {
                    var r = new List<dynamic>();
                    errorTryCounter = 0;
                    currentDateTime = parsedData.Item1;
                    if (parsedData.Item2 != null)
                    {
                        r.AddRange(parsedData.Item2);//.OrderByDescending(x => x.Value));
                    }
                    log(string.Format("Данные за {0} получены", currentDateTime.ToString(GetDataFormatString(archiveType))));
                    records(r);
                    recs.AddRange(r);
                }
                ifcv = 1 - ifcv;
            }
            while ((currentDateTime > dateEnd) && (errorTryCounter < 5));

            if (errorTryCounter < 5)
            {
                result.success = true;
            }

            result.records = recs;
            return result;
        }
    }
}

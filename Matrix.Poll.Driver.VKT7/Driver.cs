// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// в настройках проекта укажите OLD_DRIVER, если собирается драйвер для системы ниже 3.1.1

#if OLD_DRIVER
#warning Драйвер для старой системы
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Dynamic;

namespace Matrix.Poll.Driver.VKT7
{
    /// <summary>
    /// Драйвер для ВКТ-7
    /// 
    /// ранее был добавлена команда mock для оптимизации по скорости
    /// 26.01.2017 добавлен DeviceError
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        /// <summary>
        /// отправка сообщения устройству
        /// </summary>
        /// <typeparam name="TResponse">тип ожидаемого ответа</typeparam>
        /// <param name="request">запрос</param>
        /// <returns></returns>

        byte NetworkAddress = 0;
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private bool mockMode = false;
        private bool monthPoll = false;

        private bool MOCK_ZONE = false;

        private int msg2cnt = 0;

        private int mid = 0;

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

        #region Common
        private byte[] SendSimple(byte[] data, int attempt)
        {
            var buffer = new List<byte>();

            //if (debugMode) log(string.Format("{1}> {0}", string.Join(",", data.Select(b => b.ToString("X2"))), attempt));
            log(string.Format("{1:X}.OUT {0}", string.Join(",", data.Select(b => b.ToString("X2"))), mid), level: 3);


            if (mockMode && MOCK_ZONE)
            {
                var answer = new byte[] { };
                var outStr = string.Join(",", data.Select(b => b.ToString("X2")));
                if (outStr == "FF,FF,01,10,3F,FD,00,00,02,06,00,7E,E2")
                {
                    answer = new byte[] { 0x01, 0x10, 0x3F, 0xFD, 0x00, 0x00, 0x5D, 0xED };
                }
                else if (outStr == "FF,FF,01,10,3F,FF,00,00,30,2C,00,00,40,07,00,2D,00,00,40,07,00,2E,00,00,40,07,00,2F,00,00,40,07,00,30,00,00,40,07,00,35,00,00,40,07,00,37,00,00,40,07,00,38,00,00,40,07,00,2F,7C")
                {
                    answer = new byte[] { 0x01, 0x10, 0x3F, 0xFF, 0x00, 0x00, 0xFC, 0x2D };
                }
                else if (outStr == "FF,FF,01,03,3F,FE,00,00,28,2E")
                {
                    if (msg2cnt == 0)
                    {
                        answer = new byte[] { 0x01, 0x03, 0x39, 0x02, 0x00, 0xF8, 0x43, 0xC0, 0x00, 0x04, 0x00, 0xAC, 0x33, 0x2F, 0xE7, 0xC0, 0x00, 0x03, 0x00, 0x20, 0xAC, 0x33, 0xC0, 0x00, 0x02, 0x00, 0x20, 0xE2, 0xC0, 0x00, 0x06, 0x00, 0xAA, 0xA3, 0x2F, 0xE1, 0xAC, 0x32, 0xC0, 0x00, 0x04, 0x00, 0x83, 0xAA, 0xA0, 0xAB, 0xC0, 0x00, 0x01, 0x00, 0xE7, 0xC0, 0x00, 0x03, 0x00, 0x20, 0xAC, 0x33, 0xC0, 0x00, 0x7E, 0xC6 };
                    }
                    else
                    {
                        answer = new byte[] { 0x01, 0x03, 0x15, 0x02, 0xC0, 0x00, 0x02, 0xC0, 0x00, 0x02, 0xC0, 0x00, 0x02, 0xC0, 0x00, 0x02, 0xC0, 0x00, 0x02, 0xC0, 0x00, 0x03, 0xC0, 0x00, 0xC7, 0x04 };
                    }
                    msg2cnt++;
                }
                else if (outStr == "FF,FF,01,10,3F,FF,00,00,2A,39,00,00,40,01,00,3B,00,00,40,01,00,3C,00,00,40,01,00,3D,00,00,40,01,00,45,00,00,40,01,00,46,00,00,40,01,00,4C,00,00,40,01,00,80,64")
                {
                    answer = new byte[] { 0x01, 0x10, 0x3F, 0xFF, 0x00, 0x00, 0xFC, 0x2D };
                }
                buffer.AddRange(answer);
            }


            if (buffer.Count == 0)
            {
                response();
                request(data);

                var collectCycles = attempt == 0 ? 0 : (attempt == 1 ? 2 : 6);
                var timeout = attempt == 0 ? 5000 : (attempt == 1 ? 6000 : 7500);
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
                            if (waitCollected == collectCycles)
                            {
                                isCollected = true;
                            }
                        }
                    }
                }
            }


            log(string.Format("{1:X}.IN {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), mid), level: 3);

            mid++;

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

        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.code = 0;

            byte[] buffer = null;

            for (var attempt = 0; attempt < 3 && answer.success == false; attempt++)
            {
                buffer = SendSimple(data, attempt);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length <= 4)
                    {
                        answer.error = "Слишком короткий ответ";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (!Crc.Check(buffer.ToArray(), new Crc16Modbus()))
                    {
                        answer.error = "Не сошлась контрольная сумма";
                        answer.errorcode = DeviceError.CRC_ERROR;
                    }
                    else if (buffer[1] >= 0x80)
                    {
                        var code = buffer[2];
                        answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                        switch (code)
                        {
                            case 2:
                                answer.error = "Несуществующий тип значений";
                                break;

                            case 3:
                                answer.error = "В архиве отсутствуют данные за эту дату";
                                break;

                            case 5:
                                answer.error = "Зафиксировано изменение схемы измерения";
                                break;
                        }
                        answer.code = code;
                    }
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                    }
                }
            }

            if (answer.success)
            {
                answer.code = 0;
                answer.NetworkAddress = buffer[0];
                answer.Function = buffer[1];
                answer.data = buffer;
                answer.Body = buffer.Skip(2).Take(buffer.Length - 4).ToArray();
            }

            return answer;
        }
        #endregion

        #region Export

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            if (!param.ContainsKey("networkAddress") || !byte.TryParse(param["networkAddress"].ToString(), out NetworkAddress))
            {
                log(string.Format("Отсутствуют сведения о сетевом адресе, принят по умолчанию {0}", NetworkAddress));
            }
            else
            {
                log(string.Format("указан сетевой адрес: {0}", NetworkAddress));
            }

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

            if (param.ContainsKey("cmd"))
            {
                var cmds = (arg.cmd as string).Split(' ');
                foreach (var cmd in cmds)
                {
                    var args = cmd.Split('=');
                    switch (args[0])
                    {
#if OLD_DRIVER
                        case "debug":
                            if ((args.Length > 0) && (args[1] == "1"))
                            {
                                debugMode = true;
                            }
                            break;
#endif

                        case "mock":
                            if ((args.Length > 0) && (args[1] == "1"))
                            {
                                mockMode = true;
                            }
                            break;

                        case "month":
                            if ((args.Length > 0) && (args[1] == "1"))
                            {
                                monthPoll = true;
                            }
                            break;
                    }
                }
            }

            #region channels
            int[] channels = null;
            if (param.ContainsKey("channel")) //1; 2; 1,2; _
            {
                try
                {
                    string ch = arg.channel.ToString();
                    channels = ch.Split(',').Select(c => int.Parse(c)).ToArray();
                }
                catch (Exception ex)
                {

                }
            }

            if (channels == null || !channels.Any())
            {
                channels = new[] { 1 };
            }
            #endregion


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


            try
            {
                switch (what.ToLower())
                {
                    case "all": return Wrap(() => All(components, hourRanges, dayRanges, channels));
                        //case "ping": return Wrap(() => Ping());
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                return MakeResult(999, DeviceError.NO_ERROR, ex.Message);
            }

            log(string.Format("неопознаная команда '{0}'", what), level: 1);
            return MakeResult(201, DeviceError.NO_ERROR, what);
        }
        #endregion

        #region Интерфейс

        private dynamic Wrap(Func<dynamic> act)
        {
            //PREPARE
            var response = Send(MakeReadHelloRequest());
            if (!response.success)
            {
                var desc = string.Format("не удалось открыть канал связи: {0}", response.error);
                log(desc, level: 1);
                return MakeResult(101, response.errorcode, desc);
            }

            log("канал связи открыт");

            //ACTION
            return act();
        }

        private dynamic Ping()
        {
            log("Проверка связи прошла успешно", level: 1);
            return MakeResult(0, DeviceError.NO_ERROR, "Проверка связи прошла успешно");
        }

        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges, IEnumerable<int> channels)
        {
            var rules = new[] {
                //0
                new { offset = 0x00000000, length = 0x00000002 },
                new { offset = 0x00000011, length = 0x00000001 },
                new { offset = 0x00000023, length = 0x00000001 },
                new { offset = 0x00000028, length = 0x00000002 },
                new { offset = 0x00000033, length = 0x00000001 },
                new { offset = 0x0000003f, length = 0x00000001 },
                new { offset = 0x0000004d, length = 0x00000001 },
                new { offset = 0x00000059, length = 0x00000001 },
                new { offset = 0x0000006b, length = 0x00000001 },
                new { offset = 0x00000079, length = 0x00000001 },
                //10
                new { offset = 0x00000085, length = 0x00000001 },
                new { offset = 0x00000097, length = 0x00000001 },
                new { offset = 0x000000a5, length = 0x00000001 },
                new { offset = 0x000000b1, length = 0x00000001 },
                new { offset = 0x000000c1, length = 0x00000001 },
                new { offset = 0x000000cd, length = 0x00000001 },
                new { offset = 0x000000db, length = 0x00000001 },
                new { offset = 0x000000e7, length = 0x00000001 },
                new { offset = 0x000000f9, length = 0x00000001 },
                new { offset = 0x00000107, length = 0x00000001 },
                //20
                new { offset = 0x00000113, length = 0x00000001 },
                new { offset = 0x00000125, length = 0x00000001 },
                new { offset = 0x00000133, length = 0x00000001 },
                new { offset = 0x0000013f, length = 0x00000001 },
                new { offset = 0x00000147, length = 0x00000001 },
                new { offset = 0x00000200, length = 0x00000002 },
                new { offset = 0x00000211, length = 0x00000001 },
                new { offset = 0x00000223, length = 0x00000001 },
                new { offset = 0x00000228, length = 0x00000002 },
                new { offset = 0x00000204, length = 0x00000026 },
                //30
                new { offset = 0x0000022a, length = 0x00000001 },
                new { offset = 0x0000022e, length = 0x00000002 },
                new { offset = 0x000002b8, length = 0x00000001 },
                new { offset = 0x000002bc, length = 0x00000002 }
            };
            //var server = ParseReadServerVersionResponse(Send(MakeReadDataRequest()));

            int serverVersion = 1;
            //if (server.success)
            //{
            //    log(string.Format("Версия сервера: {0}", server.version));
            //    serverVersion = server.version;
            //}
            //else
            //{
            //    log(string.Format("Версия сервера не получена: {0}", server.error));
            //    serverVersion = 0;
            //}

            var date = DateTime.MinValue;
            dynamic info = null;

            var constants = new List<dynamic>();
            var currents = new List<dynamic>();

            info = ParseReadInfoResponse(Send(MakeReadInfoRequest()));
            if (!info.success)
            {
                log(string.Format("Ошибка при считывании текущей даты на вычислителе: {0}", info.error), level: 1);
                return MakeResult(102, info.errorcode, info.error);
            }

            setContractDay(info.TotalDay);

            var curDate = ParseReadCurrentDateResponse(Send(MakeReadCurrentDateRequest()), info.Version);
            if (!curDate.success)
            {
                log(string.Format("Ошибка при считывании текущей даты на вычислителе: {0}", curDate.error), level: 1);
                return MakeResult(102, curDate.errorcode, curDate.error);
            }

            date = curDate.Date;
            setTimeDifference(DateTime.Now - date);

            log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", date));

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            if (components.Contains("Constant"))
            {
                var constant = GetConstants(date, info);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                    return MakeResult(103, constant.errorcode, constant.error);
                }

                constants = constant.records as List<dynamic>;

                byte[] db;
                {
                    List<byte> temp = new List<byte>();
                    for (int i = 0; i < 8; i++)
                    {
                        var writeDb1 = ParseWriteResponse(Send(MakeWriteRequest(0x3ff7, 2, BitConverter.GetBytes((UInt16)(1320 + i)))));
                        if (!writeDb1.success)
                        {
                            log(string.Format("Ошибка при считывании констант: {0}", writeDb1.error));
                            return MakeResult(103, writeDb1.errorcode, writeDb1.error);
                        }

                        var readDb1 = ParseReadResponse(Send(MakeReadRequest(0x3ff8, 0x80)));
                        if (!readDb1.success)
                        {
                            log(string.Format("Ошибка при считывании констант: {0}", readDb1.error));
                            return MakeResult(103, readDb1.errorcode, readDb1.error);
                        }
                        temp.AddRange(readDb1.Body as IEnumerable<byte>);
                        //byte[] data = (readDb1.Body as IEnumerable<byte>).ToArray();
                    }
                    db = temp.ToArray();
                }

                {
                    byte A = db[0x212];
                    byte B = db[0x213];

                    foreach (var r in rules)
                    {
                        for (int i = 0; i < r.length; i++)
                        {
                            db[r.offset + i] = 0;
                        }
                    }

                    if (info.Version >= 0x22)
                    {
                        db[0x212] = A;
                        db[0x213] = B;
                    }
                }


                var crc = Crc.Calc(db, new Crc16Modbus());


                constants.Add(MakeConstRecord("КС", $"0x{crc.CrcData[1]:X2}{crc.CrcData[0]:X2}", date));
                constants.Add(MakeConstRecord("tх, град. C", (double)BitConverter.ToInt16(db, 18) / 100.0, date));


                log(string.Format("Константы прочитаны: всего {0}, отчётный день={1}", constants.Count, constant.TotalDay));
                records(constants);
            }

            //

            MOCK_ZONE = true;
            var properties = GetProperties(serverVersion);
            if (!properties.success)
            {
                log(string.Format("Ошибка при считывании свойств: {0}", properties.error), level: 1);
                return MakeResult(103, properties.errorcode, properties.error);
            }
            log(string.Format("Свойства прочитаны: всего - ед. измерений {0}, дробных частей {1}", properties.Units.Count, properties.Fracs.Count), level: 1);
            MOCK_ZONE = false;

            if (components.Contains("Current"))
            {
                var current = GetCurrents(properties, date, channels);
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count), level: 1);
                records(currents);
            }

            //records(cncs.constants);

            List<dynamic> hours = new List<dynamic>();
            List<dynamic> days = new List<dynamic>();
            List<dynamic> months = new List<dynamic>();

            if (components.Contains("Hour"))
            {
                if (hourRanges != null)
                {
                    foreach (var range in hourRanges)
                    {
                        var startH = range.start;
                        var endH = range.end;

                        if (startH > date) continue;
                        if (endH > date) endH = date;

                        var hour = GetHours(startH, endH, date, properties, channels);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                            return MakeResult(105, hour.errorcode, hour.error);
                        }
                        hours = hour.records;
                        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                    }
                }
                else
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");

                    var hour = GetHours(startH, endH, date, properties, channels);
                    if (!hour.success)
                    {
                        log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        return MakeResult(105, hour.errorcode, hour.error);
                    }
                    hours = hour.records;
                    log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                }
            }

            if (components.Contains("Day"))
            {
                if (dayRanges != null)
                {
                    foreach (var range in dayRanges)
                    {
                        var startD = range.start;
                        var endD = range.end;

                        if (startD > date) continue;
                        if (endD > date) endD = date;

                        var day = GetDays(startD, endD, date, properties, info.TotalDay, channels);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.errorcode, day.error);
                        }
                        days = day.records;
                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                else
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    var day = GetDays(startD, endD, date, properties, info.TotalDay, channels);
                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        return MakeResult(104, day.errorcode, day.error);
                    }
                    days = day.records;
                    log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }

            }

            if (components.Contains("Day") && monthPoll)
            {
                //чтение ежемесячных
                var startM = date.AddMonths(-3);
                var endM = date;

                var month = GetMonths(startM, endM, date, properties, info.TotalDay);
                if (!month.success)
                {
                    log(string.Format("Ошибка при считывании ежемесячных: {0}", month.error), level: 1);
                    return MakeResult(104, month.errorcode, month.error);
                }
                months = month.records;
                log(string.Format("Прочитаны ежемесячные с {0:MM.yyyy} по {1:MM.yyyy}: {2} записей", startM, endM, months.Count), level: 1);
            }

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

            //log(string.Format("Успешно прочитано: {0}/{1}/{2}/{3}/{4} записей", constants.Count, currents.Count, days.Count, hours.Count, months.Count));
            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }

        #endregion
    }
}

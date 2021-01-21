using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public struct DateTimeRange
    {
        public DateTime start;
        public DateTime end;
        public static DateTimeRange FromDynamic(dynamic source)
        {
            DateTimeRange dtr = new DateTimeRange();
            dtr.start = DateTime.MinValue;
            dtr.end = DateTime.MaxValue;
            if (source is IDictionary<string, object>)
            {
                IDictionary<string, object> dsource = source as IDictionary<string, object>;
                if (dsource.ContainsKey("start") && source.start is DateTime)
                {
                    dtr.start = (DateTime)source.start;
                }
                if (dsource.ContainsKey("end") && source.end is DateTime)
                {
                    dtr.end = (DateTime)source.end;
                }
            }
            return dtr;
        }
    }

    public class Result
    {
        public string description;
        public int code;
        public DeviceError devicecode;
        public dynamic ToDynamic()
        {
            dynamic ret = new ExpandoObject();
            ret.description = description;
            ret.code = code;
            ret.devicecode = devicecode;
            return ret;
        }
    }

    public class Driver
    {
        public Dictionary<byte, CurrentParameter> currentParameter = new Dictionary<byte, CurrentParameter>()
        {
            { (byte)0, new CurrentParameter { P=0, name="Q1", description = "расход теплоносителя в подающем или обратном трубопроводе", unit = "м3/с", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)1, new CurrentParameter { P=1, name="Q2", description = "расход теплоносителя в обратном или третьем трубопроводе", unit = "м3/с", vmask = VersionMask.No_SA94_1, type = CurrentParameterType.Float } },
            { (byte)2, new CurrentParameter { P=2, name="T1", description = "температура теплоносителя в подающем трубопроводе", unit = "°C", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)3, new CurrentParameter { P=3, name="T2", description = "температура теплоносителя в обратном трубопроводе", unit = "°C", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)4, new CurrentParameter { P=4, name="T3", description = "температура теплоносителя в третьем трубопроводе (при его наличии)", unit = "°C", vmask = VersionMask.No_SA94_1, type = CurrentParameterType.Float } },
            { (byte)5, new CurrentParameter { P=5, name="dT", description = "разность температур теплоносителя в трубопроводах", unit = "°C", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)6, new CurrentParameter { P=6, name="P", description = "потребляемая тепловая мощность", unit = "кВт", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)7, new CurrentParameter { P=7, name="E", description = "количество теплоты", unit = "MВт*ч", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)8, new CurrentParameter { P=8, name="V1", description = "объем теплоносителя, прошедшая через первый преобразователь расхода", unit = "м3", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            { (byte)9, new CurrentParameter { P=9, name="V2", description = "объем теплоносителя, прошедшая через второй преобразователь расхода", unit = "м3", vmask = VersionMask.No_SA94_1, type = CurrentParameterType.Float } },
            { (byte)10, new CurrentParameter { P=10, name="time", description = "Суточное время", unit = "", vmask = VersionMask.SA94_All, type = CurrentParameterType.Time } },
            { (byte)11, new CurrentParameter { P=11, name="date", description = "Дата", unit = "", vmask = VersionMask.SA94_All, type = CurrentParameterType.Date } },
            { (byte)12, new CurrentParameter { P=12, name="tw", description = "Время работы теплосчетчика в режиме \"Работа\" и \"Счет\"", unit = "с", vmask = VersionMask.SA94_All, type = CurrentParameterType.Float } },
            //{ (byte)13, new CurrentParameter { P=13, name="undefined", description = "", unit = "" } },
            { (byte)14, new CurrentParameter { P=14, name="p1", description = "измерение давления в первом трубопроводе", unit = "МПа", vmask = VersionMask.No_SA94_100_200_300_M100_M200_M300, type = CurrentParameterType.Float } },
            { (byte)15, new CurrentParameter { P=15, name="p2", description = "измерение давления во втором трубопроводе", unit = "МПа", vmask = VersionMask.No_SA94_100_200_300_M100_M200_M300, type = CurrentParameterType.Float } }
        };

#if OLD_DRIVER
        bool debugMode = false;
#endif

        UInt32 NetworkAddress = 0;
        Version ver;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private Dictionary<int, byte[]> blockCacheHour = new Dictionary<int, byte[]>();
        private Dictionary<int, byte[]> blockCacheDay = new Dictionary<int, byte[]>();


        #region Common
        public void log(string message, int level = 2)
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

        public static Result MakeResult(int code, DeviceError devicecode = DeviceError.NO_ERROR, string description = "")
        {
            Result result = new Result();

            switch (devicecode)
            {
                case DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.devicecode = devicecode;
            result.description = description;
            //result.success = code == 0 ? true : false;
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

        [Import("setContractHour")]
        private Action<int> setContractHour;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            //setArchiveDepth("Day", 2);

            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion

            #region version
            if (!param.ContainsKey("version") || !(arg.version is string))
            {
                log("Отсутствуют сведения о версии устройства", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "версия устройства");
            }

            try
            {
                ver = Parser.ParseVersion((arg.version as string), log);
                log($"Выбрана версия прибора: {ver.verText} ({ver.verHw} {ver.verSw})");
            }
            catch (Exception ex)
            {
                log($"Не распознана версия прибора, ожидается строка вида \"100\" или \"M301\": {ex.Message}", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "версия прибора");
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
            List<DateTimeRange> hourRanges;
            if (param.ContainsKey("hourRanges") && arg.hourRanges is IEnumerable<dynamic>)
            {
                hourRanges = (arg.hourRanges as IEnumerable<dynamic>).Select(hr => (DateTimeRange)DateTimeRange.FromDynamic(hr)).ToList();
                foreach (var range in hourRanges)
                {
                    log(string.Format("принят часовой диапазон {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", range.start, range.end));
                }
            }
            else
            {
                hourRanges = new List<DateTimeRange>();
                DateTimeRange defaultrange = new DateTimeRange();
                defaultrange.start = getStartDate("Hour");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Hour");
                hourRanges.Add(defaultrange);
            }
            #endregion

            #region dayRanges
            List<DateTimeRange> dayRanges;
            if (param.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
            {
                dayRanges = (arg.dayRanges as IEnumerable<dynamic>).Select(dr => (DateTimeRange)DateTimeRange.FromDynamic(dr)).ToList();
                foreach (var range in dayRanges)
                {
                    log(string.Format("принят суточный диапазон {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", range.start, range.end));
                }
            }
            else
            {
                dayRanges = new List<DateTimeRange>();
                DateTimeRange defaultrange = new DateTimeRange();
                defaultrange.start = getStartDate("Day");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Day");
                dayRanges.Add(defaultrange);
            }
            #endregion


            Result result;
            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap((status) => All(components, hourRanges, dayRanges, ver, status));
                        }
                        break;

                    default:
                        {
                            var description = string.Format("неопознанная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (DriverException ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, ex.DeviceError, ex.Message);
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result.ToDynamic();
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

        private DeviceResponse Send(byte[] data, int timeOut = 7500, int attemptsMaximum = 1)
        {
            DeviceResponse answer = new DeviceResponse();
            byte[] buffer = null;
            for (var attempts = 0; attempts < attemptsMaximum; attempts++)
            {
                buffer = SendSimple(data, timeOut);
                if (buffer.Length == 0) throw new Exception("Нет ответа");
            }

            answer.Body = buffer;
            return answer;
        }

        private Result Wrap(Func<Status, Result> func)
        {
            //try
            //{
            //    Send(MakeRequest.DeviceUnselect(), 2000, 1);
            //}
            //catch (Exception ex)
            //{

            //}

            Status s;
            try
            {
                s = Parser.ParseDeviceSelectResponse(Send(MakeRequest.DeviceSelect(NetworkAddress)), ver.verHw);
                log("Прибор обнаружен");
            }
            catch (Exception ex)
            {
                log("Прибор НЕ обнаружен", level: 1);
                return MakeResult(100, DeviceError.NO_ERROR, ex.Message);
            }

            Result result = func(s);

            try
            {
                Send(MakeRequest.DeviceUnselect(), 2000, 1);
            }
            catch (Exception ex)
            {

            }
            return result;
        }
        #endregion



        private Result All(string components, List<DateTimeRange> hourRanges, List<DateTimeRange> dayRanges, Version ver, Status status)
        {
            DateTime currentDate;
            try
            {
                currentDate = Parser.ParseDate(Send(MakeRequest.ReadCurrentParameters(currentParameter.Where(cp => cp.Value.type == CurrentParameterType.Date).FirstOrDefault().Value.P)).Body, 0)
                    .Add(Parser.ParseTime(Send(MakeRequest.ReadCurrentParameters(currentParameter.Where(cp => cp.Value.type == CurrentParameterType.Time).FirstOrDefault().Value.P)).Body, 0));
            }
            catch (DriverException ex)
            {
                return MakeResult(201, ex.DeviceError, $"Не удалось получить дату/время: {ex.Message}");
            }
            catch (Exception ex)
            {
                return MakeResult(201, DeviceError.NO_ERROR, $"Не удалось получить дату/время: {ex.Message}");
            }

            setTimeDifference(DateTime.Now - currentDate);
            log(string.Format("Дата/время на вычислителе: {0:dd.MM.yy HH:mm:ss}", currentDate));

            if (getEndDate == null)
            {
                getEndDate = (type) => currentDate;
            }

            if (components.Contains("Current"))
            {
                try
                {
                    List<dynamic> current = new List<dynamic>();
                    CurrentParameter par;
                    foreach (var cp in currentParameter.Where(c => c.Value.type == CurrentParameterType.Float && c.Value.name != "tw"))
                    {
                        par = cp.Value;
                        if (((par.vmask == VersionMask.No_SA94_1) && (ver.verHw == VersionHardware.SA94_1))
                            || ((par.vmask == VersionMask.No_SA94_100_200_300_M100_M200_M300) && (
                                ver.verSw == VersionSoftware.ver100
                                || ver.verSw == VersionSoftware.ver200
                                || ver.verSw == VersionSoftware.ver300
                                || ver.verSw == VersionSoftware.verM100
                                || ver.verSw == VersionSoftware.verM200
                                || ver.verSw == VersionSoftware.verM300))) continue;
                        current.Add(MakeRecord.Current(par.name, Parser.ParseFloat(Send(MakeRequest.ReadCurrentParameters(par.P)).Body, 0), par.unit, currentDate));
                    }

                    par = currentParameter.Where(c => c.Value.name == "tw").FirstOrDefault().Value;
                    if (par.unit == "с")
                    {
                        current.Add(MakeRecord.Current(par.name, Parser.ParseFloat(Send(MakeRequest.ReadCurrentParameters(par.P)).Body, 0) / 3600.0, "ч", currentDate));
                    }
                    else if (par.unit == "мин")
                    {
                        current.Add(MakeRecord.Current(par.name, Parser.ParseFloat(Send(MakeRequest.ReadCurrentParameters(par.P)).Body, 0) / 60.0, "ч", currentDate));
                    }
                    else
                    {
                        current.Add(MakeRecord.Current(par.name, Parser.ParseFloat(Send(MakeRequest.ReadCurrentParameters(par.P)).Body, 0), par.unit, currentDate));
                    }

                    records(current);
                    log(string.Format("Текущие на {0} прочитаны: всего {1}", currentDate, current.Count), level: 1);
                }
                catch (DriverException ex)
                {
                    log($"Не удалось получить текущие: {ex.Message}", level: 1);
                    return MakeResult(201, ex.DeviceError, $"Не удалось получить текущие: {ex.Message}");
                }
                catch (Exception ex)
                {
                    log($"Не удалось получить текущие: {ex.Message}", level: 1);
                    return MakeResult(201, DeviceError.NO_ERROR, $"Не удалось получить текущие: {ex.Message}");
                }
            }


            if (components.Contains("Constant"))
            {
                try
                {
                    byte[] sets1;
                    byte[] sets2;
                    if (ver.verSw == VersionSoftware.ver100 || ver.verSw == VersionSoftware.ver200 || ver.verSw == VersionSoftware.ver300 || ver.verSw == VersionSoftware.verM100 || ver.verSw == VersionSoftware.verM300) //двухбайтовая команда
                    {
                        sets1 = Send(MakeRequest.ReadStatistics(Segment.Service1, 0)).Body;
                        sets2 = Send(MakeRequest.ReadStatistics(Segment.Service1, 0x100)).Body;
                    }
                    else if (ver.verSw == VersionSoftware.ver101 || ver.verSw == VersionSoftware.ver201 || ver.verSw == VersionSoftware.verMTE1 || ver.verSw == VersionSoftware.ver301 || ver.verSw == VersionSoftware.verM101 || ver.verSw == VersionSoftware.verM301) //трехбайтовая команда
                    {
                        sets1 = Send(MakeRequest.ReadStatisticsExt(Segment.Service1, 0)).Body;
                        sets2 = Send(MakeRequest.ReadStatisticsExt(Segment.Service1, 0x100)).Body;
                    }
                    else
                    {
                        throw new Exception($"драйвер не рассчитан на считывание констант у версии {ver.verHw} {ver.verSw}");
                    }
                    Version testver = Parser.ParseVersion(sets2, 0, log);
                    if (testver.verSw != ver.verSw)
                    {
                        log($"Обнаружена версия прибора, которая отличается от выбранной: {testver.verText} ({testver.verHw} {testver.verSw})", level: 1);
                        ver = testver;
                    }
                    double Tprog = Helper.ToInt16Reverse(sets1, 0x09) / 100.0;

                    List<dynamic> constants = new List<dynamic>();
                    constants.Add(MakeRecord.Constant("Тип прибора", testver.GetVerHwText(), currentDate));
                    constants.Add(MakeRecord.Constant("Версия программы", testver.verText, currentDate));
                    constants.Add(MakeRecord.Constant("Расширенная статистика", testver.isExtended? "Да" : "Нет", currentDate));
                    constants.Add(MakeRecord.Constant("Модифицированный", testver.isM ? "Да" : "Нет", currentDate));
                    constants.Add(MakeRecord.Constant("Режим", status.b4_isModeCount ? "Счет" : "Стоп", currentDate));
                    if (ver.verHw == VersionHardware.SA94_1)
                    {
                        constants.Add(MakeRecord.Constant("Температура (T2)", status.b1_SA94_1_isT2Programmed ? "Программируется" : "Измеряется", currentDate));
                        if (status.b1_SA94_1_isT2Programmed)
                        {
                            constants.Add(MakeRecord.Constant("Значение Т2прог, °C", $"{Tprog:0.00}", currentDate));
                        }
                    }
                    else if ((ver.verHw == VersionHardware.SA94_2) || (ver.verHw == VersionHardware.SA94_2M))
                    {
                        constants.Add(MakeRecord.Constant("Температура (Т3)", status.b1_SA94_2_2M_isT3Measured ? "Измеряется" : "Программируется", currentDate));
                        if (!status.b1_SA94_2_2M_isT3Measured)
                        {
                            constants.Add(MakeRecord.Constant("Значение Т3прог, °C", $"{Tprog:0.00}", currentDate));
                        }
                    }
                    constants.Add(MakeRecord.Constant("Заводской номер", $"{(sets1[3] | ((UInt32)sets1[2] << 8) | ((UInt32)sets1[1] << 16)):000000}", currentDate));
                    constants.Add(MakeRecord.Constant("Разность dTmin, °C", $"{(Helper.ToInt16Reverse(sets1, 0x0D) / 100.0):0.00}", currentDate));
                    constants.Add(MakeRecord.Constant("Количество токов", Helper.GetNumOfCurrentsFromByte(sets1[0x18]), currentDate));
                    constants.Add(MakeRecord.Constant("Параметр для Iвых1", Helper.GetParamIoutFromByte(sets1[0x1A], ver.verHw), currentDate));
                    constants.Add(MakeRecord.Constant("Параметр для Iвых2", Helper.GetParamIoutFromByte(sets1[0x1B], ver.verHw), currentDate));
                    constants.Add(MakeRecord.Constant("Диапазон тока Iвых1, мА", Helper.GetRangeIoutFromByte(sets1[0x1E]), currentDate));
                    constants.Add(MakeRecord.Constant("Диапазон тока Iвых2, мА", Helper.GetRangeIoutFromByte(sets1[0x1F]), currentDate));
                    constants.Add(MakeRecord.Constant("Тип термодатчика", Helper.GetTermosensorTypeFromByte(sets1[0x1C]), currentDate));

                    constants.Add(MakeRecord.Constant("№ ЭМ датчика 1", $"{(sets1[0x23] | ((UInt32)sets1[0x22] << 8) | ((UInt32)sets1[0x21] << 16)):000000}", currentDate));
                    int Du1mm = Helper.GetDummFromByte(sets1[0x24], ver);
                    double Q1max = Helper.GetQmaxByDumm(sets1[0x25], Du1mm);
                    int Q1minPrc = Helper.GetQminFromByte(sets1[0x26]);
                    constants.Add(MakeRecord.Constant("Ду1, мм", Du1mm, currentDate));
                    constants.Add(MakeRecord.Constant("Q1max, м/ч", Q1max, currentDate));
                    constants.Add(MakeRecord.Constant("Q1min, %", Q1minPrc, currentDate));
                    constants.Add(MakeRecord.Constant("Q1min, м/ч", Q1max * Q1minPrc / 100.0, currentDate));

                    if (ver.isExtended)
                    {
                        constants.Add(MakeRecord.Constant("Давление p1max, МПа", Helper.GetpmaxFromByte(sets1[0x19]), currentDate));
                        constants.Add(MakeRecord.Constant("Давление p2max, МПа", Helper.GetpmaxFromByte(sets1[0x1D]), currentDate));
                        constants.Add(MakeRecord.Constant("Диапазон тока Iвх1, мА", Helper.GetRangeIinFromByte(sets1[0x27]), currentDate));
                    }

                    if (ver.verHw != VersionHardware.SA94_1)
                    {
                        constants.Add(MakeRecord.Constant("№ ЭМ датчика 2", $"{(sets1[0x2B] | ((UInt32)sets1[0x2A] << 8) | ((UInt32)sets1[0x29] << 16)):000000}", currentDate));

                        int Du2mm = Helper.GetDummFromByte(sets1[0x2C], ver);
                        double Q2max = Helper.GetQmaxByDumm(sets1[0x2D], Du2mm);
                        int Q2minPrc = Helper.GetQminFromByte(sets1[0x2E]);
                        constants.Add(MakeRecord.Constant("Ду2, мм", Du2mm, currentDate));
                        constants.Add(MakeRecord.Constant("Q2max, м/ч", Q2max, currentDate));
                        constants.Add(MakeRecord.Constant("Q2min, %", Q2minPrc, currentDate));
                        constants.Add(MakeRecord.Constant("Q2min, м/ч", Q2max * Q2minPrc / 100.0, currentDate));

                        if (ver.isExtended)
                        {
                            constants.Add(MakeRecord.Constant("Диапазон тока Iвх2, мА", Helper.GetRangeIinFromByte(sets1[0x2F]), currentDate));
                        }
                    }

                    records(constants);
                    log(string.Format("Константы на {0} прочитаны: всего {1}", currentDate, constants.Count), level: 1);
                }
                catch (DriverException ex)
                {
                    log($"Не удалось получить константы: {ex.Message}", level: 1);
                    return MakeResult(201, ex.DeviceError, $"Не удалось получить константы: {ex.Message}");
                }
                catch (Exception ex)
                {
                    log($"Не удалось получить константы: {ex.Message}", level: 1);
                    return MakeResult(201, DeviceError.NO_ERROR, $"Не удалось получить константы: {ex.Message}");
                }
            }

            if (components.Contains("Hour"))
            {
                try
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

                            var hour = GetHourly(startH, endH, currentDate, ver);
                            hours.AddRange(hour);

                            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                        }
                    }
                    else
                    {
                        //чтение часовых
                        var startH = getStartDate("Hour");
                        var endH = getEndDate("Hour");

                        var hour = GetHourly(startH, endH, currentDate, ver);
                        hours.AddRange(hour);

                        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                    }
                }
                catch (DriverException ex)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", ex.Message), level: 1);
                    return MakeResult(105, ex.DeviceError, ex.Message);
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании часовых: {0}", ex.Message), level: 1);
                    return MakeResult(105, DeviceError.NO_ERROR, ex.Message);
                }
            }

            if (components.Contains("Day"))
            {
                try
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

                            var day = GetDaily(startD.Date, endD, currentDate, ver);
                            days.AddRange(day);

                            log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                        }
                    }
                    else
                    {
                        //чтение суточных
                        var startD = getStartDate("Day");
                        var endD = getEndDate("Day");

                        var day = GetDaily(startD.Date, endD, currentDate, ver);
                        days.AddRange(day);

                        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                    }
                }
                catch (DriverException ex)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", ex.Message), level: 1);
                    return MakeResult(104, ex.DeviceError, ex.Message);
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании суточных: {0}", ex.Message), level: 1);
                    return MakeResult(104, DeviceError.NO_ERROR, ex.Message);
                }
            }

            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }

        public ParsedParameter[] ReadAndParseArchiveCached(Segment segment, int blockN, Version ver)
        {
            byte[] block;
            ParsedParameter[] ppars;

            switch (segment)
            {
                case Segment.Hourly1:
                case Segment.Hourly2:
                    {
                        int cacheBlockN = ((segment == Segment.Hourly2) ? 1024 : 0) + blockN;
                        if (blockCacheHour.ContainsKey(cacheBlockN))
                        {
                            block = blockCacheHour[cacheBlockN];
                        }
                        else
                        {
                            block = Send(MakeRequest.ReadStatisticsBlockByVer(segment, blockN, ver.isExtended)).Body;
                            blockCacheHour[cacheBlockN] = block;
                        }
                        ppars = Parser.ParseHourlyBlock(block, 0, ver);
                    }
                    break;

                case Segment.Daily1:
                case Segment.Daily2:
                    {
                        int cacheBlockN = ((segment == Segment.Daily2) ? 1024 : 0) + blockN;
                        if (blockCacheDay.ContainsKey(cacheBlockN))
                        {
                            block = blockCacheDay[cacheBlockN];
                        }
                        else
                        {
                            block = Send(MakeRequest.ReadStatisticsBlockByVer(segment, blockN, ver.isExtended)).Body;
                            blockCacheDay[cacheBlockN] = block;
                        }
                        ppars = Parser.ParseDailyBlock(block, 0, ver, null);
                    }
                    break;

                default:
                    throw new Exception("необходимо выбрать сегмент с архивом");
            }

            return ppars;
        }

        public List<dynamic> GetHourly(DateTime start, DateTime end, DateTime currentDate, Version ver)
        {
            List<dynamic> recs = new List<dynamic>();

            //поиск начальной даты в сегменте
            DateTime seg1Dt = ReadAndParseArchiveCached(Segment.Hourly1, 0, ver).DefaultIfEmpty().Select(s => s.date).Min();
            DateTime seg2Dt = ReadAndParseArchiveCached(Segment.Hourly2, 0, ver).DefaultIfEmpty().Select(s => s.date).Min();

            // сегмент пуст - должна быть максимальная дата
            if (seg1Dt == default(DateTime)) seg1Dt = DateTime.MaxValue;
            if (seg2Dt == default(DateTime)) seg2Dt = DateTime.MaxValue;


            if (seg1Dt == DateTime.MaxValue && seg2Dt == DateTime.MaxValue) //сегменты пусты
            {
                log("Часовых данных не обнаружено - сегменты пустые", level: 1);
            }
            else
            {
                // сегмент со старыми данными - первый, с новыми данными - последний
                Segment first;
                Segment last;
                DateTime firstDt;
                DateTime lsDt;

                if (seg1Dt < seg2Dt)
                {
                    first = Segment.Hourly1;
                    last = Segment.Hourly2;
                    firstDt = seg1Dt;
                    lsDt = seg2Dt;
                }
                else
                {
                    first = Segment.Hourly2;
                    last = Segment.Hourly1;
                    firstDt = seg2Dt;
                    lsDt = seg1Dt;
                }

                // поиск последней записи 
                DateTime currentDateH = currentDate.Date.AddHours(currentDate.Hour);
                int hoursOffset;
                int maxRecordOffset;

                if (lsDt == DateTime.MaxValue)
                {
                    hoursOffset = (int)(currentDateH - firstDt).TotalHours;
                    log($"тек.дата {currentDateH} нач.сегм {firstDt} смещ. {hoursOffset}");
                    maxRecordOffset = hoursOffset / ver.GetRecordsInBlock();
                }
                else
                {
                    hoursOffset = (int)(currentDateH - lsDt).TotalHours;
                    log($"тек.дата {currentDateH} нач.сегм {lsDt} смещ. {hoursOffset}");
                    maxRecordOffset = ver.GetRecordsInSegment() + hoursOffset / ver.GetRecordsInBlock();
                }

                DateTime lastRecordDt = DateTime.MaxValue;
                for (; maxRecordOffset > 0; maxRecordOffset--)
                {
                    Segment rseg;
                    int roff;
                    if (maxRecordOffset < ver.GetRecordsInSegment())
                    {
                        rseg = first;
                        roff = maxRecordOffset;
                    }
                    else
                    {
                        rseg = last;
                        roff = maxRecordOffset - ver.GetRecordsInSegment();
                    }

                    var ac = ReadAndParseArchiveCached(rseg, roff, ver);
                    lastRecordDt = ac.Select(s => s.date).DefaultIfEmpty().Min();
                    if (lastRecordDt == DateTime.MinValue) lastRecordDt = DateTime.MaxValue;
                    //log($"сегмент+блок{maxRecordOffset} сег{rseg} блок {roff + 1}: {lastRecordDt}");
                    if (ac.Length > 0) break;
                }

                if (lastRecordDt == DateTime.MaxValue)
                {
                    log("Не найдена дата последней записи часовых архивов", level: 1);
                }
                else if (lastRecordDt < start)
                {
                    log($"Последняя дата часовых архивов ({lastRecordDt:dd.MM.yyyy HH:mm}) больше, чем начало диапазона ({start:dd.MM.yyyy HH:mm})", level: 2);
                }
                else
                {
                    int startOffset = (int)(lastRecordDt - start).TotalHours;
                    //log($"дата start {start} смещ. {startOffset} часов от последней даты {lastRecordDt}");
                    int startRecordOffset = maxRecordOffset - (startOffset / ver.GetRecordsInBlock() + 1);
                    if (startRecordOffset < 0) startRecordOffset = 0;

                    for (; ; startRecordOffset++)
                    {
                        Segment rseg;
                        int roff;
                        if (startRecordOffset < ver.GetRecordsInSegment())
                        {
                            rseg = first;
                            roff = startRecordOffset;
                        }
                        else
                        {
                            rseg = last;
                            roff = startRecordOffset - ver.GetRecordsInSegment();
                        }

                        var ac = ReadAndParseArchiveCached(rseg, roff, ver);
                        var startRecordDt = ac.Select(s => s.date).DefaultIfEmpty().Min();
                        if (startRecordDt == DateTime.MinValue) startRecordDt = DateTime.MaxValue;

                        //log($"сегмент+блок{startRecordOffset} сег{rseg} блок {roff + 1}: {startRecordDt}");
                        if (startRecordDt > end) break;

                        List<ParsedParameter> phour = ac.Where(s => (start <= s.date) && (s.date <= end)).ToList();
                        if (phour.Any())
                        {
                            log($"Прочитаны часовые записи за {string.Join(", ", phour.Select(r => r.date).Distinct().Select(d => (d == DateTime.MinValue || d == DateTime.MaxValue) ? "<пусто>" : $"{d:dd.MM.yyyy HH:mm}")) }");
                        }
                        List<dynamic> chour = phour.Select(r => r.ToHourlyRecord()).ToList();

                        recs.AddRange(chour);
                        records(chour);
                    }
                }
            }

            return recs;
        }

        public List<dynamic> GetDaily(DateTime start, DateTime end, DateTime currentDate, Version ver)
        {
            List<dynamic> recs = new List<dynamic>();

            //поиск начальной даты в сегменте
            DateTime seg1Dt = ReadAndParseArchiveCached(Segment.Daily1, 0, ver).DefaultIfEmpty().Select(s => s.date).Min();
            DateTime seg2Dt = ReadAndParseArchiveCached(Segment.Daily2, 0, ver).DefaultIfEmpty().Select(s => s.date).Min();

            // сегмент пуст - должна быть максимальная дата
            if (seg1Dt == default(DateTime)) seg1Dt = DateTime.MaxValue;
            if (seg2Dt == default(DateTime)) seg2Dt = DateTime.MaxValue;


            if (seg1Dt == DateTime.MaxValue && seg2Dt == DateTime.MaxValue) //сегменты пусты
            {
                log("Суточных данных не обнаружено - сегменты пустые", level: 1);
            }
            else
            {
                // сегмент со старыми данными - первый, с новыми данными - последний
                Segment first;
                Segment last;
                DateTime firstDt;
                DateTime lsDt;

                if (seg1Dt < seg2Dt)
                {
                    first = Segment.Daily1;
                    last = Segment.Daily2;
                    firstDt = seg1Dt;
                    lsDt = seg2Dt;
                }
                else
                {
                    first = Segment.Daily2;
                    last = Segment.Daily1;
                    firstDt = seg2Dt;
                    lsDt = seg1Dt;
                }

                // поиск последней записи 
                DateTime currentDateD = currentDate.Date;
                int hoursOffset;
                int maxRecordOffset;

                if (lsDt == DateTime.MaxValue)
                {
                    hoursOffset = (int)(currentDateD - firstDt).TotalDays;
                    //log($"тек.дата {currentDateD:dd.MM.yyyy} нач.сегм {firstDt} смещ. {hoursOffset}");
                    maxRecordOffset = hoursOffset / ver.GetRecordsInBlock();
                }
                else
                {
                    hoursOffset = (int)(currentDateD - lsDt).TotalDays;
                    //log($"тек.дата {currentDateD:dd.MM.yyyy} нач.сегм {lsDt} смещ. {hoursOffset}");
                    maxRecordOffset = ver.GetRecordsInSegment() + hoursOffset / ver.GetRecordsInBlock();
                }

                DateTime lastRecordDt = DateTime.MaxValue;
                for (; maxRecordOffset > 0; maxRecordOffset--)
                {
                    Segment rseg;
                    int roff;
                    if (maxRecordOffset < ver.GetRecordsInSegment())
                    {
                        rseg = first;
                        roff = maxRecordOffset;
                    }
                    else
                    {
                        rseg = last;
                        roff = maxRecordOffset - ver.GetRecordsInSegment();
                    }

                    var ac = ReadAndParseArchiveCached(rseg, roff, ver);
                    lastRecordDt = ac.Select(s => s.date).DefaultIfEmpty().Min();
                    if (lastRecordDt == DateTime.MinValue) lastRecordDt = DateTime.MaxValue;
                    //log($"сегмент+блок{maxRecordOffset} сег{rseg} блок {roff + 1}: {lastRecordDt}");
                    if (ac.Length > 0) break;
                }

                if (lastRecordDt == DateTime.MaxValue)
                {
                    log("Не найдена дата последней записи суточных архивов", level: 1);
                }
                else if (lastRecordDt < start)
                {
                    log($"Последняя дата суточных архивов ({lastRecordDt:dd.MM.yyyy}) больше, чем начало диапазона ({start:dd.MM.yyyy})", level: 2);
                }
                else
                {
                    int startOffset = (int)(lastRecordDt - start).TotalDays;
                    //log($"дата start {start} смещ. {startOffset} суток от последней даты {lastRecordDt}");
                    int startRecordOffset = maxRecordOffset - (startOffset / ver.GetRecordsInBlock() + 1);
                    if (startRecordOffset < 0) startRecordOffset = 0;

                    for (; ; startRecordOffset++)
                    {
                        Segment rseg;
                        int roff;
                        if (startRecordOffset < ver.GetRecordsInSegment())
                        {
                            rseg = first;
                            roff = startRecordOffset;
                        }
                        else
                        {
                            rseg = last;
                            roff = startRecordOffset - ver.GetRecordsInSegment();
                        }

                        var ac = ReadAndParseArchiveCached(rseg, roff, ver);
                        var startRecordDt = ac.Select(s => s.date).DefaultIfEmpty().Min();
                        if (startRecordDt == DateTime.MinValue) startRecordDt = DateTime.MaxValue;

                        //log($"сегмент+блок{startRecordOffset} сег{rseg} блок {roff + 1}: {startRecordDt}");
                        if (startRecordDt > end) break;

                        List<ParsedParameter> pday = ac.Where(s => (start <= s.date) && (s.date <= end)).ToList();
                        if (pday.Any())
                        {
                            log($"Прочитаны суточные записи за {string.Join(", ", pday.Select(r => r.date).Distinct().Select(d => d == DateTime.MaxValue ? "<пусто>" : $"{d:dd.MM.yyyy}")) }");
                        }
                        List<dynamic> cday = pday.Select(r => r.ToDailyRecord()).ToList();

                        recs.AddRange(cday);
                        records(cday);
                    }
                }
            }

            return recs;
        }
    }
}

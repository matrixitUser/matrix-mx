using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        /// <summary>
        /// число попыток опроса в случае неуспешного запроса
        /// </summary>
        private const int TRY_COUNT = 4;

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            try
            {
                var param = (IDictionary<string, object>)arg;

                byte na = 1;
                if (!param.ContainsKey("networkAddress") || !byte.TryParse(param["networkAddress"].ToString(), out na))
                {
                    log(string.Format("сетевой адрес не указан, принят: {0}", na));
                }
                else
                {
                    log(string.Format("сетевой адрес: {0}", na));
                }

                byte channel = 0;
                if (!param.ContainsKey("channel") || !byte.TryParse(param["channel"].ToString(), out channel))
                {
                    log(string.Format("канал не указан, принят: {0}", channel));
                }
                else
                {
                    log(string.Format("канал: {0}", channel));
                }

                string version = "206D";
                if (!param.ContainsKey("version"))
                {
                    log(string.Format("параметр version не указан, принят по-умолчанию: {0} (варианты 205D, 206D, 21B)", version));
                }
                else
                {
                    version = arg.version.ToString();
                    log(string.Format("версия: {0}", version));
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

                switch (what.ToLower())
                {
                    case "all":
                        switch (version)
                        {
                            case "205D": return All20RU5D(na, components); 
                            case "206D": return All20RU6D(na, components);
                            case "21B": return All21B(na, channel, components);
                            default: return All20RU6D(na, components);
                        }
                        return MakeResult(0);
                    //case "ping": Ping(arg.networkAddress); return;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "current": Current(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
                    default:
                        {
                            log(string.Format("неопознаная команда {0}", what));
                            return MakeResult(201, what);
                        }
                }
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка: {0} {1}", ex.Message, ex.StackTrace));
                return MakeResult(999);
            }
        }

        private dynamic All20RU5D(byte na, string components)
        {
            log("драйвер не реализован для данного типа прибора SF20RU5D");
            return MakeResult(205, "драйвер не реализован");
        }

        private dynamic All20RU6D(byte na, string components)
        {
            //текущие                       
            //часы
            log("тип прибора SF20RU6D");

            #region Паспорт

            dynamic passport = null;
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel()) return MakeResult(200);
                passport = GetPassport(na);
                if (passport.success) break;

                log(string.Format("поспорт не получен, {0}", passport.error));
            }
            if (!passport.success) return MakeResult(101);

            if (getEndDate == null)
            {
                getEndDate = (type) => passport.date;
            }

            DateTime currentDate = passport.date;
            setTimeDifference(DateTime.Now - currentDate);

            int contractHour = passport.contractHour;
            setContractHour(contractHour);

            log(string.Format("поспорт получен, дата на приборе: {0:dd.MM.yyyy HH:mm:ss}, контрактный час: {1}, количество ИТ {2}", passport.date, passport.contractHour, passport.tubeCount));

            #endregion

            #region Текущие

            if (components.Contains("Current"))
            {
                for (byte tube = 1; tube <= passport.tubeCount; tube++)
                {
                    var current = GetCurrent(na, tube);
                    if (!current.success)
                    {
                        log(string.Format("текущие по трубе {0} не получены, {1}", tube, current.error));
                        return MakeResult(102);
                    }
                    log(string.Format("текущие по трубе {0} получены", tube));
                    records(current.records);
                }
            }

            #endregion

            #region Константы

            for (byte tube = 1; tube <= passport.tubeCount; tube++)
            {
                var constants = GetConstant20RU6D(na, tube);
                if (!constants.success)
                {
                    log(string.Format("константы по трубе {0} не получены, {1}", tube, constants.error));
                    return MakeResult(103);
                }
                log(string.Format("константы по трубе {0} получены", tube));
                records(constants.records);
            }
            #endregion

            #region Сутки

            if (components.Contains("Day"))
            {
                var lastDay = getStartDate("Day");
                for (byte tube = 1; tube <= passport.tubeCount; tube++)
                {
                    byte number = 0;
                    do
                    {
                        if (cancel()) return MakeResult(200);

                        var day = GetDays20RU6D(na, tube, number, lastDay, passport.date);
                        if (!day.success)
                        {
                            log(string.Format("ошибка при чтении суток, {0}", day.error));
                            return MakeResult(104);
                        }
                        log(string.Format("получены сутки, {0} шт", day.recordCount));
                        number++;
                        records(day.records);
                        if (day.state == 0) break;
                    } while (true);
                }
            }

            #endregion

            #region Часы

            if (components.Contains("Hour"))
            {
                var lastHour = getStartDate("Hour");
                for (byte tube = 1; tube <= passport.tubeCount; tube++)
                {
                    byte number = 0;
                    do
                    {
                        var hour = GetHour20RU6D(na, tube, number, lastHour, passport.date);
                        if (!hour.success)
                        {
                            log(string.Format("ошибка при чтении часов, {0}", hour.error));
                            return MakeResult(105);
                        }
                        log(string.Format("получены часы, {0} шт", hour.recordCount));
                        number++;
                        records(hour.records);
                        if (hour.state == 0) break;
                    } while (true);
                }
            }
            #endregion

            #region НС

            if (cancel()) return MakeResult(200);

            if (components.Contains("Abnormal"))
            {
                var lastAbnormal = getStartDate("Abnormal");

                byte number = 0;
                do
                {
                    var abnormal = GetAbnormal(na, 1, number, lastAbnormal, passport.date);
                    if (!abnormal.success)
                    {
                        log(string.Format("НС не прочитаны, {0}", abnormal.error));
                        return MakeResult(106);
                    }
                    log(string.Format("НС прочитаны, {0} шт", abnormal.recordCount));
                    number++;
                    records(abnormal.records);
                    if (abnormal.state == 0) break;
                } while (true);
            }

            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic All20RU7C(byte na, string components)
        {
            log("драйвер не реализован для данного типа прибора SF20RU7C");
            return MakeResult(205, "драйвер не реализован");
        }

        private dynamic All21B(byte na, byte ch, string components)
        {
            //текущие                       
            //часы

            log("тип прибора SF21B");
            #region Паспорт

            dynamic passport = new ExpandoObject();
            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel()) return MakeResult(200);

                passport = GetPassport21B(na, ch);

                if (passport.success) break;
                log(string.Format("паспорт не получен, ошибка: {0}", passport.error));
            }
            if (!passport.success) return MakeResult(101);

            var run = "";
            switch ((int)passport.runType)
            {
                case 0: run = "диафр."; break;
                case 1: run = "аннубар"; break;
                case 2: run = "турбина"; break;
                default: run = "объемн.расхдомер"; break;
            }
            var fluid = "";
            switch ((int)passport.fluidType)
            {
                case 0: fluid = "газ NX19"; break;
                case 1: fluid = "газ GERG91"; break;
                default: fluid = "вода/пар"; break;
            }
            log(string.Format("паспорт прочитан, имя ТП {0}, тип ТП {1}, тип среды {2}", passport.name, run, fluid));

            #endregion

            #region Текущие

            //1 read time
            var date = GetDate21B(na);
            if (!date.success)
            {
                log(string.Format("системное время не прочтено, {0}", date.error));
                return MakeResult(102);
            }
            int contractHour = date.contractHour;
            setContractHour(contractHour);

            log(string.Format("системное время прочтено, {0:dd.MM.yyyy HH:mm:ss}, контрактный час {1}", date.date, contractHour));
            if (getEndDate == null) getEndDate = (type) => date.date;

            var baseConstants = new List<dynamic>();
            baseConstants.Add(MakeConstRecord(string.Format("Имя ТП{0}", ch), passport.name, date.date));
            baseConstants.Add(MakeConstRecord(string.Format("Тип ТП{0}", ch), run, date.date));
            records(baseConstants);

            DateTime currentDate = date.date;
            setTimeDifference(DateTime.Now - currentDate);

            #endregion

            #region Сутки

            if (components.Contains("Day"))
            {
                //days             
                var startDay = getStartDate("Day").Date.AddHours(contractHour);
                var endDay = getEndDate("Day").Date.AddHours(contractHour);
                var currentDay = startDay;
                while (currentDay < endDay)
                {
                    if (cancel()) return MakeResult(200);
                    if (currentDay > date.date)
                    {
                        break;
                    }
                    var day = GetDay21B(na, ch, currentDay, passport.runType, date.gmt);
                    if (!day.success)
                    {
                        log(string.Format("сутки не прочтены, {0}", day.error));
                        break;
                    }
                    if (currentDay == day.maxDate) break;
                    currentDay = day.maxDate;
                    records(day.records);
                }
            }

            #endregion

            #region Часы
            if (components.Contains("Hour"))
            {
                //hours
                var startHour = getStartDate("Hour");
                var endHour = getEndDate("Hour");
                var currentHour = startHour;
                while (currentHour < endHour)
                {
                    if (cancel()) return MakeResult(200);
                    if (currentHour > date.date)
                    {
                        break;
                    }
                    var hour = GetHour21B(na, ch, currentHour, passport.runType, date.gmt);
                    if (!hour.success)
                    {
                        log(string.Format("часы не прочтены, {0}", hour.error));
                        break;
                    }
                    if (currentHour == hour.maxDate) break;
                    currentHour = hour.maxDate;
                    records(hour.records);
                }

                var diaf = GetDiaf21B(na, ch, date.date);
                if (!diaf.success)
                {
                    log(string.Format("параметры диафрагмы не получены: {0}", diaf.error));
                    return MakeResult(200);
                }
                log("параметры диафрагмы получены");
                records(diaf.records);

                var gaz = GetGaz21B(na, ch, date.date);
                if (!gaz.success)
                {
                    log(string.Format("параметры газа не получены: {0}", gaz.error));
                    return MakeResult(200);
                }
                log("параметры газа получены");
                records(gaz.records);

                var curr = GetFlow21B(na, ch, date.date);
                if (!curr.success)
                {
                    log(string.Format("параметры потока не получены: {0}", curr.error));
                    return MakeResult(200);
                }
                log(string.Format("параметры потока получены"));
                records(curr.records);
            }

            #endregion

            #region НС
            if (components.Contains("Abnormal"))
            {
                var startAbnormal = getStartDate("Abnormal");
                var endAbnormal = getEndDate("Abnormal");
                var currentAbnormal = startAbnormal;
                while (currentAbnormal < endAbnormal)
                {
                    if (cancel()) return MakeResult(200);
                    if (currentAbnormal > date.date)
                    {
                        break;
                    }
                    var abnormal = GetAbnormal21B(na, ch, currentAbnormal, passport.runType, date.gmt);
                    if (!abnormal.success)
                    {
                        log(string.Format("НС не прочтены, {0}", abnormal.error));
                        break;
                    }
                    if (currentAbnormal == abnormal.maxDate) break;
                    currentAbnormal = abnormal.maxDate;
                    records(abnormal.records);
                }
            }
            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private DateTime ToDate(int utc)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(utc);
        }

        private int ToUtc(DateTime time)
        {
            return (int)(time - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private dynamic GetGaz21B(byte na, Int16 ch, DateTime date)
        {
            var arg = new List<byte>();
            arg.AddRange(BitConverter.GetBytes(ch));

            dynamic gaz = ParseResponse21B(Send21B(MakeRequest21B(na, 0x27, arg.ToArray())));
            if (!gaz.success) return gaz;
            gaz.records = new List<dynamic>();
            gaz.records.Add(MakeConstRecord(string.Format("плотность {0}, кг/м³", ch), BitConverter.ToSingle(gaz.body, 4), date));
            gaz.records.Add(MakeConstRecord(string.Format("теплота сгорания {0}, Дж/м³", ch), BitConverter.ToSingle(gaz.body, 8), date));
            gaz.records.Add(MakeConstRecord(string.Format("N2 {0}, моль", ch), BitConverter.ToSingle(gaz.body, 12) * 100, date));
            gaz.records.Add(MakeConstRecord(string.Format("CO2 {0}, моль", ch), BitConverter.ToSingle(gaz.body, 16) * 100, date));
            gaz.success = true;
            return gaz;
        }

        private dynamic GetDate21B(byte na)
        {
            dynamic date = ParseResponse21B(Send21B(MakeRequest21B(na, 0x41, new byte[] { })));
            if (!date.success) return date;
            var utc = BitConverter.ToInt32(date.body, 0);
            var gmt = BitConverter.ToInt32(date.body, 4);
            date.gmt = gmt;
            date.date = ToDate(utc + gmt);
            date.daylight = BitConverter.ToInt16(date.body, 8);
            date.contractHour = BitConverter.ToInt32(date.body, 10) / 3600;
            date.success = true;
            return date;
        }

        private dynamic GetDiaf21B(byte na, byte ch, DateTime date)
        {
            dynamic constants = ParseResponse21B(Send21B(MakeRequest21B(na, 0x25, new byte[] { ch })));
            if (!constants.success) return constants;

            constants.records = new List<dynamic>();

            constants.records.Add(MakeConstRecord(string.Format("Внутренний диаметр измерительного трубопровода {0}, м", ch), BitConverter.ToSingle(constants.body, 4), date));
            constants.records.Add(MakeConstRecord(string.Format("Температурный коэффициент линейного расширения материала измерительного трубопровода {0}, °C", ch), BitConverter.ToSingle(constants.body, 8), date));
            constants.records.Add(MakeConstRecord(string.Format("Эквивалентный радиус шероховатости внутренней поверхности измерительного трубопровода {0}, м", ch), BitConverter.ToSingle(constants.body, 12), date));
            constants.records.Add(MakeConstRecord(string.Format("Диаметр отверстия измерительной диафрагмы {0}, м", ch), BitConverter.ToSingle(constants.body, 16), date));
            constants.records.Add(MakeConstRecord(string.Format("Температурный коэффициент линейного расширения материала измерительной диафрагмы {0}, °C", ch), BitConverter.ToSingle(constants.body, 20), date));
            constants.records.Add(MakeConstRecord(string.Format("Радиус притупления входной кромки диафрагмы {0}, м", ch), BitConverter.ToSingle(constants.body, 24), date));
            var tap = "";
            switch ((int)BitConverter.ToInt32(constants.body, 28))
            {
                case 0: tap = "угловой"; break;
                case 1: tap = "фланцевый"; break;
                default: tap = "трехрадиусный"; break;
            }
            constants.records.Add(MakeConstRecord(string.Format("Способ отбора дифференциального давления {0}, м", ch), tap, date));

            constants.success = true;
            return constants;
        }

        private dynamic GetFlow21B(byte na, byte ch, DateTime date)
        {
            dynamic currents = ParseResponse21B(Send21B(MakeRequest21B(na, 0x2A, new byte[] { ch })));
            if (!currents.success) return currents;

            currents.records = new List<dynamic>();

            currents.records.Add(MakeCurrentRecord(string.Format("Атмосферное давление {0}", ch), BitConverter.ToDouble((byte[])currents.body, 8), "Па", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Избыточное давление {0}", ch), BitConverter.ToDouble((byte[])currents.body, 16), "Па", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Абсолютное давление {0}", ch), BitConverter.ToDouble((byte[])currents.body, 24), "Па", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Дифференциальное давление {0}", ch), BitConverter.ToDouble((byte[])currents.body, 32), "Па", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Расход при рабочих условиях {0}", ch), BitConverter.ToDouble((byte[])currents.body, 40), "м³/c", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Расход энергии {0}", ch), BitConverter.ToDouble((byte[])currents.body, 48), "Вт", date));
            currents.records.Add(MakeCurrentRecord(string.Format("Приведённый объём с начала контрактного часа {0}", ch), BitConverter.ToDouble((byte[])currents.body, 56), "м³", date));
            currents.records.Add(MakeCurrentRecord(string.Format("нергия с начала контрактного часа {0}", ch), BitConverter.ToDouble((byte[])currents.body, 64), "Дж", date));

            currents.success = true;
            return currents;
        }

        private dynamic GetArchive21B(byte na, Int16 ch, DateTime date, Int16 archType)
        {
            var param = new List<byte>();
            param.AddRange(BitConverter.GetBytes(archType));
            param.AddRange(BitConverter.GetBytes(ch));
            param.AddRange(BitConverter.GetBytes((Int32)ToUtc(date)));
            dynamic archive = ParseResponse21B(Send21B(MakeRequest21B(na, 0x90, param.ToArray())));
            if (!archive.success) return archive;

            //archive.records = new List<dynamic>();

            //archive.records.Add(MakeCurrentRecord(string.Format("Атмосферное давление {0}", ch), BitConverter.ToDouble((byte[])archive.body, 8), "Па", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Избыточное давление {0}", ch), BitConverter.ToDouble((byte[])archive.body, 16), "Па", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Абсолютное давление {0}", ch), BitConverter.ToDouble((byte[])archive.body, 24), "Па", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Дифференциальное давление {0}", ch), BitConverter.ToDouble((byte[])archive.body, 32), "Па", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Расход при рабочих условиях {0}", ch), BitConverter.ToDouble((byte[])archive.body, 40), "м³/c", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Расход энергии {0}", ch), BitConverter.ToDouble((byte[])archive.body, 48), "Вт", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("Приведённый объём с начала контрактного часа {0}", ch), BitConverter.ToDouble((byte[])archive.body, 56), "м³", date));
            //archive.records.Add(MakeCurrentRecord(string.Format("нергия с начала контрактного часа {0}", ch), BitConverter.ToDouble((byte[])archive.body, 64), "Дж", date));

            archive.success = true;
            return archive;
        }

        private dynamic GetPassport21B(byte na, Int16 ch)
        {
            var arg = new List<byte>();
            arg.AddRange(BitConverter.GetBytes(ch));
            dynamic passport = ParseResponse21B(Send21B(MakeRequest21B(na, 0x23, arg.ToArray())));
            if (!passport.success) return passport;

            passport.channel = BitConverter.ToInt16(passport.body, 0);
            passport.name = Encoding.GetEncoding(866).GetString(passport.body, 4, 32);
            passport.fluidType = BitConverter.ToInt16(passport.body, 36);
            passport.runType = BitConverter.ToInt16(passport.body, 38);
            passport.success = true;
            return passport;
        }

        //private dynamic GetDay21B(byte na, byte ch, DateTime date, int type, int gmt)
        //{
        //    var udate = ToUtc(date) - gmt;
        //    dynamic archive = GetArchive21B(na, ch, ToDate(udate), 2);
        //    if (!archive.success) return archive;

        //    archive.records = new List<dynamic>();
        //    archive.maxDate = DateTime.MinValue;

        //    for (var offset = 0; offset < archive.len; offset += 40)
        //    {
        //        var db = BitConverter.ToInt16(archive.body, offset + 0);
        //        var channel = BitConverter.ToInt16(archive.body, offset + 2);
        //        archive.date = ToDate(BitConverter.ToInt32(archive.body, offset + 4) + gmt).Date;
        //        //log(string.Format("получены сутки за {0:dd.MM.yy}", archive.date));
        //        archive.maxDate = archive.maxDate < archive.date ? archive.date : archive.maxDate;
        //        float fvalue = BitConverter.ToSingle(archive.body, offset + 12);
        //        archive.records.Add(MakeDayRecord(string.Format("Абсолютное давление {0}", ch), BitConverter.ToSingle(archive.body, offset + 12), "Па", archive.date));
        //        archive.records.Add(MakeDayRecord(string.Format("Температура {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 16), "°C", archive.date));
        //        switch (type)
        //        {
        //            case 0:
        //            case 1:
        //                archive.records.Add(MakeDayRecord(string.Format("Дифференциальное давление {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "Па", archive.date));
        //                break;
        //            case 2:
        //                archive.records.Add(MakeDayRecord(string.Format("Объём при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "м³", archive.date));
        //                break;
        //            default:
        //                archive.records.Add(MakeDayRecord(string.Format("Средний расход при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "м³/с", archive.date));
        //                break;
        //        }
        //        //archive.records.Add(MakeDayRecord(string.Format("Объём при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, 20), "Па", archive.date));            
        //        archive.records.Add(MakeDayRecord(string.Format("Приведённый объём газа {0}", ch), BitConverter.ToDouble((byte[])archive.body, offset + 24), "м³", archive.date));

        //        archive.records.Add(MakeDayRecord(string.Format("Энергия газа {0}", ch), BitConverter.ToDouble((byte[])archive.body, offset + 32), "м³", archive.date));

        //     //   log(string.Format("Приведённый объём газа {0}: {1}м³", archive.date, BitConverter.ToDouble((byte[])archive.body, offset + 24)));
        //        log(string.Format("получены сутки за {0:dd.MM.yy}; Приведённый объём газа {1}м³; Температура{2}°C", archive.date, BitConverter.ToDouble((byte[])archive.body, offset + 24), BitConverter.ToSingle((byte[])archive.body, offset + 16)));
        //    }
        //    archive.success = true;
        //    return archive;
        //}

        private dynamic GetDay21B(byte na, byte ch, DateTime date, int type, int gmt)
        {
            var udate = ToUtc(date) - gmt;
            dynamic archive = GetArchive21B(na, ch, ToDate(udate), 2);
            if (!archive.success) return archive;

            archive.records = new List<dynamic>();
            archive.maxDate = DateTime.MinValue;

            for (var offset = 0; offset < archive.len; offset += 40)
            {
                var db = BitConverter.ToInt16(archive.body, offset + 0);
                var channel = BitConverter.ToInt16(archive.body, offset + 2);
                archive.date = ToDate(BitConverter.ToInt32(archive.body, offset + 4) + gmt).Date;
                //log(string.Format("получены сутки за {0:dd.MM.yy}", archive.date));
                archive.maxDate = archive.maxDate < archive.date ? archive.date : archive.maxDate;

                float fvalue = BitConverter.ToSingle(archive.body, offset + 12);
                if (!float.IsNaN(fvalue))
                    archive.records.Add(MakeDayRecord(string.Format("Абсолютное давление {0}", ch), fvalue, "Па", archive.date));

                fvalue = BitConverter.ToSingle(archive.body, offset + 16);
                if (!float.IsNaN(fvalue))
                    archive.records.Add(MakeDayRecord(string.Format("Температура {0}", ch), fvalue, "°C", archive.date));
                switch (type)
                {
                    case 0:
                    case 1:
                        fvalue = BitConverter.ToSingle(archive.body, offset + 20);
                        if (!float.IsNaN(fvalue))
                            archive.records.Add(MakeDayRecord(string.Format("Дифференциальное давление {0}", ch), fvalue, "Па", archive.date));
                        break;
                    case 2:
                        fvalue = BitConverter.ToSingle(archive.body, offset + 20);
                        if (!float.IsNaN(fvalue))
                            archive.records.Add(MakeDayRecord(string.Format("Объём при рабочих условиях {0}", ch), fvalue, "м³", archive.date));
                        break;
                    default:
                        fvalue = BitConverter.ToSingle(archive.body, offset + 20);
                        if (!float.IsNaN(fvalue))
                            archive.records.Add(MakeDayRecord(string.Format("Средний расход при рабочих условиях {0}", ch), fvalue, "м³/с", archive.date));
                        break;
                }

                double dvalue = BitConverter.ToDouble(archive.body, offset + 24);
                if (!double.IsNaN(dvalue))
                    archive.records.Add(MakeDayRecord(string.Format("Приведённый объём газа {0}", ch), dvalue, "м³", archive.date));

                dvalue = BitConverter.ToDouble(archive.body, offset + 32);
                if (!double.IsNaN(dvalue))
                    archive.records.Add(MakeDayRecord(string.Format("Энергия газа {0}", ch), dvalue, "м³", archive.date));

                log(string.Format("получены сутки за {0:dd.MM.yy}; Приведённый объём газа {1}м³; Температура{2}°C", archive.date, BitConverter.ToDouble((byte[])archive.body, offset + 24), BitConverter.ToSingle((byte[])archive.body, offset + 16)));
            }
            archive.success = true;
            return archive;
        }

        private dynamic GetHour21B(byte na, byte ch, DateTime date, int type, int gmt)
        {
            var udate = ToUtc(date) - gmt;
            dynamic archive = GetArchive21B(na, ch, ToDate(udate), 1);
            if (!archive.success) return archive;

            archive.records = new List<dynamic>();
            archive.maxDate = DateTime.MinValue;

            for (var offset = 0; offset < archive.len; offset += 40)
            {
                var db = BitConverter.ToInt16(archive.body, offset + 0);
                var channel = BitConverter.ToInt16(archive.body, offset + 2);
                archive.date = ToDate(BitConverter.ToInt32(archive.body, offset + 4) + gmt).AddHours(1);
                archive.maxDate = archive.maxDate < archive.date ? archive.date : archive.maxDate;

                archive.records.Add(MakeHourRecord(string.Format("Абсолютное давление {0}", ch), BitConverter.ToSingle(archive.body, offset + 12), "Па", archive.date));
                archive.records.Add(MakeHourRecord(string.Format("Температура {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 16), "°C", archive.date));
                switch (type)
                {
                    case 0:
                    case 1:
                        archive.records.Add(MakeHourRecord(string.Format("Дифференциальное давление {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "Па", archive.date));
                        break;
                    case 2:
                        archive.records.Add(MakeHourRecord(string.Format("Объём при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "м³", archive.date));
                        break;
                    default:
                        archive.records.Add(MakeHourRecord(string.Format("Средний расход при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, offset + 20), "м³/с", archive.date));
                        break;
                }
                //archive.records.Add(MakeHourRecord(string.Format("Объём при рабочих условиях {0}", ch), BitConverter.ToSingle((byte[])archive.body, 20), "Па", archive.date));            
                archive.records.Add(MakeHourRecord(string.Format("Приведённый объём газа {0}", ch), BitConverter.ToDouble((byte[])archive.body, offset + 24), "м³", archive.date));

                archive.records.Add(MakeHourRecord(string.Format("Энергия газа {0}", ch), BitConverter.ToDouble((byte[])archive.body, offset + 32), "м³", archive.date));

                log(string.Format("получены часы за {0:dd.MM.yy HH:mm}", archive.date));
                //   log(string.Format("получены часы за {0:dd.MM.yy HH:mm}, приведенный объем {1} м³", archive.date, BitConverter.ToDouble((byte[])archive.body, offset + 24)));
            }
            archive.success = true;
            return archive;
        }

        private dynamic GetAbnormal21B(byte na, byte ch, DateTime date, int type, int gmt)
        {
            var udate = ToUtc(date) - gmt;
            dynamic archive = GetArchive21B(na, ch, ToDate(udate), 5);
            if (!archive.success) return archive;
            archive.records = new List<dynamic>();
            archive.maxDate = DateTime.MinValue;
            for (var offset = 0; offset < archive.len; offset += 50)
            {
                var db = BitConverter.ToInt16(archive.body, offset + 0);
                var channel = BitConverter.ToInt16(archive.body, offset + 2);

                archive.date = ToDate(BitConverter.ToInt32(archive.body, offset + 4) + gmt);
                archive.maxDate = archive.maxDate < archive.date ? archive.date : archive.maxDate;
                log(string.Format("получена НС за {0:dd.MM.yy HH:mm:ss}", archive.date));

                var source = BitConverter.ToInt16(archive.body, offset + 8);
                var msg = Encoding.GetEncoding(866).GetString(archive.body, 10, 40);
                archive.records.Add(MakeAbnormalRecord(msg, 0, date));
            }
            archive.success = true;
            return archive;
        }

        private byte[] Send21B(byte[] data)
        {
            request(data);
            //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));            
            var timeout = 10000;
            var sleep = 100;
            var all = new List<byte>();
            while ((timeout -= sleep) > 0 && !(all.Any() && all.LastOrDefault() == 0x3f))
            {
                Thread.Sleep(sleep);
                var buffer = response();
                all.AddRange(buffer);
            }
            //log(string.Format("пришло {0}", string.Join(",", all.Select(b => b.ToString("X2")))));
            return all.ToArray(); ;
        }

        private byte[] Send(byte[] data)
        {
            request(data);
            //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            var all = new List<byte>();
            var timeout = 7000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !(all.Any() && CheckCrc16(all.ToArray())))
            {
                Thread.Sleep(sleep);
                var buffer = response();
                all.AddRange(buffer);
            }
            //log(string.Format("пришло {0}", string.Join(",", all.Select(b => b.ToString("X2")))));
            return all.ToArray();
        }

        private byte[] MakeRequest(byte na, byte fn)
        {
            return MakeRequest(na, fn, new byte[] { });
        }

        private byte[] MakeRequest(byte na, byte fn, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(0xaa);
            bytes.Add(na);
            bytes.Add((byte)(4 + body.Length + 2));
            bytes.Add(fn);

            bytes.AddRange(body);

            var crc = CalcCrc16(bytes.ToArray());
            bytes.AddRange(crc);

            return bytes.ToArray();
        }

        private dynamic ParseResponse(byte[] bytes)
        {
            dynamic response = new ExpandoObject();
            response.success = true;
            if (bytes.Length == 6)
            {
                response.success = false;
                response.error = "ошибка при обработке запроса";
                return response;
            }

            if (!CheckCrc16(bytes))
            {
                response.success = false;
                response.error = "не сошлась контрольная сумма";
                return response;
            }

            response.body = bytes.Skip(4).Take(bytes.Length - 4 - 2).ToArray();

            return response;
        }

        private byte[] MakeRequest21B(byte na, byte cmd, byte[] data)
        {
            var bytes = new List<byte>();
            bytes.Add(0x55);
            bytes.Add(na);
            bytes.Add(cmd);
            bytes.Add(0x00);
            bytes.Add((byte)data.Length);
            bytes.AddRange(data);
            var crc = CalcCrc16(bytes.ToArray());
            bytes.AddRange(crc);

            bytes.InsertRange(0, new byte[] { 0xff, 0xff, 0xff });
            bytes.AddRange(new byte[] { 0x3f, 0x3f, 0x3f });
            return bytes.ToArray();
        }

        private dynamic ParseResponse21B(byte[] bytes)
        {
            dynamic response = new ExpandoObject();
            response.success = true;
            if (bytes.Length < 7)
            {
                response.success = false;
                response.error = "ошибка при обработке запроса";
                return response;
            }

            var clear = bytes.SkipWhile(b => b == 0xff).Reverse().SkipWhile(b => b == 0x3f).Reverse().ToArray();

            if (!CheckCrc16(clear))
            {
                response.success = false;
                response.error = "не сошлась контрольная сумма";
                return response;
            }

            response.sta = clear[3];
            response.len = clear[4];
            response.body = clear.Skip(5).Take(clear.Length - 5 - 2).ToArray();

            if (response.sta != 0)
            {
                response.success = false;
                response.error = string.Format("ошибка от вычислителя: {0}", Encoding.GetEncoding(866).GetString(response.body));
                return response;
            }

            return response;
        }
    }
}

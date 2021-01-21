// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SPG741
{
    /// <summary>
    /// Драйвер для вычислителей СПГ741
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        /// <summary>
        /// число попыток опроса в случае неуспешного запроса
        /// </summary>
        private const int TRY_COUNT = 6;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private int logCounter = 0;

        ///// <summary>
        ///// debug log wrapper
        ///// </summary>
        ///// <param name="text"></param>
        //private void logDebug(string text)
        //{
        //    for (var i = 0; i < text.Length; i += 200)
        //    {
        //        var part = new string(text.Skip(i).Take(200).ToArray());
        //        logRedirected(string.Format("{0:X02} {2}{1}{3}", logCounter & 0xFF, part, i == 0 ? "" : "...", (i + 200) >= text.Length ? "" : "..."));
        //        logCounter++;
        //    }
        //}

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

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            try
            {
                var parameters = (IDictionary<string, object>)arg;

                var driverParameters = new HashSet<string>();

                int debug = 0;
                if (parameters.ContainsKey("debug") && int.TryParse(parameters["debug"].ToString(), out debug))
                {
                    if (debug == 1)
                    {
                        driverParameters.Add("debug");
                    }
                    else if (debug == 2)
                    {
                        log("Подстройка драйвера под Абдулино База");
                        driverParameters.Add("test");
                    }
                    else if (debug == 3)
                    {
                        log("Подстройка драйвера под БЗЖБИ");
                        driverParameters.Add("bzjbi");
                    }
                }

#if OLD_DRIVER
                if(driverParameters.Contains("debug"))
                {
                    debugMode = true;
                }
#endif

                byte na = 0x00;
				if (!parameters.ContainsKey("networkAddress") || !byte.TryParse(parameters["networkAddress"].ToString(), out na))
					log(string.Format("отсутствутют сведения о сетевом адресе, принят по-умолчанию {0}", na));
				else
					log(string.Format("указан сетевой адрес: {0}", na));
				
                byte channel = 0x01;
                if (!parameters.ContainsKey("channel") || !byte.TryParse(parameters["channel"].ToString(), out channel))
                    log(string.Format("отсутствуют сведения о канале, принят по-умолчанию {0}", channel));

                byte password = 0x00;
                if (!parameters.ContainsKey("password") || !byte.TryParse(parameters["password"].ToString(), out channel))
                    log(string.Format("отсутствуют сведения о пароле, принят по-умолчанию {0}", password));
				
                if (parameters.ContainsKey("start") && arg.start is DateTime)
                {

                    getStartDate = (type) => (DateTime)arg.start;
                    log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
                }
                else
                {
                    getStartDate = (type) => getLastTime(type);
                    log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
                }

				if (parameters.ContainsKey("end") && arg.end is DateTime)
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
				if (parameters.ContainsKey("components"))
				{
					components = arg.components;
					log(string.Format("указаны архивы {0}", components));
				}
				else
				{
					log(string.Format("архивы не указаны, будут опрошены все"));
				}
               
                var isStel = false;
                if (parameters.ContainsKey("isStel"))
                {
                    isStel = arg.isStel.ToString() == "1";
                }
                else
                {
                    log(string.Format("параметр СТЕЛ не указан, будет принят 1"));
                    isStel = true;
                }
                if (isStel)
                {
                    log(string.Format("опрос через контроллер СТЕЛ"));
                }
				
                switch (what.ToLower())
                {
                    case "all": return All(na, channel, password, components, isStel, driverParameters);
                    case "ping": return Ping(na, isStel);
                }
                log(string.Format("неопознаная команда '{0}'", what), level: 1);
            }
            catch (Exception ex)
            {
                log("ошибка " + ex.Message, level: 1);
            }
            return MakeResult(201, what);
        }

        private dynamic Ping(byte na, bool isStel)
        {
            var passport = SayHello(na, isStel);
            if (!passport.success)
            {
                log(string.Format("паспорт не получен, ошибка: {0}", passport.error), level: 1);
                return MakeResult(101);
            }
            log(string.Format("паспорт получен, версия прошивки регистратора: {0}", passport.version), level: 1);
            return MakeResult(0, "опрос успешно завершен");
        }

        private dynamic All(byte na, byte ch, byte pass, string components, bool isStel, HashSet<string> parameters)
        {
            try
            {
                #region Паспорт
                dynamic passport = SayHello(na, isStel);
                if (!passport.success)
                {
                    log(string.Format("паспорт не получен, ошибка: {0}", passport.error), level: 1);
                    return MakeResult(101);
                }

                log(string.Format("паспорт получен, версия прошивки регистратора: {0:0.0}", passport.version));

                #endregion

                #region Константы
                ///необходимо заново прочесть константы
                var needRead = true;
                int contractHour = -1;
                var units = new Dictionary<string, string>();
                if (!components.Contains("Constant"))
                {
                    contractHour = getContractHour();

                    if (contractHour == -1)
                    {
                        needRead = true;
                    }
                    else
                    {
                        dynamic constant = ReadConstantsFromDB();
                        needRead = constant.needRead;
                        if (!needRead) units = constant.units;
                    }
                }

                //if (needRead)
                if (components.Contains("Constant"))
                {
                    log("чтение констант");
                    dynamic constant = GetConstant(na);
                    if (!constant.success)
                    {
                        log(string.Format("константы не получены, ошибка: {0}", constant.error), level: 1);
                        return MakeResult(103);
                    }

                    units = constant.units;
                    contractHour = constant.contractHour;
                    setContractHour(contractHour);
                    log(string.Format("константы получены, отчетный час: {0}", contractHour), level: 1);
                    records(constant.records);
                }
                else
                {
                    var date = DateTime.Now;
                    dynamic serviceOptions = GetServiceOptions(na, date);
                    if (!serviceOptions.success) return serviceOptions;
                    contractHour = serviceOptions.contractHour;
                    //log(string.Format("отчетный час={0}", contractHour));

                    dynamic OptionsOnChannels = GetOptionsOnChannels(na, date);
                    if (!OptionsOnChannels.success) return OptionsOnChannels;

                    //records.AddRange(OptionsOnChannels.records);
                    units = OptionsOnChannels.units;
                }
                #endregion

                #region Текущие
                dynamic current = null;

                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    current = GetCurrent(na, units);
                    if (current.success) break;
                }

                if (!current.success)
                {
                    log(string.Format("текущие параметры не получены, ошибка: {0}", current.error), level: 1);
                    return MakeResult(102);
                }

                log(string.Format("текущие параметры получены, время регистратора: {0:dd.MM.yy HH:mm:ss}", current.date), level: 1);
                records(current.records);

                if (getEndDate == null)
                {
                    getEndDate = (type) => current.date;
                }

                DateTime currentDate = current.date;
                setTimeDifference(DateTime.Now - currentDate);

                #endregion

                #region Сутки
                if (components.Contains("Day"))
                {
                    DateTime startDay = getStartDate("Day").Date;
                    DateTime endDay = getEndDate("Day").Date;

                    for (DateTime date = startDay; date < endDay; date = date.AddDays(1))
                    {
                        //иногда контрактный час неправильно считается как 0?!
                        if (date >= currentDate.AddHours((contractHour == 0)? -10 : -contractHour).Date)
                        {
                            log(string.Format("данные за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }
                        else if (parameters.Contains("test") && ((date >= currentDate.AddHours(-12).Date) || (date >= DateTime.Now.AddHours(-13).AddMinutes(-30).Date)))
                        {
                            log(string.Format("*данные за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }
                        else if (parameters.Contains("bzjbi") && ((date >= currentDate.AddHours(-12).Date) || (date >= DateTime.Now.AddHours(-15).AddMinutes(0).Date)))
                        {
                            log(string.Format("**данные за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }

                        dynamic day = null;
                        int tries = 0;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);

                            day = GetArchive(na, date, ArchiveType.Day, units);

                            if (day.success) break;
                            if (day.code == 0x21) break;
                            tries++;
                        }

                        if (!day.success)
                        {
                            log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error), level: 1);
                            return MakeResult(104);
                        }

                        log(string.Format("суточная запись {0:dd.MM.yy} получена{1}", day.date, tries > 0? string.Format(" ({0} повторений)", tries) : ""), level: 1);
                        records(day.records);
                    }
                }

                #endregion

                #region Часы

                if (components.Contains("Hour"))
                {
                    DateTime startHour = getStartDate("Hour");
                    startHour = startHour.Date.AddHours(startHour.Hour);
                    DateTime endHour = getEndDate("Hour");
                    endHour = endHour.Date.AddHours(endHour.Hour);

                    for (DateTime date = startHour; date < endHour; date = date.AddHours(1))
                    {
                        if (date > currentDate)
                        {
                            log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не сформированы", date));
                            break;
                        }

                        int tries = 0;
                        dynamic hour = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);

                            hour = GetArchive(na, date, ArchiveType.Hour, units);
                            if (hour.success) break;
                            if (hour.code == 0x21) break;
                            tries++;
                        }

                        if (!hour.success)
                        {
                            log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", date, hour.error), level: 1);
                            return MakeResult(105);
                        }

                        log(string.Format("часовая запись {0:dd.MM.yy HH:mm} получена{1}", hour.date, tries > 0 ? string.Format(" ({0} повторений)", tries) : ""));
                        records(hour.records);
                    }
                }

                #endregion

                #region НС

                if (cancel()) return MakeResult(200);

                if (components.Contains("Abnormal"))
                {
                    DateTime startAbnormal = getStartDate("Abnormal");
                    DateTime endAbnormal = getEndDate("Abnormal");
                    log(string.Format("последняя НС запись {0:dd.MM.yy HH:00}", startAbnormal));

                    var abnormal = GetAbnormal(na);
                    if (!abnormal.success)
                    {
                        log(string.Format("чтение записей НС прекращено, причина: {0}", abnormal.error), level: 1);
                    }
                    var recs = (abnormal.records as IEnumerable<dynamic>).Where(r => r.date > startAbnormal);
                    log(string.Format("прочитано {0} новых записей НС", recs.Count()), level: 1);
                    records(recs);
                }

                #endregion
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                return MakeResult(999, ex.Message);
            }
            return MakeResult(0, "опрос успешно завершен");
        }

        /// <summary>
        /// попытка прочесть необходимые для опроса константы
        /// </summary>
        private dynamic ReadConstantsFromDB()
        {
            dynamic constant = new ExpandoObject();
            constant.needRead = true;

            var constants = getLastRecords("Constant").ToList();
            if (constants != null && constants.Any())
            {
                constant.needRead = true;
                List<dynamic> unitRecords = constants.Where(r => r.s1.StartsWith("единица измерения")).ToList();

                if (unitRecords == null || unitRecords.Count == 0) return constant;
                constant.units = new Dictionary<string, string>();
                foreach (var record in unitRecords)
                {
                    constant.units.Add(record.s1.Replace("единица измерения ", ""), record.s2);
                }
                constant.needRead = false;
                return constant;
            }
            return constant;
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }
    }
}

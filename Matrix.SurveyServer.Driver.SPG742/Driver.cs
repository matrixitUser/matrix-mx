// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        /// <summary>
        /// число попыток опроса в случае неудачи
        /// </summary>
        private const int TRY_COUNT = 5;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

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
            var param = (IDictionary<string, object>)arg;

            byte networkAddress = 0x00;
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(param["networkAddress"].ToString(), out networkAddress))
            {
                log(string.Format("отсутствуют сведения о сетевом адресе, принят по-умолчанию {0}", networkAddress));
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


            var channels = new List<byte>();
            if (param.ContainsKey("channel")) //каналов может быть несколько, разделяются запятыми: 1,2
            {
                var chs = param["channel"].ToString().Split(',');
                foreach(var chstr in chs)
                {
                    var chpar = chstr.Split(':');
                    byte chb = 0;
                    if(byte.TryParse(chpar[0], out chb))
                    {
                        channels.Add(chb);
                    }
                }
            }

            if (channels.Count == 0)
            {
                channels.Add(1);
                log(string.Format("отсутствуют сведения о каналах, принят по-умолчанию {0}", channels[0]));
            }

            byte password = 0x00;
            if (!param.ContainsKey("password") || !byte.TryParse(param["password"].ToString(), out password))
                log(string.Format("отсутствуют сведения о пароле, принят по-умолчанию {0}", password));

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


            switch (what.ToLower())
            {
                case "all": return All(networkAddress, password, channels, components);
            }

            log(string.Format("неопознаная команда '{0}'", what), level: 1);
            return MakeResult(201, what);
        }

        private dynamic All(byte na, byte password, List<byte> channels, string components)
        {
            try
            {
                #region Паспорт
                var passport = SayHello(na);
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
                        needRead = true;
                    else
                    {
                        dynamic constant = ReadConstantsFromDB();
                        needRead = constant.needRead;
                        if (!needRead) units = constant.units;
                    }
                }


                if (needRead)
                {
                    log("чтение констант");
                    dynamic constant = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);

                        constant = GetConstant(na, channels, DateTime.Now);
                        if (constant.success) break;

                        log(string.Format("константы не получены, ошибка: {0}", constant.error), level: 1);
                    }

                    if (!constant.success) return MakeResult(103);

                    units = constant.units;
                    contractHour = constant.contractHour;
                    setContractHour(contractHour);
                    log(string.Format("константы получены, отчетный час: {0}", contractHour), level: 1);
                    records(constant.records);
                }
                else
                {
                    log(string.Format("константы получены из БД: отчетный час={0}", contractHour));
                }
                #endregion

                #region Текущие
                dynamic current = null;

                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    current = GetCurrent(na, channels, units);
                    if (current.success) break;

                    log(string.Format("текущие параметры не получены, ошибка: {0}", current.error), level: 1);
                }

                if (!current.success) return MakeResult(102);

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

                    log(string.Format("чтение суточных архивов с {0:dd.MM.yy} по {1:dd.MM.yy}", startDay, endDay));


                    for (DateTime date = startDay; date < endDay; date = date.AddDays(1))
                    {
                        if (date >= currentDate.AddHours(-contractHour).Date)
                        {
                            log(string.Format("данные за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }

                        dynamic day = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel())
                            {
                                log("вызвана остановка процесса опроса");
                                return MakeResult(200);
                            }

                            day = GetArchive(na, channels, date, ArchiveType.Day, units);

                            if (day.success) break;
                            log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error), level: 1);
                            if (day.msgcode == 0x21) break;
                        }

                        if (!day.success) return MakeResult(104);

                        if (day.isEmpty)
                            log(string.Format("суточная запись {0:dd.MM.yy} отсутствует", date), level: 1);
                        else
                        {
                            log(string.Format("суточная запись {0:dd.MM.yy} получена", day.date), level: 1);
                            records(day.records);
                        }
                    }
                }
                #endregion

                #region Часы

                if (components.Contains("Hour"))
                {
                    DateTime startHour = getStartDate("Hour");
                    startHour = startHour.Date.AddHours(startHour.Hour);
                    DateTime endHour = getEndDate("Hour");

                    for (DateTime date = startHour; date < endHour; date = date.AddHours(1))
                    {
                        if (date > currentDate)
                        {
                            log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не сформированы", date));
                            break;
                        }

                        dynamic hour = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);

                            hour = GetArchive(na, channels, date, ArchiveType.Hour, units);
                            if (hour.success) break;
                            log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", date, hour.error), level: 1);
                            if (hour.msgcode == 0x21) break;
                        }

                        if (!hour.success) return MakeResult(105);

                        if (hour.isEmpty)
                            log(string.Format("часовая запись {0:dd.MM.yy HH:mm} отсутствует", date), level: 1);
                        else
                        {
                            log(string.Format("часовая запись {0:dd.MM.yy HH:mm} получена", hour.date));
                            records(hour.records);
                        }
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

                    var abnormal = GetAbnormal(na, channels, startAbnormal, endAbnormal);
                    if (!abnormal.success)
                    {
                        log(string.Format("чтение записей НС прекращено, причина: {0}", abnormal.error), level: 1);
                        return MakeResult(106);
                    }
                    var recs = (abnormal.records as IEnumerable<dynamic>).Where(r => r.date > startAbnormal);
                    log(string.Format("прочитано {0} новых записей НС", recs.Count()), level: 1);
                    records(recs);
                }
                #endregion
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка {0} {1}", ex.Message, ex.StackTrace), level: 1);
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

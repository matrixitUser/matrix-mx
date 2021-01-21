// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.SPG761.Protocol;
using System.Collections;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SPG761
{
    /// <summary>
    /// Драйвер для вычислителей типа СПГ761
    /// вычислитель имеет по одной таблице на каждый тип архива (часовой, суточный и т.д.)
    /// строка таблицы представляет собой временной срез
    /// первое поле строки - дата, последующие - параметры
    /// опрос архивов проходит в два этапа:
    /// 1. опрашивается структура архива 
    /// 2. опрашивается строка с данными
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        #region Константы

        private const bool TRACE_MODE = false;
        /// <summary>
        /// символ префикс
        /// </summary>
        private const byte DLE = 0x10;

        /// <summary>
        /// начало заголовка
        /// </summary>
        private const byte SOH = 0x01;

        /// <summary>
        /// указатель кода функции FNC
        /// </summary>
        private const byte ISI = 0x1f;

        /// <summary>
        /// начало тела сообщения
        /// </summary>
        private const byte STX = 0x02;

        /// <summary>
        /// конец тела сообщения
        /// </summary>
        private const byte ETX = 0x03;

        /// <summary>
        /// код горизонтальной табуляции
        /// </summary>
        private const byte HT = 0x09;

        /// <summary>
        /// подача формы
        /// </summary>
        private const byte FF = 0x0C;

        #endregion

        /// <summary>
        /// число попыток опроса в случае неуспешного запроса
        /// </summary>
        private const int TRY_COUNT = 4;

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

            byte sad = 0x80;
            if (!param.ContainsKey("sad") || !byte.TryParse(param["sad"].ToString(), out sad))
                log(string.Format("отсутствуют сведения об адресе источника (SAD), принят по-умолчанию {0}", sad));
            else
            {
                if (sad < 128 || sad > 157)
                {
                    log(string.Format("адрес источника (SAD) может принимать только значения из диапазона 128...157"));
                    sad = 0x80;
                }
                log(string.Format("указан адрес источника (SAD): {0}", sad));
            }

            byte dad = 0x00;
            if (!param.ContainsKey("dad") || !byte.TryParse(param["dad"].ToString(), out dad))
                log(string.Format("отсутствуют сведения об адресе приёмника (DAD), принят по-умолчанию {0}", dad));
            else
                log(string.Format("указан адрес приёмника (DAD): {0}", dad));

            byte channel = 0x01;
            if (!param.ContainsKey("channel") || !byte.TryParse(param["channel"].ToString(), out channel))
                log(string.Format("отсутствуют сведения о канале, принят по-умолчанию {0}", channel));


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

            switch (what.ToLower())
            {
                case "all": return All(dad, sad, channel, components);
            }

            log(string.Format("неопознаная команда '{0}'", what), level: 1);
            return MakeResult(201, what);
        }

        private dynamic All(byte dad, byte sad, byte ch, string components)
        {
            try
            {
                #region Паспорт
                dynamic passport = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);

                    passport = GetPassport(dad, sad);
                    if (passport.success) break;
                    log(string.Format("паспорт не получен, ошибка: {0}", passport.error), level: 1);
                }
                if (!passport.success) return MakeResult(101);
                log(string.Format("паспорт прочитан, тип прибора {0}, версия {1}", passport.model, passport.n));

                bool needDad = passport.needDad;
                #endregion

                #region Текущие
                dynamic current = null;

                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);
                    current = GetCurrent(dad, sad, needDad, ch);
                    if (current.success) break;

                    log(string.Format("текущие параметры не получены, ошибка: {0}", current.error), level: 1);
                }

                if (!current.success) return MakeResult(102);

                log(string.Format("текущие параметры получены, текущая дата регистратора: {0:dd.MM.yy HH:mm:ss}", current.date), level: 1);
                records(current.records);
                if (getEndDate == null)
                {
                    getEndDate = (type) => current.date;
                }

                DateTime currentDate = current.date;
                setTimeDifference(DateTime.Now - currentDate);
                #endregion

                #region Константы

                ///необходимо заново прочесть константы
                var needRead = false;

                int contractHour = getContractHour();

                if (contractHour == -1) needRead = true;

                if (needRead || components.Contains("Constant"))
                {
                    log("начато чтение констант");
                    dynamic constant = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);
                        constant = GetConstants(dad, sad, needDad, ch, passport.n, current.date);
                        if (constant.success) break;

                        log(string.Format("константы не получены, ошибка: {0}", constant.error), level: 1);
                    }

                    if (!constant.success) return MakeResult(103);

                    records(constant.records);
                    contractHour = constant.contractHour;
                    setContractHour(contractHour);
                    log(string.Format("константы прочитаны, параметр 003: {1}, контрактный час: {0}", contractHour, constant.p003), level: 1);
                }
                else
                {
                    log(string.Format("контрактный час был прочитан из локальной БД: {0}", contractHour), level: 1);
                }

                #endregion

                #region Сутки
                if (components.Contains("Day"))
                {
                    dynamic dayHead = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);
                        dayHead = GetHeadArchive(dad, sad, needDad, DAY);
                        if (dayHead.success) break;
                        log(string.Format("заголовок суточных архивов не получен: {0}", dayHead.error), level: 1);
                    }
                    if (!dayHead.success) return MakeResult(104, "заголовок суточных архивов не получен");

                    log(string.Format("заголовок суточных архивов получен"));

                    DateTime startDay = getStartDate("Day").Date;
                    DateTime endDay = getEndDate("Day").Date;

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
                            if (cancel()) return MakeResult(200);
                            day = GetArchive(dad, sad, needDad, date, DAY, passport.n, dayHead.parameters);
                            if (day.success) break;
                            log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error), level: 1);
                        }

                        if (!day.success) return MakeResult(104);

                        log(string.Format("суточная запись {0:dd.MM.yy} получена", day.date), level: 1);
                        records(day.records);
                    }
                }
                #endregion

                #region Часы

                if (components.Contains("Hour"))
                {
                    dynamic hourHead = null;
                    for (int i = 0; i < TRY_COUNT; i++)
                    {
                        if (cancel()) return MakeResult(200);

                        hourHead = GetHeadArchive(dad, sad, needDad, HOUR);
                        if (hourHead.success) break;
                        log(string.Format("заголовок часовых архивов не получен: {0}", hourHead.error), level: 1);
                    }
                    if (!hourHead.success) return MakeResult(105, "заголовок часовых архивов не получен");

                    log(string.Format("заголовок часовых архивов получен"));


                    var h = getStartDate("Hour");
                    DateTime startHour = h.Date.AddHours(h.Hour);
                    DateTime endHour = getEndDate("Hour");

                    for (DateTime date = startHour; date < endHour; date = date.AddHours(1))
                    {
                        if (date > current.date)
                        {
                            log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не сформированы", date));
                            break;
                        }
                        dynamic hour = null;
                        for (int i = 0; i < TRY_COUNT; i++)
                        {
                            if (cancel()) return MakeResult(200);

                            hour = GetArchive(dad, sad, needDad, date, HOUR, passport.n, hourHead.parameters);
                            if (hour.success) break;
                            log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", date, hour.error), level: 1);
                        }

                        if (!hour.success) return MakeResult(105);

                        log(string.Format("часовая запись {0:dd.MM.yy HH:00} получена", hour.date));
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
                    log(string.Format("Начат поиск НС с {0:dd.MM.yy HH:00}", startAbnormal));

                    var abnormal = GetAbnormal(dad, sad, needDad, startAbnormal, endAbnormal);
                    if (!abnormal.success)
                    {
                        log(string.Format("Поиск записей НС прекращен с ошибкой: {0}", abnormal.error), level: 1);
                        return MakeResult(106);
                    }
                    // log(string.Format("прочитано {0} новых записей НС", (abnormal.records as IEnumerable<dynamic>).Count()));


                    var recs = (abnormal.records as IEnumerable<dynamic>).Where(r => r.date > startAbnormal).ToArray();
                    if (!recs.Any())
                    {
                        log(string.Format("Новые записи НС не найдены"));
                    }
                    else
                    {
                        log(string.Format("Прочитано {0} новых записей НС", recs.Length), level: 1);
                        records(recs);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка {0}", ex.Message), level: 1);
                return MakeResult(999, ex.Message);
            }
            return MakeResult(0, "опрос успешно завершен");
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

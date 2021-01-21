#define DEBUG //*
#if DEBUG
#define DEBUG_HOLES
#endif

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;
using Matrix.PollServer.Storage;
using Newtonsoft.Json;
using System.Diagnostics;
using NLog;

namespace Matrix.PollServer.Nodes.Tube
{
    class TubeNode : PollNode
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        IDictionary<Guid, object> cacheLocal = new Dictionary<Guid, object>();
        Load load = new Load();
        
        public dynamic cacheGet(Guid id)
        {
            try { return (cacheLocal.ContainsKey(id)) ? cacheLocal[id] : null; }
            catch (Exception ex) { Log(ex.Message + " in cacheGet"); return null; }
        }
        public void cacheSet(Guid id, dynamic cache)
        {
            try
            {
                if (cacheLocal.ContainsKey(id)) cacheLocal[id] = cache;
                else cacheLocal.Add(id, cache);
            }
            catch(Exception ex) { Log(ex.Message + " in cacheSet"); }
        }
        public TubeNode(dynamic content)
        {
            this.content = content;
        }

        private bool IsLogEnabled()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("logEnable") || !(content.logEnable is bool))
            {
                return false;
                ////[deprecated]
                //if (!dcontent.ContainsKey("traceMode") || !(content.traceMode is bool))
                //{
                //}
                //return content.traceMode;
            }
            return content.logEnable;
        }

        public int? GetNetworkAddress()
        {
            var dcontent = content as IDictionary<string, object>;
            int networkAddress = 1;

            if (!dcontent.ContainsKey("networkAddress") || !int.TryParse(content.networkAddress.ToString(), out networkAddress))
            {
                return null;
            }

            return networkAddress;
        }

        private dynamic HoursProcess(DateTime start, DateTime end, DateTime date, int contractHour, bool hoursDaily, bool onlyHoles)
        {
#if DEBUG_HOLES
            Log(string.Format("[часовые {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}] КЧ={2} ЧД{3} ДЫРЫ{4}", start, end, contractHour, hoursDaily ? 1 : 0, onlyHoles ? 1 : 0));
#endif

            dynamic result = new ExpandoObject();
            result.success = false;
            result.error = string.Empty;

            DateTime startHour = (start == DateTime.MinValue) ? GetLastTime("Hour") : start;
            DateTime endHour = end;

            int depth = GetArchiveDepth("Hour");
            if (depth == 0)
            {
                result.error = "вычислитель не содержит часовые архивы";
                return result;
            }

            // диапазон start-end в хэш-массив дат dates
            HashSet<DateTime> hours = new HashSet<DateTime>();

            DateTime startHourC = DateTime.MinValue;
            DateTime endHourC = DateTime.MaxValue;

            // часовые по суткам
            if (hoursDaily)
            {
                startHourC = startHour.AddHours(-contractHour).Date.AddHours(contractHour);
                endHourC = endHour == DateTime.MaxValue ? date.AddHours(-contractHour).Date.AddHours(contractHour) : endHour.AddHours(-contractHour).Date.AddHours(contractHour);
            }
            else
            {
                startHourC = startHour.Date.AddHours(startHour.Hour);
                endHourC = endHour == DateTime.MaxValue ? date.Date.AddHours(date.Hour) : endHour.Date.AddHours(endHour.Hour);
            }

            if ((depth != -1) && ((date.Date.AddHours(date.Hour) - startHourC.Date.AddHours(startHourC.Hour)).TotalHours > depth))
            {
                result.error = string.Format("диапазон опроса часовых скорректирован с учётом глубины архива вычислителя, новый диапазон {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", startHour, endHour);
                startHourC = date.Date.AddHours(date.Hour - depth);
            }

            if (startHourC >= endHourC)
            {
                result.error = string.Format("неправильный диапазон опроса {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", startHourC, endHourC);
                return result;
            }

            // массив часов
            for (var d = startHourC; d < endHourC; d = d.AddHours(1))
            {
                hours.Add(d);
            }

#if DEBUG_HOLES
            Log(string.Format("[диапазон опроса часовых {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}] часов {2}; а получилось - {3}", startHourC, endHourC, (endHourC - startHourC).TotalHours, hours.Count));
#endif

            // только дыры
            if (onlyHoles)
            {
                IEnumerable<DateTime> dates = GetRecordDates("Hour", startHourC, endHourC);
                if (dates != null)
                {
                    foreach (var d in dates)
                    {
                        if (hours.Contains(d))
                        {
                            hours.Remove(d);
                        }
                    }
                }
            }
            else if (hours.Count() > 240)
            {
                result.error = string.Format("можно считать не более 240 часовых записей подряд (сейчас - {0})", hours.Count());
                return result;
            }


            // фильтры
            if (hours.Count() == 0)
            {
                result.error = "нет часовых для опроса";
                return result;
            }

            // диапазоны опроса 
            try
            {
                result.hourRanges = DatesToRanges(hours, "hour", date, endHour).ToList();
                result.success = true;
            }
            catch (Exception e)
            {

            }

            return result;
        }

        private dynamic DaysProcess(DateTime start, DateTime end, DateTime date, int contractHour, int contractDay, bool onlyHoles)//, bool hoursDaily)
        {
            dynamic result = new ExpandoObject();
            result.success = false;
            result.error = string.Empty;

            DateTime startDay;
            if (start == DateTime.MinValue)
            {
                if (onlyHoles)
                {
                    //date.Day;//текущий день, 1-31
                    //GetContractDay();// КД устройства, 1..31 или -1, если не установлен
                    //contractDay;// КД программный; 1..31; по умолчанию равен КД устройства или 1
                    //start - начало периода опроса

                    int deviceContractDay = GetContractDay();
                    if (deviceContractDay < 1 || deviceContractDay > 31)
                    {
                        deviceContractDay = contractDay;
                    }

                    // текущий, предыдущий и пред-предыдущий месяц
                    DateTime dateNormalized = date.Date.AddDays(-1);
                    //DateTime thisMonth = dateNormalized.AddDays(1 - dateNormalized.Day);
                    //int daysInThisMonth = DateTime.DaysInMonth(thisMonth.Year, thisMonth.Month);
                    //DateTime previousMonth = thisMonth.AddMonths(-1);
                    //int daysInPreviousMonth = DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
                    //DateTime veryPreviousMonth = previousMonth.AddMonths(-1);
                    //int daysInVeryPreviousMonth = DateTime.DaysInMonth(veryPreviousMonth.Year, veryPreviousMonth.Month);

                    if (deviceContractDay <= contractDay)
                    {
                        if (dateNormalized.Day < contractDay)
                        {
                            startDay = GetStartDate(dateNormalized, -1, deviceContractDay);
                            ////сегодня 10е, КД=25 => взять на месяц больше
                            //if (deviceContractDay > daysInPreviousMonth)
                            //{
                            //    start = previousMonth.AddDays(daysInPreviousMonth - 1);
                            //}
                            //else
                            //{
                            //    start = previousMonth.AddDays(deviceContractDay - 1);
                            //}
                        }
                        else
                        {
                            startDay = GetStartDate(dateNormalized, 0, deviceContractDay);
                            ////сегодня 10е, КД=1 => от КД
                            //if (deviceContractDay > daysInThisMonth)
                            //{
                            //    start = thisMonth.AddDays(daysInThisMonth - 1);
                            //}
                            //else
                            //{
                            //    start = thisMonth.AddDays(deviceContractDay - 1);
                            //}
                        }
                    }
                    else
                    {
                        // два (различных) КД - заточено под ВКТ-7; ориентируемся на КДп с оглядкой на КДу
                        // на устройстве КД больше, например КДу=25, КДп=22 => надо закрывать дыры, начиная с 25го числа предыдущего КМу
                        if (dateNormalized.Day < contractDay)
                        {
                            startDay = GetStartDate(dateNormalized, -2, deviceContractDay);
                            //// если еще не был КДп в этом месяце, то интересуемый период начинается с пред. месяца КДп; например, сегодня 1.03, КДп=22 => интересуемый период 22.02~22.03
                            //// однако, т.к КДу=25, то период сдвигается и становится 25.01(!)~22.03
                            //if (deviceContractDay > daysInVeryPreviousMonth)
                            //{
                            //    start = veryPreviousMonth.AddDays(daysInVeryPreviousMonth - 1);
                            //}
                            //else
                            //{
                            //    start = veryPreviousMonth.AddDays(deviceContractDay - 1);
                            //}
                        }
                        else
                        {
                            startDay = GetStartDate(dateNormalized, -1, deviceContractDay);
                            //// КДп был в этом месяце, интересуемый период начинается с этого месяца КДп; например, сегодня 30.03, КДп=22 => интересуемый период 22.03~22.04
                            //// т.к КДу=25, то период сдвигается и становится 25.02~22.04
                            //if (deviceContractDay > daysInPreviousMonth)
                            //{
                            //    start = previousMonth.AddDays(daysInPreviousMonth - 1);
                            //}
                            //else
                            //{
                            //    start = previousMonth.AddDays(deviceContractDay - 1);
                            //}
                        }
                        result.error = $"Начало периода установлено в {startDay:dd.MM.yyyy} для расчета интегратора";
                    }
                }
                else
                {
                    startDay = GetLastTime("Day");
                }
            }
            else
            {
                startDay = start;
            }
            DateTime endDay = end;

            // глубина архива в вычислителе
            int depth = GetArchiveDepth("Day");
            if (depth == 0)
            {
                result.error = "вычислитель не содержит суточные архивы";
                return result;
            }

            // диапазон start-end в хэш-массив дат dates
            HashSet<DateTime> days = new HashSet<DateTime>();

            DateTime dateCH = date.AddHours(-contractHour).Date;

            DateTime startDayC = startDay.Date;
            DateTime endDayC = endDay == DateTime.MaxValue ? dateCH : endDay.Date;

            if ((depth != -1) && ((dateCH - startDay.Date).TotalDays > depth))
            {
                startDayC = dateCH.AddDays(-depth);
            }

            for (var d = startDayC; d < endDayC; d = d.AddDays(1))
            {
                if (d < date.Date)
                {
                    days.Add(d);
                }
            }

            //

            if (onlyHoles)
            {
                IEnumerable<DateTime> dates = GetRecordDates("Day", startDayC, endDayC);
                if (dates != null)
                {
                    foreach (var d in dates)
                    {
                        if (days.Contains(d))
                        {
                            days.Remove(d);
                        }
                    }
                }
            }


            if (days.Count() > 90)
            {
                result.error = string.Format("можно считать не более 90 суточных записей подряд (сейчас - {0})", days.Count());
                return result;
            }

            // фильтры
            if (days.Count() == 0)
            {
                result.error = "нет суточных для опроса";
                return result;
            }

            // диапазоны опроса 
            try
            {
                result.dayRanges = DatesToRanges(days, "day", date, endDay).ToList();
                result.success = true;
            }
            catch (Exception e)
            {

            }
            
            return result;
        }

        private DateTime GetStartDate(DateTime date, int monthStep, int CDay)
        {
            DateTime date1 = date.AddDays(1 - date.Day);
            DateTime monthBack = date1.AddMonths(monthStep);
            int daysInMonthBack = DateTime.DaysInMonth(monthBack.Year, monthBack.Month);

            if (CDay > daysInMonthBack)
            {
                return monthBack.AddDays(daysInMonthBack - 1);
            }
            else
            {
                return monthBack.AddDays(CDay - 1);
            }
        }

        public override bool BeforeTaskAdd(PollTask task)
        {
            bool result = true;

            ////обновление состояния обьекта            
            if (!inProccess)
            {
                //try
                //Stopwatch sw = new Stopwatch();
                //Log(string.Format("[началось добавление задачи '{0}']", task.What));
                //sw.Start();

                #region инициализация кэша
                if (cache == null)
                {
                    //cache = RecordsRepository.Instance.Get(GetId());
                    cache = cacheGet(GetId());
                }
                #endregion

                //время на счётчике:
                DateTime date = GetDeviceTime();

                // что делать, если аргументов нет???
                #region Получение аргументов таска
                dynamic arg = task.Arg;
                var darg = task.Arg as IDictionary<string, object>;
                #endregion

                // старые аргументы: start, end, components
                //  start-end - диапазон дат для ВСЕХ компонентов
                //  components - компоненты опроса: архивы и текущие
                // новые аргументы:
                //  startHour-endHour - диапазон дат только для часовых (приоритет)
                //  startDay-endDay - диапазон дат только для суточных (приоритет)
                //  startAbnormal-endAbnormal - диапазон дат только для НС (приоритет)

                //контрактный час
                int contractHour;
                //контрактные сутки (программные)
                int contractDay;
                // опрос нужен, если есть "дыры"
                bool onlyHoles = false;
                // часовые раз в день
                bool hoursDaily = false;
                //DateTime startHour, startDay, startAbnormal;
                //DateTime endHour, endDay, endAbnormal;
                DateTime start = DateTime.MinValue;
                DateTime end = DateTime.MaxValue;
                Dictionary<string, IEnumerable<string>> components;

                #region обработка аргументов таска
                #region contractHour
                if (darg.ContainsKey("contractHour"))
                {
                    if (arg.contractHour is string)
                    {
                        int.TryParse(arg.contractHour as string, out contractHour);
                    }
                    else if (arg.contractHour is int)
                    {
                        contractHour = (int)arg.contractHour;
                    }
                    else
                    {
                        contractHour = GetContractHour();
                    }
                }
                else
                {
                    contractHour = GetContractHour();
                }
                if (contractHour == -1)
                {
                    contractHour = 0;
                }
                #endregion

                #region contractDay
                if (darg.ContainsKey("contractDay"))
                {
                    if (arg.contractDay is string)
                    {
                        int.TryParse(arg.contractDay as string, out contractDay);
                    }
                    else if (arg.contractDay is int)
                    {
                        contractDay = (int)arg.contractDay;
                    }
                    else
                    {
                        contractDay = GetContractDay();
                    }
                }
                else
                {
                    contractDay = GetContractDay();
                }
                if (contractDay < 1)
                {
                    contractDay = 1;
                }
                else if (contractDay > 31)
                {
                    contractDay = 31;
                }
                #endregion

                #region onlyHoles
                if (darg.ContainsKey("onlyHoles"))
                {
                    if (arg.onlyHoles is bool)
                    {
                        onlyHoles = (bool)arg.onlyHoles;
                    }
                    else if (arg.onlyHoles is string)
                    {
                        bool.TryParse(arg.onlyHoles as string, out onlyHoles);
                    }
                }
                #endregion

                #region hoursDaily
                if (darg.ContainsKey("hoursDaily"))
                {
                    if (arg.hoursDaily is bool)
                    {
                        hoursDaily = (bool)arg.hoursDaily;
                    }
                    else if (arg.hoursDaily is string)
                    {
                        bool.TryParse(arg.hoursDaily as string, out hoursDaily);
                    }
                }
                #endregion

                /* Компоненты играют важную роль в автоматическом опросе
                 * Существуют 2 варианта опроса:
                 * - полный опрос
                 * - опрос дыр
                 * 
                 * При автоопросе нужный вариант будет выбран исходя из аргумента "onlyHoles"
                 * 
                 * При полном опросе будут опрошены все выбранные компоненты по всему выбранному диапазону
                 * 
                 * При опросе дыр в архивах (суточные, часовые) будет запущен поиск дыр в пределах диапазона. 
                 * Если выбран компонент констант, то они опросятся в случае устаревания. Условие устаревания - наличие записей в текущем месяце
                 * Если дыры найдены и выбраны компоненты текущих и/или НС, то они также будут опрошены
                 * 
                 * Если в аргументах опроса не указаны компоненты, то они будут взяты из аргумента componentsDefault
                 * 
                 */

                #region componentsDefault (зависит от onlyHoles)
                // компоненты опроса по умолчанию
                string componentsDefault = (darg.ContainsKey("componentsDefault") && (arg.componentsDefault is string) && ((arg.componentsDefault) != "")) ?
                    (string)arg.componentsDefault :
                    (onlyHoles ? "Constants;Current;Hour;Day;Abnormal" : "Current;Hour;Day;Abnormal");
                #endregion

                #region components
                // компоненты опроса
                string componentsInput = (darg.ContainsKey("components") && (arg.components is string) && ((arg.components) != "")) ? (string)arg.components : componentsDefault;
                components = parseComponents(componentsInput);
                #endregion

                #region start => startHour, startDay, startAbnormal
                // начало опроса
                if (darg.ContainsKey("start") && arg.start is DateTime)
                {
                    start = (DateTime)arg.start;
                }
                //else
                //{
                //    startHour = GetLastTime("Hour");
                //    startDay = GetLastTime("Day");
                //    startAbnormal = GetLastTime("Abnormal");
                //}
                #endregion

                #region end => endHour, endDay, endAbnormal
                // конец опроса
                if (darg.ContainsKey("end") && arg.end is DateTime)
                {
                    end = (DateTime)arg.end;
                }
                //else
                //{
                //    endHour = endDay = endAbnormal = DateTime.MaxValue; //date;
                //}
                #endregion
                #endregion

                #region проверки и корректировки диапазонов
                // на случай некорретного диапазона


                //Log(string.Format("[часовой диапазон опроса {0}-{1}]", startHour, endHour));
                //Log(string.Format("[суточный диапазон опроса {0}-{1}]", startDay, endDay));
                #endregion

                // диапазоны дат для драйвера
                List<dynamic> hourRanges = null;
                List<dynamic> dayRanges = null;

                // 1 раз в час ИЛИ 1 раз в сутки (только дыры)
                #region обработка часового компонента
                if (components.ContainsKey("Hour"))
                {
                    dynamic answer = HoursProcess(start, end, date, contractHour, hoursDaily, onlyHoles);
                    if (!answer.success)
                    {
                        components.Remove("Hour");
                        Log("Опрос часовых отменён: " + answer.error);
                    }
                    else
                    {
                        if (answer.error != "")
                        {
                            Log("Внимание: " + answer.error);
                        }
                        hourRanges = answer.hourRanges;
                    }
                }
                #endregion

                // 1 раз в сутки (только дыры)
                #region обработка суточного компонента
                if (components.ContainsKey("Day"))
                {
                    dynamic answer = DaysProcess(start, end, date, contractHour, contractDay, onlyHoles);
                    if (!answer.success)
                    {
                        components.Remove("Day");
                        Log("Опрос суточных отменён: " + answer.error);
                    }
                    else
                    {
                        if (answer.error != "")
                        {
                            Log("Внимание: " + answer.error);
                        }
                        dayRanges = answer.dayRanges;
                    }
                }
                #endregion

                // 1 раз в месяц (только дыры)
                #region обработка компонента констант
                if (components.ContainsKey("Constants"))
                {

                    if (onlyHoles)
                    {
                        DateTime constStart = new DateTime(date.Year, date.Month, 1);
                        DateTime constEnd = constStart.AddMonths(1).AddSeconds(-1);
                        IEnumerable<DateTime> records = GetRecordDates("Constant", constStart, constEnd);
                        if (records != null && records.Any())
                        {
#if DEBUG_HOLES
                            //Log(string.Format("[константы не устарели, т.к найдены в диапазоне {0:dd.MM.yyyy}-{1:dd.MM.yyyy}]", new DateTime(date.Year, date.Month, 1), date));
#endif
                            components.Remove("Constants");
                        }
                    }
                }
                #endregion

                // пассивный опрос (только дыры)
                #region обработка компонента НС
                if (components.ContainsKey("Abnormal"))
                {
                    DateTime startAbnormal = (start == DateTime.MinValue) ? GetLastTime("Abnormal") : start;
                    DateTime endAbnormal = end;

                    if (startAbnormal >= endAbnormal)
                    {
                        Log(string.Format("Внимание: неправильный диапазон НС {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", startAbnormal, endAbnormal));
                    }
                    else if (onlyHoles)
                    {
                        if (!components.ContainsKey("Constants") && !components.ContainsKey("Hour") && !components.ContainsKey("Day"))
                        {
                            components.Remove("Abnormal");
                        }
                    }
                }
                #endregion

                // пассивный опрос (только дыры)
                #region обработка компонента текущих
                if (components.ContainsKey("Current"))
                {
                    if (onlyHoles)
                    {
                        if (!components.ContainsKey("Constants") && !components.ContainsKey("Hour") && !components.ContainsKey("Day"))
                        {
                            //Log("Опрос текущих отменён: нет архивов для опроса");
                            components.Remove("Current");
                        }
                    }
                }
                #endregion

                if (!components.Any())
                {
                    Log("Опрос отменён: нет компонентов для опроса");
                    result = false;
                }

                // отправка аргументов в драйвер
                arg.hourRanges = hourRanges;
                arg.dayRanges = dayRanges;
                arg.components = makeComponents(components);

                //sw.Stop();
                //Log(string.Format("[завершилось добавление задачи '{0}' длительностью {1} с результатом {2}", task.What, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds), result.ToString()));
            }

            return result ? base.BeforeTaskAdd(task) : false;
        }

        public override void AfterTaskAdd(PollTask task)
        {
            ////обновление состояния обьекта            
            if (!inProccess)
            {
                UpdateState(Codes.TASK_ADDED, task.What);
            }
        }

        public override void AfterTaskSkip(PollTask task)
        {
            ////обновление состояния обьекта            
            if (!inProccess)
            {
                //UpdateState(Codes.EMPTY_TASK, task.What);
            }
        }

        private bool inProccess = false;

        protected override void AfterCancel()
        {
            isCancel = true;
            //  AppendCache(STATE_IDLE, "отмена опроса");
        }

        private readonly object bufferLocker = new object();

        private Action<bool, byte[]> acceptVComCallback = null;
        public override void AcceptVirtualCom(dynamic message)
        {
            if (acceptVComCallback == null) return;

            if (message.body.what == "vcom-close")
            {
                acceptVComCallback(true, null);
            }

            if (message.body.what == "vcom-bytes")
            {
                acceptVComCallback(false, Convert.FromBase64String(message.body.bytes));
            }
        }

        bool isCancel = false;

        #region дополнительные функции
        private string makeComponents(Dictionary<string, IEnumerable<string>> components)
        {
            List<string> result = new List<string>();
            foreach (var c in components)
            {
                List<string> composite = new List<string>();
                composite.Add(c.Key);
                composite.AddRange(c.Value);
                result.Add(string.Join(":", composite.ToArray()));
            }
            return string.Join(";", result.ToArray());
        }

        private Dictionary<string, IEnumerable<string>> parseComponents(string components)
        {
            var result = new Dictionary<string, IEnumerable<string>>();
            var c = components.Split(';');
            foreach (var component in c)
            {
                if (component == "") continue;
                var Part = component.Split(':');
                result.Add(Part[0], Part.Skip(1));
            }
            return result;
        }

        private IEnumerable<dynamic> DatesToRanges(IEnumerable<DateTime> dates, string type, DateTime currentDate, DateTime maxDate)
        {
            DateTime currentDateType;
            TimeSpan d;
            if (type.ToLower() == "hour")
            {
                d = new TimeSpan(1, 0, 0);
                currentDateType = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, 0, 0);
            }
            else if (type.ToLower() == "day")
            {
                d = new TimeSpan(1, 0, 0, 0);
                currentDateType = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 0, 0, 0);
            }
            else
            {
                return null;
            }

            List<dynamic> ranges = new List<dynamic>();
            if (dates.Any())
            {
                List<DateTime> dateList = dates.ToList();
                dateList.Sort();
                HashSet<DateTime> dateHashset = new HashSet<DateTime>(dateList);

                //Log(string.Format("[получение диапазонов из дат: {0}; тип {1}; текущее время {2:dd.MM.yy HH:mm}; максимум {3:dd.MM.yy HH:mm}]",
                //    dateList.Count() == 0? "[]" : (dateList.Count < 4? 
                //        string.Format("[{0}]", string.Join(",", dateList.Select(di => string.Format("{0:dd.MM.yy HH:mm}", di)))) :
                //        string.Format("[{0:dd.MM.yy HH:mm}, {1:dd.MM.yy HH:mm}, ..., {2:dd.MM.yy HH:mm}, {3:dd.MM.yy HH:mm}]", dateList.First(), dateList.Skip(1).First(), dateList.Skip(dateList.Count() - 1).First(), dateList.Last())
                //    ),
                //    type,
                //    currentDate,
                //    maxDate));

                dynamic range = new ExpandoObject();
                range.start = DateTime.MinValue;
                range.end = DateTime.MinValue;

                for (DateTime date = dateList.First(); date <= dateList.Last(); date = date.Add(d))
                {
                    if (dateHashset.Contains(date))
                    {
                        if (range.start == DateTime.MinValue)
                        {
                            range.start = date;
                        }
                        range.end = date.Add(d);
                    }
                    else //if (!dateHashset.Contains(date))
                    {
                        if (range.start != DateTime.MinValue)
                        {
#if DEBUG_HOLES
                            Log(string.Format("[диапазон {2} сформирован: {0:dd.MM.yy HH:mm}-{1:dd.MM.yy HH:mm}]", range.start, range.end, type));
#endif
                            ranges.Add(range);
                            range = new ExpandoObject();
                            range.start = DateTime.MinValue;
                            range.end = DateTime.MinValue;
                        }
                    }
                }

                if (range.end != DateTime.MinValue)
                {
                    bool addInfiniteRange = false;
                    if (maxDate == DateTime.MaxValue)
                    {
                        if (range.end == currentDateType)
                        {
                            range.end = DateTime.MaxValue;
                        }
                        else
                        {
                            addInfiniteRange = true;
                        }
                    }
                    ranges.Add(range);
#if DEBUG_HOLES
                    Log(string.Format("[диапазон {2} сформирован: {0:dd.MM.yy HH:mm}-{1:dd.MM.yy HH:mm}]", range.start, range.end, type));
#endif
                    if (addInfiniteRange)
                    {
                        range = new ExpandoObject();
                        range.start = currentDateType.Add(d);
                        range.end = DateTime.MaxValue;
                        ranges.Add(range);
#if DEBUG_HOLES
                        Log(string.Format("[диапазон {2} сформирован: {0:dd.MM.yy HH:mm}-{1:dd.MM.yy HH:mm}]", range.start, range.end, type));
#endif
                    }
                }
            }

            return ranges;
        }

        //private IEnumerable<object> GetRecords(string type, DateTime start, DateTime end)
        //{
        //    dynamic message = Helper.BuildMessage("records-get");
        //    message.body.targets = new string[] { GetId().ToString() };
        //    message.body.start = start;
        //    message.body.end = end;
        //    message.body.type = type;
        //    var connector = UnityManager.Instance.Resolve<IConnector>();
        //    dynamic file = connector.SendMessage(message);

        //    if (file.body is IDictionary<string, object> && (file.body as IDictionary<string, object>).ContainsKey("records") && (file.body.records is IEnumerable<object>))
        //    {
        //        return (file.body.records as IEnumerable<object>);
        //    }
        //    return null;
        //}

        private IEnumerable<DateTime> GetRecordDates(string type, DateTime start, DateTime end)
        {
            dynamic message = Helper.BuildMessage("records-get-dates");
            message.body.id = GetId().ToString();
            message.body.start = start;
            message.body.end = end;
            message.body.type = type;
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);

            if (file.body is IDictionary<string, object> && (file.body as IDictionary<string, object>).ContainsKey("records") && (file.body.records is IEnumerable<object>))
            {
                IEnumerable<DateTime> dates = (file.body.records as IEnumerable<object>).Select(o => (DateTime)o);
                return dates;
            }
            return null;
        }
        #endregion

        #region вспомогательные функции драйвера
        private DateTime GetLastTime(string type)
        {
            var dcache = cache as IDictionary<string, object>;
            if (dcache != null && dcache.ContainsKey("lastDate"))
            {
                var dlast = cache.lastDate as IDictionary<string, object>;
                if (dlast.ContainsKey(type))
                {
                    switch (type.ToLower())
                    {
                        case "day": return ((DateTime)dlast[type]).AddDays(-2);
                        case "hour": return ((DateTime)dlast[type]).AddHours(-2);
                        default: return (DateTime)dlast[type];
                    }
                }
                else
                {
                    switch (type.ToLower())
                    {
                        case "day": return DateTime.Today.AddDays(-2);
                        case "hour": return DateTime.Today;
                        default: return DateTime.Today;
                    }
                }
            }
            else
            {
                switch (type.ToLower())
                {
                    case "day": return DateTime.Today.AddDays(-2);
                    case "hour": return DateTime.Today;
                    default: return DateTime.Today;
                }
            }
        }

        private void SetTimeDifference(TimeSpan timeSpan)
        {
            if (cache == null) return;

            if (Math.Abs(timeSpan.TotalDays) > 1)
            {
                //какое-то действие в случае большого расхождения
                return;
            }

#if DEBUG_HOLES
            Log(string.Format("[получено время на вычислителе {0} на сервере {1}]", DateTime.Now.Add(-timeSpan), DateTime.Now));
#endif
            cache.timeDifference = timeSpan;
            //RecordsRepository.Instance.Set(GetId(), cache);
        }

        private DateTime GetDeviceTime()
        {
            if (cache != null && (cache is IDictionary<string, object>) && (cache as IDictionary<string, object>).ContainsKey("timeDifference") && (cache.timeDifference is TimeSpan))
            {
                DateTime date = DateTime.Now.Add(-(TimeSpan)cache.timeDifference);
#if DEBUG_HOLES
                Log(string.Format("[расчётное время на счётчике {0} на сервере {1}]", date, DateTime.Now));
#endif
                return date;
            }

            //Log(string.Format("[расчётное время на счётчике НЕИЗВЕСТНО на сервере {0}]", DateTime.Now));
            return DateTime.Now;
        }

        private void SetContractHour(int contractHour)
        {
            if (cache == null) return;
            cache.contractHour = contractHour;
        }
       
        private void SetModbusControl(dynamic control, Guid objectId)
        {
            if (control == null) return;
            if (objectId == new Guid()) return;
            var dcontrol = control as IDictionary<string, object>;
            ModbusControl.Instance.NodeControllerData(control, objectId);
        }
       
        private void SetIndicationForRowCache(double indication, string indicatioUnitMeasurement, DateTime date, Guid objectId)
        {
            if (objectId == new Guid()) return;
            Api.ApiProxy.Instance.SaveIndication(indication, indicatioUnitMeasurement, date, objectId);
        }
        private int GetContractHour()
        {
            var dcache = cache as IDictionary<string, object>;
            if (dcache != null && dcache.ContainsKey("contractHour"))
            {
                return (int)cache.contractHour;
            }
            else
            {
                return -1;
            }
        }

        private void SetContractDay(int contractDay)
        {
            if (cache == null) return;
            cache.contractDay = contractDay;
        }

        private int GetContractDay()
        {
            var dcache = cache as IDictionary<string, object>;
            if (dcache != null && dcache.ContainsKey("contractDay"))
            {
                return (int)cache.contractDay;
            }
            else
            {
                return -1;
            }
        }

        private void SetArchiveDepth(string archiveType, int depth)
        {
            if (cache == null) return;
            if (archiveType == "") return;

            var dcache = cache as IDictionary<string, object>;
            if (dcache == null) return;

            if (!dcache.ContainsKey("depth"))
            {
                cache.depth = new ExpandoObject();
            }
            (cache.depth as IDictionary<string, object>)[archiveType.ToLower()] = depth;
        }
        public List<dynamic> LoadRecords(DateTime start, DateTime end, string type)
        {
            List<dynamic> recordList = new List<dynamic>();
            dynamic message = Helper.BuildMessage("records-get");
            message.body.targets = new Guid[] { GetId() };
            message.body.start = start;
            message.body.end = end;
            message.body.type = type;
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            recordList.AddRange(file.body.records);
           
            return recordList;
        }
        public List<dynamic> LoadRecordsWithId(DateTime start, DateTime end, string type, Guid objectId)
        {
            List<dynamic> recordList = new List<dynamic>();
            dynamic message = Helper.BuildMessage("records-get");
            message.body.targets = new Guid[] { objectId };
            message.body.start = start;
            message.body.end = end;
            message.body.type = type;
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            recordList.AddRange(file.body.records);

            return recordList;
        }
        public List<dynamic> LoadRowsCache(Guid objectId)
        {
            List<dynamic> rows = new List<dynamic>();
            dynamic message = Helper.BuildMessage("rows-get-4");
            message.body.ids = new Guid[] { objectId };
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            rows.AddRange(file.body.rows);
            return rows;
        }
        public List<dynamic> LoadRecordsPowerful(DateTime start, DateTime end, string type, string s1, string cmd)
        {
            List<dynamic> recordList = new List<dynamic>();
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic message = Helper.BuildMessage("records-get-with-ids-and-s1");
            message.body.objectId = GetId();
            message.body.start = start;
            message.body.end = end;
            message.body.type = type;
            message.body.s1 = s1;
            message.body.cmd = cmd;
            dynamic file = connector.SendMessage(message);
            recordList.AddRange(file.body.records);

            return recordList;
        }
        private int GetArchiveDepth(string archiveType)
        {
            if (archiveType == "") return -1;

            if (cache != null && (cache is IDictionary<string, object>) && (cache as IDictionary<string, object>).ContainsKey("depth"))
            {
                if ((cache.depth is IDictionary<string, object>) && (cache.depth as IDictionary<string, object>).ContainsKey(archiveType.ToLower()))
                {
                    int depth;
                    if (int.TryParse((cache.depth as IDictionary<string, object>)[archiveType.ToLower()].ToString(), out depth))
                    {
                        return depth;
                    }
                }
                else if ((cache.depth is Dictionary<string, int>) && (cache.depth as Dictionary<string, int>).ContainsKey(archiveType.ToLower()))
                {
                    return (cache.depth as Dictionary<string, int>)[archiveType.ToLower()];
                }
            }

            //по умолчанию
            switch (archiveType.ToLower())
            {
                case "hour":
                    return (24 * 365 * 1);
                case "day":
                    return (365 * 10);
                default:
                    return 5000;
            }
        }
        #endregion

        private readonly object locker = new object();

        protected override int OnPrepare(Route route, int port, PollTask task)
        {
            #region virtualcom

            isCancel = false;

            if (task.What == "vcom-open")
            {
                dynamic body = new ExpandoObject();
                body.what = "vcom-ready";
                body.objectId = GetId();
                Api.ApiProxy.Instance.VCom(body);

                //  AppendCache(STATE_PROCCESSING, string.Format("виртуальный ком {0}", "VCOM"));

                bool loop = true;

                route.Subscribe(this, (b, d) =>
                {
                    dynamic bytesBody = new ExpandoObject();
                    bytesBody.what = "vcom-bytes";
                    bytesBody.objectId = GetId();

                    bytesBody.bytes = Convert.ToBase64String(b);
                    Api.ApiProxy.Instance.VCom(bytesBody);
                });

                acceptVComCallback = (close, bytes) =>
                {
                    loop = !close;
                    if (bytes != null) route.Send(this, bytes, Direction.FromInitiator);
                };

                while (loop && !isCancel)
                {
                    Thread.Sleep(100);
                }
                logger.Debug("виртуальный ком закрыт");
                acceptVComCallback = null;
                dynamic closeBody = new ExpandoObject();
                closeBody.what = "vcom-closed";
                closeBody.objectId = GetId();
                Api.ApiProxy.Instance.VCom(closeBody);

                return Codes.SUCCESS;
            }

            #endregion
            
            Func<int> poll = () =>
            {
                inProccess = true;
                if (task == null)
                {
                    logger.Warn(string.Format("пустая задача для тюба {0}", this));
                    return Codes.EMPTY_TASK;
                }

                #region нод драйвера
                DriverGhost driver;
                if (task.What.ToLower().Contains("matrixterminal"))
                {
                    driver = DriverManager.Instance.GetDriverGhost("matrixterminal");
                }
                else
                {
                    var relation = RelationManager.Instance.GetOutputs(GetId(), "device").FirstOrDefault();
                    if (relation == null)
                    {
                        task.Repeats = 0;
                        return Codes.BROKEN_DRIVER;
                    }

                    driver = DriverManager.Instance.GetDriverGhost(relation.GetEndId());
                    if (driver == null)
                    {
                        task.Repeats = 0;
                        return Codes.BROKEN_DRIVER;
                    }
                }
                #endregion

                #region подписки

                //var cache = RecordsRepository.Instance.Get(GetId());
                if (cache == null)
                {
                    //cache = RecordsRepository.Instance.Get(GetId());
                    cache = cacheGet(GetId());
                    if (cache == null)
                    {
                        cache = new ExpandoObject();
                        cache.lastDate = new ExpandoObject();
                        cache.records = new ExpandoObject();
                    }
                }

                var buffer = new List<byte>();
                route.Subscribe(this, (bytes, forward) =>
                {
                    lock (bufferLocker)
                    {
                        buffer.AddRange(bytes);
                    }
                });

                var logEnable = IsLogEnabled();
                
                driver.recordLoad = LoadRecords;

                driver.recordLoadWithId = LoadRecordsWithId; 

                driver.loadRowsCache = LoadRowsCache;

                driver.loadRecordsPowerful = LoadRecordsPowerful;

                driver.SetContractHour = SetContractHour;

                driver.GetContractHour = GetContractHour;

                driver.SetContractDay = SetContractDay;

                driver.GetContractDay = GetContractDay;

                driver.SetArchiveDepth = SetArchiveDepth;

                driver.GetArchiveDepth = GetArchiveDepth;

                driver.SetTimeDifference = SetTimeDifference;

                driver.GetLastTime = GetLastTime;

                driver.SetIndicationForRowCache = (records, indicatioUnitMeasurement, date) =>
                {
                    Guid objectId = new Guid();
                    if (task != null && task.Arg is IDictionary<string, object>)
                    {
                        var garg = task.Arg as IDictionary<string, object>;
                        if (garg.ContainsKey("id"))
                        {
                            objectId = Guid.Parse(garg["id"].ToString());
                        }
                    }
                    SetIndicationForRowCache(records, indicatioUnitMeasurement, date, objectId);
                };
                driver.SetModbusControl = (control) =>
                {

                    Guid objectId = new Guid();
                    if (task != null && task.Arg is IDictionary<string, object>)
                    {
                        var garg = task.Arg as IDictionary<string, object>;
                        if (garg.ContainsKey("id"))
                        {
                            objectId = Guid.Parse(garg["id"].ToString());
                        }
                    }
                    SetModbusControl(control, objectId);
                }; 

                driver.Log = (msg) =>
                {
                    if (IsLogEnabled()) //(logEnable) // разрешение логов по объекту
                    {
                        Log(msg, 2);
                    }
                };

                driver.Logger = (msg, level) =>
                {
                    if (IsLogEnabled()) //(logEnable) // разрешение логов по объекту
                    {
                        // разрешение по уровню логов
                        int maxLevel = 1;
                        if (task != null && task.Arg is IDictionary<string, object>)
                        {
                            var darg = task.Arg as IDictionary<string, object>;
                            if (darg.ContainsKey("logLevel"))
                            {
                                int.TryParse(darg["logLevel"].ToString(), out maxLevel);
                            }
                        }

                        //0 - system, 1 - info, 2 - debug, 3 - trace => maxLevel
                        if (level <= maxLevel)
                        {
                            Log(msg, level);
                        }
                    }
                };

                driver.Request = bytes =>
                {
                    route.Send(this, bytes, Direction.FromInitiator);
                };

                driver.Response = () =>
                {
                    lock (bufferLocker)
                    {
                        var bts = buffer.ToArray();
                        buffer.Clear();
                        return bts;
                    }
                };

                driver.Records = records =>
                {
                    foreach (var record in records)
                    {
                        record.id = Guid.NewGuid();
                        record.objectId = GetId();
                    }
                    RecordsAcceptor.Instance.Save(records);

                    //upd cache here
                    var dcache = cache as IDictionary<string, object>;
                    if (!dcache.ContainsKey("lastDate"))
                    {
                        cache.lastDate = new ExpandoObject();
                    }
                    var dlast = cache.lastDate as IDictionary<string, object>;

                    foreach (var type in records.GroupBy(r => r.type))
                    {
                        var date = type.Max(r => r.date);
                        if (!dlast.ContainsKey(type.Key))
                        {
                            dlast.Add(type.Key, date);
                        }
                        else
                        {
                            var old = (DateTime)dlast[type.Key];

                            //switch ((string)type.Key)
                            //{
                            //    case "Day":
                            //        date = date.AddDays(1);
                            //        break;
                            //}

                            dlast[type.Key] = date > old ? date : old;
                        }
                    }
                };

                driver.GetLastRecords = (type) =>
                {
                    var dlast = cache.records as IDictionary<string, object>;
                    if (dlast.ContainsKey(type))
                    {
                        return dlast[type] as IEnumerable<dynamic>;
                    }
                    return new dynamic[] { };
                };

                driver.GetRange = (type, start, end) =>
                {
                    var dlast = cache.records as IDictionary<string, object>;
                    if (dlast.ContainsKey(type))
                    {
                        return dlast[type] as IEnumerable<dynamic>;
                    }

                    return new dynamic[] { };
                };

                isCancel = false;
                driver.Cancel = () =>
                {
                    if (isCancel)
                    {
                        Log("вызвана остановка текущего процесса опроса");
                    }

                    return isCancel;
                };

                


                #endregion

                logger.Debug(string.Format("начался опрос вычислителя {0}", GetId()));

                Stopwatch sw = new Stopwatch();
                try
                {
                    // AppendCache(STATE_PROCCESSING, string.Format("опрос {0}", initiator.What));
                    Log(string.Format("началось выполнение задачи '{0}' (осталось попыток {1})", task.What, task.Repeats));
                    sw.Start();

                    //

                    var ans = driver.Doing(task.What, task.Arg);

                    var dans = ans as IDictionary<string, object>;
                    //если опрос провалился, попыткуем, и если попыток не осталось дропаем
                    if (!dans.ContainsKey("code"))
                    {
                        return Codes.SUCCESS;
                    }

                    var description = "";
                    if (dans.ContainsKey("description"))
                    {
                        description = ans.description;
                    }

                    return ans.code;
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "ошибка в драйвере");
                    // AppendCache(STATE_ERROR, string.Format("опрос завершен с ошибкой"));
                    return Codes.UNKNOWN;
                }
                finally
                {
                    sw.Stop();
                    Log(string.Format("завершилось выполнение задачи '{0}' длительностью {1}", task.What, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds)));
                    inProccess = false;
                    logger.Debug("завершился опрос вычислителя {0}", GetId());
                    //RecordsRepository.Instance.Set(GetId(), cache);
                    cacheSet(GetId(), cache);
                    //cacheLocal.Add(GetId(), cache);
                }
                //save cache here                
            };
            var code = poll();

            CheckAvailability(code == 0, task.Priority);

            //update cache here            
            //RecordsRepository.Instance.Set(string.Format("poll-tube{0}", GetId()), pollCache);

            return code;
        }

        public override bool HasChance(PollTask task)
        {
            return task.Repeats > 0;
        }

        public override string ToString()
        {
            return string.Format("счетчик {0}", GetId());
        }

        public string GetInfo()
        {
            return string.Format("id:{0};{1}", GetId(), inProccess ? "в процессе" : "отдых");
        }

        private dynamic cache = null;

        public override bool NeedPoll(string what, dynamic arg)
        {
            if (cache == null)
            {
                //cache = RecordsRepository.Instance.Get(GetId());
                cache = cacheGet(GetId());
                if (cache == null)
                {
                    cache = new ExpandoObject();
                    cache.lastDate = new ExpandoObject();
                    cache.records = new ExpandoObject();
                }
            }

            return true;
        }

        /// <summary>
        /// количество времени, прошедшее с последнего опроса указанного типа архива
        /// </summary>
        public int TimeWithoutPolling(PollType type)
        {
            //var cache = RecordsRepository.Instance.Get(GetId());
            dynamic cache = cacheGet(GetId());
            int defaultvalue = int.MaxValue;
            //если нет данных, возможно, объект впервые опрашивается
            if (cache == null)
                return defaultvalue;

            var dcache = cache as IDictionary<string, object>;
            if (dcache.ContainsKey("lastDate") && dcache.ContainsKey("contractHour") && dcache.ContainsKey("timeDifference"))
            {
                /// разница во времени, '-' спешит, '+' отстает
                var timeDifference = TimeSpan.Parse(cache.timeDifference.ToString());
                // log.Debug(string.Format("[{1}] timeDifference {0}", timeDifference, GetId()));

                DateTime actualtime = DateTime.Now - timeDifference;

                var dLastDate = cache.lastDate as IDictionary<string, object>;
                if (!(type == PollType.Day && dLastDate.ContainsKey("Day")) &&
                    !(type == PollType.Hour && dLastDate.ContainsKey("Hour")))
                {
                    logger.Debug("return defaultvalue;");
                    return defaultvalue;
                }

                //добавляем минуту, так как нет округления с заданной точностью
                switch (type)
                {
                    case PollType.Day:
                        {
                            //время формирования очередного архива
                            DateTime timeToPoll = (DateTime)cache.lastDate.Day + TimeSpan.FromHours((int)cache.contractHour);

                            int value = (int)((actualtime - timeToPoll) + TimeSpan.FromMinutes(1)).TotalDays;
                            // log.Debug(string.Format("[{0}]: текущее время прибора {1}; дата последнего архива {4}; время формирования архива {2}; число доступных архивов {3}", GetId(), actualtime, timeToPoll, value, ((DateTime)cache.lastDate.Day).AddDays(-1)));

                            if (value < 0)
                                logger.Warn("время косячное! время последнего опроса {0}; обьект {1}", (DateTime)cache.lastDate.Day, GetId());

                            return value;
                        }
                    case PollType.Hour:
                        {
                            //время формирования очередного архива
                            DateTime timeToPoll = (DateTime)cache.lastDate.Hour;
                            return (int)((actualtime - timeToPoll) + TimeSpan.FromMinutes(1)).TotalHours;
                        }
                }
            }
            return defaultvalue;
        }

        private void MakeDisable()
        {
            dynamic message = Helper.BuildMessage("edit-disable-tube");
            message.body.id = GetId();
            var connector = UnityManager.Instance.Resolve<IConnector>();
            dynamic file = connector.SendMessage(message);
            logger.Info("[{0}]: отправлено сообщение об отключении", this);
        }

        dynamic pollCache = null;

        public override void Dispose()
        {
            //RecordsRepository.Instance.Set(GetId(), cache);

            if (inProccess)
            {
                UpdateState(Codes.TASK_CANCEL, "");
            }
            inProccess = false;

            base.Dispose();
        }

        public override byte[] GetKeepAlive()
        {
            return base.GetKeepAlive();
        }
    }
}

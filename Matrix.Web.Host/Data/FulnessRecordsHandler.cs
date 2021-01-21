using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Dynamic;
using NLog;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// перехват суточных записей для определения полноты данных
    /// </summary>
    class FulnessRecordsHandler : IRecordHandler, IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public FulnessRecordsHandler()
        {

        }

        public void Handle(IEnumerable<DataRecord> records, Guid userId)
        {
            //новые записи
            var fulnessDay = records.Where(r => r.Type == "Day");
            var fulnessHour = records.Where(r => r.Type == "Hour");  //
            //if (!fulnessDay.Any()) return;
            if (fulnessDay.Any())
            {
                log.Trace("Начата обработка {0} записей (целевых {1}) для [userId:{2}]", records.Count(), fulnessDay.Count(), userId.ToString());

                //преобразование рекордов в даты
                var dates = new List<DataRecordDate>();
                foreach (var record in fulnessDay)
                {
                    dates.Add(new DataRecordDate { Date = record.Date, ObjectId = record.ObjectId, Type = record.Type });
                }

               
                //обработка
                dates.Distinct().GroupBy(r => r.ObjectId).ToList().ForEach(g => FulnessCache.Instance.UpdateDay(g.Key, g.ToList())); // g.Where(p => DateTime.Compare(p.Date, yesterday) >= 0)));
                
                log.Trace("Закончена обработка");
            }
            else if (fulnessHour.Any())
            {
                //преобразование рекордов в часы //
                var hours = new List<DataRecordDate>(); //
                foreach (var record in fulnessHour) //
                {
                    hours.Add(new DataRecordDate { Date = record.Date, ObjectId = record.ObjectId, Type = record.Type }); //
                    hours.Distinct().GroupBy(r => r.ObjectId).ToList().ForEach(g => FulnessCache.Instance.UpdateHour(g.Key, g.ToList()));
                }
            }
        }

        public void Dispose()
        {

        }
    }

    class FulnessCache
    {
        private FulnessCache()
        {
            try
            {
                userId = StructureGraph.Instance.GetRootUser();
                Load();

                //
                syncTimer.Interval = 20 * 1000;
                syncTimer.Elapsed += (se, ea) =>
                {
                    SaveDay();
                    SaveHour();
                };
                syncTimer.AutoReset = true;
                syncTimer.Start();
            }
            catch (Exception ex)
            {
                log.Error("ловец признака полноты не запущен: {0}", ex.Message);
            }
        }

        static FulnessCache() { }
        private static readonly FulnessCache instance = new FulnessCache();
        public static FulnessCache Instance
        {
            get
            {
                return instance;
            }
        }

        public void Start()
        {

        }

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<Guid, IEnumerable<DataRecordDate>> cacheDay = new Dictionary<Guid, IEnumerable<DataRecordDate>>();
        private readonly Dictionary<Guid, IEnumerable<DataRecordDate>> newRecordsDay = new Dictionary<Guid, IEnumerable<DataRecordDate>>();
        private readonly Dictionary<Guid, IEnumerable<DataRecordDate>> cacheHour = new Dictionary<Guid, IEnumerable<DataRecordDate>>();
        private readonly Dictionary<Guid, IEnumerable<DataRecordDate>> newRecordsHour = new Dictionary<Guid, IEnumerable<DataRecordDate>>();
        private readonly Timer syncTimer = new Timer();
        private DateTime oldDate = DateTime.MinValue;
        private Guid userId;
        private Dictionary<Guid, bool> tubeIds;
        private Guid userHourId;
        private Dictionary<Guid, bool> tubeHourIds;

        private void Load()
        {
            // период для начальной загрузки данных
            var now = DateTime.Now.AddHours(-10).Date.AddHours(-24);
            var monthStart3 = now.AddDays(1 - now.Day).AddMonths(-2);   //два месяца назад
            var monthNext = now.AddMonths(1);                           //один вперёд
            var monthStartNext = monthNext.AddDays(1 - monthNext.Day);  //начало следующего месяца

            // записи за 3 месяца (с запасом, т.к нужны tubeId)
            var daysMonth3 = Cache.Instance.GetDatesAll("Day", monthStart3, monthStartNext);

            cacheDay.Clear();

            var grouping = daysMonth3.GroupBy(r => r.ObjectId);
            tubeIds = grouping.ToDictionary(g => g.Key, g => true);
            grouping.ToList().ForEach(g => UpdateDay(g.Key, g.ToList()));


            
            var nowHour = DateTime.Now.Date.AddHours(-24);//
            var HourNext = nowHour.AddDays(2);//
            var hoursDays3 = Cache.Instance.GetDatesAll("Hour", nowHour, HourNext);//

            cacheHour.Clear(); //

            var groupingHour = hoursDays3.GroupBy(r => r.ObjectId);
            tubeHourIds = groupingHour.ToDictionary(g => g.Key, g => true);
            groupingHour.ToList().ForEach(g => UpdateHour(g.Key, g.ToList()));
            //var datesByTube = .. ToDictionary(g => g.Key, g => g.ToList());//.
            //foreach (var tubeId in tubeIds)
            //{
            //    var dates = new List<DataRecordDate>();
            //    if(datesByTube.ContainsKey(tubeId))
            //    {
            //        dates.AddRange(datesByTube[tubeId]);
            //    }
            //    Update(tubeId, dates);
            //}


            //log.DebugFormat("загружен кеш полноты по {0} объектам", newRecords.Keys.Count);
        }

        private void SaveDay()
        {
            //отчётное число на текущий момент времени
            //смена происходит в 10:00
            var thisDay = DateTime.Now.AddHours(-10).Date.AddHours(-24);
            var updateTubeIds = new List<Guid>();

            if (thisDay != oldDate)
            {
                //var tubeIds = StructureGraph.Instance.GetTubeIds(userId).ToArray();
                updateTubeIds.AddRange(tubeIds.Keys);
                oldDate = thisDay;
            }

            if (newRecordsDay.Any())
            {
                updateTubeIds.AddRange(newRecordsDay.Keys);
                updateTubeIds = updateTubeIds.Distinct().ToList();
            }

            if (updateTubeIds.Any())
            {
                var newRecordsDayCopy = new Dictionary<Guid, IEnumerable<DataRecordDate>>(newRecordsDay);
                newRecordsDay.Clear();

                int reportDayDefault = 1;

                //today=24.02 this=23.02 st=23.02 + (25-23) = 25.02-1mon = 25.01 end=25.02
                //today=25.02 this=24.02 st=24.02 + (25-24) = 25.02-1mon = 25.01 end=25.02
                //today=26.02 this=25.02 st=25.02 + 0 = 25.02-1mon = 25.01 end=25.02
                //today=27.02 this=26.02 st=26.02 - 1 = 25.02...
                //1.03, 28.02=>25.02
                //2.03, 1.03=> 1.03+24

                var monthStartDefault = thisDay.AddDays(reportDayDefault - thisDay.Day);
                if (thisDay.Day < reportDayDefault)
                {
                    monthStartDefault = monthStartDefault.AddMonths(-1);
                }
                DateTime monthStartNextDefault = monthStartDefault.AddMonths(1);

                
                //var constants = Cache.Instance.GetLastRecords("Constant", updateTubeIds.ToArray());

                //var monthStartThis = thisDay.AddDays(1 - thisDay.Day);      // начало месяца 1-е число
                //var monthNext = thisDay.AddMonths(1);
                //var monthStartNext = monthNext.AddDays(1 - monthNext.Day);
                ////var monthStartNext = monthStartThis.AddMonths(1);

                foreach (var tubeId in updateTubeIds)
                {
                    dynamic tube = StructureGraph.Instance.GetTube(tubeId, userId);
                    //List<DataRecord> tubeConstants = constants.Where(dr => dr.ObjectId == tubeId).ToList();
                    DateTime monthStartThis = monthStartDefault;
                    DateTime monthStartNext = monthStartNextDefault;

                    int total = -1;
                    int reportDay = reportDayDefault;
                    //if (tubeConstants.Where(c => c.S1 == "Отчётный день").Any())
                    //{
                    //    int reportDayParse;
                    //    string reportDayStr = tubeConstants.Where(c => c.S1 == "Отчётный день").FirstOrDefault().S2;
                    //    if(int.TryParse(reportDayStr, out reportDayParse))
                    //    {
                    //        if (reportDayParse > 28) break;
                    //        reportDay = reportDayParse;
                    //        monthStartThis = thisDay.AddDays(reportDay - thisDay.Day);
                    //        if (thisDay.Day < reportDay)
                    //        {
                    //            monthStartNext = monthStartThis;
                    //            monthStartThis = monthStartThis.AddMonths(-1);
                    //        }
                    //        else
                    //        {
                    //            monthStartNext = monthStartThis.AddMonths(1);
                    //        }
                    //        total = (int)(thisDay - monthStartThis).TotalDays + 1;
                    //    }
                    //}


                    if (!tubeIds.ContainsKey(tubeId))
                    {
                        tubeIds[tubeId] = true;
                    }

                    var tmp = new List<DataRecordDate>();
                    if (cacheDay.ContainsKey(tubeId))
                    {
                        tmp.AddRange(cacheDay[tubeId].ToList());
                    }

                    if (newRecordsDayCopy.ContainsKey(tubeId))
                    {
                        tmp.AddRange(newRecordsDayCopy[tubeId].ToList());
                    }

                    cacheDay[tubeId] = tmp.Where(r => (DateTime.Compare(monthStartThis, r.Date) <= 0) && (DateTime.Compare(r.Date, monthStartNext) <= 0));

                    var monthDays = cacheDay[tubeId].Select(c => c.Date.Day);
                    var maxDay = 0;

                    if (monthDays.Any())
                    {
                        maxDay = monthDays.Max();
                    }

                    if(total == -1)
                    {
                        total = (int)(thisDay - monthStartDefault).TotalDays + 1; //(maxDay > thisDay.Day) ? maxDay : thisDay.Day;//
                    }

                    //создание признака полноты
                    dynamic fulness = new ExpandoObject();
                    fulness.dates = cacheDay[tubeId].Select(c => c.Date).Distinct().OrderBy(r => r);
                    fulness.start = monthStartThis;
                    fulness.end = monthStartNext;
                    fulness.day = total;// (maxDay > thisDay.Day) ? maxDay : thisDay.Day;
                    fulness.daysInPeriod = DateTime.DaysInMonth(monthStartThis.Year, monthStartThis.Month);
                    fulness.reportDay = reportDay;

                    RowsCache.Instance.UpdateFulness(fulness, tubeId, userId);
                    Carantine.Instance.Push(tubeId);
                }

                log.Info("на дату {0:dd.MM.yy} обновлено признаков полноты для {1} т.у.", thisDay, updateTubeIds.Count());
            }
        }

        private void SaveHour()
        {
            //отчётное число на текущий момент времени
            var thisHour = DateTime.Now.Date;
            var updateTubeIds = new List<Guid>();

            if (thisHour != oldDate)
            {
                updateTubeIds.AddRange(tubeIds.Keys);
                oldDate = thisHour;
            }

            if (newRecordsHour.Any())
            {
                updateTubeIds.AddRange(newRecordsHour.Keys);
                updateTubeIds = updateTubeIds.Distinct().ToList();
            }

            if (updateTubeIds.Any())
            {
                var newRecordsHourCopy = new Dictionary<Guid, IEnumerable<DataRecordDate>>(newRecordsHour);
                newRecordsHour.Clear();

                int reportHourDefault = 1;

                foreach (var tubeId in updateTubeIds)
                {
                    dynamic tube = StructureGraph.Instance.GetTube(tubeId, userId);
                   
                    DateTime hourStartThis = DateTime.Now.Date.AddHours(1);
                    DateTime hourStartNext = DateTime.Now.Date.AddHours(24);

                    int total = -1;
                    int reportDay = reportHourDefault;
                    
                    if (!tubeIds.ContainsKey(tubeId))
                    {
                        tubeIds[tubeId] = true;
                    }

                    var tmp = new List<DataRecordDate>();
                    if (cacheHour.ContainsKey(tubeId))
                    {
                        tmp.AddRange(cacheHour[tubeId].ToList());
                    }

                    if (newRecordsHourCopy.ContainsKey(tubeId))
                    {
                        tmp.AddRange(newRecordsHourCopy[tubeId].ToList());
                    }

                    cacheHour[tubeId] = tmp.Where(r => (DateTime.Compare(hourStartThis, r.Date) <= 0) && (DateTime.Compare(r.Date, hourStartNext) <= 0));

                    if (total == -1)
                    {
                        total = (int)DateTime.Now.Hour; 
                    }

                    //создание признака полноты
                    dynamic fulness = new ExpandoObject();
                    fulness.dates = cacheHour[tubeId].Select(c => c.Date).Distinct().OrderBy(r => r);
                    fulness.start = hourStartThis;
                    fulness.end = hourStartNext;
                    fulness.currentHour = total;
                    fulness.hoursInPeriod = 24;
                    fulness.reportDay = reportDay;

                    RowsCache.Instance.UpdateFulnessHour(fulness, tubeId, userId);
                    Carantine.Instance.Push(tubeId);
                }

                log.Info("на дату {0:dd.MM.yy} обновлено признаков полноты часов для {1} т.у.", thisHour, updateTubeIds.Count());
            }
        }

        public void UpdateDay(Guid tubeId, IEnumerable<DataRecordDate> dates)
        {
            if (dates == null) return;
            
            var tmp = new List<DataRecordDate>();
            if (newRecordsDay.ContainsKey(tubeId))
            {
                tmp.AddRange(newRecordsDay[tubeId].ToList());
            }
            tmp.AddRange(dates);
            newRecordsDay[tubeId] = tmp;
        }

        public void UpdateHour(Guid tubeId, IEnumerable<DataRecordDate> dates)
        {
            if (dates == null) return;

            var tmp = new List<DataRecordDate>();
            if (newRecordsHour.ContainsKey(tubeId))
            {
                tmp.AddRange(newRecordsHour[tubeId].ToList());
            }
            tmp.AddRange(dates);
            newRecordsHour[tubeId] = tmp;
        }

        //public void Dispose()
        //{
        //    syncTimer.Stop();
        //    syncTimer.Dispose();
        //}
    }
}

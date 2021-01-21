using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private dynamic GetDaysIsPresent(DateTime start, DateTime end, DevType devType, float version)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.supportDays = false;

            var arc = 7;
            if (devType == DevType.TC215)
            {
                arc = 3;
            }

            for (int i = 0; i < TRY_COUNT; i++)
            {
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    return answer;
                }

                var data = Send(MakeArchiveRequest(arc, start, end, 1));
                var rsp = ParseArchiveResponse(data);
                if (!rsp.success)
                {
                    log(string.Format("ошибка при считывании суточных показаний: {0}", rsp.error));
                    continue;
                }

                var ar = ParseArchiveRecords(rsp.rows, devType, version);
                if (ar.success)
                {
                    List<dynamic> arRecs = ar.records;
                    if (arRecs.Any()) answer.supportDays = true;
                    return answer;
                }
                log(string.Format("при считывании суточных показаний: {0}", ar.error));
            }

            return answer;
        }

        private dynamic GetDays(DateTime start, DateTime end, int contractHour, DevType devType, float version, bool tsDays = false)
        {
            dynamic archive = new ExpandoObject();
            archive.records = new List<dynamic>();
            archive.emptyDays = new List<DateTime>();

            for (DateTime date = start; date < end; date = date.AddDays(1))
            {
                var dt = date.Date.AddDays(1).AddHours(contractHour);
                dynamic day = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel())
                    {
                        archive.success = false;
                        archive.error = "опрос отменен";
                        return archive;
                    }

                    day = GetArchiveRecord(dt, devType, version, tsDays, true);
                    if (day.success || (!day.success && !day.badChannel)) break;

                    //log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error));
                }

                if (!day.success)
                {
                    log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error));
                    if (day.badChannel) return day;
                    archive.emptyDays.Add(dt);
                    continue;
                }


                //foreach (var rec in day.records)
                //{
                //    rec.date = rec.date.AddHours(contractHour).Date;
                //}

                log(string.Format("суточная запись {0:dd.MM.yy HH:mm} получена", date));
                records(day.records);
                archive.records.AddRange(day.records);
            }

            archive.success = true;
            archive.error = string.Empty;
            return archive;
        }

        private dynamic GetDaysNezhinka(DateTime start, DateTime end, int contractHour, DevType devType, float version, bool tsDays = false)
        {
            dynamic archive = new ExpandoObject();
            archive.records = new List<dynamic>();
            archive.emptyDays = new List<DateTime>();

            //обновление кеша
            GetArchiveRecordCache(end.Date.AddHours(contractHour), devType, version, tsDays, true);

            for (DateTime date = start; date < end; date = date.AddDays(1))
            {
                var dt = date.Date.AddDays(1).AddHours(contractHour);
                dynamic day = null;
                
                day = GetArchiveRecordCache(dt, devType, version, tsDays, true);

                if (!day.success)
                {
                    log(string.Format("суточная запись {0:dd.MM.yy} не получена, ошибка: {1}", date, day.error));
                    archive.emptyDays.Add(dt);
                    continue;
                }
                
                log(string.Format("суточная запись  {0:dd.MM.yy} получена", date));
                records(day.records);
                archive.records.AddRange(day.records);
            }

            archive.success = true;
            archive.error = string.Empty;
            return archive;
        }

        //private IEnumerable<dynamic> CalcDay2(IEnumerable<dynamic> hours, DateTime day, int contractHour)
        //{
        //    List<dynamic> result = new List<dynamic>();
        //    foreach (var x in hours.Where(h => h.date >= day.Date.AddHours(contractHour) && h.date < day.Date.AddDays(1).AddHours(contractHour)).GroupBy(g => g.s1))
        //    {
        //        if (x.Count() < 24)
        //        {
        //            log(string.Format("записи по параметру {0} за {1:dd.MM.yyyy} неполные ({2} шт)", x.Key, day.Date, x.Count()));
        //        }

        //        if (x.Key.StartsWith(Glossary.VNR))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Sum(y => (double)y.d1), x.First().s2, day.Date));
        //            continue;
        //        }

        //        /// средние значения
        //        if (x.Key.StartsWith(Glossary.pMP) ||
        //           x.Key.StartsWith(Glossary.TMP) ||
        //           x.Key.StartsWith(Glossary.KMP) ||
        //           x.Key.StartsWith(Glossary.CMP))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Average(y => (double)y.d1), x.First().s2, day.Date));
        //            continue;
        //        }
        //        /// тотальные значения
        //        else
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day.Date));
        //            continue;
        //        }
        //    }
        //    return result;
        //}

        private IEnumerable<dynamic> CalcDay(IEnumerable<dynamic> hours, int contractHour, DateTime day)
        {
            List<dynamic> result = new List<dynamic>();

            var start = day.Date.AddHours(contractHour);
            var end = day.Date.AddDays(1).AddHours(contractHour - 1);
            hours = hours.Where(h => h.date >= start && h.date <= end);
            log(string.Format("поиск [{0:dd.MM.yy HH:mm},{1:dd.MM.yy HH:mm}] в [{0:dd.MM.yy HH:mm},{1:dd.MM.yy HH:mm}]", start, end, hours.Min(h => h.date), hours.Max(h => h.date)));
            var count = hours.Where(h => h.s1.StartsWith(Glossary.VbT)).Count();
            //  log(string.Format("подсчет суток по часовым архивам {0}", count));
            if (count == 0) return result;

            if (count < 24)
            {
                log(string.Format("недостаточное количество часовых архивов ({0} из 24) за сутки {1:dd.MM.yy}. Попытка использовать архивы локальной БД", count, day));

                var dates = hours.Select(h => (DateTime)h.date).ToArray();
                //var localHour = getRange("Hour", start, end).Where(h => !dates.Contains((DateTime)h.date));
                //log(string.Format("из локальной БД прочитано {0} часовых архивов за сутки {1:dd.MM.yy}", localHour.Where(h => h.s1.StartsWith(Glossary.VbT)).Count(), day));
                //hours = hours.Union(localHour);
            }
            // log(string.Format("подсчет суток по часовым архивам {0}, время {1}", hours.Where(h => h.s1.StartsWith(Glossary.VbT)).Count(), string.Join(";", hours.Where(h => h.s1.StartsWith(Glossary.VbT)).Select(x => ((DateTime)x.date).ToString("dd HH")))));


            foreach (var x in hours.GroupBy(g => g.s1))
            {
                if (x.Count() < 24)
                {
                    log(string.Format("записи по параметру {0} за {1:dd.MM.yyyy} неполные ({2} шт)", x.Key, day.Date, x.Count()));
                }

                if (x.Key.StartsWith(Glossary.VNR))
                {
                    result.Add(MakeDayRecord(x.Key, x.Sum(y => (double)y.d1), x.First().s2, day.Date));
                    continue;
                }

                /// средние значения
                if (x.Key.StartsWith(Glossary.pMP) ||
                   x.Key.StartsWith(Glossary.TMP) ||
                   x.Key.StartsWith(Glossary.KMP) ||
                   x.Key.StartsWith(Glossary.CMP))
                {
                    //if (x.Key.StartsWith(Glossary.pMP))
                    //{
                    //    log(string.Format("давления {0}",string.Join(";",x.Select(d=>string.Format("{0:0.000}", d.d1)))));
                    //}
                    result.Add(MakeDayRecord(x.Key, x.Average(y => (double)y.d1), x.First().s2, day.Date));
                    continue;
                }
                /// тотальные значения
                else
                {
                    log(string.Format("итоговое значение параметра {0} = {1}", x.Key, x.Max(y => (double)y.d1)));
                    result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day.Date));
                    continue;
                }
            }
            log(string.Format("рассчитана суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm}", day, start, end));
            return result;
        }
    }
}

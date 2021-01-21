using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Vrsg1
{
    public partial class Driver
    {
        //private IEnumerable<dynamic> CalcDay(IEnumerable<dynamic> hours, int contractHour, DateTime day)
        //{
        //    List<dynamic> result = new List<dynamic>();
        //    foreach (var x in hours.Where(h => h.date >= day.Date.AddHours(contractHour) && h.date < day.Date.AddDays(1).AddHours(contractHour)).GroupBy(g => g.s1))
        //    {
        //        if (x.Key.StartsWith(Glossary.T))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Average(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.P))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Average(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.Qn))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Sum(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.Qw))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Sum(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.Twork))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.Vn))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //        if (x.Key.StartsWith(Glossary.Vw))
        //        {
        //            result.Add(MakeDayRecord(x.Key, x.Max(y => (double)y.d1), x.First().s2, day));
        //            continue;
        //        }
        //    }
        //    return result;
        //}

        private IEnumerable<dynamic> CalcDay(IEnumerable<dynamic> hours, int contractHour, DateTime day)
        {
            List<dynamic> result = new List<dynamic>();
            DateTime start = day.Date.AddHours(contractHour);
            DateTime end = day.Date.AddDays(1).AddHours(contractHour - 1);
            hours = hours.Where(h => h.date >= start && h.date <= end);
            var count = hours.Where(h => h.s1.StartsWith(Glossary.Qn)).Count();

            if (count == 0)
            {
                log(string.Format("суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm} НЕ расчитана", day, start, end));
                return result;
            }
            if (count < 24)
            {
                log(string.Format("недостаточное количество часовых архивов ({0} из 24) за сутки {1:dd.MM.yy}. Попытка использовать архивы локальной БД", count, day));

                var dates = hours.Select(h => (DateTime)h.date).ToArray();
                var localHour = getRange("Hour", start, end).Where(h => !dates.Contains((DateTime)h.date));
                log(string.Format("из локальной БД прочитано {0} часовых архивов за сутки {1:dd.MM.yy}", localHour.Where(h => h.s1.StartsWith(Glossary.Qn)).Count(), day));
                hours = hours.Union(localHour);
            }
            if (count > 24)
            {
                log(string.Format("количество часовых архивов {0} из 24 за сутки {1:dd.MM.yy}", count, day));
            }

            if (count == 24)
            {
                log(string.Format("количество часовых архивов {0} из 24 за сутки {1:dd.MM.yy}", count, day));
            }

            foreach (var x in hours.GroupBy(g => g.s1))
            {
                if (x.Key.StartsWith(Glossary.Qn) ||
                    x.Key.StartsWith(Glossary.Qw))
                {
                    result.Add(MakeDayRecord(x.Key,
                        x.GroupBy(y => y.date).Select(y => y.Max(z => (double)z.d1)).Sum(y => y),
                        x.First().s2,
                        day));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.Vn) ||
                    x.Key.StartsWith(Glossary.Vw) ||
                    x.Key.StartsWith(Glossary.Twork))
                {
                    result.Add(MakeDayRecord(x.Key, x.Select(y => (double)y.d1).Max(y => y), x.First().s2, day));
                    continue;
                }

                if (x.Key.StartsWith(Glossary.T) ||
                    x.Key.StartsWith(Glossary.P))
                {
                    result.Add(MakeDayRecord(x.Key, x.GroupBy(y => y.date).Select(y => y.Max(z => (double)z.d1)).Average(y => y), x.First().s2, day));
                    continue;
                }
            }

            log(string.Format("рассчитана суточная запись {0:dd.MM.yy} по часовым архивам {1:dd.MM.yy HH:mm} — {2:dd.MM.yy HH:mm}", day, start, end));
            return result;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

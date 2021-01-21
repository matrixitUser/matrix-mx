using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Data
{
    public class TableDef
    {
        public string Type { get; set; }
        public string TypeFull { get; set; }
        public string Format { get; set; }
        public DateTime[] Dates { get; set; }

        public string GetFormat(int inx)
        {
            return (Format == null || Format == "") ? "" : $"{{{inx}:{Format}}}";
        }

        public string[] GetTablesSorted(bool desc)
        {
            IEnumerable<DateTime> dates;
            if (desc)
            {
                dates = Dates.OrderByDescending(d => d);
            }
            else
            {
                dates = Dates.OrderBy(d => d);
            }
            return dates.Select(d => TypeFull + string.Format(GetFormat(0), d)).ToArray();
        }

        public string[] GetTablesRange(DateTime start, DateTime end)
        {
            IEnumerable<DateTime> dates;
            if (Format == "MMyyyy")
            {
                dates = Dates.Where(d =>
                {
                    DateTime d1 = d.AddMonths(1);
                    bool startInRange = start >= d && start < d1;
                    bool endInRange = end >= d && end < d1;
                    bool rangeInPeriod = start <= d && end > d1;
                    return startInRange || endInRange || rangeInPeriod;
                });
            }
            else if (Format == "yyyy")
            {
                dates = Dates.Where(d =>
                {
                    DateTime d1 = d.AddYears(1);
                    bool startInRange = start >= d && start < d1;
                    bool endInRange = end >= d && end < d1;
                    bool rangeInPeriod = start <= d && end > d1;
                    return startInRange || endInRange || rangeInPeriod;
                });
            }
            else
            {
                dates = Dates;
            }
            return dates.Select(d => TypeFull + string.Format(GetFormat(0), d)).ToArray();
        }
        
        public string GetTableDate(DateTime date)
        {
            IEnumerable<DateTime> dates;
            if (Format == "MMyyyy")
            {
                dates = Dates.Where(d =>
                {
                    DateTime d1 = d.AddMonths(1);
                    return date >= d && date < d1;
                });
            }
            else if (Format == "yyyy")
            {
                dates = Dates.Where(d =>
                {
                    DateTime d1 = d.AddYears(1);
                    return date >= d && date < d1;
                });
            }
            else
            {
                dates = Dates;
            }
            return dates.Select(d => TypeFull + string.Format(GetFormat(0), d)).FirstOrDefault();
        }
    }
}

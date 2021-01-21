using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date.Date;
            record.dt1 = DateTime.Now;
            return record;
        }

        /// <summary>
        /// преобразует формат чч:mm в чч
        /// </summary>
        /// <param name="shour"></param>
        /// <returns></returns>
        private static double GetHour(double shour)
        {
            Regex pattern = new Regex(@"(?<val1>\d*)[,|.]?(?<val2>\d*)?");
            var match = pattern.Match(shour.ToString());
            int hour = 0;
            string minsec = string.Empty;
            if (match.Success)
            {
                hour = int.Parse(match.Groups["val1"].Value);
                minsec = match.Groups["val2"].Value;
            }
            int min = 0;
            int sec = 0;
            if (minsec.Length >= 2)
                min = int.Parse(minsec.Substring(0, 2));
            if (minsec.Length >= 4)
                sec = int.Parse(minsec.Substring(2, 2));

            return Math.Round(new TimeSpan(hour, min, sec).TotalHours, 4);
        }
    }
}

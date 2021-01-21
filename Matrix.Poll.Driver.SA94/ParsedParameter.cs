using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public struct ParsedParameter
    {
        public string parameter;
        public string unit;
        public DateTime date;
        public double value;
        public dynamic ToHourlyRecord()
        {
            return MakeRecord.Hour(parameter, value, unit, date);
        }
        public dynamic ToDailyRecord()
        {
            return MakeRecord.Day(parameter, value, unit, date);
        }
    }
}

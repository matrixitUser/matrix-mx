using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    class DayBorderResponse
    {
        public int Hour { get; private set; }

        public DayBorderResponse(byte[] data)
        {
            Hour = 0;
            var str = Encoding.GetEncoding(1252).GetString(data);
            var pattern = @"\((?<hour>\d+)";
            var regex = new Regex(pattern);
            var match = regex.Match(str);
            if (match.Success)
            {
                var hourStr = match.Groups["hour"].Value;
                int hour = 0;
                int.TryParse(hourStr, out hour);
                Hour = hour;
            }
        }
    }
}

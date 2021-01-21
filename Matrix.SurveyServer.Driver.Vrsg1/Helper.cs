using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Vrsg1
{
    public static class Helper
    {
        public static DateTime ParseDateTime(byte[] data, int startIndex = 0)
        {
            if (data == null || data.Length < startIndex + 5) return default(DateTime);

            return new DateTime(2000 + data[4 + startIndex],
                                       data[3 + startIndex],
                                       data[2 + startIndex],
                                       data[1 + startIndex],
                                       data[startIndex],
                                       0);
        }

        public static int ParseTimeSeconds(byte[] data, int startIndex = 0)
        {
            if (data == null || data.Length < startIndex + 3) return 0;
            var second = data[0 + startIndex];
            var minute = data[1 + startIndex];
            var hour = data[2 + startIndex];
            return second + minute * 60 + hour * 60 * 60;
        }
    }
}

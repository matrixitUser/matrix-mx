using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        dynamic ParseLastPowerRecordInfo(dynamic answer)
        {
            if (!answer.success) return answer;

            answer.Profile = (byte)(answer.Body[2] & (1 << 4));
            answer.IsByte17 = answer.Profile > 0;

            answer.Index = Helper.ToUInt16(answer.Body, 0);// | (answer.IsByte17 ? (1 << 16) : 0);

            var hour = Helper.FromBCD(answer.Body[3]);
            var minute = Helper.FromBCD(answer.Body[4]);
            var day = Helper.FromBCD(answer.Body[5]);
            var month = Helper.FromBCD(answer.Body[6]);
            var year = 2000 + Helper.FromBCD(answer.Body[7]);
            answer.Date = new DateTime(year, month, day, hour, minute, 0);

            answer.IntervalMinutes = answer.Body[8];

            return answer;
        }
    }
}

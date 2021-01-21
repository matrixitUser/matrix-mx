using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        dynamic ParseJournalResponse(dynamic answer, DateTime lastDate)
        {
            if (!answer.success) return answer;

            var ssOff = Helper.ToBCD(answer.Body[0]);
            var mmOff = Helper.ToBCD(answer.Body[1]);
            var HHOff = Helper.ToBCD(answer.Body[2]);
            var ddOff = Helper.ToBCD(answer.Body[4]);
            var MMOff = Helper.ToBCD(answer.Body[5]);
            var yyOff = Helper.ToBCD(answer.Body[6]);
            try
            {
                answer.TurnOff = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
            }
            catch
            {
                answer.TurnOff = lastDate;
            }

            var ssOn = Helper.ToBCD(answer.Body[7]);
            var mmOn = Helper.ToBCD(answer.Body[8]);
            var HHOn = Helper.ToBCD(answer.Body[9]);
            var ddOn = Helper.ToBCD(answer.Body[11]);
            var MMOn = Helper.ToBCD(answer.Body[12]);
            var yyOn = Helper.ToBCD(answer.Body[13]);
            
            try
            {
                answer.TurnOn = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
            }
            catch
            {
                answer.TurnOn = DateTime.MinValue;
            }


            return answer;
        }
    }
}

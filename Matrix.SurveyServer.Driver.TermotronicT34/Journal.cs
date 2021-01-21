using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        byte[] MakeJournalRequest(byte number)
        {
            var Data = new List<byte>();
            Data.Add(0x01);
            Data.Add(number);
            return MakeBaseRequest(0x04, Data);
        }


        dynamic ParseJournalResponse(dynamic answer, DateTime lastDate)
        {
            if (!answer.success) return answer;

            byte[] body = answer.Body;

            answer.IsEmpty = true;
            foreach (byte b in body)
            {
                if (b != 0x00)
                {
                    answer.IsEmpty = false;
                    break;
                }
            }

            var ssOn = Helper.FromBCD(body[0]);
            var mmOn = Helper.FromBCD(body[1]);
            var HHOn = Helper.FromBCD(body[2]);
            var ddOn = Helper.FromBCD(body[3]);
            var MMOn = Helper.FromBCD(body[4]);
            var yyOn = Helper.FromBCD(body[5]);
            
            try
            {
                answer.TurnOn = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
            }
            catch(Exception ex)
            {
                answer.TurnOn = DateTime.MinValue;
            }

            var ssOff = Helper.FromBCD(body[6]);
            var mmOff = Helper.FromBCD(body[7]);
            var HHOff = Helper.FromBCD(body[8]);
            var ddOff = Helper.FromBCD(body[9]);
            var MMOff = Helper.FromBCD(body[10]);
            var yyOff = Helper.FromBCD(body[11]);
            
            try
            {
                answer.TurnOff = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
            }
            catch
            {
                answer.TurnOff = lastDate;
            }

            return answer;
        }
    }
}

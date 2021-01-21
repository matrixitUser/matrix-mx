using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    class JournalResponse : Response
    {
        public DateTime TurnOn { get; private set; }
        public DateTime TurnOff { get; private set; }

        public JournalResponse(byte[] data, DateTime lastDate, byte networkAddress)
            : base(data, networkAddress)
        {
            var ssOn = Helper.ToBCD(Body[0]);
            var mmOn = Helper.ToBCD(Body[1]);
            var HHOn = Helper.ToBCD(Body[2]);
            var ddOn = Helper.ToBCD(Body[3]);
            var MMOn = Helper.ToBCD(Body[4]);
            var yyOn = Helper.ToBCD(Body[5]);
            TurnOn = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);

            var ssOff = Helper.ToBCD(Body[6]);
            var mmOff = Helper.ToBCD(Body[7]);
            var HHOff = Helper.ToBCD(Body[8]);
            var ddOff = Helper.ToBCD(Body[9]);
            var MMOff = Helper.ToBCD(Body[10]);
            var yyOff = Helper.ToBCD(Body[11]);


            try
            {
                TurnOff = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
            }
            catch
            {
                TurnOff = lastDate;
            }
        }
    }
}

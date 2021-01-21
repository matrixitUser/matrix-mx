using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetPassport(byte na)
        {
            return ParsePassport(Send(MakeRequest(na, 0x01, new byte[] { })));
        }

        private dynamic ParsePassport(byte[] bytes)
        {
            dynamic passport = ParseResponse(bytes);
            if (!passport.success) return passport;

            passport.tubeCount = passport.body[0];

            passport.tube1Name = Encoding.ASCII.GetString(passport.body, 1, 16);
            passport.tube2Name = Encoding.ASCII.GetString(passport.body, 18, 16);

            passport.date = new DateTime(2000 + passport.body[54], passport.body[52], passport.body[53], passport.body[55], passport.body[56], passport.body[57]);
            passport.contractHour = bytes[58];

            return passport;
        }
    }
}

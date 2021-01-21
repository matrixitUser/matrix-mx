using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetPassport(byte na, short password)
        {
            var bytes = SendWithCrc(MakePassportRequest(na, password));
            if (bytes.Any())
                return ParsePassportResponse(bytes);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            return answer;
        }

        private byte[] MakePassportRequest(byte na, short password)
        {
            var bytes = new byte[] { (byte)(password >> 8), (byte)(password & 0x00FF) };
            return Make70Request(na, 4, bytes);
        }

        private dynamic ParsePassportResponse(byte[] bytes)
        {
            var passport = Parse70Response(bytes);
            if (!passport.success) return passport;

            passport.factoryNumber = BitConverter.ToInt16(passport.body, 1);
            var str = Encoding.ASCII.GetString(passport.body, 3, 3);
            passport.version = int.Parse(str);
            return passport;
        }
    }
}

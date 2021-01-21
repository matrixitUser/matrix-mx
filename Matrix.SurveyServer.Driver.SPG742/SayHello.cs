using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
        private dynamic SayHello(byte na, string password = "1")
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";

            /// Процедура инициализации сеанса начинается с передачи в магистраль 
            /// стартовой последовательности из 16 байтов 0xFF

            var initbytes = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                initbytes[i] = 0xff;
            }

            int count = TRY_COUNT;
            do
            {
                request(initbytes);
                Thread.Sleep(100);

                var bytes = Send(MakeHelloRequest(na));
                if (bytes.Any())
                    answer = ParseHelloResponse(bytes);
            }
            while (!answer.success && count-- > 0);

            return answer;
        }

        private byte[] MakeHelloRequest(byte na)
        {
            var msgbody = new byte[] { (byte)Codes.Session, 0x00, 0x00, 0x00, 0x00 };
            return MakeBaseRequest(na, msgbody);
        }

        private dynamic ParseHelloResponse(byte[] bytes)
        {
            var passport = ParseBaseResponse(bytes);
            if (!passport.success) return passport;

            var DVC_L = passport.body[0];
            var DVC_H = passport.body[1];
            var VX = passport.body[2];

            if (DVC_L != 0x47)
            {
                passport.success = false;
                passport.error = "код прибора отличается от кода СПГ74x (0x47)";
                return passport;
            }

            passport.version = 700 + DVC_H + VX / 10f;
            return passport;
        }
    }
}

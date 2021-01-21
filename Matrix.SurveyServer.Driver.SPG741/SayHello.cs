using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        private dynamic SayHello(byte na, bool isStel, string password = "1")
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";

			var result = SendShort(MakeHelloRequest(na, isStel), 0x3F);
			if(result.success)
			{
				answer = ParseHelloResponse(result);	
			}
            return answer;
        }

        private byte[] MakeHelloRequest(byte na, bool isStel)
        {
            var bytes = new List<byte>();
            if (isStel)
            {
                for (var i = 0; i < 4; i++)
                {
                    bytes.Add(0x01);
                }
            }

            for (int i = 0; i < 32; i++)
            {
                bytes.Add(0xff);
            }
            bytes.AddRange(MakeShortRequest(na, 0x3F, 0x00, 0x00, 0x00, 0x00));
            return bytes.ToArray();
        }

        private dynamic ParseHelloResponse(dynamic passport)
        {
            if (!passport.success) return passport;

            //log(string.Format("байты для парсинга {0}", string.Join(",", (passport.body as byte[]).Select(b => b.ToString("X2")))));
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

        private bool StelCheck()
        {
            var data = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0xEA };
            request(data);
            log(string.Format("проверка СТЕЛ, ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            byte[] buffer = new byte[] { };
            var timeout = 5000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }

            if (!buffer.Any())
            {
                log("ответ не пришел");
                return false;
            }
            log(string.Format("ответ {0}", string.Join(",", Encoding.ASCII.GetString(buffer, 8, buffer.Length - 8))));
            return true;
        }
    }
}

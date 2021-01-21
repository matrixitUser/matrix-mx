using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        const byte ACK = 0x06;

        /// <summary>
        /// Начинает сессию работы с прибором
        /// </summary>
        /// <param name="na"></param>
        /// <param name="ch"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private dynamic GetSession(string na, string password, bool? isConsumer, int speed)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "";

            if (speed != -1)
            {

                //SendInstant(MakeInit0Request(), true);
                {
                    var initData = new byte[] { 0x00 };

                    int timeout = 2100;
                    while (timeout > 0)
                    {
                        timeout -= 20;
                        SendInstant(initData, true, 0);
                        Thread.Sleep(20);
                    }
                }

                Thread.Sleep(1600);

            }

            dynamic init1 = null;
            //init1 = ParseInit1Response(Send(MakeInit1Request(na)));
            //if (!init1.success) return init1;
            int repeats = 3;

            for (int i = 0; i < repeats; i++)
            {
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "прервано";
                    return answer;
                }

                init1 = ParseInit1Response(Send(MakeInit1Request(na)));
                if (init1.success) break;
            }
            if (!init1.success) return init1;

            answer.devType = init1.devType;
            var speedcode = init1.SpeedCode;

            if (speed > 0)
            {
                speedcode = speed;
            }

            var pseudoPass = ParsePseudoPassResponse(Send(MakeInit2Request(speedcode, "1")));
            if (!pseudoPass.success) return pseudoPass;

            log("открытие замка");
            SetConsumerCastle(init1.devType, password, isConsumer);

            log("инициализация успешна");

            answer.success = true;
            answer.error = string.Empty;
            answer.init1 = init1;
            // answer.r2 = pseudoPass.PseudoPass;
            return answer;
        }

        private dynamic ParsePseudoPassResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                return answer;
            }

            answer.success = true;
            answer.error = string.Empty;

            answer.PseudoPass = Encoding.ASCII.GetString(bytes);

            return answer;
        }

        private byte[] MakeInit2Request(int speedcode, string regim)
        {
            var bytes = new List<byte>();
            bytes.Add(ACK);
            bytes.AddRange(Encoding.ASCII.GetBytes(string.Format("0{0}{1}\r\n", speedcode, regim)));
            return bytes.ToArray();
        }

        private dynamic ParseInit1Response(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (bytes == null || !bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                return answer;
            }

            answer.success = true;
            answer.error = string.Empty;

            var str = Encoding.GetEncoding(1252).GetString(bytes);

            if (str.Contains("+++") || str.Contains("NO CARRIER"))
            {
                answer.success = false;
                answer.error = string.Format("удаленный модем бросил трубку");
                return answer;
            }

            if (str.Length <= 7)
            {
                log(string.Format("полученный ответ '{0}' слишком короткий для расшифровки", str));
                answer.success = false;
                answer.error = string.Format("полученный ответ '{0}' слишком короткий для расшифровки", str);
                return answer;
            }

            var regex = new Regex(@"/[A-Za-z]+\d[A-Za-z0-9]+");
            var match = regex.Match(str);
            str = match.Value;
            //str = "/Els6EK270";
            var devType = DevType.EK260;
            if (str.Contains("EK270")) devType = DevType.EK270;
            if (str.Contains("TC210")) devType = DevType.TC210;
            if (str.Contains("TC215")) devType = DevType.TC215;
            if (str.Contains("TC220")) devType = DevType.TC220;

            answer.Raw = str;
            answer.devType = devType;
            answer.XXX = str.Substring(1, 3);
            int z = 0;
            int.TryParse(str.Substring(4, 1), out z);
            answer.SpeedCode = z;
            answer.Ident = new string(str.Substring(5).TakeWhile(c => c != '\r' || c != '\n').ToArray());
            return answer;
        }

        private byte[] MakeInit0Request()
        {
            var bytes = new List<byte>();
            int count = 75;
            for (int i = 0; i < count; i++)
            {
                bytes.Add(0x00);
            }
            return bytes.ToArray();
        }

        private byte[] MakeInit1Request(string address = "")
        {
            return Encoding.ASCII.GetBytes(string.Format("/?{0}!\r\n", address));
        }

        private byte[] MakeSessionByeRequest()
        {
            var bytes = new byte[]
            {
                SOH,
                0x42,
                0x30, //B0
                ETX,
                0x00
            };
            //var crc = Crc.Calc(bytes, 1, 3, new BccCalculator()).CrcData;
            var crc = CalcCrc(bytes, 1, 3);
            bytes[4] = crc;
            return bytes;
        }
    }
}

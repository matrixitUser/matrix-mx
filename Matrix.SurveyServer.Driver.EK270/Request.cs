using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private enum RequestType
        {
            Read,
            Write
        }

        private const byte SOH = 0x01;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private byte[] MakeRequest(RequestType requestType, string parameter, string value)
        {
            var e = Encoding.ASCII;

            var bytes = new List<byte>();
            bytes.Add(SOH);
            var rt = "R1";
            if (requestType == RequestType.Write) rt = "W1";
            bytes.AddRange(e.GetBytes(rt));

            bytes.Add(STX);
            bytes.AddRange(e.GetBytes(string.Format("{0}({1})", parameter, value)));
            bytes.Add(ETX);
            //bytes.AddRange(Crc.Calc(bytes.ToArray(), 1, bytes.Count - 1, new BccCalculator()).CrcData);
            bytes.Add(CalcCrc(bytes.ToArray(), 1, bytes.Count - 1));

            //bytes.Add(na);
            //bytes.Add(func);
            //bytes.AddRange(body);
            //var crc = CalcCrc16(bytes.ToArray());
            //bytes.AddRange(crc);
            return bytes.ToArray();
        }

        private dynamic ParseResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                return answer;
            }

            if (bytes.Count() == 1 && bytes[0] == ACK)
            {
                answer.success = true;
                answer.error = string.Empty;
                answer.Raw = "";
                answer.Parameter = "";
                answer.Values = new List<string>();
                return answer;
            }

            if (bytes.Length == 1 && bytes[0] != ACK)
            {
                answer.success = false;
                answer.error = "пришел отрицательный ответ";
                return answer;
            }


            if (bytes.Length == 3 && Encoding.ASCII.GetString(bytes) == "+++")
            {
                answer.success = false;
                answer.error = "удаленный модем бросил трубку";
                return answer;
            }

            //if (false)// || !CheckCrc(bytes))
            //{
            //    log(string.Format("неправильный пакет [{0}]", string.Join(",", bytes.Select(b => b.ToString("X2")))));
            //    answer.success = false;
            //    answer.error = "не сошлась контрольная сумма";
            //    answer.body = bytes;
            //    return answer;
            //}

            var rawString = Encoding.ASCII.GetString(bytes);

            answer.Raw = rawString;

            if (rawString.Contains("(#"))
            {
                answer.errorCode = ParseErrorCode(rawString);
                answer.error = GetErrorText(answer.errorCode);
                answer.success = false;
                return answer;
            }

            rawString = rawString.Replace("\r\n", "").Replace(")(", "\n").Replace("(", "\n").Replace(")", "\n");
            var elements = rawString.Split('\n');
            answer.Parameter = elements.FirstOrDefault();
            answer.Values = elements.Skip(1).Take(elements.Count() - 2).ToList();

            answer.success = true;
            answer.error = string.Empty;
            return answer;
        }

        private int ParseErrorCode(string raw)
        {
            string pattern = @"(#(?<int>\d+))";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(raw);
            if (!match.Success)
                return -1;

            int errcode;

            if (int.TryParse(match.Groups["int"].Value, out errcode))
                return errcode;
            else
                return -1;
        }

        private dynamic OpenConsumerCastle(bool isConsumerPassword = true, string password = "0")
        {
            var castle = ParseResponse(Send(MakeRequest(RequestType.Write, isConsumerPassword? "4:171.0" : "3:171.0", password)));
            if(castle.success == false)
            {
                log(string.Format("ответ на замок: {0}", castle.error));
            }
            return castle;
            //log(string.Format("ответ на замок: {0}", castle));
        }
    }
}

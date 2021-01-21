using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private byte[] MakeSingleValueRequest(string address)
        {
            return MakeRequest(RequestType.Read, address, "1");
        }

        private dynamic ParseSingleValueResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            answer.success = true;
            answer.error = string.Empty;
            answer.isExist = false;

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                return answer;
            }

            var errcode = ParseCheckError(bytes);
            if (errcode == 4)
            {
                //answer.success = false;
                //answer.error = "код 4";
                return answer;
            }
            else if (errcode < 0)
            {
                answer.success = false;
                answer.error = "неожиданный ответ";
                return answer;
            }
            else if (errcode > 0)
            {
                answer.success = false;
                answer.error = GetErrorText(errcode);
                return answer;
            }

            answer.isExist = true;

            Regex regex = new Regex(@"\((?<foo>[-]?\d+(\.\d+)?)\*");
            var str = Encoding.ASCII.GetString(bytes);
            var match = regex.Match(str);
            answer.Value = 0.0;
            if (match.Success)
            {
                var strValue = match.Groups["foo"].Value.Replace('.', ',');
                double val = 0;
                double.TryParse(strValue, out val);
                answer.Value = val;
            }

            return answer;
        }
    }
}

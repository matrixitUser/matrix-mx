using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private dynamic SetConsumerCastle(DevType type, string password, bool? isConsumer)
        {
            dynamic answer = new ExpandoObject();

            answer.password = (string.IsNullOrEmpty(password)) ? GetDefaultPassword(type) : password;
            if (isConsumer == null)
			{
				var passResponse = Send(MakeRequest(RequestType.Write, "4:171.0", answer.password));
				var passAns = ParseResponse(passResponse);
				if (!passAns.success) return passAns;

				//TODO: если на passAns отвечает ACK(06h) - неправильный пароль

				if (passAns != null && !string.IsNullOrEmpty(passAns.Raw) && passAns.Raw.Contains("17"))
				{
					SendInstant(MakeRequest(RequestType.Write, "3:171.0", answer.password));
				}
			}
            else if(isConsumer == true)
            {
                var passAns = ParseResponse(Send(MakeRequest(RequestType.Write, "4:171.0", answer.password)));
                if (!passAns.success) return passAns;
            }
            else
            {
                var passAns = ParseResponse(Send(MakeRequest(RequestType.Write, "3:171.0", answer.password)));
                if (!passAns.success) return passAns;
            }

            answer.error = string.Empty;
            answer.success = true;

            return answer;
        }

        private string GetDefaultPassword(DevType type)
        {
            var pass = "0";
            switch (type)
            {
                case DevType.EK260:
                case DevType.EK270:
                    pass = "00000000";
                    break;

                case DevType.TC210:
                case DevType.TC215:
                case DevType.TC220:
                    pass = "0";
                    break;
            }
            log(string.Format("{0}: принят пароль по умолчанию '{1}'", type, pass));
            return pass;
        }
    }
}

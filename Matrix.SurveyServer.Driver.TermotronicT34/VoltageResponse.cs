using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        dynamic ParseVoltageResponse(dynamic answer, DateTime date, int volt)
        {
            if (!answer.success) return answer;
            
            var records = new List<dynamic>();

            var offset = 0;

            if (volt == 0 || volt == 1)
            {
                var value1 = Helper.MercuryStrange(answer.Body, offset) / 100.0;
                records.Add(MakeCurrentRecord("Напряжение (фаза 1)", value1, "В", date));
                offset += 3;
            }

            if (volt == 0 || volt == 2)
            {
                var value2 = Helper.MercuryStrange(answer.Body, offset) / 100.0;
                records.Add(MakeCurrentRecord("Напряжение (фаза 2)", value2, "В", date));
                offset += 3;
            }

            if (volt == 0 || volt == 3)
            {
                var value3 = Helper.MercuryStrange(answer.Body, offset) / 100.0;
                records.Add(MakeCurrentRecord("Напряжение (фаза 3)", value3, "В", date));
                offset += 3;
            }

            dynamic result = new ExpandoObject();
            result.success = true;
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;
            result.records = records;
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        dynamic ParseCurrentResponse(dynamic answer, DateTime date, int curt)
        {
            if (!answer.success) { return answer; }

            dynamic result = new ExpandoObject();
            result.success = true;
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            var data = answer.Body;

            var offset = 0;

            if (curt == 0 || curt == 1)
            {
                var value1 = Helper.MercuryStrange(data, offset) / 1000.0;
                records.Add(MakeCurrentRecord("Ток (фаза 1)", value1, "А", date));
                offset += 3;
            }

            if (curt == 0 || curt == 2)
            {
                var value2 = Helper.MercuryStrange(data, offset) / 1000.0;
                records.Add(MakeCurrentRecord("Ток (фаза 2)", value2, "А", date));
                offset += 3;
            }

            if (curt == 0 || curt == 3)
            {
                var value3 = Helper.MercuryStrange(data, offset) / 1000.0;
                records.Add(MakeCurrentRecord("Ток (фаза 3)", value3, "А", date));
                offset += 3;
            }

            result.records = records;
            return result;
        }
    }
}

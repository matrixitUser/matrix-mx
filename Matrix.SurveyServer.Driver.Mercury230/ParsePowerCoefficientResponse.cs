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
        dynamic ParsePowerCoefficientResponse(dynamic answer, int number, DateTime date)
        {
            if (!answer.success) return answer;

            var records = new List<dynamic>();

            var value = Helper.MercuryStrange(answer.Body, 0, true) / 1000.0;
            records.Add(MakeCurrentRecord(string.Format("cos φ ({0})", number == 0 ? "по сумме фаз" : "фаза " + number), value, "", date));

            dynamic result = new ExpandoObject();
            result.success = true;
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;
            result.records = records;
            return result;
        }
    }
}

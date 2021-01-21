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
        dynamic ParseEnergyResponse(dynamic answer, DateTime date)
        {
            if (!answer.success) return answer;

            var records = new List<dynamic>();

            var div = 1000.0;

            var value1 = Helper.ToInt32(answer.Body, 0) / div;
            if (value1 < 0) value1 = 0;
            records.Add(MakeCurrentRecord("Энергия A+", value1, "кВт*ч", date));

            var value2 = Helper.ToInt32(answer.Body, 4) / div;
            if (value2 < 0) value2 = 0;
            records.Add(MakeCurrentRecord("Энергия A-", value2, "кВт*ч", date));

            var value3 = Helper.ToInt32(answer.Body, 8) / div;
            if (value3 < 0) value3 = 0;
            records.Add(MakeCurrentRecord("Энергия R+", value3, "кВт*ч", date));

            var value4 = Helper.ToInt32(answer.Body, 12) / div;
            if (value4 < 0) value4 = 0;
            records.Add(MakeCurrentRecord("Энергия R-", value4, "кВт*ч", date));

            dynamic energy = new ExpandoObject();
            energy.success = true;
            energy.error = string.Empty;
            energy.errorcode = DeviceError.NO_ERROR;
            energy.records = records;
            return energy;
        }
    }
}

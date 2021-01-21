using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        dynamic GetConstants()
        {
            dynamic constants = new ExpandoObject();
            constants.success = true;
            constants.error = string.Empty;

            if (cancel())
            {
                constants.success = false;
                constants.error = "опрос отменен";
                return constants;
            }

            var info = ParseReadInfoResponse(Send(MakeReadInfoRequest()));
            if (!info.success) return info;

            var currentDateResponse = ParseReadCurrentDateResponse(Send(MakeReadCurrentDateRequest()), info.Version);
            if (!currentDateResponse.success) return currentDateResponse;

            var recs = new List<dynamic>();
            var date = currentDateResponse.Date;

            recs.Add(MakeConstRecord("Версия ПО", string.Format("{0}.{1}", (info.Version >> 4) & 0x0F, info.Version & 0x0F), date));
            recs.Add(MakeConstRecord("Отчётный день", info.TotalDay, date));
            if (info.FactoryNumber != "")
            {
                recs.Add(MakeConstRecord("Заводской номер", info.FactoryNumber, date));
            }

            constants.date = date;
            constants.records = recs;
            constants.TotalDay = info.TotalDay;
            return constants;
        }
    }
}

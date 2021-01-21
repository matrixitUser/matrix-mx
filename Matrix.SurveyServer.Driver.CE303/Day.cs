using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.CE303
{
    public partial class Driver
    {
        dynamic GetDays(DateTime start, DateTime end, DateTime dtcounter)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            archive.records = new List<dynamic>();

            var date = start.Date.AddDays(-1);

            log(string.Format("до while (date < end)"));
            while (date < end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    archive.errorcode = DeviceError.NO_ERROR;
                    break;
                }
                log(string.Format("до if (date >= dtcounter.Date)"));
                if (date >= dtcounter.Date)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", date));
                    break;
                }

                var recs = new List<dynamic>();

                //

                var endpe = ParseEndxxResponse(Send(MakeDataRequest(string.Format("ENDPE({0:dd.MM.yy})", date))), "ENDPE", date);
                if ((!endpe.success) && (endpe.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return endpe;
                if (endpe.success) recs.AddRange(endpe.records);
                /*
                var endpi = ParseEndxxResponse(Send(MakeDataRequest(string.Format("ENDPI({0:dd.MM.yy})", date))), "ENDPI", date);
                if ((!endpi.success) && (endpi.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return endpi;
                if (endpi.success) recs.AddRange(endpi.records);

                var endqe = ParseEndxxResponse(Send(MakeDataRequest(string.Format("ENDQE({0:dd.MM.yy})", date))), "ENDQE", date);
                if ((!endqe.success) && (endqe.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return endqe;
                if (endqe.success) recs.AddRange(endqe.records);

                var endqi = ParseEndxxResponse(Send(MakeDataRequest(string.Format("ENDQI({0:dd.MM.yy})", date))), "ENDQI", date);
                if ((!endqi.success) && (endqi.errorcode != DeviceError.UNSUPPORTED_PARAMETER)) return endqi;
                if (endqi.success) recs.AddRange(endqi.records);
                */
                //            //запроса  ENDxx(DD.MM.YY.НомерНачалаДиапазона.ЧислоДиапазонов) 
                //            var bytes = DataByNameParameter(nameParameter + "(" + date.ToString("dd.MM.yy")  + ")");
                //            var power = new ResponseENDxx(nameParameter, bytes, date);
                //            //OnSendMessage(power.Data[0].ToString());
                //            return power.Data;
                int count = recs.Count;
                //log(string.Format("cуточные данные на конец {0:dd.MM.yyyy} P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}", date, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));
                log(string.Format("cуточные данные на конец {0:dd.MM.yyyy} прочитаны", date));



                //

                date = date.AddDays(1);
                records(recs);

                archive.records.AddRange(recs);
            }

            return archive;
        }

    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
        dynamic GetMonths(DateTime start, DateTime end, DateTime current, dynamic properties, int totalDay)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var recs = new List<dynamic>();


            //monthly
            Send(MakeWriteValueTypeRequest(ValueType.Month));
            var elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            Send(MakeWriteElementsRequest(elements.ActiveElements));


            for (var date = start.Date.AddDays(start.Day - 1); date < end; date = date.AddMonths(1))
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= current.Date)
                {
                    log(string.Format("данные за {0:MM.yyyy} еще не собраны", date));
                    break;
                }

                Send(MakeWriteDateRequest(date.AddHours(23)));

                var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), date.Date, properties.Fracs, properties.Units, elements.ActiveElements, ValueType.Month);
                if (!data.success)
                {
                    return data;
                }

                log(string.Format("прочитаны показания за {0:MM.yyyy}", date));
                recs.AddRange(data.Data);

                //int count = recs.Count;
                //log(string.Format("cуточные данные за {0:dd.MM.yyyy} P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}", date, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));
            }

            records(recs);

            archive.records = recs;
            return archive;
        }
    }
}

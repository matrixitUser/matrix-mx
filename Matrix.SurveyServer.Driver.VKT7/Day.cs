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
        dynamic GetDays(DateTime start, DateTime end, DateTime current, dynamic properties, int totalDay)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            var recs = new List<dynamic>();

            var totalDates = new List<DateTime>();


            //daily
            Send(MakeWriteValueTypeRequest(ValueType.Day));
            var elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            Send(MakeWriteElementsRequest(elements.ActiveElements));


            for (var date = start.Date; date < end; date = date.AddDays(1))
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= current.Date)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", date));
                    break;
                }

                Send(MakeWriteDateRequest(date.AddHours(23)));

                var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), date.Date, properties.Fracs, properties.Units, elements.ActiveElements, ValueType.Day);
                log(string.Format("прочитаны показания за {0:dd.MM.yyyy}", date));
                recs.AddRange(data.Data);

                if (date.Day == totalDay)
                {
                    totalDates.Add(date);
                }

                //int count = recs.Count;
                //log(string.Format("cуточные данные за {0:dd.MM.yyyy} P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}", date, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));
            }


            //чтение тотальных суточных данных
            if (totalDates.Any())
            {
                Send(MakeWriteValueTypeRequest(ValueType.Total));
                elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
                Send(MakeWriteElementsRequest(elements.ActiveElements));

                foreach (var date in totalDates)
                {
                    Send(MakeWriteDateRequest(date));
                    var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), date.Date, properties.Fracs, properties.Units, elements.ActiveElements, ValueType.Day);
                    if (data != null && data.Data != null)
                    {
                        log(string.Format("прочитаны показания расчетного дня {0:dd.MM.yyyy}", date));
                        recs.AddRange(data.Data);
                    }
                }
            }
            
            records(recs);

            archive.records = recs;
            return archive;
        }
    }
}

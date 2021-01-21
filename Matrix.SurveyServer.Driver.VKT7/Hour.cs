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
        dynamic GetHours(DateTime start, DateTime end, DateTime current, dynamic properties)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            var hours = new List<dynamic>();

            var currentH = current.Date.AddHours(current.Hour);
            
            Send(MakeWriteValueTypeRequest(ValueType.Hour));

            var elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            if (!elements.success) return elements;

            var filterElements = FilterElements((List<dynamic>)elements.ActiveElements, ValueType.Hour);

            var write = ParseWriteResponse(Send(MakeWriteElementsRequest(filterElements)));
            if (!write.success) return write;
            
            //сбор получасовок
            for (var date = start.Date.AddHours(start.Hour); date <= end; date = date.AddHours(1))
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= currentH)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    break;
                }

                write = ParseWriteResponse(Send(MakeWriteDateRequest(date)));
                if (!write.success)
                {
                    if (write.code == 0)
                    {
                        return write;
                    }
                    log(string.Format("Ошибка при чтении записи за {0:dd.MM.yyyy HH:mm}: {1}", date, write.error));
                    continue;
                }

                var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), date, properties.Fracs, properties.Units, elements.ActiveElements, ValueType.Hour);
                log(string.Format("прочитаны показания за {0:dd.MM.yyyy HH:mm}", date));
                hours.AddRange(data.Data);
            }


            records(hours);

            archive.records = hours;
            return archive;
        }
    }
}

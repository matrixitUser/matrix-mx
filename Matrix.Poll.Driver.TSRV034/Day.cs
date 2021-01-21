using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV034
{
    public partial class Driver
    {
        dynamic GetDays(DateTime start, DateTime end, dynamic consumptionProperties, bool isIvk)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            var days = new List<dynamic>();

            if (cancel())
            {
                archive.success = false;
                archive.error = "опрос отменен";
                return archive;
            }


            DateTime date = start.Date;//.AddMinutes(-30);

            //сбор
            while (date <= end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                //if (date >= lastDate)
                //{
                //    log(string.Format("суточные данные за {0:dd.MM.yyyy} еще не собраны", date));
                //    break;
                //}

                //

                var rsp = ParseResponse65(Send(MakeRequest65ByDate(isIvk ? ArchiveType.DailyIvk : ArchiveType.DailySystem1, date)), consumptionProperties, "Day", isIvk);
                if (!rsp.success)
                {
                    var drsp = rsp as IDictionary<string, object>;
                    if (drsp.ContainsKey("exceptionCode") && rsp.exceptionCode == ModbusExceptionCode.ILLEGAL_DATA_VALUE)
                    {
                        log(string.Format("суточные данные за {0:dd.MM.yyyy}, возможно, еще не собраны", date));
                        break;
                    }
                    return rsp;
                }

                if (DateTime.Compare(rsp.date.Date, date.Date) != 0)
                {
                    log(string.Format("суточные данные за {0:dd.MM.yyyy} не получены", date));
                }
                else
                {
                    log(string.Format("суточные данные за {0:dd.MM.yyyy}: {1}", date, rsp.text));

                    var day = new List<dynamic>();
                    foreach (var d in rsp.records)
                    {
                        //убираем лишние 23:59:59
                        //ТСРВ: запись за вчера 23:59:59 => запись за вчера 00:00:00
                        //ИВК: запись за вчера 22:59:59

                        d.date = d.date.Date;//AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                        //log.DebugFormat("данные: {0}", d);
                        day.Add(d);
                    }

                    records(day);
                    days.AddRange(day);
                    //}
                    //else
                    //{
                    //    log(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} будет пропущена", date));
                    //}
                }
                //
                date = date.AddDays(1);
            }

            archive.records = days;
            return archive;
        }
    }
}

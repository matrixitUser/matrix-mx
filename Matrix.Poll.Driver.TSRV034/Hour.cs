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
        private dynamic GetHours(DateTime start, DateTime end, dynamic consumptionProperties, bool isIvk)
        {
            dynamic archive = new ExpandoObject();
            archive.success = false;
            archive.error = string.Empty;
            var hours = new List<dynamic>();

            //

            //каналы для часового архива
            Dictionary<int, ArchiveType> channels = new Dictionary<int, ArchiveType>
                {
                    {1,ArchiveType.HourlySystem1},
                    {2,ArchiveType.HourlySystem2},
                    {3,ArchiveType.HourlySystem3},
                };


            DateTime date = start.Date.AddHours(start.Hour);//.AddMinutes(-30);
            //var lastDateH = current.Date.AddHours(current.Hour);

            //сбор
            while (date <= end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                //if (date >= lastDateH)
                //{
                //    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                //    break;
                //}

                var rsp = ParseResponse65(Send(MakeRequest65ByDate(isIvk ? ArchiveType.HourlyIvk : ArchiveType.HourlySystem1, date)), consumptionProperties, "Hour", isIvk);
                if (!rsp.success)
                {
                    var drsp = rsp as IDictionary<string, object>;
                    if (drsp.ContainsKey("exceptionCode") && rsp.exceptionCode == ModbusExceptionCode.ILLEGAL_DATA_VALUE)
                    {
                        log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm}, возможно, еще не собраны", date));
                        break;
                    }
                    return rsp;
                }

                if (DateTime.Compare(rsp.date.Date.AddHours(rsp.date.Hour), date) != 0)
                {
                    log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm} не получены", date));
                }
                else
                {

                    var hour = new List<dynamic>();
                    //if (rsp.success == true)
                    //{
                    foreach (var d in rsp.records)
                    {
                        //убираем лишние 59:59
                        d.date = d.date.AddMinutes(-59).AddSeconds(-59);
                        hour.Add(d);
                    }

                    records(hour);
                    hours.AddRange(hour);

                    log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm:ss}: {1}", date, rsp.text));
                    //}
                    //else
                    //{
                    //    log(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy HH:mm} будет пропущена", date));
                    //}
                }
                date = date.AddHours(1);
            }

            //

            archive.success = true;
            archive.records = hours;
            return archive;
        }
    }




}

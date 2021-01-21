using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV024
{
    public partial class Driver
    {
        dynamic GetDays(DateTime start, DateTime end, DateTime current)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var recs = new List<dynamic>();

            if (cancel())
            {
                archive.success = false;
                archive.error = "опрос отменен";
                return archive;
            }

            //каналы для суточного архива
            Dictionary<int, ArchiveType> channels = new Dictionary<int, ArchiveType>
				{
					{1,ArchiveType.DailySystem1},
					{2,ArchiveType.DailySystem2},
					{3,ArchiveType.DailySystem3},
				};

            DateTime date = start.Date;//.AddMinutes(-30);
            var lastDate = current.Date;

            //сбор
            while (date <= end)
            {
                var curRecs = new List<dynamic>();
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= lastDate)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    break;
                }

                //

                foreach (var channel in channels)
                {
                    log(string.Format("чтение суточных данных за {0:dd.MM.yyyy} по теплосистеме {1}", date, channel.Key));

                    var rsp = ParseResponse65(Send(MakeRequest65ByDate(channel.Value, date)), channel.Key, "Day", TM24Modification.Normal);
                    if(rsp.success == true)
                    {
                        var tmpRsp = (IDictionary<string, object>)rsp;
                        if (tmpRsp.ContainsKey("records"))
                        {
                            foreach (var d in rsp.records)
                            {
                                //убираем лишние 23:59:59
                                d.date = d.date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                                curRecs.Add(d);
                            }
                        }
                        if (tmpRsp.ContainsKey("text"))
                        {
                            //log($"{date:dd.MM.yyyy HH:mm} {rsp.text}");
                            log($"{rsp.text}");
                        }
                    }
                    else
                    {
                        log(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} по ТС {1} будет пропущена: {2}", date, channel.Key, rsp.error));
                        if (rsp.errorcode != DeviceError.DEVICE_EXCEPTION) return rsp; 
                    }
                }

                //

                log(string.Format("чтение суточных данных нарастающим итогом за {0:dd.MM.yyyy} ", date));

                var rspGrowing = ParseResponse65Growing(Send(MakeRequest65ByDate(ArchiveType.DailyGrowing, date)));
                if(rspGrowing.success == true)
                {
                    foreach (var d in rspGrowing.records)
                    {
                        //убираем лишние 23:59:59
                        d.date = d.date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                        curRecs.Add(d);
                    }
                    //log($"{date:dd.MM.yyyy} {rspTotals.text}");
                    log($"{rspGrowing.text}");
                }
                else
                {
                    log(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} нарастающим итогом: {1}", date, rspGrowing.error));
                    if (rspGrowing.errorcode != DeviceError.DEVICE_EXCEPTION) return rspGrowing;
                }

                //

                log(string.Format("чтение суммарных суточных данных за {0:dd.MM.yyyy} ", date));
                           
                var rspTotals = ParseResponse65Totals(Send(MakeRequest65ByDate(ArchiveType.DailyTotal, date)));
                if (rspTotals.success == true)
                {
                    foreach (var d in rspTotals.records)
                    {
                        //убираем лишние 23:59:59
                        d.date = d.date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                        curRecs.Add(d);
                    }
                    //log($"{date:dd.MM.yyyy} {rspTotals.text}");
                    log($"{rspTotals.text}");
                }
                else
                {
                    log(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} (суммарная): {1}", date, rspTotals.error));
                    if (rspTotals.errorcode != DeviceError.DEVICE_EXCEPTION) return rspTotals;
                }

                //

                //if(date.Day == contractDay)
                //{
                //    log(string.Format("чтение суммарных месячных данных за {0:dd.MM.yyyy} ", date));

                //    var rspTotalsMonth = ParseResponse65Totals(Send(MakeRequest65ByDate(ArchiveType.MonthlyTotal, date)));
                //    if (rspTotalsMonth.success == true)
                //    {
                //        foreach (var d in rspTotalsMonth.records)
                //        {
                //            //убираем лишние 23:59:59
                //            d.date = d.date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                //            curRecs.Add(d);
                //        }
                //        //log($"{date:dd.MM.yyyy} {rspTotals.text}");
                //        log($"{rspTotalsMonth.text}");
                //    }
                //    else
                //    {
                //        log(string.Format("ошибка при разборе ответа, месячная запись за {0:dd.MM.yy} (суммарная): {1}", date, rspTotalsMonth.error));
                //        if (rspTotalsMonth.errorcode != DeviceError.DEVICE_EXCEPTION) return rspTotalsMonth;
                //    }
                //}

                //

                records(curRecs);
                recs.AddRange(curRecs);
                //
                date = date.AddDays(1);
            }

            archive.records = recs;
            return archive;
        }
    }
}

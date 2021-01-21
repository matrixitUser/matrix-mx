using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV024
{
    public partial class Driver
    {
        private dynamic GetHours(DateTime start, DateTime end, DateTime current)
        {
            dynamic archive = new ExpandoObject();
            archive.success = false;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
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
            var lastDateH = current.Date.AddHours(current.Hour);

            //сбор
            while (date <= end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    break;
                }

                if (date >= lastDateH)
                {
                    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    break;
                }

                foreach (var channel in channels)
                {
                    //log(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm} по теплосистеме {1}", date, channel.Key));

                    var rsp = ParseResponse65(Send(MakeRequest65ByDate(channel.Value, date)), channel.Key, "Hour", TM24Modification.Normal);
                    if (!rsp.success) return rsp;
                    //Response65.Channel = channel.Key;
                    //var dataResponse = SendMessageToDevice<Response65>(new Request65ByDate(NetworkAddress, date, channel.Value));
                    
                    if (rsp.success == true)
                    {
                        var tmpRsp = (IDictionary<string, object>)rsp;
                        if (tmpRsp.ContainsKey("records"))
                        {
                            foreach (var d in rsp.records)
                            {
                                //убираем лишние 59:59
                                d.date = d.date.AddMinutes(-59).AddSeconds(-59);
                                hours.Add(d);
                            }
                        }
                        if (tmpRsp.ContainsKey("text"))
                        {
                            //log($"{date:dd.MM.yyyy HH:mm} {rsp.text}");
                            log(string.Format("{0:dd.MM.yyyy HH:mm:ss} {1}", date, rsp.text));
                        }
                    }
                }

                date = date.AddHours(1);

            }
            
            //{
            //    var sInx = 0;

            //    DateTime sDate;
            //    var last = ReadRecordByInx(sInx, out sDate);
            //    if (last == null)
            //    {
            //        archive.error = string.Format("Не удалось прочитать запись {0}", sInx);
            //        return archive;
            //    }

            //    if (sDate == DateTime.MinValue)
            //    {
            //        archive.error = "Нет записей в архиве";
            //        return archive;
            //    }

            //    //log(string.Format("{4:HH:mm:ss dd.MM.yyyy} - запись 0: {0}={1} {2} от {3}", last[0].name, last[0].value, last[0].unit, last[0].date, sDate));

            //    var date = start.Date.AddHours(start.Hour);
            //    var offset = (int)(sDate - date).TotalHours;

            //    //сбор часов
            //    for (var i = offset; i >= 0; i--)
            //    {
            //        if (cancel())
            //        {
            //            archive.success = false;
            //            archive.error = "опрос отменен";
            //            break;
            //        }

            //        if (i > 5842) continue;//capacity

            //        //DateTime date;
            //        var record = (List<dynamic>)ReadRecordByInx(i, out date);

            //        if (date == DateTime.MinValue)
            //        {
            //            log(string.Format("Записи #{0} от {1:dd.MM.yyyy HH:00:00} нет в архиве", i, sDate.AddHours(-i)));
            //            continue;
            //        }

            //        if (record == null)
            //        {
            //            archive.error = string.Format("Не удалось прочитать запись {0}", i);
            //            break;
            //        }

            //        if (date < new DateTime(2015, 10, 1))//past
            //        {
            //            log(string.Format("Запись #{0}: слишком ранняя дата", i));
            //            continue;
            //        }

            //        //if (date > sDate)//future
            //        //{
            //        //    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
            //        //    continue;
            //        //}

            //        if (date > end)//start-end
            //        {
            //            continue;
            //        }

            //        if (date < start)//start-end
            //        {
            //            continue;
            //        }

            //        foreach (var r in record)
            //        {
            //            hours.Add(MakeHourRecord(r.name, r.value, r.unit, r.date));
            //        }

            //        log(string.Format("Запись #{0} за {1} успешно прочтена", i, date));
            //    }

            //}

            //

            records(hours);

            archive.success = true;
            archive.records = hours;
            return archive;
        }
    }




}

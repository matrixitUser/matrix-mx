using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        Dictionary<UInt16, dynamic> hours = new Dictionary<UInt16, dynamic>();

        private void HourlyFlush()
        {
            hours.Clear();
        }

        private dynamic GetHourlyInx(UInt16 inx, UInt16 devid, Dictionary<int, Parameter> parameterConfiguration)
        {
            if (!hours.ContainsKey(inx))
            {
                hours[inx] = Parse65Response(Send(Make65Request(ArchiveType.Hourly, (UInt16)inx)), devid, parameterConfiguration);
            }
            return hours[inx];
        }

        //private IEnumerable<dynamic> ReadRecordByInx(int inx, out DateTime cDate, UInt16 devid)
        //{
        //    var response = GetHourlyInx((UInt16)inx, devid);
        //    if (!response.success) return null;

        //    cDate = response.Date;
        //    return response.Data;
        //}

        DateTime unixDtStart = new DateTime(1970, 1, 1, 0, 0, 0);

        private DateTime recoverDt(DateTime src, DateTime rtcResetDate)
        {
            if ((rtcResetDate != DateTime.MinValue) && (DateTime.Compare(src, unixDtStart) >= 0) && (DateTime.Compare(src, new DateTime(1980, 1, 1)) < 0))
            {
                DateTime recover = rtcResetDate.Add(src - unixDtStart);
                log(string.Format("Дата {0:dd.MM.yyyy HH:mm} восстановлена как {1:dd.MM.yyyy HH:mm}", src, recover), 3);
                return recover;
            }
            return src;
        }

        private dynamic GetHours(DateTime start, DateTime end, DateTime current, byte counters, UInt16 devid, Dictionary<int, Parameter> parameterConfiguration, DateTime rtcResetDate)
        {
            dynamic archive = new ExpandoObject();
            archive.success = false;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var hours = new List<dynamic>();

            {
                UInt16 sInx = 0;
                DateTime lastHour;

                var response = GetHourlyInx(sInx, devid, parameterConfiguration);
                if (!response.success) return response;

                // последняя запись в архиве
                lastHour = response.Date;
                var lastData = response.Data;
                if (lastData == null)
                {
                    archive.error = string.Format("Не удалось прочитать запись {0}", sInx);
                    archive.errorcode = DeviceError.NO_ERROR;
                    return archive;
                }

                if (lastHour == DateTime.MinValue)
                {
                    archive.errorcode = DeviceError.NO_ERROR;
                    archive.error = "Нет записей в архиве";
                    return archive;
                }

                lastHour = recoverDt(lastHour, rtcResetDate);

                //log(string.Format("{4:HH:mm:ss dd.MM.yyyy} - запись 0: {0}={1} {2} от {3}", last[0].name, last[0].value, last[0].unit, last[0].date, sDate));

                var startHour = start.Date.AddHours(start.Hour);
                var offset = (int)(lastHour - startHour).TotalHours;

                //сбор часов
                for (var i = offset; i >= 0; i--)
                {
                    if (cancel())
                    {
                        archive.success = false;
                        archive.errorcode = DeviceError.NO_ERROR;
                        archive.error = "опрос отменен";
                        break;
                    }

                    if (i > 5842) continue;//capacity

                    response = GetHourlyInx((UInt16)i, devid, parameterConfiguration);
                    if (!response.success) return response;

                    DateTime reqDate = lastHour.AddHours(-i);
                    DateTime rspDate = response.Date;
                    rspDate = recoverDt(rspDate, rtcResetDate);
                    var record = response.Data;

                    if (rspDate == DateTime.MinValue)
                    {
                        log(string.Format("Записи #{0} от {1:dd.MM.yyyy HH:00:00} нет в архиве", i, reqDate));
                        continue;
                    }

                    if (record == null)
                    {
                        archive.errorcode = DeviceError.NO_ERROR;
                        archive.error = string.Format("Не удалось прочитать запись {0}", i);
                        break;
                    }

                    if (rspDate < new DateTime(2015, 10, 1))//past
                    {
                        log(string.Format("Запись #{0}: слишком ранняя дата", i));
                        continue;
                    }

                    //if (date > sDate)//future
                    //{
                    //    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    //    continue;
                    //}

                    if (rspDate > end)//start-end
                    {
                        log(string.Format("Запись #{0}: прочтённая дата {1:dd.MM.yyyy HH:mm} за пределами установленного периода опроса", i, rspDate));
                        break;
                    }

                    if (rspDate < start)//start-end
                    {
                        log(string.Format("Запись #{0}: прочтённая дата {1:dd.MM.yyyy HH:mm} за пределами установленного периода опроса", i, rspDate));
                        continue;
                    }

                    //получили нужную запись date
                    for (var j = 24; j > 0; j--)
                    {
                        response = GetHourlyInx((UInt16)(i + j), devid, parameterConfiguration);
                        if (!response.success) return response;

                        DateTime dateD = response.Date;
                        dateD = recoverDt(dateD, rtcResetDate);
                        var recordD = response.Data;

                        if (dateD == DateTime.MinValue)
                        {
                            //нет в архиве
                            continue;
                        }

                        if ((dateD == rspDate.AddHours(-24)) && (recordD != null)) //получили требуемую запись!
                        {
                            //record, recordD
                            var dChannel1 = record[0].value - recordD[0].value;//dГВС24 > 0
                            var dChannel2 = record[1].value - recordD[1].value;//dХВС24 > 0

                            hours.Add(MakeHourRecord("dКанал1,2(24)", (dChannel1 == 0) ? dChannel2 : 0, "", rspDate)); //остановка на канале 1
                            hours.Add(MakeHourRecord("dКанал2,1(24)", (dChannel2 == 0) ? dChannel1 : 0, "", rspDate)); //остановка на канале 2
                            break;
                        }

                        if (dateD > rspDate.AddHours(-24))
                        {
                            //дыра в архиве
                            break;
                        }
                    }

                    var hour = new List<dynamic>();

                    foreach (var r in record)
                    {
                        hour.Add(MakeHourRecord(r.name, r.value, r.unit, rspDate));
                    }

                    records(hour);

                    hours.AddRange(hour);

                    log(string.Format("Запись #{0} за {1:dd.MM.yyyy HH:mm} успешно прочтена{2}", i, rspDate, rspDate == reqDate ? "" : " (дыра в архиве?)"));
                }

            }
            //result = ReadTrackDatesByInx(dates, new HourTrack());


            //var currentH = current.Date.AddHours(current.Hour);

            //Send(MakeWriteValueTypeRequest(ValueType.Hour));

            //var elements = ParseReadActiveElementsResponse(Send(MakeReadActiveElementsRequest()));
            //if (!elements.success) return elements;

            //var filterElements = FilterElements((List<dynamic>)elements.ActiveElements, ValueType.Hour);

            //var write = ParseWriteResponse(Send(MakeWriteElementsRequest(filterElements)));
            //if (!write.success) return write;

            ////сбор получасовок
            //for (var date = start.Date.AddHours(start.Hour); date <= end; date = date.AddHours(1))
            //{
            //    if (cancel())
            //    {
            //        archive.success = false;
            //        archive.error = "опрос отменен";
            //        break;
            //    }

            //    if (date >= currentH)
            //    {
            //        log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
            //        break;
            //    }

            //    write = ParseWriteResponse(Send(MakeWriteDateRequest(date)));
            //    if (!write.success)
            //    {
            //        if (write.code == 0)
            //        {
            //            return write;
            //        }
            //        log(string.Format("Ошибка при чтении записи за {0:dd.MM.yyyy HH:mm}: {1}", date, write.error));
            //        continue;
            //    }

            //    var data = ParseReadDataResponse(Send(MakeReadArchiveRequest()), date, properties.Fracs, properties.Units, elements.ActiveElements, ValueType.Hour);
            //    log(string.Format("прочитаны показания за {0:dd.MM.yyyy HH:mm}", date));
            //    hours.AddRange(data.Data);
            //}

            archive.success = true;
            archive.records = hours;
            return archive;
        }
        

    }




}

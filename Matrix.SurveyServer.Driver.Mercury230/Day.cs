using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        dynamic GetDays(DateTime start, DateTime end, DateTime currentDt, dynamic variant)
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
                archive.errorcode = DeviceError.NO_ERROR;
                return archive;
            }

            //recs.Add(MakeDayRecord("__datetime", 0, "", currentDt));

            //дни, преобразованные к datestart
            var yesterday = currentDt.Date.AddDays(-1).AddDays(-1);
            //if (start <= yesterday && yesterday <= end)
            {
                //1. читаем показания на начало вчерашнего дня
                for (byte tariff = 0; tariff <= 4; tariff++)
                {
                    var tariffText = tariff == 0 ? "сумме тарифов" : "тарифу " + tariff;
                    var offset = (UInt16)(tariff * 17 + 1787);
                    var response = ParseDataResponse(Send(MakeTariffScheduleRequest(offset, 0x10)), yesterday, tariff, variant.A);
                    if (!response.success) return response;

                    recs.AddRange(response.records);

                    int count = response.records.Count;
                    if (count == 3) //OR 4
                    {
                        log(string.Format("cуточные данные по {4} за {0:dd.MM.yyyy} A+(фаза 1)={1:0.000} A+(фаза 2)={2:0.000} A+(фаза 3)={3:0.000}", yesterday, response.records[count - 3].d1, response.records[count - 2].d1, response.records[count - 1].d1, tariffText));
                    }
                    else
                    {
                        log(string.Format("cуточные данные по {5} за {0:dd.MM.yyyy} A+={1:0.000} A-={2:0.000} R+={3:0.000} R-={4:0.000}", yesterday, response.records[count - 4].d1, response.records[count - 3].d1, response.records[count - 2].d1, response.records[count - 1].d1, tariffText));
                    }
                }
            }

            var today = currentDt.Date.AddDays(-1);

            //if (start <= today && today <= end)
            {
                //1. читаем показания на начало сегодняшнего дня
                for (byte tariff = 0; tariff <= 4; tariff++)
                {
                    var tariffText = tariff == 0 ? "сумме тарифов" : "тарифу " + tariff;
                    var offset = (UInt16)(tariff * 17 + 1702);
                    var response = ParseDataResponse(Send(MakeTariffScheduleRequest(offset, 0x10)), today, tariff, variant.A);
                    if (!response.success) return response;

                    recs.AddRange(response.records);

                    int count = response.records.Count;
                    if (count == 3)
                    {
                        log(string.Format("cуточные данные по {4} за {0:dd.MM.yyyy} A+(фаза 1)={1:0.000} A+(фаза 2)={2:0.000} A+(фаза 3)={3:0.000}", today, response.records[count - 3].d1, response.records[count - 2].d1, response.records[count - 1].d1, tariffText));
                    }
                    else //count == 4
                    {
                        log(string.Format("cуточные данные по {5} за {0:dd.MM.yyyy} A+={1:0.000} A-={2:0.000} R+={3:0.000} R-={4:0.000}", today, response.records[count - 4].d1, response.records[count - 3].d1, response.records[count - 2].d1, response.records[count - 1].d1, tariffText));
                    }
                }
            }

            //log(string.Format("Суточные ({0}):", recs.Count));
            //foreach (var data in recs)
            //{
            //    log(string.Format("{0:dd.MM.yyyy} {1} = {2} {3}", data.date, data.s1, data.d1, data.s2));
            //}

            records(recs);

            archive.records = recs;
            return archive;
        }
    }
}

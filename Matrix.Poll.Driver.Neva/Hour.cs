using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace Matrix.Poll.Driver.Neva
{
    public partial class Driver
    {
        dynamic GetHours(DateTime start, DateTime end, DateTime dtcounter)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            archive.records = new List<dynamic>();

            // 1. дата на вычислителе может быть либо ДО 0:30 - тогда за нулевой оффсет принимается предыдущий день, либо после 0:30 - начало текущего дня
            // 2. дата начала берётся с точностью до ДНЯ (начиная с 0:00)
            // 3. данные за текущий день нельзя брать полностью; необходимо взять ДО даты dtcounter
            // 4. 

            DateTime date0 = dtcounter.Date.AddDays((dtcounter.Hour == 0 && dtcounter.Minute < 30)? -1 : 0); // дата начала отсчёта
            start = start.Date > date0 ? date0 : start;
            end = end.Date > date0 ? date0 : end;
            int offStart = (int)(date0 - start).TotalDays;
            int offEnd = (int)(date0 - end).TotalDays;

            int offset = offStart;
            DateTime date = start;

            if (offset > 127) offset = 127;

            while (date <= end)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    archive.errorcode = DeviceError.NO_ERROR;
                    break;
                }

                log(string.Format("дата опроса {0:dd.MM.yyyy}; смещение {1}", date, offset), 3);
                
                var energyA = ParseValueArray(Send(MakeDataRequest(string.Format("630100{0:X2}()", offset))));
                if (!energyA.success) return energyA;

                var energyRp = ParseValueArray(Send(MakeDataRequest(string.Format("630101{0:X2}()", offset))));
                if (!energyRp.success) return energyRp;

                var energyRm = ParseValueArray(Send(MakeDataRequest(string.Format("630102{0:X2}()", offset))));
                if (!energyRm.success) return energyRm;

                List<dynamic> recs = new List<dynamic>();

                try
                {
                    for (int i = 0; i < 24; i++)
                    {
                        DateTime hourDate = date.AddHours(i);
                        if (hourDate.AddHours(-1) > dtcounter) break;

                        recs.Add(MakeHourRecord("Активная мощность", (energyA.values[i * 2 + 0] + energyA.values[i * 2 + 1]) / (2 * 1000), "кВт", hourDate));
                        recs.Add(MakeHourRecord("Реактивная мощность +", (energyRp.values[i * 2 + 0] + energyRp.values[i * 2 + 1]) / (2 * 1000), "кВАр", hourDate));
                        recs.Add(MakeHourRecord("Реактивная мощность -", (energyRm.values[i * 2 + 0] + energyRm.values[i * 2 + 1]) / (2 * 1000), "кВАр", hourDate));
                    }
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании часовой энергии : {0}:", ex));
                }
                

                log(string.Format("Прочитаны часовые за {0:dd.MM.yyyy}", date));

                records(recs);
                archive.records.AddRange(recs);

                date = date.AddDays(1);
                offset--;
            }
            
            //log(string.Format("Часовые Q+ ({0}):", hours.Count));
            //foreach (var data in hours)
            //{
            //    if (data.s1 == "Q+")
            //    {
            //        log(string.Format("{0:dd.MM.yyyy HH:mm} {1} = {2} {3}", data.date, data.s1, data.d1, data.s2));
            //    }
            //}
            return archive;
        }

    }

}

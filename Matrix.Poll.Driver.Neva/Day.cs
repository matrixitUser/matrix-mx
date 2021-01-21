using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

namespace Matrix.Poll.Driver.Neva
{
    public partial class Driver
    {
        dynamic GetDays(DateTime start, DateTime end, DateTime dtcounter)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            archive.records = new List<dynamic>();

            DateTime date0 = dtcounter.Date; // дата начала отсчёта
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

                var energyA = ParseValueArray(Send(MakeDataRequest(string.Format("0F8080{0:X2}()", offset))));
                if (!energyA.success) return energyA;

                var energyRp = ParseValueArray(Send(MakeDataRequest(string.Format("038080{0:X2}()", offset))));
                if (!energyRp.success) return energyRp;

                var energyRm = ParseValueArray(Send(MakeDataRequest(string.Format("048080{0:X2}()", offset))));
                if (!energyRm.success) return energyRm;

                List<double> valuesA = energyA.values;
                List<double> valuesRp = energyRp.values;
                List<double> valuesRm = energyRm.values;

                List<dynamic> recs = new List<dynamic>();

                log(string.Format("A: {0}, R+: {1}, R-: {2}", valuesA.Count, valuesRp.Count, valuesRm.Count));

                DateTime dayDate = date.AddDays(-1).Date;

                try
                {
                    recs.Add(MakeDayRecord("Активная энергия Итого", valuesA[0], "кВт*ч", dayDate));
                    recs.Add(MakeDayRecord("Активная энергия T1", valuesA[1], "кВт*ч", dayDate));
                    recs.Add(MakeDayRecord("Активная энергия T2", valuesA[2], "кВт*ч", dayDate));
                    recs.Add(MakeDayRecord("Активная энергия T3", valuesA[3], "кВт*ч", dayDate));
                    recs.Add(MakeDayRecord("Активная энергия T4", valuesA[4], "кВт*ч", dayDate));
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании суточной активной энергии : {0}:", ex));
                }
                try
                {
                    recs.Add(MakeDayRecord("Реактивная энергия+ Итого", valuesRp[0], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия+ T1", valuesRp[1], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия+ T2", valuesRp[2], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия+ T3", valuesRp[3], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия+ T4", valuesRp[4], "кВАр*ч", dayDate));
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании суточной реактивной энергии+ : {0}:", ex));
                }
                try
                {
                    recs.Add(MakeDayRecord("Реактивная энергия- Итого", valuesRm[0], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия- T1", valuesRm[1], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия- T2", valuesRm[2], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия- T3", valuesRm[3], "кВАр*ч", dayDate));
                    recs.Add(MakeDayRecord("Реактивная энергия- T4", valuesRm[4], "кВАр*ч", dayDate));
                }
                catch (Exception ex)
                {
                    log(string.Format("Ошибка при считывании суточной реактивной энергии- : {0}:", ex));
                }

              

                log(string.Format("Прочитаны суточные за {0:dd.MM.yyyy}", dayDate));

                records(recs);
                archive.records.AddRange(recs);

                date = date.AddDays(1);
                offset--;
            }

            return archive;
        }

    }

}

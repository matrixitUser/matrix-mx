using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        private Dictionary<int, byte[]> cacheDay = new Dictionary<int, byte[]>();

        private dynamic GetDayCached(int sn, int index)
        {
            if (0 > index || index >= 300)
            {
                dynamic err = new ExpandoObject();
                err.success = false;
                err.error = string.Format("индекс записи {0} за пределами архива [0..299]", index);
                return err;
            }

            var start = (short)(0x5480 + 20 * index);

            byte[] bytesDay;
            if (!cacheDay.ContainsKey(index))
            {
                bytesDay = Send(MakeMemoryRequest(sn, start, 20));
            }
            else
            {
                bytesDay = cacheDay[index];
            }

            return ParseDay(bytesDay);
        }

        private dynamic ParseDay(byte[] bytes)
        {
            var day = ParseResponse(bytes);

            if (!day.success)
            {
                return day;
            }

            if (day.body.Length < 20)
            {
                day.success = false;
                day.error = "длина пакета с записью меньше допустимой";
                return day;
            }

            day.records = new List<dynamic>();

            if ((day.body[19 - 1] == 0xFF) || (day.body[18 - 1] == 0xFF) || (day.body[17 - 1] == 0xFF))
            {
                day.date = RECORDDT_EMPTY_MIN;
            }
            else
            {
                day.date = new DateTime(
                    2000 + day.body[19 - 1],
                    day.body[18 - 1],
                    day.body[17 - 1],
                    day.body[16 - 1],
                    day.body[15 - 1],
                    0
                ).AddDays(-1);

                //var record = MakeDayRecord(Glossary.V_norm, (float)BitConverter.ToInt32(day.body, 4) / 10000f, "м³", day.date);
                //log(string.Format("расход {0}, время {1:dd.MM.yy HH:mm}", record.d1, record.date));

                day.records.Add(MakeDayRecord(Glossary.V_work, (float)BitConverter.ToInt32(day.body, 0) / 10000f, "м³", day.date));
                day.records.Add(MakeDayRecord(Glossary.V_norm, (float)BitConverter.ToInt32(day.body, 4) / 10000f, "м³", day.date));
                day.records.Add(MakeDayRecord(Glossary.P, (float)BitConverter.ToInt16(day.body, 9 - 1) / 10f, "кПа", day.date));
                day.records.Add(MakeDayRecord(Glossary.T, (float)BitConverter.ToInt16(day.body, 11 - 1) / 100f, "°C", day.date));
                day.records.Add(MakeDayRecord(Glossary.NWTime, BitConverter.ToInt16(day.body, 13 - 1), "ч", day.date));
            }

            return day;
        }
    }
}

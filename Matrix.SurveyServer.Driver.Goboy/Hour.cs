using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        private Dictionary<int, byte[]> cacheHour = new Dictionary<int, byte[]>();

        private dynamic GetHourCached(int sn, int index)
        {
            if (0 > index || index >= 25 * 45)
            {
                dynamic err = new ExpandoObject();
                err.success = false;
                err.error = string.Format("индекс записи {0} за пределами архива [0..1124]", index);
                return err;
            }

            var start = (short)(0x20 + 20 * index);
            
            byte[] bytesHour;
            if(!cacheHour.ContainsKey(index))
            {
                bytesHour = Send(MakeMemoryRequest(sn, start, 20));
            }
            else
            {
                bytesHour = cacheHour[index];
            }

            return ParseHour(bytesHour);
        }

        private dynamic ParseHour(byte[] bytes)
        {
            var hour = ParseResponse(bytes);

            if (!hour.success)
            {
                return hour;
            }

            if (hour.body.Length < 20)
            {
                hour.success = false;
                hour.error = "длина пакета с записью меньше допустимой";
                return hour;
            }

            hour.records = new List<dynamic>();

            hour.date = new DateTime(
                2000 + hour.body[19 - 1],
                hour.body[18 - 1],
                hour.body[17 - 1],
                hour.body[16 - 1],
                hour.body[15 - 1],
                0
            ).AddHours(-1);

            hour.records.Add(MakeHourRecord(Glossary.V_work, (float)BitConverter.ToInt32(hour.body, 0) / 10000f, "м³", hour.date));
            hour.records.Add(MakeHourRecord(Glossary.V_norm, (float)BitConverter.ToInt32(hour.body, 4) / 10000f, "м³", hour.date));
            hour.records.Add(MakeHourRecord(Glossary.P, (float)BitConverter.ToInt16(hour.body, 9 - 1) / 10f, "кПа", hour.date));
            hour.records.Add(MakeHourRecord(Glossary.T, (float)BitConverter.ToInt16(hour.body, 11 - 1) / 100f, "°C", hour.date));
            hour.records.Add(MakeHourRecord(Glossary.NWTime, BitConverter.ToInt16(hour.body, 13 - 1), "ч", hour.date));

            return hour;
        }
    }
}

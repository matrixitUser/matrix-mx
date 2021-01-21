using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetHour01(byte na, byte ch, short password, DateTime date, byte mode, int version)
        {
            date = date.AddDays(0);
            var bytes = SendWithCrc(MakeHour01Request(na, ch, password, date, mode));
            if (bytes.Any())
                return ParseHour01Response(bytes, version);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            answer.n = -1;
            return answer;
        }

        private byte[] MakeHour01Request(byte na, byte ch, short password, DateTime date, byte mode)
        {
            var bytes = new byte[] 
            { 
                ch,
                mode,
                (byte)date.Day,
                (byte)date.Month,
                (byte)(date.Year - 2000),
                (byte)(password >> 8), 
                (byte)(password & 0x00FF) 
            };
            return Make70Request(na, 1, bytes);
        }

        private dynamic ParseHour01Response(byte[] bytes, int version)
        {
            dynamic hour = Parse70Response(bytes);
            hour.n = -1;
            if (!hour.success) return hour;
            hour.records = new List<dynamic>();

            // log(string.Format("часы {0}", string.Join(",", (hour.body as byte[]).Select(b => b.ToString("X2")))));

            hour.channel = hour.body[0];
            hour.package = hour.body[1];
            hour.n = hour.body[2];
            if (hour.n == 0)
            {
                hour.success = false;
                hour.error = "данные отсутствуют";
                return hour;
            }

            hour.dates = new List<DateTime>();

            for (int i = 0; i < hour.n; i++)
            {
                int year = hour.body[i * 33 + 3 + 4] + 2000;
                int mounth = hour.body[i * 33 + 3 + 3];
                int day = hour.body[i * 33 + 3 + 2];
                int h = hour.body[i * 33 + 3 + 1];
                int minute = hour.body[i * 33 + 3 + 0];

                var date = new DateTime(year, mounth, day, h, minute, 0);

                //если интервал [A..B], то здесь время указывается B, 
                //поэтому уменьшаем до A
                date = date.AddHours(-1);
                
                hour.dates.Add(date);
                //время наработки
                var timeWork = new TimeSpan(BitConverter.ToUInt16(hour.body, i * 33 + 3 + 7), hour.body[i * 33 + 3 + 6], hour.body[i * 33 + 3 + 5]).TotalHours;
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Twork, hour.channel), timeWork, "ч", date));

                //накопленный на конец часа|суток объем при нормальных условиях, нм3
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vn, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 3 + 9), "м³", date));

                //накопленный на конец часа|суток объем при рабочих условиях, м3
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vw, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 3 + 13), "м³", date));

                //среднечасовое|среднесуточное значение расхода при нормальных условиях, нм3/час| нм3/сут
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qn, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 3 + 17), "м³", date));

                //среднечасовое|среднесуточное значение расхода при рабочих условиях, м3/час| м3/сут
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qw, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 3 + 21), "м³", date));

                //среднечасовое|среднесуточное значение давления, кПа
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.P, hour.channel), BitConverter.ToSingle(hour.body, i * 33 + 3 + 25), "кПа", date));

                //среднечасовое|среднесуточное знчение температуры, °C
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.T, hour.channel), BitConverter.ToSingle(hour.body, i * 33 + 3 + 29), "°C", date));
            }
            return hour;
        }
    }
}

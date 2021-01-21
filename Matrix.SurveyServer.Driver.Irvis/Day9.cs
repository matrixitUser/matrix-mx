using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private List<dynamic> GetDay09(byte na, List<byte> channels, short password, DateTime date)
        {
            var ret = new List<dynamic>();
            foreach (var ch in channels)
            {
                var bytes = SendWithCrc(MakeDay09Request(na, ch, password, date));
                if (bytes.Any())
                {
                    ret.Add(ParseDay09Response(bytes));
                }
            }
            return ret;
        }

        private byte[] MakeDay09Request(byte na, byte ch, short password, DateTime date)
        {
            var bytes = new byte[] 
            { 
                ch,
                0x00,
                0x00,
                (byte)24,
                (byte)date.Day,
                (byte)date.Month,
                (byte)(date.Year - 2000),
                (byte)(password >> 8), 
                (byte)(password & 0x00FF) 
            };
            return Make70Request(na, 9, bytes);
        }

        private dynamic ParseDay09Response(byte[] bytes)
        {
            dynamic day = Parse70Response(bytes);
            if (!day.success) return day;
            day.records = new List<dynamic>();
            day.channel = day.body[0];
            day.isEmpty = (bool)(day.body[5 - 3] == 0);

            if (day.isEmpty)
                return day;

            day.date = new DateTime(day.body[10 - 3] + 2000, day.body[9 - 3], day.body[8 - 3], day.body[7 - 3], day.body[6 - 3], 0);

            var timeWork = new TimeSpan(BitConverter.ToUInt16(day.body, 13 - 3), day.body[12 - 3], day.body[11 - 3]).TotalHours;

            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Twork, day.channel), timeWork, "ч", day.date));

            //накопленный на конец часа|суток объем при нормальных условиях, нм3
            var volumeNormal = BitConverter.ToUInt32(day.body, 16);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Vn, day.channel), volumeNormal, "м³", day.date));

            //накопленный на конец часа|суток объем при рабочих условиях, м3
            var volumeWork = BitConverter.ToUInt32(day.body, 20);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Vw, day.channel), volumeWork, "м³", day.date));

            //среднечасовое|среднесуточное значение расхода при нормальных условиях, нм3/час| нм3/сут
            var volumeConsumptionNormal = BitConverter.ToUInt32(day.body, 24);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Qn, day.channel), volumeConsumptionNormal, "м³", day.date));

            //среднечасовое|среднесуточное значение расхода при рабочих условиях, м3/час| м3/сут
            var volumeConsumptionWork = BitConverter.ToUInt32(day.body, 28);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Qw, day.channel), volumeConsumptionWork, "м³", day.date));

            //среднечасовое|среднесуточное значение давления, кПа
            var pressure = BitConverter.ToSingle(day.body, 32);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.P, day.channel), pressure, "кПа", day.date));

            //среднечасовое|среднесуточное знчение температуры, 0C
            var temperature = BitConverter.ToSingle(day.body, 36);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.T, day.channel), temperature, "°C", day.date));

            //время при НС0
            var tns0 = BitConverter.ToInt16(day.body, 40);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Tns0, day.channel), tns0, "с", day.date));

            //время при НС1
            var tns1 = BitConverter.ToInt16(day.body, 42);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Tns1, day.channel), tns1, "с", day.date));

            //время при НС2
            var tns2 = BitConverter.ToInt16(day.body, 44);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Tns2, day.channel), tns2, "с", day.date));

            //время при НС3
            var tns3 = BitConverter.ToInt16(day.body, 46);
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Tns3, day.channel), tns3, "с", day.date));

            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Qns2, day.channel), BitConverter.ToInt32(day.body, 48), "н.м³", day.date));

            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Vns, day.channel), BitConverter.ToInt32(day.body, 52), "н.м³", day.date));

            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_a, day.channel), day.body[59 - 3], "", day.date));
            day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Fl_b, day.channel), BitConverter.ToInt16(day.body, 60 - 3), "", day.date));

            return day;
        }
    }
}

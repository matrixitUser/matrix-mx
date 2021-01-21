using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private List<dynamic> GetHour09(byte na, List<byte> channels, short password, DateTime date)
        {
            var ret = new List<dynamic>();
            foreach (var ch in channels)
            {
                var bytes = SendWithCrc(MakeHour09Request(na, ch, password, date));
                if (bytes.Any())
                {
                    ret.Add(ParseHour9Response(bytes));
                }
            }
            return ret;
        }

        private byte[] MakeHour09Request(byte na, byte ch, short password, DateTime date)
        {
            var bytes = new byte[] 
            { 
                ch,
                0x00,
                0x00,
                (byte)date.Hour,
                (byte)date.Day,
                (byte)date.Month,
                (byte)(date.Year - 2000),
                (byte)(password >> 8), 
                (byte)(password & 0x00FF) 
            };
            return Make70Request(na, 9, bytes);
        }

        private dynamic ParseHour9Response(byte[] bytes)
        {
            dynamic hour = Parse70Response(bytes);
            if (!hour.success) return hour;
            hour.records = new List<dynamic>();
            hour.channel = hour.body[0];
            var rowsCount = hour.body[2];
            
            hour.isEmpty = false;
            if (rowsCount == 0)
            {
                hour.success = true;
                hour.error = "данные за запрошенную дату отсутствуют";
                hour.isEmpty = true;
                return hour;
            }

            hour.date = new DateTime(hour.body[10 - 3] + 2000, hour.body[9 - 3], hour.body[8 - 3], hour.body[7 - 3], hour.body[6 - 3], 0);

            //время наработки
            var timeWork = new TimeSpan(BitConverter.ToUInt16(hour.body, 13 - 3), hour.body[12 - 3], hour.body[11 - 3]).TotalHours;
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Twork, hour.channel), timeWork, "ч", hour.date));

            //накопленный на конец часа|суток объем при нормальных условиях, нм3
            var volumeNormal = BitConverter.ToUInt32(hour.body, 19 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vn, hour.channel), volumeNormal, "м³", hour.date));

            //накопленный на конец часа|суток объем при рабочих условиях, м3
            var volumeWork = BitConverter.ToUInt32(hour.body, 23 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vw, hour.channel), volumeWork, "м³", hour.date));

            //среднечасовое|среднесуточное значение расхода при нормальных условиях, нм3/час| нм3/сут
            var volumeConsumptionNormal = BitConverter.ToUInt32(hour.body, 27 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qn, hour.channel), volumeConsumptionNormal, "м³", hour.date));

            //среднечасовое|среднесуточное значение расхода при рабочих условиях, м3/час| м3/сут
            var volumeConsumptionWork = BitConverter.ToUInt32(hour.body, 31 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qw, hour.channel), volumeConsumptionWork, "м³", hour.date));

            //среднечасовое|среднесуточное значение давления, кПа
            var pressure = BitConverter.ToSingle(hour.body, 35 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.P, hour.channel), pressure, "кПа", hour.date));

            //среднечасовое|среднесуточное знчение температуры, 0C
            var temperature = BitConverter.ToSingle(hour.body, 39 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.T, hour.channel), temperature, "°C", hour.date));

            //время при НС0
            var tns0 = BitConverter.ToInt16(hour.body, 43 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Tns0, hour.channel), tns0, "с", hour.date));

            //время при НС1
            var tns1 = BitConverter.ToInt16(hour.body, 45 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Tns1, hour.channel), tns1, "с", hour.date));

            //время при НС2
            var tns2 = BitConverter.ToInt16(hour.body, 47 - 3);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Tns2, hour.channel), tns2, "с", hour.date));

            //время при НС3
            var tns3 = BitConverter.ToInt16(hour.body, 46);
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Tns3, hour.channel), tns3, "с", hour.date));

            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qns2, hour.channel), BitConverter.ToInt32(hour.body, 48), "н.м³", hour.date));

            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vns, hour.channel), BitConverter.ToInt32(hour.body, 52), "н.м³", hour.date));

            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Fl_a, hour.channel), hour.body[59 - 3], "", hour.date));
            hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Fl_b, hour.channel), BitConverter.ToInt16(hour.body, 60 - 3), "", hour.date));

            return hour;
        }
    }
}

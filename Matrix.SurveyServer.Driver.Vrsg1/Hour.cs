using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Vrsg1
{
    public partial class Driver
    {
        private dynamic GetHour(byte na, byte ch, short password, DateTime date, byte mode)
        {
            var bytes = SendWithCrc(MakeHourRequest(na, ch, password, date, mode));
            if (bytes.Any())
                return ParseHourResponse(bytes);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            return answer;
        }

        private byte[] MakeHourRequest(byte na, byte ch, short password, DateTime date, byte mode)
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

        private dynamic ParseHourResponse(byte[] bytes)
        {
            dynamic hour = Parse70Response(bytes);
            hour.n = -1;
            if (!hour.success) return hour;
            hour.records = new List<dynamic>();
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

                var date = new DateTime(year, mounth, day, h, minute, 0).AddHours(-1);

                hour.dates.Add(date);

                //время наработки
                //  var timeWork = (float)(BitConverter.ToInt16(hour.body, i * 33 + 10) * 3600 + hour.body[i * 33 + 9] * 60 + hour.body[i * 33 + 8]) / 3600f;
                //var work = (hour.body as IEnumerable<byte>).Skip(i * 33 + 3 + 5).Take(4).ToArray();
                //string path = @"D:\VRSG.txt";
                //System.IO.File.AppendAllText(path, string.Format("{0}\r\n", string.Join(" ", work.Select(b => b.ToString("X2")))));

                var timeWork = new TimeSpan(BitConverter.ToUInt16(hour.body, i * 33 + 3 + 7), hour.body[i * 33 + 3 + 6], hour.body[i * 33 + 3 + 5]).TotalHours;
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Twork, hour.channel), timeWork, "ч", date));

                //накопленный на конец часа|суток объем при нормальных условиях, нм3
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vn, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 12), "м³", date));

                //накопленный на конец часа|суток объем при рабочих условиях, м3
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Vw, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 16), "м³", date));

                //среднечасовое|среднесуточное значение расхода при нормальных условиях, нм3/час| нм3/сут
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qn, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 20), "м³", date));

                //среднечасовое|среднесуточное значение расхода при рабочих условиях, м3/час| м3/сут
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qw, hour.channel), BitConverter.ToUInt32(hour.body, i * 33 + 24), "м³", date));

                //среднечасовое|среднесуточное значение давления, кПа
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.P, hour.channel), BitConverter.ToSingle(hour.body, i * 33 + 28), "кПа", date));

                //среднечасовое|среднесуточное знчение температуры, 0C
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.T, hour.channel), BitConverter.ToSingle(hour.body, i * 33 + 32), "°C", date));
            }
            return hour;
        }

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }


        private byte[] MakeModbusRequest(byte na, byte func, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(na);
            bytes.Add(func);
            bytes.AddRange(body);
            var crc = CalcCrc16(bytes.ToArray());
            bytes.AddRange(crc);
            return bytes.ToArray();
        }

        private dynamic ParseModbusResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на запрос";
                return answer;
            }

            if (!CheckCrc16(bytes))
            {
                answer.success = false;
                answer.error = "не сошлась контрольная сумма";
                answer.body = bytes;
                return answer;
            }

            byte function = bytes[1];

            if (function > 0x80)
            {
                var exceptionCode = (ModbusExceptionCode)bytes[2];

                answer.success = false;
                answer.error = string.Format("устройство вернуло ошибку: {0}", exceptionCode);
                answer.body = bytes;
                return answer;
            }

            answer.success = true;
            answer.error = string.Empty;
            answer.body = (bytes as byte[]).Skip(2).Take((int)bytes.Length - (2 + 2)).ToArray();
            return answer;
        }


        private byte[] Make70Request(byte na, byte cmd, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(cmd);
            bytes.AddRange(body);
            return MakeModbusRequest(na, 70, bytes.ToArray());
        }

        private dynamic Parse70Response(byte[] bytes)
        {
            var x = ParseModbusResponse(bytes);
            if (!x.success) return x;

            x.body = (x.body as byte[]).Skip(1).ToArray();
            return x;
        }
    }
}

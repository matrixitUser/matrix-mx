using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private List<dynamic> GetCurrent(byte na, List<byte> channels, short password)
        {
            var ret = new List<dynamic>();
            foreach (var ch in channels)
            {
                var bytes = SendWithCrc(MakeCurrentRequest(na, ch, password));
                if (bytes.Any())
                ret.Add(ParseCurrentResponse(ch, bytes));
            }
            return ret;
        }

        private byte[] MakeCurrentRequest(byte na, byte ch, short password)
        {
            var bytes = new byte[] { ch, (byte)(password >> 8), (byte)(password & 0x00FF) };
            return Make70Request(na, 3, bytes);
        }

        private dynamic ParseDateTime(byte[] bytes)
        {
            dynamic time = new ExpandoObject();
            time.success = true;
            time.error = string.Empty;

            if (bytes == null || bytes.Length < 5)
            {
                time.success = false;
                time.error = "недостаточно данных для разбора";
                return time;
            }

            var minute = bytes[0];
            if (minute > 59 || minute < 0)
            {
                time.success = false;
                time.error = string.Format("не верное значение параметра 'минута' ({0})", minute);
                return time;
            }

            var hour = bytes[1];
            if (hour > 23 || hour < 0)
            {
                time.success = false;
                time.error = string.Format("не верное значение параметра 'час' ({0})", hour);
                return time;
            }
            var day = bytes[2];
            if (day > 31 || day < 0)
            {
                time.success = false;
                time.error = string.Format("не верное значение параметра 'день' ({0})", day);
                return time;
            }

            var month = bytes[3];
            if (month > 12 || month < 0)
            {
                time.success = false;
                time.error = string.Format("не верное значение параметра 'день' ({0})", month);
                return time;
            }
            var year = bytes[4];
            if (year < 0)
            {
                time.success = false;
                time.error = string.Format("не верное значение параметра 'день' ({0})", year);
                return time;
            }

            time.date = new DateTime(year + 2000, month, day, hour, minute, 0);

            if (Math.Abs((time.date - DateTime.Now).TotalDays) > 1)
            {
                time.success = false;
                time.error = string.Format("рассинхронизация времени вычислителя с серверным более суток ({0} ч)", (time.date - DateTime.Now).TotalHours);
            }
            return time;
        }

        private dynamic ParseCurrentResponse(byte ch, byte[] bytes)
        {
            var current = Parse70Response(bytes);
            if (!current.success) return current;

            current.records = new List<dynamic>();
            current.channel = current.body[0];


            var blockDate = ParseDateTime((current.body as byte[]).Skip(1).Take(5).ToArray());
            if (!blockDate.success)
                return blockDate;
            current.date = blockDate.date;

            //время наработки
            var timeWork = new TimeSpan(BitConverter.ToUInt16(current.body, 8), current.body[7], current.body[6]).TotalHours;
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Twork, current.channel), timeWork, "ч", blockDate.date));

            //текущее значение накопленного объема при нормальных условиях (нм3)
            var volumeNormal = BitConverter.ToUInt32(current.body, 10);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Vn, current.channel), volumeNormal, "м³", blockDate.date));

            //текущее значение расхода при нормальных условиях (нм3/ч)
            var volumeConsumptionNormal = BitConverter.ToSingle(current.body, 14);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Qn, current.channel), volumeConsumptionNormal, "м³", blockDate.date));

            //текущее значение давления (кПа)
            var pressure = BitConverter.ToSingle(current.body, 18);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.P, current.channel), pressure, "кПа", blockDate.date));

            //текущее значение температуры (град. С)
            var temperature = BitConverter.ToSingle(current.body, 22);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.T, current.channel), temperature, "°C", blockDate.date));

            return current;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

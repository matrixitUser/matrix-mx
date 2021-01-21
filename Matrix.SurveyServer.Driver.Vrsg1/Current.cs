using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Vrsg1
{
    public partial class Driver
    {
        private dynamic GetCurrent(byte na, byte ch, short password)
        {
            var bytes = SendWithCrc(MakeCurrentRequest(na, ch, password));
            if (bytes.Any())
                return ParseCurrentResponse(bytes);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            return answer;
        }

        private byte[] MakeCurrentRequest(byte na, byte ch, short password)
        {
            var bytes = new byte[] { ch, (byte)(password >> 8), (byte)(password & 0x00FF) };
            return Make70Request(na, 3, bytes);
        }

        private dynamic ParseCurrentResponse(byte[] bytes)
        {
            var current = Parse70Response(bytes);
            if (!current.success) return current;

            current.records = new List<dynamic>();
            current.channel = current.body[0];

            // log(string.Format("ответ текущих {0}", string.Join(",", (current.body as IEnumerable<byte>).Select(x => x.ToString("X")))));
			
            var blockDate = new DateTime(current.body[5] + 2000, current.body[4], current.body[3], current.body[2], current.body[1], 0);
            // var blockDate = Helper.ParseDateTime(current.body, 0);
            current.date = blockDate;

            //время наработки
			var foo=1;
            var timeWork = new TimeSpan(BitConverter.ToUInt16(current.body, 8), current.body[7], current.body[6]).TotalHours;
            //var timeWork = current.body[6] + current.body[7] * 60 + current.body[8] * 3600;
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Twork, current.channel), (float)timeWork / 3600f, "ч", blockDate));

            //текущее значение накопленного объема при нормальных условиях (нм3)
            var volumeNormal = BitConverter.ToUInt32(current.body, 10);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Vn, current.channel), volumeNormal, "м3", blockDate));

            //текущее значение расхода при нормальных условиях (нм3/ч)
            var volumeConsumptionNormal = BitConverter.ToSingle(current.body, 14);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Qn, current.channel), volumeConsumptionNormal, "м3", blockDate));

            //текущее значение давления (кПа)
            var pressure = BitConverter.ToSingle(current.body, 18);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.P, current.channel), pressure, "кПа", blockDate));

            //текущее значение температуры (град. С)
            var temperature = BitConverter.ToSingle(current.body, 22);
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.T, current.channel), temperature, "C", blockDate));

            var contractHour = current.body[26];
            current.contractHour = contractHour;
            current.records.Add(MakeConstantRecord("расчетный час", contractHour, blockDate));
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

        private dynamic MakeConstantRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

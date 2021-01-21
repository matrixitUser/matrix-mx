using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        private dynamic GetCurrent(byte na, dynamic units)
        {
            var time = GetCurrentTime(na);
            if (!time.success)
                return time;

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";

            List<byte> bytes = new List<byte>();

            var data = Send(MakeRamRequest(na, 0x0228, 0x30));
            if (!data.Any())
                return answer;

            var current = ParseRamResponse(data);
            if (!current.success) return current;

            bytes.AddRange(current.body);

            data = Send(MakeRamRequest(na, 0x0260, 0x14));
            if (!data.Any())
                return answer;

            current = ParseRamResponse(data);
            if (!current.success) return current;

            bytes.AddRange(current.body);

            return ParseCurrent(bytes.ToArray(), time.date, units);
        }

        private dynamic ParseCurrent(byte[] bytes, DateTime date, dynamic units)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.body = bytes;
            current.date = date;
            current.records = new List<dynamic>();

            int offset = 0;

            var P1 = Helper.SpgFloatToIEEE(current.body, offset);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.P1), P1, units["Р1"], date));

            var dP1 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.dP1), dP1, units["dP1"], date));

            var t1 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.t1), t1, "C", date));

            var Qp1 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.Qp1), Qp1, "м3", date));

            var Q1 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.Q1), Q1, "м3", date));

            //Вторая труба	
            offset = 28;
            var P2 = Helper.SpgFloatToIEEE(current.body, offset);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.P2), P2, units["Р2"], date));

            var dP2 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.dP2), dP2, units["dР2"], date));

            var t2 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.t2), t2, "C", date));

            var Qp2 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.Qp2), Qp2, "м3", date));

            var Q2 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.Q2), Q2, "м3", date));

            //Общие		
            offset = 56;
            var dP3 = Helper.SpgFloatToIEEE(current.body, offset);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.dP3), dP3, units["dР3"], date));

            var Pb = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.Pb), Pb, units["Рb"], date));

            var P3 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.P3), P3, units["Р3"], date));

            var P4 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.P4), P4, units["Р4"], date));

            var t3 = Helper.SpgFloatToIEEE(current.body, offset += 4);
            current.records.Add(MakeCurrentRecord(string.Format("{0}", Glossary.t3), t3, "C", date));

            return current;
        }

        private dynamic GetCurrentTime(byte na)
        {
            //var foo = new List<byte>();
            //foo.AddRange(MakeRamRequest(na, 0xf3, 10));
            //for (var i = 0; i < 15; i++)
            //{
            //    foo.Add(0xff);
            //}
            var bytes = Send(MakeRamRequest(na, 0xf3, 10));
            dynamic answer = ParseRamResponse(bytes);
            if (!answer.success) return answer;

            try
            {
                int year = answer.body[0];
                int month = answer.body[1];
                int day = answer.body[2];
                int watch_hh = answer.body[3];
                int watch_mm = answer.body[4];
                int watch_ss = answer.body[5];
                answer.date = new DateTime(2000 + year, month, day, watch_hh, watch_mm, watch_ss);
            }
            catch (Exception ex)
            {
                answer.success = false;
                answer.error = "ошибка при обработке значения текущего времени";
            }
            return answer;
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

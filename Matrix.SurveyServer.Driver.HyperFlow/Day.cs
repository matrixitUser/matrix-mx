using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private dynamic GetDays(byte na, int index)
        {
            var response = Send(MakeRequest(Direction.MasterToSlave, na, 142, BitConverter.GetBytes(index)));
            return ParseDay(response);
        }

        private dynamic ParseDay(byte[] data)
        {
            dynamic day = ParseResponse(data);
            if (!day.success) return day;

            day.records = new List<dynamic>();

            if (day.length == 0) return day;

            var timeSeconds = BitConverter.ToUInt32(day.body, 0);
            day.date = new DateTime(1997, 01, 01).AddSeconds(timeSeconds).AddDays(-1); //коррекция

            day.records.Add(MakeDayRecord(Glossary.err, day.body[4], "", day.date));

            day.records.Add(MakeDayRecord(Glossary.Qr, BitConverter.ToSingle(day.body, 5), "м³", day.date));

            day.records.Add(MakeDayRecord(Glossary.P, BitConverter.ToSingle(day.body, 9), "кгс/см²", day.date));

            day.records.Add(MakeDayRecord(Glossary.T, BitConverter.ToSingle(day.body, 13), "°С", day.date));

            day.records.Add(MakeDayRecord(Glossary.Q, BitConverter.ToSingle(day.body, 17), "м³", day.date));

            day.records.Add(MakeDayRecord(Glossary.W, BitConverter.ToSingle(day.body, 21), "ГДж", day.date));

            return day;
        }
    }
}

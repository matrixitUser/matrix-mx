using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private dynamic GetHour(byte na, int index)
        {
            var response = Send(MakeRequest(Direction.MasterToSlave, na, 140, BitConverter.GetBytes(index)));
            return ParseHour(response);
        }

        private dynamic ParseHour(byte[] data)
        {
            dynamic hour = ParseResponse(data);
            if (!hour.success) return hour;

            hour.records = new List<dynamic>();

            if (hour.length == 0) return hour;

            var timeSeconds = BitConverter.ToUInt32(hour.body, 0);
            hour.date = new DateTime(1997, 01, 01).AddSeconds(timeSeconds).AddHours(-1); //коррекция            

            hour.records.Add(MakeHourRecord(Glossary.err, hour.body[4], "", hour.date));

            hour.records.Add(MakeHourRecord(Glossary.Qr, BitConverter.ToSingle(hour.body, 5), "м³", hour.date));

            hour.records.Add(MakeHourRecord(Glossary.P, BitConverter.ToSingle(hour.body, 9), "кгс/см²", hour.date));

            hour.records.Add(MakeHourRecord(Glossary.T, BitConverter.ToSingle(hour.body, 13), "°С", hour.date));

            hour.records.Add(MakeHourRecord(Glossary.Q, BitConverter.ToSingle(hour.body, 17), "м³", hour.date));

            hour.records.Add(MakeHourRecord(Glossary.W, BitConverter.ToSingle(hour.body, 21), "ГДж", hour.date));

            return hour;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetHour20RU6D(byte na, byte channel, byte number, DateTime start, DateTime end)
        {
            return ParseHour20RU6D(Send(MakeHourRequest(na, channel, number, start, end)));
        }

        private byte[] MakeHourRequest(byte na, byte channel, byte number, DateTime start, DateTime end)
        {
            return MakeRequest(na, 21, new byte[] { 
                channel,
                number,
                (byte)start.Month,
                (byte)start.Day,
                (byte)(start.Year-2000),
                (byte)start.Hour,
                (byte)end.Month,
                (byte)end.Day,
                (byte)(end.Year-2000),
                (byte)start.Hour,
            });
        }

        private dynamic ParseHour20RU6D(byte[] bytes)
        {
            dynamic hour = ParseResponse(bytes);
            if (!hour.success)
            {
                return hour;
            }

            hour.channel = hour.body[0];
            hour.recordCount = hour.body[1];

            hour.state = hour.body[2];

            hour.records = new List<dynamic>();

            for (var rec = 0; rec < hour.recordCount; rec++)
            {
                var offset = rec * 29;
                var date = new DateTime(2000 + hour.body[offset + 5], hour.body[offset + 3], hour.body[offset + 4], hour.body[offset + 6], hour.body[offset + 7], 0);
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qnt, hour.channel), BitConverter.ToSingle(hour.body, offset + 8), "м3", date));
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.E, hour.channel), BitConverter.ToSingle(hour.body, offset + 12), "МДж", date));
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.dP, hour.channel), BitConverter.ToSingle(hour.body, offset + 16), "кПа", date));
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Pa, hour.channel), BitConverter.ToSingle(hour.body, offset + 20), "кПа", date));
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.T, hour.channel), BitConverter.ToSingle(hour.body, offset + 24), "'C", date));
                hour.records.Add(MakeHourRecord(string.Format("{0}{1}", Glossary.Qn, hour.channel), BitConverter.ToInt32(hour.body, offset + 28), "м3", date));
            }

            return hour;
        }
    }
}

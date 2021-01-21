﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetDays20RU6D(byte na, byte channel, byte number, DateTime start, DateTime end)
        {
            return ParseDay20RU6D(Send(MakeDayRequest(na, channel, number, start, end)));
        }

        private dynamic ParseDay20RU6D(byte[] bytes)
        {
            dynamic day = ParseResponse(bytes);
            if (!day.success)
            {
                return day;
            }

            day.channel = day.body[0];
            day.recordCount = day.body[1];

            day.state = day.body[2];

            day.records = new List<dynamic>();

            for (var rec = 0; rec < day.recordCount; rec++)
            {
                var offset = rec * 27;
                var date = new DateTime(2000 + day.body[offset + 5], day.body[offset + 3], day.body[offset + 4]);
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Qnt, day.channel), BitConverter.ToSingle(day.body, offset + 6), "м³", date));
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.E, day.channel), BitConverter.ToSingle(day.body, offset + 10), "МДж", date));
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.dP, day.channel), BitConverter.ToSingle(day.body, offset + 14), "кПа", date));
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Pa, day.channel), BitConverter.ToSingle(day.body, offset + 18), "кПа", date));
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.T, day.channel), BitConverter.ToSingle(day.body, offset + 22), "'C", date));
                day.records.Add(MakeDayRecord(string.Format("{0}{1}", Glossary.Qn, day.channel), BitConverter.ToInt32(day.body, offset + 26), "м³", date));
            }

            return day;
        }
    }
}
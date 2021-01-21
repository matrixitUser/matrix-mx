using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private dynamic GetHour2(byte na, int number)
        {
            dynamic mix = new ExpandoObject();
            mix.success = true;
            mix.records = new List<dynamic>();

            //берем самую новую запись, ежели она больше начальной, идем дальше            
            var h = ParseRecord(Send(MakeRecordRequest(na, 8, 0, number))); if (!h.success) return h;
            mix.date = h.date.AddHours(-1);
            mix.number = h.number;
            var h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 1, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qw, h.value + h1.value, GetUnit(Glossary.Qw), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 2, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 3, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qn, h.value + h1.value, GetUnit(Glossary.Qn), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 4, number))); if (!h.success) return h;
            mix.records.Add(MakeHourRecord(Glossary.P, h.value, GetUnit(Glossary.P), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 5, number))); if (!h.success) return h;
            mix.records.Add(MakeHourRecord(Glossary.T, h.value, GetUnit(Glossary.T), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 12, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 13, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qnns, h.value + h1.value, GetUnit(Glossary.Qnns), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 14, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 15, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qwns, h.value + h1.value, GetUnit(Glossary.Qwns), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 16, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 17, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qnt, h.value + h1.value, GetUnit(Glossary.Qnt), h.date.AddHours(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 18, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 19, number))); if (!h1.success) return h1;
            mix.records.Add(MakeHourRecord(Glossary.Qwt, h.value + h1.value, GetUnit(Glossary.Qwt), h.date.AddHours(-1)));
            //----сутки-----

            return mix;
        }

        private dynamic GetDay2(byte na, int number, int ch)
        {
            dynamic mix = new ExpandoObject();
            mix.success = true;
            mix.records = new List<dynamic>();

            //берем самую новую запись, ежели она больше начальной, идем дальше            
            var h = ParseRecord(Send(MakeRecordRequest(na, 8, 6, number))); if (!h.success) return h;
            //log(string.Format("", h.date,));
            mix.date = h.date.AddHours(-ch).AddDays(-1).Date;
            mix.number = h.number;
            var h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 7, number))); if (!h1.success) return h1;
            mix.records.Add(MakeDayRecord(Glossary.Qn, (double)h.value + (double)h1.value, GetUnit(Glossary.Qn), mix.date));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 8, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 9, number))); if (!h1.success) return h1;
            mix.records.Add(MakeDayRecord(Glossary.Qw, (double)h.value + (double)h1.value, GetUnit(Glossary.Qw), mix.date));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 10, number))); if (!h.success) return h;
            mix.records.Add(MakeDayRecord(Glossary.P, (double)h.value, GetUnit(Glossary.P), mix.date));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 11, number))); if (!h.success) return h;
            mix.records.Add(MakeDayRecord(Glossary.T, (double)h.value, GetUnit(Glossary.T), mix.date));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 12, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 13, number))); if (!h1.success) return h1;
            mix.records.Add(MakeDayRecord(Glossary.Qnns, (double)h.value + (double)h1.value, GetUnit(Glossary.Qnns), mix.date));


            return mix;
        }
    }
}

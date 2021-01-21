using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {

        /// <summary>
        ///  switch (channel)
        //{
        //    case 0:
        //    case 1:
        //        return Glossary.Qnt;
        //    case 2:
        //    case 3:
        //        return Glossary.Qwt;
        //    case 4:
        //    case 5:
        //        return Glossary.Qn;
        //    case 6:
        //    case 7:
        //        return Glossary.Qw;
        //    case 9: return Glossary.P;
        //    case 11: return Glossary.T;
        //    case 12:
        //    case 13:
        //        return Glossary.Qnns;
        //}
        /// </summary>
        /// <param name="na"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        private dynamic GetDay(byte na, int number)
        {
            dynamic days = new ExpandoObject();
            days.success = true;
            days.records = new List<dynamic>();

            //берем самую новую запись, ежели она больше начальной, идем дальше            
            var h = ParseRecord(Send(MakeRecordRequest(na, 8, 11, number)));
            if (!h.success) return h;
            days.records.Add(MakeDayRecord(Glossary.T, h.value, GetUnit(Glossary.T), h.date.AddDays(-1)));
            days.date = h.date.AddDays(-1);
            days.number = h.number;

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 12, number))); if (!h.success) return h;
            var h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 13, number))); if (!h1.success) return h1;
            days.records.Add(MakeDayRecord(Glossary.Qnns, (double)h.value + (double)h1.value, GetUnit(Glossary.Qnns), h.date.AddDays(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 0, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 1, number))); if (!h1.success) return h1;            
            days.records.Add(MakeDayRecord(Glossary.Qnt, (double)h.value + (double)h1.value, GetUnit(Glossary.Qnt), h.date.AddDays(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 4, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 5, number))); if (!h1.success) return h1;
            days.records.Add(MakeDayRecord(Glossary.Qn, (double)h.value + (double)h1.value, GetUnit(Glossary.Qn), h.date.AddDays(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 2, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 3, number))); if (!h1.success) return h1;
            days.records.Add(MakeDayRecord(Glossary.Qwt, (double)h.value + (double)h1.value, GetUnit(Glossary.Qwt), h.date.AddDays(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 6, number))); if (!h.success) return h;
            h1 = ParseRecord(Send(MakeRecordRequest(na, 8, 7, number))); if (!h1.success) return h1;
            days.records.Add(MakeDayRecord(Glossary.Qw, (double)h.value + (double)h1.value, GetUnit(Glossary.Qw), h.date.AddDays(-1)));

            h = ParseRecord(Send(MakeRecordRequest(na, 8, 9, number))); if (!h.success) return h;
            days.records.Add(MakeDayRecord(Glossary.P, (double)h.value, GetUnit(Glossary.P), h.date.AddDays(-1)));

            return days;
        }
    }
}

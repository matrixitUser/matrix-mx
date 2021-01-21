using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private dynamic GetCurrent(byte na)
        {
            dynamic currents = new ExpandoObject();
            currents.success = true;
            currents.records = new List<dynamic>();

            var date = ParseDate(Send(MakeModbus3Request(na, 475, 6)));
            if (!date.success)
            {
                currents.success = false;
                currents.error = "не получена дата";
                return currents;
            }

            currents.date = date.date;

            var qn = ParseFloat(Send(MakeModbus3Request(na, 204, 2)));
            if (qn.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.Qn, qn.value, "м³", currents.date));
            }

            var qw = ParseFloat(Send(MakeModbus3Request(na, 206, 2)));
            if (qw.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.Qw, qw.value, "м³", currents.date));
            }

            var p = ParseFloat(Send(MakeModbus3Request(na, 300, 2)));
            if (p.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.P, p.value, "бар", currents.date));
            }

            var t = ParseFloat(Send(MakeModbus3Request(na, 302, 2)));
            if (t.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.T, t.value, "°C", currents.date));
            }

            var qnta = ParseInt32(Send(MakeModbus3Request(na, 500, 2)));
            var qntb = ParseFloat(Send(MakeModbus3Request(na, 502, 2)));
            if (qnta.success && qntb.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.Qnt, qnta.value + qntb.value, "м³", currents.date));
            }

            var qwta = ParseInt32(Send(MakeModbus3Request(na, 520, 2)));
            var qwtb = ParseFloat(Send(MakeModbus3Request(na, 522, 2)));
            if (qwta.success && qwtb.success)
            {
                currents.records.Add(MakeCurrentRecord(Glossary.Qwt, qwta.value + qwtb.value, "м³", currents.date));
            }

            return currents;
        }
    }
}

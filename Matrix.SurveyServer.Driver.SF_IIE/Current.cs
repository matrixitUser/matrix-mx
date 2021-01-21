using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetCurrent(byte na, byte channel)
        {
            return ParseCurrent(Send(MakeRequest(na, 4, new byte[] { channel })));
        }

        private dynamic ParseCurrent(byte[] bytes)
        {
            dynamic current = ParseResponse(bytes);
            if (!current.success)
            {
                return current;
            }

            current.channel = current.body[0];
            current.records = new List<dynamic>();

            current.date = new DateTime(2000 + current.body[131], current.body[129], current.body[130], current.body[132], current.body[133], current.body[134]);

            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.dP, current.channel), BitConverter.ToSingle(current.body, 1), "кПа", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.P, current.channel), BitConverter.ToSingle(current.body, 5), "кПа", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.T, current.channel), BitConverter.ToSingle(current.body, 9), "'C", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.E, current.channel), BitConverter.ToSingle(current.body, 13), "МДж", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Qc, current.channel), BitConverter.ToSingle(current.body, 17), "м³/ч", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Vt, current.channel), BitConverter.ToSingle(current.body, 21), "м³", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Vd, current.channel), BitConverter.ToSingle(current.body, 25), "м³", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Qacc, current.channel), BitConverter.ToSingle(current.body, 25), "тыс.м³", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.K, current.channel), BitConverter.ToSingle(current.body, 33), "", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Zc, current.channel), BitConverter.ToSingle(current.body, 38), "", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Hs, current.channel), BitConverter.ToSingle(current.body, 41), "МДж/м³", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Pa, current.channel), BitConverter.ToSingle(current.body, 45), "кПа", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Nsh, current.channel), BitConverter.ToSingle(current.body, 49), "МДж/м³", current.date));
            current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", Glossary.Densw, current.channel), BitConverter.ToSingle(current.body, 73), "кг/м³", current.date));

            return current;
        }
    }
}

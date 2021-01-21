using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        private dynamic GetCurrents(int na)
        {
            return ParseCurrents(Send(MakeCurrentsRequest(na)));
        }

        private byte[] MakeCurrentsRequest(int na)
        {
            return MakeRequest(na, 0x01, new byte[] { });
        }

        private dynamic ParseCurrents(byte[] bytes)
        {
            dynamic currents = ParseResponse(bytes);

            if (!currents.success) return currents;

            if (bytes.Length < 25)
            {
                currents.success = false;
                currents.error = "недостаточно данных";
                return currents;
            }

            var bts = (currents.body as byte[]);

            var date = new DateTime(
                2000 + bts[5],
                bts[4],
                bts[3],
                bts[2],
                bts[1],
                bts[0]
            );

            currents.date = date;

            currents.records = new List<dynamic>();

            currents.records.Add(MakeCurrentRecord(Glossary.Rate, BitConverter.ToSingle(bts, 6), "м³", date));
            currents.records.Add(MakeCurrentRecord(Glossary.NormRate, BitConverter.ToSingle(bts, 10), "м³", date));
            currents.records.Add(MakeCurrentRecord(Glossary.P, BitConverter.ToSingle(bts, 14), "кПа", date));
            currents.records.Add(MakeCurrentRecord(Glossary.T, BitConverter.ToSingle(bts, 18), "°C", date));
            currents.records.Add(MakeCurrentRecord(Glossary.TimeError, BitConverter.ToUInt16(bts, 22), "ч", date));
            currents.records.Add(MakeCurrentRecord(Glossary.Acc, bts[24], "", date));

            return currents;
        }

    }
}

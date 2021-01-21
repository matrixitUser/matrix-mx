using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private dynamic GetCurrents(byte na)
        {
            dynamic currents = new ExpandoObject();
            currents.success = true;
            currents.records = new List<dynamic>();

            var dateResp = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 23 })));
            if (!dateResp.success) return dateResp;            
            currents.date = new DateTime(1997, 1, 1, 0, 0, 0, 0).AddSeconds(dateResp.value);

            var r1 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 0 })));
            if (!r1.success) return r1;
            currents.records.Add(MakeCurrentRecord(Glossary.Qr, r1.value, "м³/ч", currents.date));

            var r2 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 1 })));
            if (!r2.success) return r2;
            currents.records.Add(MakeCurrentRecord(Glossary.P, r2.value, "кгс/см²", currents.date));

            var r3 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 2 })));
            if (!r3.success) return r3;
            currents.records.Add(MakeCurrentRecord(Glossary.T, r3.value, "°C", currents.date));

            var r4 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 3 })));
            if (!r4.success) return r4;
            currents.records.Add(MakeCurrentRecord(Glossary.Q, r4.value, "м³/ч", currents.date));

            var r5 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 4 })));
            if (!r5.success) return r5;
            currents.records.Add(MakeCurrentRecord(Glossary.W, r5.value, "ГДж", currents.date));

            var r6 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 5, 6 })));
            if (!r6.success) return r6;
            currents.records.Add(MakeCurrentRecord("накопленный расход с.у", r6.value, "", currents.date));

            var r7 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 10 })));
            if (!r7.success) return r7;
            currents.records.Add(MakeCurrentRecord("баром.давление", r7.value, "кгс/см²", currents.date));

            var r8 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 24 })));
            if (!r8.success) return r8;
            currents.records.Add(MakeCurrentRecord("напряжение литиевой батареи", r8.value, "мВ", currents.date));

            var r9 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 33, 34 })));
            if (!r9.success) return r9;
            currents.records.Add(MakeCurrentRecord("накопленная теплота сгорания", r9.value, "", currents.date));

            var r10 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 40 })));
            if (!r10.success) return r10;
            currents.records.Add(MakeCurrentRecord("время наработки от литиевой батареи", r10.value, "c", currents.date));

            var r11 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 41 })));
            if (!r11.success) return r11;
            currents.records.Add(MakeCurrentRecord("общее время наработки", r11.value, "c", currents.date));

            var r12 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 108, 109 })));
            if (!r12.success) return r12;
            currents.records.Add(MakeCurrentRecord("накопленный расход р.у", r12.value, "", currents.date));

            return currents;
        }
    }
}

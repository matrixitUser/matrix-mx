using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private dynamic GetConstants(byte na, DateTime date)
        {
            dynamic constants = new ExpandoObject();
            constants.success = true;
            constants.records = new List<dynamic>();

            var r1 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 7 })));
            if (!r1.success) return r1;
            constants.records.Add(MakeConstRecord("Коммерческий час", r1.value, date));
            constants.contractHour = r1.value;

            var r2 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 8 })));
            if (!r2.success) return r2;
            constants.records.Add(MakeConstRecord("скорость отсечки", string.Format("{0} м/сек", r2.value), date));

            var r3 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 9 })));
            if (!r3.success) return r3;
            constants.records.Add(MakeConstRecord("плотность н.у.", string.Format("{0} кг/м3", r3.value), date));

            var r4 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 11 })));
            if (!r4.success) return r4;
            constants.records.Add(MakeConstRecord("содержание СО2", string.Format("{0} мол.долей", r4.value), date));

            var r5 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 12 })));
            if (!r5.success) return r5;
            constants.records.Add(MakeConstRecord("содержание N2", string.Format("{0} мол.долей", r5.value), date));

            var r6 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 13 })));
            if (!r6.success) return r6;
            constants.records.Add(MakeConstRecord("диаметр трубопровода", string.Format("{0} мм", r6.value), date));

            var r7 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 14 })));
            if (!r7.success) return r7;
            constants.records.Add(MakeConstRecord("базовое расстояние в канале А", string.Format("{0} мм", r7.value), date));

            var r8 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 15 })));
            if (!r8.success) return r8;
            constants.records.Add(MakeConstRecord("материал трубопровода", string.Format("{0}", r8.value), date));

            var r9 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 20 })));
            if (!r9.success) return r9;
            constants.records.Add(MakeConstRecord("измеряемая среда", r9.value == 1 ? "природный газ" : r9.value == 4 ? "другое" : string.Format("другое ({0})", r9.value), date));

            var r10 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 21 })));
            if (!r10.success) return r10;
            constants.records.Add(MakeConstRecord("эмуляция канала P", (r10.value == -800) ? "выключена" : string.Format("{0} кгс/см2", r10.value), date));

            var r11 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 33, new byte[] { 22 })));
            if (!r11.success) return r11;
            constants.records.Add(MakeConstRecord("эмуляция канала T", (r11.value == -800) ? "выключена" : string.Format("{0} град. Ц", r11.value), date));

            var r12 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 28 })));
            if (!r12.success) return r12;
            constants.records.Add(MakeConstRecord("метод расчета коэфф.сжимаемости газа", (r12.value == 0) ? "NX19m" : (r12.value == 1) ? "GERG91" : string.Format("другое ({0})", r12.value), date));

            var r13 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 29 })));
            if (!r13.success) return r13;
            constants.records.Add(MakeConstRecord("тип термодатчика", (r13.value == 0) ? "100М" : (r13.value == 1) ? "50М" : (r13.value == 2) ? "100П" : (r13.value == 3) ? "50П" : string.Format("другое ({0})", r13.value), date));

            var r14 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 30 })));
            if (!r14.success) return r14;
            constants.records.Add(MakeConstRecord("эмуляция канала измерения скорости", (r14.value == -800) ? "выключена" : string.Format("{0} м/сек", r14.value), date));

            var r15 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 32 })));
            if (!r15.success) return r15;
            constants.records.Add(MakeConstRecord("цикл измерения", string.Format("{0}", r15.value), date));

            var r16 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 42 })));
            if (!r16.success) return r16;
            constants.records.Add(MakeConstRecord("заводской номер прибора", string.Format("{0}", r16.value), date));

            var r17 = ParseAsULong(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 64 })));
            if (!r17.success) return r17;
            constants.records.Add(MakeConstRecord("Направление потока", (r17.value == 0) ? "прямое" : (r17.value == 1) ? "обратное" : (r17.value == 2) ? "автовыбор (реверс)" : string.Format("другое ({0})", r17.value), date));

            var r18 = ParseAsFloat(Send(MakeRequest(Direction.MasterToSlave, na, 136, new byte[] { 121 })));
            if (!r18.success) return r18;
            constants.records.Add(MakeConstRecord("базовое расстояние в канале B", string.Format("{0} мм", r18.value), date));

            return constants;
        }
    }
}

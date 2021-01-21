using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        dynamic GetCurrent(DateTime date)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();
            var cur = Send(MakeBaseRequest0X03(3540, 109));
            if (!cur.success) return cur;

            IEnumerable<byte> dataCurM = (IEnumerable<byte>)cur.Body;
            byte[] byteDataCurM = dataCurM.Take(6).ToArray();
            DateTime dtDataM = new DateTime(2000 + byteDataCurM[3], byteDataCurM[0], byteDataCurM[1], byteDataCurM[2], byteDataCurM[5], byteDataCurM[4]);
            log(string.Format("Текущие мгновенные данные за {0: dd.MM.yyyy HH:mm:ss}", dtDataM), level: 3);
            Single t1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataCurM.Skip(6).Take(4).ToArray()), 0);
            Single P1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataCurM.Skip(30).Take(4).ToArray()), 0) * 10;
            Single t2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataCurM.Skip(10).Take(4).ToArray()), 0);
            Single P2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataCurM.Skip(34).Take(4).ToArray()), 0) * 10;
            
            var curF = Send(MakeBaseRequest0X03(3412, 109));
            if (!curF.success) return curF;
            IEnumerable<byte> dataCurFinal = (IEnumerable<byte>)curF.Body;
            byte[] byteDataCurF = dataCurFinal.Take(6).ToArray();
            DateTime dtDataF = new DateTime(2000 + byteDataCurF[3], byteDataCurF[0], byteDataCurF[1], byteDataCurF[2], byteDataCurF[5], byteDataCurF[4]);
            log(string.Format("Текущие итоговые данные за {0: dd.MM.yyyy HH:mm:ss}", dtDataM), level: 3);
            double V1 = BitConverter.ToDouble(Helper.Reverse8Bytes(dataCurFinal.Skip(6).Take(8).ToArray()), 0);
            double M1 = BitConverter.ToDouble(Helper.Reverse8Bytes(dataCurFinal.Skip(14).Take(8).ToArray()), 0);
            double V2 = BitConverter.ToDouble(Helper.Reverse8Bytes(dataCurFinal.Skip(22).Take(8).ToArray()), 0);
            double M2 = BitConverter.ToDouble(Helper.Reverse8Bytes(dataCurFinal.Skip(30).Take(8).ToArray()), 0);
            double dM = M1 - M2;
            double Qtv = BitConverter.ToDouble(Helper.Reverse8Bytes(dataCurFinal.Skip(110).Take(8).ToArray()), 0) * 0.239006;
            Int16 BHP = BitConverter.ToInt16(dataCurFinal.Skip(134).Take(2).Reverse().ToArray(), 0);
            Int16 BOC = BitConverter.ToInt16(dataCurFinal.Skip(136).Take(2).Reverse().ToArray(), 0);

            log(string.Format("t1 = {0}; p1 = {1}; v1 = {2}; m1 = {3};", t1, P1, V1, M1), level: 3);
            log(string.Format("t2 = {0}; p2 = {1}; v2 = {2}; m2 = {3};", t2, P2, V2, M2), level: 3);
            log(string.Format("dM = {0}; Qтв = {1}; BHP = {2}; BOC = {3};", dM, Qtv, BHP, BOC), level: 3);

            records.Add(MakeCurrentRecord("t1", t1, "°C", dtDataF));
            records.Add(MakeCurrentRecord("P1", P1, "кгс/см2", dtDataF));
            records.Add(MakeCurrentRecord("V1", V1, "м3", dtDataF));
            records.Add(MakeCurrentRecord("M1", M1, "т", dtDataF));
            records.Add(MakeCurrentRecord("t2", t2, "°C", dtDataF));
            records.Add(MakeCurrentRecord("P2", P2, "кгс/см2", dtDataF));
            records.Add(MakeCurrentRecord("V2", V2, "м3", dtDataF));
            records.Add(MakeCurrentRecord("M2", M2, "т", dtDataF));
            records.Add(MakeCurrentRecord("dM", dM, "т", dtDataF));
            records.Add(MakeCurrentRecord("Qтв", Qtv, "Гкал", dtDataF));
            records.Add(MakeCurrentRecord("ВНР", BHP, "ч", dtDataF));
            records.Add(MakeCurrentRecord("ВОС", BOC, "ч", dtDataF));

            setIndicationForRowCache(Qtv, "Гкал", dtDataF);
            current.records = records;
            return current;
        }
    }
}

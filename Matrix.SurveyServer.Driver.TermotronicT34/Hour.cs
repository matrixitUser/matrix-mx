// закомментируйте следующую строку, если ДАННЫЕ_ЗА_ЭТОТ_ЧАС = (ЭТОТ_ЧАС:30 + СЛЕД_ЧАС:00) / 2,
//*#define HALF_NEXT // ДАННЫЕ_ЗА_ЭТОТ_ЧАС = (ЭТОТ_ЧАС:00 + ЭТОТ_ЧАС:30) / 2   //Закомментировал 27/06/1917 ЭСКБ-Айрат утверждает, что это усредненная мощность выдается для конца периода

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
        private dynamic GetHours(DateTime start, DateTime end, DateTime current, byte version)//, dynamic variant)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var halfs = new List<dynamic>();
            var hours = new List<dynamic>();
            var status = new Dictionary<DateTime, int>();
            DateTime dt1 = start;
            DateTime dt2 = end;
            bool state = true;
            while (state)
            {
                byte[] registersForWrit = { (byte)dt1.Month, (byte)dt1.Day, (byte)dt1.Hour, (byte)(dt1.Year - 2000), 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                var last = Send(MakeBaseRequest0X48(2740, 109, 99, 6, 12, 0, registersForWrit));
                if (!last.success) return last;

                IEnumerable<byte> dataHour = (IEnumerable<byte>)last.Body;
                byte[] byteDataHour = dataHour.Take(4).ToArray();
                DateTime dtData = new DateTime(2000 + byteDataHour[3], byteDataHour[0], byteDataHour[1], byteDataHour[2], 0, 0);
                log(string.Format("Часовые данные за {0: dd.MM.yyyy HH:mm}", dtData), level: 3);

                Single t1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(4).Take(4).ToArray()), 0);
                Single P1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(8).Take(4).ToArray()), 0) * 10;
                Single V1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(12).Take(4).ToArray()), 0);
                Single M1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(16).Take(4).ToArray()), 0);
                Single t2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(20).Take(4).ToArray()), 0);
                Single P2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(24).Take(4).ToArray()), 0) * 10;
                Single V2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(28).Take(4).ToArray()), 0);
                Single M2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(32).Take(4).ToArray()), 0);
                Single dt = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(112).Take(4).ToArray()), 0);
                Single dM = M1 - M2;
                Single Qtv = BitConverter.ToSingle(Helper.Reverse4Bytes(dataHour.Skip(120).Take(4).ToArray()), 0) * (Single)0.239006;
                Int16 BHP = BitConverter.ToInt16(dataHour.Skip(132).Take(2).Reverse().ToArray(), 0);
                Int16 BOC = BitConverter.ToInt16(dataHour.Skip(134).Take(2).Reverse().ToArray(), 0);

                log(string.Format("t1 = {0}; p1 = {1}; v1 = {2}; m1 = {3};", t1, P1, V1, M1), level: 3);
                log(string.Format("t2 = {0}; p2 = {1}; v2 = {2}; m2 = {3};", t2, P2, V2, M2), level: 3);
                log(string.Format("dt = {0}; dM = {1}; Qтв = {2}; BHP = {3}; BOC = {4};", dt, dM, Qtv, BHP, BOC), level: 3);

                string NCtoTube1 = NCToTUBE(Helper.BitsMask(dataHour.ToArray()[177], 8));
                string NCtoTube2 = NCToTUBE(Helper.BitsMask(dataHour.ToArray()[176], 8));

                log(string.Format("НС по Трубе 1: {0}", NCtoTube1), level: 3);
                log(string.Format("НС по Трубе 2: {0}", NCtoTube2), level: 3);
                Int16 int16NCtoTV = BitConverter.ToInt16(dataHour.Skip(182).Take(2).Reverse().ToArray(), 0);
                string NCtoTV1 = NCToTV(Helper.BitsMask(int16NCtoTV, 16));
                log(string.Format("НС по ТВ1: {0}", NCtoTV1), level: 3);
              
                hours.Add(MakeHourRecord("t1", t1, "°C", dtData));
                hours.Add(MakeHourRecord("P1", P1, "кгс/см2", dtData));
                hours.Add(MakeHourRecord("V1", V1, "м3", dtData));
                hours.Add(MakeHourRecord("M1", M1, "т", dtData));
                hours.Add(MakeHourRecord("t2", t2, "°C", dtData));
                hours.Add(MakeHourRecord("P2", P2, "кгс/см2", dtData));
                hours.Add(MakeHourRecord("V2", V2, "м3", dtData));
                hours.Add(MakeHourRecord("M2", M2, "т", dtData));
                hours.Add(MakeHourRecord("dt", dt, "°C", dtData));
                hours.Add(MakeHourRecord("dM", dM, "т", dtData));
                hours.Add(MakeHourRecord("Qтв", Qtv, "Гкал", dtData));
                hours.Add(MakeHourRecord("ВНР", BHP, "ч", dtData));
                hours.Add(MakeHourRecord("ВОС", BOC, "ч", dtData));
                /*
                hours.Add(MakeHourRecord("t1", t1, "°C", dt1));
                hours.Add(MakeHourRecord("P1", P1, "кгс/см2", dt1));
                hours.Add(MakeHourRecord("V1", V1, "м3", dt1));
                hours.Add(MakeHourRecord("M1", M1, "т", dt1));
                hours.Add(MakeHourRecord("t2", t2, "°C", dt1));
                hours.Add(MakeHourRecord("P2", P2, "кгс/см2", dt1));
                hours.Add(MakeHourRecord("V2", V2, "м3", dt1));
                hours.Add(MakeHourRecord("M2", M2, "т", dt1));
                hours.Add(MakeHourRecord("dt", dt, "°C", dt1));
                hours.Add(MakeHourRecord("dM", dM, "т", dt1));
                hours.Add(MakeHourRecord("Qтв", Qtv, "Гкал", dt1));
                hours.Add(MakeHourRecord("ВНР", BHP, "ч", dt1));
                hours.Add(MakeHourRecord("ВОС", BOC, "ч", dt1));*/
                dt1 = dt1.AddHours(1);
                if (dt1.Month == dt2.Month && dt1.Day == dt2.Day && dt1.Hour == dt2.Hour)
                    state = false;
            }
            records(hours);
            return archive;
        }
      
    }
}

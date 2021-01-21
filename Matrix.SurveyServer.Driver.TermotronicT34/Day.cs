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
        dynamic GetDays(DateTime start, DateTime end, DateTime currentDt, byte version)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var days = new List<dynamic>();
            if (cancel())
            {
                archive.success = false;
                archive.error = "Опрос отменен";
                archive.errorcode = DeviceError.NO_ERROR;
                return archive;
            }
            DateTime dt1 = start;
            DateTime dt2 = end;
            bool state = true;
            while (state)
            {
                byte[] registersForWrit = { (byte)dt1.Month, (byte)dt1.Day, (byte)dt1.Hour, (byte)(dt1.Year - 2000), 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 };
                var response
                    = Send(MakeBaseRequest0X48(2740, 109, 99, 6, 12, 0, registersForWrit));
                if (!response.success) return response;
                
                IEnumerable<byte> dataDay = (IEnumerable<byte>)response.Body;
                byte[] byteDataDay = dataDay.Take(4).ToArray();
                DateTime dtData = new DateTime(2000 + byteDataDay[3], byteDataDay[0], byteDataDay[1], 0, 0, 0);
                log(string.Format("Суточные данные за {0: dd.MM.yyyy HH:mm}", dtData), level: 3);

                Single t1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(4).Take(4).ToArray()), 0);
                Single P1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(8).Take(4).ToArray()), 0) * 10;
                Single V1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(12).Take(4).ToArray()), 0);
                Single M1 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(16).Take(4).ToArray()), 0);
                Single t2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(20).Take(4).ToArray()), 0);
                Single P2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(24).Take(4).ToArray()), 0) * 10;
                Single V2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(28).Take(4).ToArray()), 0);
                Single M2 = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(32).Take(4).ToArray()), 0);
                Single dt = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(112).Take(4).ToArray()), 0);
                Single dM = M1 - M2;
                Single Qtv = BitConverter.ToSingle(Helper.Reverse4Bytes(dataDay.Skip(120).Take(4).ToArray()), 0) * (Single)0.239006;
                Int16 BHP = BitConverter.ToInt16(dataDay.Skip(132).Take(2).Reverse().ToArray(), 0);
                Int16 BOC = BitConverter.ToInt16(dataDay.Skip(134).Take(2).Reverse().ToArray(), 0);

                log(string.Format("t1 = {0}; p1 = {1}; v1 = {2}; m1 = {3};", t1, P1, V1, M1), level: 3);
                log(string.Format("t2 = {0}; p2 = {1}; v2 = {2}; m2 = {3};", t2, P2, V2, M2), level: 3);
                log(string.Format("dt = {0}; dM = {1}; Qтв = {2}; BHP = {3}; BOC = {4};", dt, dM, Qtv, BHP, BOC), level: 3);

                string NCtoTube1 = NCToTUBE(Helper.BitsMask(dataDay.ToArray()[177], 8));
                string NCtoTube2 = NCToTUBE(Helper.BitsMask(dataDay.ToArray()[176], 8));

                log(string.Format("НС по Трубе 1: {0}", NCtoTube1), level: 3);
                log(string.Format("НС по Трубе 2: {0}", NCtoTube2), level: 3);
                Int16 int16NCtoTV = BitConverter.ToInt16(dataDay.Skip(182).Take(2).Reverse().ToArray(), 0);
                string NCtoTV1 = NCToTV(Helper.BitsMask(int16NCtoTV, 16));
                log(string.Format("НС по ТВ1: {0}", NCtoTV1), level: 3);
               
                days.Add(MakeDayRecord("t1", t1, "°C", dtData));
                days.Add(MakeDayRecord("P1", P1, "кгс/см2", dtData));
                days.Add(MakeDayRecord("V1", V1, "м3", dtData));
                days.Add(MakeDayRecord("M1", M1, "т", dtData));
                days.Add(MakeDayRecord("t2", t2, "°C", dtData));
                days.Add(MakeDayRecord("P2", P2, "кгс/см2", dtData));
                days.Add(MakeDayRecord("V2", V2, "м3", dtData));
                days.Add(MakeDayRecord("M2", M2, "т", dtData));
                days.Add(MakeDayRecord("dt", dt, "°C", dtData));
                days.Add(MakeDayRecord("dM", dM, "т", dtData));
                days.Add(MakeDayRecord("Qтв", Qtv, "Гкал", dtData));
                days.Add(MakeDayRecord("ВНР", BHP, "ч", dtData));
                days.Add(MakeDayRecord("ВОС", BOC, "ч", dtData));

                dt1 = dt1.AddDays(1);
                if (dt1.Month == dt2.Month && dt1.Day == dt2.Day)
                    state = false;
            }
            records(days);

            archive.records = days;
            return archive;
        }
    }
}

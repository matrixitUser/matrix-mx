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
        private dynamic GetFinalArchive(DateTime start, DateTime end, DateTime current, byte version, string type)//, dynamic variant)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var halfs = new List<dynamic>();
            var resp = new List<dynamic>();
            var status = new Dictionary<DateTime, int>();
            DateTime dt1 = start;
            DateTime dt2 = end;
            bool state = true;
            while (state)
            {
                byte[] registersForWrit = { (byte)dt1.Month, (byte)dt1.Day, (byte)dt1.Hour, (byte)(dt1.Year - 2000), 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00 };
                var last = Send(MakeBaseRequest0X48(2868, 109, 99, 6, 12, 0, registersForWrit));
                if (!last.success) return last;

                IEnumerable<byte> dataHour = (IEnumerable<byte>)last.Body;
                byte[] byteDataHour = dataHour.Take(4).ToArray();
                
                if(type == "Hour")
                {
                    DateTime dtData = new DateTime(2000 + byteDataHour[3], byteDataHour[0], byteDataHour[1], byteDataHour[2], 0, 0);
                    log(string.Format("Итоговые данные за {0: dd.MM.yyyy HH:mm}", dtData), level: 3);
                    double V1s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(4).Take(8).ToArray()), 0);
                    double M1s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(12).Take(8).ToArray()), 0);
                    double V2s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(20).Take(8).ToArray()), 0);
                    double M2s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(28).Take(8).ToArray()), 0);
                    double dMs = M1s - M2s;
                    double Qtvs = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(108).Take(8).ToArray()), 0) * (Single)0.239006;
                    Int16 BHPs = BitConverter.ToInt16(dataHour.Skip(132).Take(2).Reverse().ToArray(), 0);
                    Int16 BOCs = BitConverter.ToInt16(dataHour.Skip(134).Take(2).Reverse().ToArray(), 0);

                    log(string.Format("v1s = {0}; m1s = {1}; v2s = {2}; m2s = {3};", V1s, M1s, V2s, M2s), level: 3);
                    log(string.Format("dMs = {0}; Qтвs = {1}; BHPs = {2}; BOCs = {3};", dMs, Qtvs, BHPs, BOCs), level: 3);

                    log(string.Format("зашел в type==Hour"), level: 3);
                    resp.Add(MakeHourRecord("V1s", V1s, "м3", dtData));
                    resp.Add(MakeHourRecord("M1s", M1s, "т", dtData));
                    resp.Add(MakeHourRecord("V2s", V2s, "м3", dtData));
                    resp.Add(MakeHourRecord("M2s", M2s, "т", dtData));
                    resp.Add(MakeHourRecord("dMs", dMs, "т", dtData));
                    resp.Add(MakeHourRecord("Qтвs", Qtvs, "Гкал", dtData));
                    resp.Add(MakeHourRecord("ВНРs", BHPs, "ч", dtData));
                    resp.Add(MakeHourRecord("ВОСs", BOCs, "ч", dtData));
                    dt1 = dt1.AddHours(1);
                    if (dt1.Month == dt2.Month && dt1.Day == dt2.Day && dt1.Hour == dt2.Hour)
                        state = false;
                }
                if(type == "Day")
                {
                    DateTime dtData = new DateTime(2000 + byteDataHour[3], byteDataHour[0], byteDataHour[1], 0, 0, 0);
                    log(string.Format("Итоговые данные за {0: dd.MM.yyyy HH:mm}", dtData), level: 3);
                    double V1s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(4).Take(8).ToArray()), 0);
                    double M1s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(12).Take(8).ToArray()), 0);
                    double V2s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(20).Take(8).ToArray()), 0);
                    double M2s = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(28).Take(8).ToArray()), 0);
                    double dMs = M1s - M2s;
                    double Qtvs = BitConverter.ToDouble(Helper.Reverse8Bytes(dataHour.Skip(108).Take(8).ToArray()), 0) * (Single)0.239006;
                    Int16 BHPs = BitConverter.ToInt16(dataHour.Skip(132).Take(2).Reverse().ToArray(), 0);
                    Int16 BOCs = BitConverter.ToInt16(dataHour.Skip(134).Take(2).Reverse().ToArray(), 0);

                    log(string.Format("v1s = {0}; m1s = {1}; v2s = {2}; m2s = {3};", V1s, M1s, V2s, M2s), level: 3);
                    log(string.Format("dMs = {0}; Qтвs = {1}; BHPs = {2}; BOCs = {3};", dMs, Qtvs, BHPs, BOCs), level: 3);
                    resp.Add(MakeDayRecord("V1s", V1s, "м3", dtData));
                    resp.Add(MakeDayRecord("M1s", M1s, "т", dtData));
                    resp.Add(MakeDayRecord("V2s", V2s, "м3", dtData));
                    resp.Add(MakeDayRecord("M2s", M2s, "т", dtData));
                    resp.Add(MakeDayRecord("dMs", dMs, "т", dtData));
                    resp.Add(MakeDayRecord("Qтвs", Qtvs, "Гкал", dtData));
                    resp.Add(MakeDayRecord("ВНРs", BHPs, "ч", dtData));
                    resp.Add(MakeDayRecord("ВОСs", BOCs, "ч", dtData));
                    dt1 = dt1.AddDays(1);
                    if (dt1.Month == dt2.Month && dt1.Day == dt2.Day)
                        state = false;
                }
                
            }
            records(resp);
            return archive;
        }
      
    }
}

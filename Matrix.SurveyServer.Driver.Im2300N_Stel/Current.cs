using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {

        private dynamic GetCurrent(byte na, string version, dynamic passport)
        {
            var length = 120 + 8;

            if (version.Contains("Z") || version.Contains("X") || version.Contains("T") || version.Contains("W"))
            {
                length = 80 + 8;
            }
            var bytes = GetBlocks(na, 0x91, 1, 1, length);

            if (bytes == null || !bytes.Any() || bytes.Length < length - 1)
            {
                dynamic current = new ExpandoObject();
                current.success = false;
                current.error = "недостаточно данных для разбора";
                return current;
            }

            return ParseCurrent(bytes, version, passport);
        }

        private dynamic ParseCurrent(byte[] bytes, string version, dynamic passport)
        {
            dynamic current = new ExpandoObject();
            current.success = true;

            if (bytes == null || !bytes.Any())
            {
                current.success = false;
                current.error = "недостаточно данных для разбора";
                return current;
            }

            current.records = new List<dynamic>();

            var channelCount = 24;
            int n = 120;
            if (version.Contains("Z") || version.Contains("X") || version.Contains("T") || version.Contains("W"))
            {
                n = 80;
                channelCount = 16;
            }

            var length = bytes.Length;
            // log(string.Format("{0}: {1}", length, string.Join(" ", bytes.Skip(n).Select(x => x.ToString("X2")))));
            try
            {
                var year = DateTime.Today.Year;
                var archiveRecordYearLeapOffset = ((bytes[n - 1 + 5] & 0xc0) >> 6);
                var curYearLeapOffset = year % 4;
                var archiveRecordYear = year - curYearLeapOffset + archiveRecordYearLeapOffset - ((archiveRecordYearLeapOffset > curYearLeapOffset) ? 4 : 0);

                var mon = BinDecToInt((byte)(bytes[n - 1 + 6] & 0x1f));
                var day = BinDecToInt((byte)(bytes[n - 1 + 5] & 0x3f));
                var hour = BinDecToInt(bytes[n - 1 + 4]);
                var min = BinDecToInt(bytes[n - 1 + 3]);
                var sec = BinDecToInt(bytes[n - 1 + 2]);

                log(string.Format("текущее время прибора {0}.{1}.{2}  {3}:{4}:{5}", day, mon, archiveRecordYear, hour, min, sec));


                current.date = new DateTime(archiveRecordYear,
                    BinDecToInt((byte)(bytes[n - 1 + 6] & 0x1f)), //mon
                    BinDecToInt((byte)(bytes[n - 1 + 5] & 0x3f)), //day
                    BinDecToInt(bytes[n - 1 + 4]), //hh
                    BinDecToInt(bytes[n - 1 + 3]), //mm
                    BinDecToInt(bytes[n - 1 + 2]) //sec
                   );
            }
            catch (Exception ex)
            {
                current.success = false;
                current.error = ex.Message;
                return current;
            }

            for (int i = 0; i < channelCount; i++)
            {
                //SPBT двоично-дес Остальные float
                var channel = passport.channels[i];
                if (channel.isOff())
                {
                    continue;
                }

                int offset = i * 5;
                double value = 0;
                if (channel.isSummed)
                {
                    var x1 = (float)BinDecToInt(bytes[offset + 3]) * 10000f;
                    var x2 = (float)BinDecToInt(bytes[offset + 2]) * 100f;
                    var x3 = (float)BinDecToInt(bytes[offset + 1]) * 1f;
                    var x4 = (float)BinDecToInt(bytes[offset + 0]) / 100f;
                    value = x1 + x2 + x3 + x4;
                }
                else
                {
                    value = BitConverter.ToSingle(bytes, offset + 0) / 2f;
                }

                current.records.Add(MakeCurrentRecord(string.Format("{0}{1}", channel.name, channel.number), value, channel.unit, current.date));
            }

            return current;
        }


    }
}

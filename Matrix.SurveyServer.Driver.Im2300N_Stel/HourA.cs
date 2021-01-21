using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetHourA(byte na, dynamic passport, DateTime start, DateTime end, DateTime current)
        {
            var nRegChn = (passport.channels as IEnumerable<dynamic>).Where(c => !c.notUsed).Count();
            var lReg = 4 * (passport.channelCount + 1);
            var lBlock = 772;

            var recsInBlock = lBlock / lReg;

            var hours = (int)(current - start).TotalHours;
            byte far = (byte)(hours / recsInBlock + 1);

            var nearHours = (int)(current - end).TotalHours;
            var near = nearHours / recsInBlock + 1;

            log(string.Format("длина записи {0}, длина блока {1}, всего запрошено часов {2}, дальнаяя запись {3}", lReg, lBlock, hours, far));

            return ParseHourA(GetBlocks(na, 0xcb, 1, far, 772), passport);
        }

        private dynamic ParseHourA(byte[] bytes, dynamic passport)
        {
            dynamic hour = new ExpandoObject();
            hour.success = true;

            hour.records = new List<dynamic>();

            if (bytes == null || bytes.Length < 772)
            {
                hour.success = false;
                hour.error = "недостаточно данных";
                return hour;
            }

            ///длина блока 772, из них 4 последних - служебные

            int blockLength = 772;
            int recordLength = passport.channelCount * 4;

            log(string.Format("длина блока {0}", blockLength));
            log(string.Format("длина записи {0}", recordLength));

            var offset = bytes.Length % blockLength;
            bytes = bytes.Skip(offset).ToArray();

            for (int blockStart = 0; blockStart < bytes.Length; blockStart += blockLength)
            {
                var blockData = bytes.Skip(blockStart).Take(blockLength).ToArray();
                for (int recordStart = 0; recordStart < blockLength - 4; recordStart += recordLength)
                {
                    var recordData = blockData.Skip(recordStart).Take(recordLength).ToArray();
                    var minutes = BitConverter.ToInt32(recordData, 0);
                    var date = new DateTime(2000, 1, 1).AddSeconds(minutes).AddHours(-1);
                    var channelOffset = 4;
                    foreach (var channel in passport.channels)
                    {
                        if (channel.isOff())
                        {
                            channelOffset += 4;
                            continue;
                        }
                        var value = BitConverter.ToSingle(recordData, channelOffset);
                        if (channel.name == Glossary.ts)
                            value = (float)GetHour(value);
                        hour.records.Add(MakeHourRecord(string.Format("{0}{1}", channel.name, channel.number), value, channel.unit, date));
                        channelOffset += 4;
                    }
                }
            }

            return hour;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetDayA(byte na, dynamic passport, DateTime start, DateTime end, DateTime current, bool is2318 = false)
        {
            var nRegChn = (passport.channels as IEnumerable<dynamic>).Where(c => !c.notUsed).Count();
            var lReg = 4 * (passport.channelCount + 1);
            var lBlock = 768;

            var recsInBlock = lBlock / lReg;

            var days = (int)(current - start).TotalDays;
            byte far = (byte)(days / recsInBlock + 1);///ACHTUNG

            var nearDays = (int)(current - end).TotalDays;
            byte near = (byte)(nearDays / recsInBlock );///ACHTUNG

            log(string.Format("длина записи {0}, длина блока {1}, всего запрошено суток {2}, дальняя запись {3}", lReg, lBlock, days, far));
            return ParseDayA(GetBlocks(na, 0xd4, near, far, lBlock), passport, is2318);
        }

        private dynamic ParseDayA(byte[] bytes, dynamic passport, bool is2318)
        {
            dynamic day = new ExpandoObject();
            day.success = true;

            if (bytes == null || bytes.Length < 768)
            {
                day.success = false;
                day.error = "недостаточно данных";
                return day;
            }
            day.records = new List<dynamic>();
            ///длина блока 772, из них 4 последних - служебные

            int blockLength = 772;
            int recordLength = passport.channelCount * 4;

            //log(string.Format("длина блока {0}", blockLength));
            //log(string.Format("длина записи {0}", recordLength));

            var offset = is2318 ? 0 : bytes.Length % blockLength;
            offset = 0;
            bytes = bytes.Skip(offset).ToArray();

            for (int blockStart = 0; blockStart < bytes.Length; blockStart += blockLength)
            {
                var blockData = bytes.Skip(blockStart).Take(blockLength).ToArray();

                //string path = @"D:\Im2300\";
                //string fileName = string.Format("{0}day_{1:dd-HH.mm.ss.fff}.txt", path, DateTime.Now);
                //System.IO.File.WriteAllText(fileName, string.Join(" ", blockData.Select(b => b.ToString("X2"))));

                for (int recordStart = 0; recordStart < blockLength - 4; recordStart += recordLength)
                {
                    var recordData = blockData.Skip(recordStart).Take(recordLength).ToArray();
                    var minutes = BitConverter.ToInt32(recordData, 0);
                    var date = new DateTime(2000, 1, 1).AddSeconds(minutes).AddDays(-1);
                    var channelOffset = 4;
                    foreach (var channel in (passport.channels as IEnumerable<dynamic>).Take((int)passport.channels.Count - 1))
                    {
                        if (channel.isOff())
                        {
                            channelOffset += 4;
                            continue;
                        }
                        var value = BitConverter.ToSingle(recordData, channelOffset);

                        day.records.Add(MakeDayRecord(string.Format("{0}{1}", DayParameterName(channel.name), channel.number), value, channel.unit, date));
                        channelOffset += 4;
                    }
                }
            }

            return day;
        }

        /// <summary>
        /// добавляет к имени префикс,
        /// нужно для того чтобы различать параметры часовых и суток
        /// так как у суток параметры уже не итоговые
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private string DayParameterName(string parameter)
        {
            switch (parameter)
            {
                case Glossary.tm:
                case Glossary.ts:
                case Glossary.Go:
                case Glossary.Gn:
                    return string.Format("Сут{0}", parameter);
                default:
                    return parameter;
            }
        }
    }
}

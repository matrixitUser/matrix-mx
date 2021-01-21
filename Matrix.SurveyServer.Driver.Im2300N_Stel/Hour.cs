using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetHour(byte na, string version, dynamic passport, DateTime start, DateTime end, DateTime current)
        {
            var nRegChn = (passport.channels as IEnumerable<dynamic>).Where(c => !c.notUsed).Count();
            var lReg = passport.lRec;
            var lBlock = passport.lBlock;

            var recsInBlock = lBlock / lReg;

            var hours = (int)(current - start).TotalHours;
            var far = hours / recsInBlock + 1;

            var nearHours = (int)(current - end).TotalHours;
            var near = nearHours / recsInBlock + 1;

            log(string.Format("длина записи {0}, длина блока {1}, всего запрошено часов {2}, дальнаяя запись {3}", lReg, passport.lBlock, hours, far));

            if (near == 1) near = 0;
            return ParseHour(GetBlocks(na, 0x9b, (byte)(near), (byte)(far), lBlock + 4), passport);
        }

        private dynamic ParseHour(byte[] bytes, dynamic passport)
        {
            //string path = string.Format(@"D:\ImLogs\ImHours_{0}.txt", Guid.NewGuid());
            //string text = string.Format("\r\nlBlock: {0}; lRec: {1}\r\n", passport.lBlock, passport.lRec);
            //text += string.Join(" ", bytes.Select(b => b.ToString("X2")));
            //File.WriteAllText(path, text);

            dynamic hour = new ExpandoObject();
            hour.success = true;

            if (bytes == null || !bytes.Any())
            {
                hour.success = false;
                hour.error = "нет данных для разбора";
                return hour;
            }

            hour.records = new List<dynamic>();


            int blockLength = passport.lBlock;
            int recordLength = passport.lRec;

            for (int blockStart = 0; blockStart < bytes.Length - (blockLength + 4); blockStart += (blockLength + 4))
            {
                var blockData = bytes.Skip(blockStart).Take(blockLength + 4).ToArray();

                blockData = blockData.Take(blockData.Length - 4).Reverse().ToArray();

                for (int recordStart = 0; recordStart < blockLength - 4; recordStart += recordLength)
                {
                    var recordData = blockData.Skip(recordStart).Take(recordLength).ToArray();

                    if (recordData.Length < recordLength)
                        continue;

                    try
                    {
                        var curYear = DateTime.Today.Year;
                        var archiveRecordYearLeapOffset = ((recordData[2] & 0xc0) >> 6);
                        var curYearLeapOffset = curYear % 4;
                        var year = curYear - curYearLeapOffset + archiveRecordYearLeapOffset - ((archiveRecordYearLeapOffset > curYearLeapOffset) ? 4 : 0);
                        var month = BinDecToInt((byte)(recordData[3] & 0x1f));
                        var day = BinDecToInt((byte)(recordData[2] & 0x3f));
                        var hour1 = BinDecToInt(recordData[1]);
                        var minute = BinDecToInt(recordData[0]);
                        //log(string.Format("year {0}, month {1}, day {2}, hour {3}, min {4}", year, month, day, hour1, minute));
                        
                        var date = new DateTime(year, month, day, hour1, minute, 0).AddHours(-1);                        
                        var channelOffset = 0;
                        foreach (var channel in passport.channels)
                        {
                            if (channel.isOff())
                                continue;

                            channelOffset++;

                            var channelData = recordData.Skip(channelOffset * 4).Take(4).ToArray();

                            if (channelData.Length < 4)
                            {
                                continue;
                            }

                            float value = 0;

                            if (channel.isSummed)
                            {
                                var x1 = (float)BinDecToInt(channelData[3]) * 10000f;
                                var x2 = (float)BinDecToInt(channelData[2]) * 100f;
                                var x3 = (float)BinDecToInt(channelData[1]) * 1f;
                                var x4 = (float)BinDecToInt(channelData[0]) / 100f;
                                value = x1 + x2 + x3 + x4;
                            }
                            else
                            {
                                value = BitConverter.ToSingle(channelData, 0) / 2f;
                            }

                            //value = value∙channel.koefficient;
                            var floorValue = Math.Round(value, channel.floor);

                            if (channel.name == Glossary.ts)
                                value = (float)GetHour(value);

                            var record = MakeHourRecord(string.Format("{0}{1}", channel.name, channel.number), value, channel.unit, date);
                            hour.records.Add(record);
                        }
                    }
                    catch (Exception ex)
                    {
                        log(string.Format("ошибка в часах {0}", ex.Message));
                    }
                }
            }

            if (hour.records.Count == 0)
            {
                hour.success = false;
                hour.error = "архивы не распознаны";
            }
            return hour;
        }
    }
}

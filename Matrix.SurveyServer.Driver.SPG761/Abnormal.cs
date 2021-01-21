using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        private bool IsEventImportant(int eventId)
        {
            return ((new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 21, 22, 23, 24, 29, 30, 31 }).Contains(eventId));
        }

        private dynamic GetAbnormal(byte dad, byte sad, bool needDad, DateTime start, DateTime end)
        {
            dynamic abnormal = new ExpandoObject();
            abnormal.success = true;
            abnormal.error = string.Empty;
            List<dynamic> records = new List<dynamic>();

            var archive = GetArhiveArray(dad, sad, needDad, "00", "098", start, end);
            if (!archive.success)
                return archive;

            foreach (var category in (archive.categories as IEnumerable<string[]>))
            {
                    log(string.Format("НС кат.: {0}", string.Join("|", category)), level: 3);

                if (category.Length != 3 || string.IsNullOrEmpty(category[2]))
                {
                    continue;
                }
                DateTime date = DateTime.Parse(category[2].Trim().Replace("/", " "));
                records.Add(MakeAbnormalRecord(-1, string.Format("{0}({1}), статус: {2}", category[1], category[0], category[0][0] == '+' ? "появилась" : "устранилась"), date));
            }
            abnormal.records = records;
            return abnormal;

            //byte index = 0;
            //byte step = 5;
            //DateTime last = DateTime.Now;
            //bool hasRecord = false;
            //do
            //{
            //    //var param = new string[] { "0", "098", index.ToString(), step.ToString() };
            //    dynamic answer = GetArray(na, pass, 0, 098, index, step);
            //    if (!answer.success)
            //        return answer;
            //    hasRecord = false;
            //    foreach (var category in (answer.categories as IEnumerable<string[]>))
            //    {
            //        if (category.Length != 3 || string.IsNullOrEmpty(category[2]))
            //        {
            //            //   log(string.Format("странная категория {0}", string.Join("; ", category)));
            //            continue;
            //        }
            //        DateTime date = DateTime.Parse(category[2].Trim().Replace("/", " "));
            //        last = date;
            //        // log(string.Format("нормальная категория {0}", string.Join("; ", category)));
            //        records.Add(MakeAbnormalRecord(string.Format("{0}({1}), статус: {2}", category[1], category[0], category[0][0] == '+' ? "появилась" : "устранилась"), date));
            //        hasRecord = true;
            //    }
            //    index += step;
            //}
            //while (index < 400 - step && last > lastAbnormal && hasRecord);

            //abnormal.records = records;
            //return abnormal;
        }

        private dynamic MakeAbnormalRecord(int eventId, string name, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = 0;
            record.i2 = eventId + (IsEventImportant(eventId) ? 1000 : 0);
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        //private byte[] MakeArchiveRequest(byte dad, byte sad, DateTime start, DateTime end, string param)
        //{
        //    var bytes = new List<byte>();
        //    bytes.AddRange(MakeHeader(dad, sad, 0x0e));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode("0"));
        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(param));
        //    bytes.Add(FF);

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Day.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Month.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Year.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Hour.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Minute.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(start.Second.ToString("00")));

        //    bytes.Add(FF);

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Day.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Month.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Year.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Hour.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Minute.ToString("00")));

        //    bytes.Add(HT);
        //    bytes.AddRange(Encode(end.Second.ToString("00")));
        //    bytes.Add(FF);
        //    bytes.Add(DLE);
        //    bytes.Add(ETX);

        //    var crc = CrcCalc(bytes.ToArray(), 2, bytes.Count - 2);
        //    bytes.AddRange(crc);
        //    return bytes.ToArray();
        //}
    }
}

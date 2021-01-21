using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
        private dynamic GetArchive(byte na, List<byte> chs, DateTime date, ArchiveType type, dynamic units)
        {
            TimeSpan span = TimeSpan.FromHours(1);
            if (type == ArchiveType.Day)
            {
                span = TimeSpan.FromDays(1);
            }
            
            DateTime start = date + span;
            DateTime end = date + span + span;
            //if (type == ArchiveType.Day)
            //{
            //    start = date.AddDays(2);
            //    end = date.AddDays(2);
            //}
            //if (type == ArchiveType.Hour)
            //{
            //    end = date.AddHours(1);
            //}

            dynamic answer = new ExpandoObject();
            //answer.isEmpty
            //answer.msgcode
            //answer.date
            var records = new List<dynamic>();

            foreach (var ch in chs)
            {
                dynamic archive = ReadArchive(na, ch, type, start, end);
                if (!archive.success) return archive;

                List<dynamic> archives = archive.archives;
                if (!archives.Any())
                {
                    // dynamic answer = new ExpandoObject();
                    archive.success = true;
                    archive.isEmpty = true;
                    archive.error = "архив за запрашиваемое время пуст?";
                    return archive;
                }

                var ret = ParseArchiveResponse(archives.First().body, date, type, units, ch);
                if (!ret.success || ret.isEmpty) return ret;

                answer.date = ret.date;
                records.AddRange(ret.records);
            }

            answer.isEmpty = false;
            answer.records = records;
            answer.error = "";
            answer.success = true;
            //

            return answer;
        }

        private dynamic ParseArchiveResponse(List<dynamic> parameters, DateTime date, ArchiveType type, dynamic units, byte ch)
        {
            dynamic archive = new ExpandoObject();
            archive.isEmpty = false;
            archive.success = true;
            archive.error = string.Empty;
            archive.date = date;

            List<dynamic> records = new List<dynamic>();

            //Среднее значение давления P3
            records.Add(MakeArchiveRecord(Glossary.P(3), parameters[3], units[Glossary.P(3)], archive.date, type));

            //Среднее значение перепада давления ∆Р3
            records.Add(MakeArchiveRecord(Glossary.dP(3), parameters[4], units[Glossary.dP(3)], archive.date, type));

            //Среднее значение перепада давления ∆Р4
            records.Add(MakeArchiveRecord(Glossary.dP(4), parameters[5], units[Glossary.dP(4)], archive.date, type));

            //Стандартный объем
            records.Add(MakeArchiveRecord(Glossary.Vс, parameters[7], "м³", archive.date, type));

            //Время интегрирования
            records.Add(MakeArchiveRecord(Glossary.ВНР, parameters[9], "ч", archive.date, type));


            ////Нештатные ситуации
            //records.Add(MakeArchiveRecord(Glossary.HC, parameters[10], "", archive.date, type));


            int offset = 0;
            if (ch == 0x02) offset = 8;

            //Среднее значение давления по каналу 
            records.Add(MakeArchiveRecord(Glossary.P(ch), parameters[11 + offset], units[Glossary.P(ch)], archive.date, type));

            //Среднее значение температуры по каналу
            records.Add(MakeArchiveRecord(Glossary.T(ch), parameters[12 + offset], "°C", archive.date, type));

            //Среднее значение перепада давления по каналу 
            records.Add(MakeArchiveRecord(Glossary.dP(ch), parameters[13 + offset], units[Glossary.dP(ch)], archive.date, type));

            //Рабочий объем по каналу
            records.Add(MakeArchiveRecord(Glossary.Vp(ch), parameters[14 + offset], "м³", archive.date, type));

            //Приведенный объем по каналу
            records.Add(MakeArchiveRecord(Glossary.V(ch), parameters[15 + offset], "м³", archive.date, type));

            //Допускаемый перепад давления по каналу
            records.Add(MakeArchiveRecord(Glossary.dPd(ch), parameters[16 + offset], units[Glossary.dP(ch)], archive.date, type));

            //Среднее значение коэффициента сжимаемости по каналу
            records.Add(MakeArchiveRecord(Glossary.Ksj(ch), parameters[17 + offset], "", archive.date, type));

            //Среднее значение коэффициента приведения по каналу
            records.Add(MakeArchiveRecord(Glossary.Kpr(ch), parameters[18 + offset], "", archive.date, type));

            archive.records = records;
            return archive;
        }

        private dynamic MakeArchiveRecord(string parameter, double value, string unit, DateTime date, ArchiveType type)
        {
            dynamic record = new ExpandoObject();
            if (type == ArchiveType.Hour)
                record.type = "Hour";

            if (type == ArchiveType.Day)
            {
                record.type = "Day";
                date = date.Date;
            }
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

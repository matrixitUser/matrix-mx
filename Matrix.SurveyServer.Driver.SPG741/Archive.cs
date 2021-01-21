using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG741
{
    enum ArchiveType
    {
        Day,
        Hour
    }

    public partial class Driver
    {
        private dynamic GetArchive(byte na, DateTime date, ArchiveType type, dynamic units)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            answer.code = -1;

            var bytes = Send(MakeArchiveRequest(na, date, type));
            if (bytes.Any())
                answer = ParseArchiveResponse(bytes, date, type, units);

            return answer;
        }

        private byte[] MakeArchiveRequest(byte na, DateTime date, ArchiveType type)
        {
            if (type == ArchiveType.Hour)
            {
                var hour = date.AddHours(1);
                return MakeShortRequest(na, 0x48, (byte)(hour.Year - 2000 + 100), (byte)hour.Month, (byte)hour.Day, (byte)hour.Hour);
            }
            if (type == ArchiveType.Day)
            {
                var day = date.AddDays(1);
                return MakeShortRequest(na, 0x59, (byte)(day.Year - 2000 + 100), (byte)day.Month, (byte)day.Day, 0x00);
            }

            return new byte[] { };
        }

        private dynamic ParseArchiveResponse(byte[] bytes, DateTime date, ArchiveType type, dynamic units)
        {
            byte code = 0x48;
            if (type == ArchiveType.Day)
                code = 0x59;
            dynamic archive = ParseShortResponse(bytes, code);
            if (!archive.success) return archive;


            archive.records = new List<dynamic>();
            archive.date = date;

            //Время счета			
            archive.records.Add(MakeArchiveRecord(Glossary.TC, Helper.SpgFloatToIEEE(archive.body, 0), "ч", archive.date, type));

            ////Сборка признаков НС, возникавших на интервале архивирования 
            //var emergency = BitConverter.ToInt32(bytes, 7);
            //Data.Add(new Data(Glossary.HC, MeasuringUnitType.Unknown, date, emergency));
            archive.records.Add(MakeArchiveRecord("НС4", archive.body[4], "", archive.date, type));
            archive.records.Add(MakeArchiveRecord("НС3", archive.body[5], "", archive.date, type));
            archive.records.Add(MakeArchiveRecord("НС2", archive.body[6], "", archive.date, type));
            archive.records.Add(MakeArchiveRecord("НС1", archive.body[7], "", archive.date, type));

            //Среднее давление газа 
            archive.records.Add(MakeArchiveRecord(Glossary.P1, Helper.SpgFloatToIEEE(archive.body, 8), units["Р1"], archive.date, type));

            //Средняя температура газа             
            archive.records.Add(MakeArchiveRecord(Glossary.t1, Helper.SpgFloatToIEEE(archive.body, 12), "°C", archive.date, type));

            //Интегральный объем газа в рабочих условиях
            archive.records.Add(MakeArchiveRecord(Glossary.Vp1, Helper.SpgFloatToIEEE(archive.body, 16), "м³", archive.date, type));

            //Интегральный объем газа, приведенный к стандартным условиям            
            archive.records.Add(MakeArchiveRecord(Glossary.V1, Helper.SpgFloatToIEEE(archive.body, 20), "м³", archive.date, type));

            //Среднее давление газа             
            archive.records.Add(MakeArchiveRecord(Glossary.P2, Helper.SpgFloatToIEEE(archive.body, 24), units["Р2"], archive.date, type));

            //Средняя температура газа             
            archive.records.Add(MakeArchiveRecord(Glossary.t2, Helper.SpgFloatToIEEE(archive.body, 28), "°C", archive.date, type));

            //Интегральный объем газа в рабочих условиях            
            archive.records.Add(MakeArchiveRecord(Glossary.Vp2, Helper.SpgFloatToIEEE(archive.body, 32), "м³", archive.date, type));

            //Интегральный объем газа, приведенный к стандартным условиям            
            archive.records.Add(MakeArchiveRecord(Glossary.V2, Helper.SpgFloatToIEEE(archive.body, 36), "м³", archive.date, type));

            archive.records.Add(MakeArchiveRecord(Glossary.V, Helper.SpgFloatToIEEE(archive.body, 40), "м³", archive.date, type));

            archive.records.Add(MakeArchiveRecord(Glossary.Vp, Helper.SpgFloatToIEEE(archive.body, 44), "м³", archive.date, type));

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

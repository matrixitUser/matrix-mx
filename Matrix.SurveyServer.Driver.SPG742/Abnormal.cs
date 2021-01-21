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
        private dynamic GetAbnormal(byte na, List<byte> chs, DateTime start, DateTime end)
        {
            byte count = 20;

            dynamic answer = new ExpandoObject();
            var records = new List<dynamic>();

            foreach (var ch in chs)
            {
                dynamic Event = ReadArchive(na, ch, ArchiveType.Event, start, end, count);
                if (!Event.success) return Event;

                List<dynamic> archives = Event.archives;
                if (!archives.Any())
                {
                    dynamic archive = new ExpandoObject();
                    archive.success = false;
                    archive.error = "архив за запрашиваемое время пуст?";
                    return archive;
                }

                var ret = ParseEventAbnormals(archives);
                if (!ret.success) return ret;

                records.AddRange(ret.records);
            }

            answer.success = true;
            answer.error = "";
            answer.records = records;

            return answer;
        }

        private dynamic ParseEventAbnormals(List<dynamic> archives)
        {
            dynamic answer1 = new ExpandoObject();
            answer1.success = true;
            answer1.error = string.Empty;

            List<dynamic> records = new List<dynamic>();
            answer1.records = records;

            foreach (var archive in archives)
            {
                //  log(string.Format("archive {0} {1}", archive.date, archive.body));
                int code = 0;
                if (!int.TryParse(archive.body.Substring(1, 2), out code))
                {
                    log(string.Format("трудности парсинга archive {0} {1}", archive.date, archive.body));
                    answer1.success = false;
                    answer1.error = "трудности парсинга archive";
                    return answer1;
                }
                string status = "возникло";
                if (archive.body[3] == '-')
                {
                    status = "устранено";
                }
                if (!situations.ContainsKey(code)) continue;
                string name = string.Format("{0}, статус: {1}", situations[code], status);

                records.Add(MakeAbnormalRecord(code, name, 1, archive.date));
            }
            return answer1;
        }

        private dynamic ParseAuditAbnormals(List<dynamic> archives)
        {
            dynamic answer1 = new ExpandoObject();
            answer1.success = true;
            answer1.error = string.Empty;

            List<dynamic> records = new List<dynamic>();
            answer1.records = records;

            foreach (var archive in archives)
            {
                log(string.Format("archive {0} {1}", archive.date, archive.body));
                //int code = 0;
                //if (!int.TryParse(archive.body.Substring(1, 2), out code))
                //{
                //    log(string.Format("трудности парсинга archive {0} {1}", archive.date, archive.body));
                //    answer1.success = false;
                //    answer1.error = "трудности парсинга archive";
                //    return answer1;
                //}
                //string status = "возникло";
                //if (archive.body[3] == '-')
                //{
                //    status = "устранено";
                //}
                //if (!situations.ContainsKey(code)) continue;
                //string name = string.Format("{0}, статус: {1}", situations[code], status);

                //records.Add(MakeAbnormalRecord(name, 1, archive.date));
            }
            return answer1;
        }


        private dynamic MakeAbnormalRecord(int eventId, string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        /// <summary>
        /// 742re страница 47
        /// </summary>
        private Dictionary<int, string> situations = new Dictionary<int, string>()
        {
	        {0, "Разряд батареи. Предупреждение об исчерпании ресурса встроенной батареи"},
            {1, "Частота входного сигнала на разъеме Х7 превышает 1,5 кГц"},
	        {2, "Частота входного сигнала на разъеме Х8 превышает 1,5 кГц"},
	        {3, "Изменение сигнала на дискретном входе"},
	        {4, "Рабочий расход Qр1 ниже нижнего предела"},
	        {5, "Рабочий расход Qр2 ниже нижнего предела"},
	        {6, "Рабочий расход Qр1 выше верхнего предела"},
	        {7, "Рабочий расход Qр2 выше верхнего предела"},
            {8, "Измеренное значение давления датчика P1 вышло за пределы диапазона 0,03…1,03 от верхнего предела измерений датчика"},
	        {9, "Измеренное значение давления датчика P2 вышло за пределы диапазона 0,03…1,03 от верхнего предела измерений датчика"},
	        {10, "Измеренное значение перепада давления ΔP1 вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {11, "Измеренное значение перепада давления ΔP2 вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {12, "Измеренное значение давления P3 вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {13, "Измеренное значение перепада давления ΔP3 вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {14, "Измеренное значение перепада давления ΔP4 вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {15, "Измеренное значение барометрического давления Pб вне пределов диапазона измерений датчика более, чем на 3 %"},
	        {16, "Измеренное значение температуры t1 вне пределов диапазона [-52...107]°С"},
	        {17, "Измеренное значение температуры t2 вне пределов диапазона [-52...107]°С"},
	        {18, "Значение контролируемого параметра, определяемого КУ1, вне пределов диапазона УН1...УВ1"},
	        {19, "Значение контролируемого параметра, определяемого КУ2, вне пределов диапазона УН2...УВ2"},
	        {20, "Значение контролируемого параметра, определяемого КУ3, вне пределов диапазона УН3...УВ3"},
	        {21, "Значение контролируемого параметра, определяемого КУ4, вне пределов диапазона УН4...УВ4"},
	        {22, "Значение контролируемого параметра, определяемого КУ5, вне пределов диапазона УН5...УВ5"},
	        {23, "Пропадание напряжения питания на разъеме X1"},
	        {25, "Объем выше нормы поставки"},
	        {26, "Некорректные вычисления по первому трубопроводу"},
	        {27, "Некорректные вычисления по второму трубопроводу"},
            {28, "Измеренное значение перепада давления ΔP1 превышает вычисленное предельное значение, при этом Qр1>НП/Qр1" }
        };
    }
}

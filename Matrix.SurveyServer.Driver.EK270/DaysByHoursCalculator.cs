using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.EK270
{
    class DaysByHoursCalculator
    {
        public IEnumerable<Record> Calculate(IEnumerable<Record> records)
        {
            var hour = 12;
            var days = new List<Record>();

            foreach (var gr in records.GroupBy(r => r.S1))
            {                
                Console.WriteLine("рассматриваем {0}", gr.Key);
                switch (gr.Key)
                {
                    case Glossary.pMP:
                    case Glossary.TMP:
                    case Glossary.KMP:
                    case Glossary.CMP:
                        {
                            var sum = 0.0;

                            var tmpl = gr.First();
                            //days.Add(new Record()
                            //{
                            //    Date = gr.Key.Date,
                            //    D1 = gr.Average(r => r.D1),
                            //    Dt1 = DateTime.Now,
                            //    S1 = gr.Key.S1,
                            //    S2 = tmpl.S2,
                            //    I1 = 1,
                            //    Type = "Day"
                            //});
                            break;
                        }
                    case Glossary.Vb:
                    case Glossary.VbT:
                    case Glossary.V:
                    case Glossary.Vo:
                        {
                            //var tmpl = gr.First();
                            //days.Add(new Record()
                            //{
                            //    Date = gr.Key.Date,
                            //    D1 = gr.Last().D1,
                            //    Dt1 = DateTime.Now,
                            //    S1 = gr.Key.S1,
                            //    S2 = tmpl.S2,
                            //    I1 = 1,
                            //    Type = "Day"
                            //});
                            break;
                        }
                }
            }
            return days;
        }
    }
}

/*
public static string GONo { get { return "Глобальный номер"; } }
		public static string AONo { get { return "Номер в архиве"; } }
		public static string Vb { get { return "Стандартный объем"; } }
		public static string VbT { get { return "Стандартный объем общий"; } }
		public static string V { get { return "Рабочий объем"; } }
		public static string Vo { get { return "Рабочий объем общий"; } }
		public static string pMP { get { return "Ср. давление за интервал"; } }
		public static string TMP { get { return "Ср. температура за интервал"; } }
		public static string KMP { get { return "Ср. значение K за интервал"; } }
		public static string CMP { get { return "Ср. значение C за интервал"; } }
		public static string St2 { get { return "Статус 2 (вкл. Vb)"; } }
		public static string St4 { get { return "Статус 4 (вкл. V)"; } }
		public static string St7 { get { return "Статус 7 (вкл. p)"; } }
		public static string St6 { get { return "Статус 6 (вкл. T)"; } }
		public static string StSy { get { return "Системный статус"; } }
		public static string StAe { get { return "Событие, вызвавшее запись в архив"; } }

		public static string dpTe { get { return "dpTe"; } }
		public static string T2Tek { get { return "T2Tek"; } }


*/
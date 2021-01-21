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
        private dynamic GetCurrent(byte na, List<byte> chs, dynamic units)
        {
            var time = GetCurrentTime(na);
            if (!time.success)
                return time;

            dynamic currents = new ExpandoObject();
            var date = DateTime.MinValue;
            var records = new List<dynamic>();

            //оборачиваем current'ы в currents 
            foreach (var ch in chs)
            {
                dynamic current = GetPrivateCurrent(na, ch, units, time.date);
                if (!current.success) return current;

                records.AddRange(current.records);
                date = current.date;
            }

            currents.success = true;
            currents.error = string.Empty;
            currents.records = records;
            currents.date = date;
            //

            return currents;
        }

        private dynamic GetCurrentTime(byte na)
        {
            short[] array = new short[] { 1024, 1025 };
            dynamic answer = ReadParameters(na, 0x00, array);
            if (!answer.success) return answer;

            var parameters = answer.parameters;
            DateTime date = (DateTime)parameters[1] + (TimeSpan)parameters[0];

            dynamic time = new ExpandoObject();
            time.success = true;
            time.error = string.Empty;
            time.date = date;
            return time;
        }

        private dynamic GetPrivateCurrent(byte na, byte ch, dynamic dunits, DateTime date)
        {
            var units = (Dictionary<string, string>)dunits;
            log(string.Format("Приватные курренты на {0} канал {1} юнитов {2}", date, ch, units.Count), level: 3);

            dynamic answer = ReadParameters(na, ch, PrivateCurrentParameters.Keys.ToArray());
            if (!answer.success) return answer;

            dynamic current = new ExpandoObject();
            List<dynamic> records = new List<dynamic>();
            List<dynamic> parameters = answer.parameters;
            if (parameters.Count != 10)
            {
                current.success = false;
                current.error = "получены не все необходимые параметры";
                return current;
            }

            log(string.Format("Параметры({0}): {1}", parameters.Count, string.Join("; ", parameters.Select(r => r.ToString()).ToArray())), level: 3);

            double p = 0.0;

            if (double.TryParse(parameters[0].ToString(), out p))
            {
                records.Add(MakeCurrentRecord(Glossary.Vp(ch), p, "м³", date));
            }

            if (double.TryParse(parameters[1].ToString(), out p))
            {
                records.Add(MakeCurrentRecord(Glossary.V(ch), p, "м³", date));
            }

            if (double.TryParse(parameters[2].ToString(), out p))
            {
                var unit = units.ContainsKey(Glossary.P(ch)) ? units[Glossary.P(ch)] : "";
                records.Add(MakeCurrentRecord(Glossary.P(ch), p, unit, date));
            }

            if (double.TryParse(parameters[3].ToString(), out p))
            {
                records.Add(MakeCurrentRecord(Glossary.T(ch), p, "°C", date));
            }

            if (double.TryParse(parameters[4].ToString(), out p))
            {
                var unit = units.ContainsKey(Glossary.dP(ch)) ? units[Glossary.dP(ch)] : "";
                records.Add(MakeCurrentRecord(Glossary.dP(ch), p, unit, date));
            }

            if (double.TryParse(parameters[5].ToString(), out p))
            {
                records.Add(MakeCurrentRecord(Glossary.Vрч(ch), p, "м³", date));
            }

            if (double.TryParse(parameters[6].ToString(), out p))
            {
                records.Add(MakeCurrentRecord(Glossary.Vч(ch), p, "м³", date));
            }


            current.success = true;
            current.error = string.Empty;
            current.records = records;
            current.date = date;
            return current;
        }

        /// <summary>
        /// Текущие параметры по каналу ОБЩ (Ch=0)
        /// </summary>
        private Dictionary<short, string> GeneralCurrentParameters = new Dictionary<short, string>()
        {
            {1024, "(T) Текущее время"},
            {1025, "(Д) Текущая дата"},
            {1026, "(СП) Текущая схема потребления"},
            {1027, "(Q) Стандартный расход"},
            {1028, "(P3) Давление P3"},
            {1029, "(∆Р3) Перепад давления ∆Р3"},
            {1030, "(∆Р4) Перепад давления ∆Р4"},
            {1031, "(Рб) Барометрическое давление"},
            {1032, "(Vч) Часовое приращение приведенного объема"},
            {1033, "(Vпч) Часовое приращение объема сверх нормы поставки"},
            {1034, "(Тич) Часовое приращение времени интергирования"},
            {1035, "(НС) Сборка НС"}
        };

        /// <summary>
        /// Текущие параметры по каналам ТР1, ТР2 (Ch=1, 2)
        /// </summary>
        private Dictionary<short, string> PrivateCurrentParameters = new Dictionary<short, string>()
        {
            {1024, "(Qp{0}) Рабочий расход"},
            {1025, "(Q{0}) Стандартный расход"},
            {1026, "(P{0}) Давление газа"},
            {1027, "(t{0}) Температура газа"},
            {1028, "(∆P{0}) Перепад давления"},
            {1029, "(Vp{0}ч) Приращение рабочего объема с начала часа"},
            {1030, "(V{0}ч) Приращение стандартного объема с начала часа"},
            {1031, "(∆P{0}д) Допускаемое значение перепада"},
            {1032, "(Ксж{0}) Коэффициент сжимаемости газа"},
            {1033, "(Кпр{0}) Коэффициент приведения"}
        };

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

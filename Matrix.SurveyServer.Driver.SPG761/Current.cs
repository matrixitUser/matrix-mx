using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        private dynamic GetCurrent(byte dad, byte sad, bool needDad, byte ch)
        {
            Dictionary<string, string> categories = new Dictionary<string, string>()
            {
                { "060", "00" }, //Текущая дата
                { "061", "00" }, //Текущее время
                { "156", ch.ToString("00") }, //температура газа
                { "159", ch.ToString("00") },
                { "162", ch.ToString("00") },
                { "155", ch.ToString("00") },
                { "158", ch.ToString("00") },
                { "151", ch.ToString("00") }, //измеренное значение перепада давления, соответствующее первому (основному) датчику перепада давления
                { "152", ch.ToString("00") }, //измеренное значение перепада давления, соответствующее второму (дополнительному) датчику перепада давления
                { "153", ch.ToString("00") }, //измеренное значение перепада давления, соответствующее третьему (дополнительному) датчику перепада давления
                { "163", ch.ToString("00") }
            };

            dynamic parameters = GetParameters(dad, sad, categories, needDad);
            if (!parameters.success)
                return parameters;

            var units = GetUnitsCurrent(parameters.categories);
            // log(string.Format("GetUnitsCurrent\r\n\t {0}", string.Join("\r\n\t", units)));
            List<dynamic> records = new List<dynamic>();

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;

            var date = parameters.categories[0][0];
            var time = parameters.categories[1][0];
            var currDate = DateTime.Parse(string.Format("{0} {1}", time.Replace("-", ":"), date.Replace("-", ".")));
            //  var currDate = DateTime.ParseExact(date + time, "dd-MM-yyHH:mm:ss", null);
            current.date = currDate;

            double value = 0;
            Char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            if (double.TryParse(parameters.categories[2][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Tтек", value, units[2], currDate));
            }
            if (double.TryParse(parameters.categories[3][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Qтек", value, units[3], currDate));
            }
            if (double.TryParse(parameters.categories[4][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Qитог", value, units[4], currDate));
            }
            if (double.TryParse(parameters.categories[5][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Pтек", value, units[5], currDate));
            }
            if (double.TryParse(parameters.categories[6][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Qoтек", value, units[6], currDate));
            }
            if (double.TryParse(parameters.categories[7][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("∆Р1", value, units[7], currDate));
            }
            if (double.TryParse(parameters.categories[8][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("∆Р2", value, units[8], currDate));
            }
            if (double.TryParse(parameters.categories[9][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("∆Р3", value, units[9], currDate));
            }
            if (double.TryParse(parameters.categories[10][0].Replace('.', separator), out value))
            {
                records.Add(MakeCurrentRecord("Qoитог", value, units[10], currDate));
            }

            current.records = records;
            return current;
        }

        private string[] GetUnitsCurrent(List<string[]> categories)
        {
            List<string> units = new List<string>();
            units.Add("");
            for (int i = 1; i < categories.Count; i++)
            {
                // log(string.Format("categories[{0}]: {1}", i, string.Join(",", categories[i])));
                if (categories[i][0].Contains("?") || categories[i][0].ToLower().Contains("не ")) // не исполняется
                {
                    units.Add("");
                    continue;
                }

                string unit = categories[i][1];
                foreach (var key in replaces.Keys)
                {
                    unit = unit.Replace(key, replaces[key]);
                }
                units.Add(unit);
            }

            return units.ToArray();
        }

        private Dictionary<string, string> replaces = new Dictionary<string, string>()
        {
            {"м2", "м²"},
            {"м3", "м³"},
            {"'C", "°C"},
            {"'K", "°K"}
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

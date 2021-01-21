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
        private dynamic GetConstant(byte na, List<byte> chs, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;

            dynamic generalConstant = GetGeneralConstants(na, date);
            if (!generalConstant.success) return generalConstant;

            List<dynamic> records = new List<dynamic>();
            records.AddRange(generalConstant.records);

            constant.contractHour = generalConstant.contractHour;
            var units = (Dictionary<string, string>)generalConstant.units;

            foreach (var ch in chs)
            {
                dynamic privateConstant = GetPrivateConstants(na, ch, date);
                if (!privateConstant.success) return privateConstant;
                records.AddRange(privateConstant.records);

                var pcUnits = (Dictionary<string, string>)privateConstant.units;
                units = units.Concat(pcUnits).ToDictionary(x => x.Key, x => x.Value);
            }

            constant.units = units;

            foreach (var key in constant.units.Keys)
            {
                records.Add(MakeConstantRecord(string.Format("единица измерения {0}", key), constant.units[key], date));
            }

            constant.records = records;
            return constant;
        }

        private dynamic GetGeneralConstants(byte na, DateTime date)
        {
            dynamic answer = ReadParameters(na, 0x00, GeneralParameters.Keys.ToArray());
            if (!answer.success) return answer;

            dynamic constant = new ExpandoObject();
            List<dynamic> records = new List<dynamic>();
            List<dynamic> parameters = answer.parameters;

            var units = new Dictionary<string, string>();

            units.Add(Glossary.P(3), GetUnit(parameters[14]));
            units.Add(Glossary.dP(3), GetUnit(parameters[19]));
            units.Add(Glossary.dP(4), GetUnit(parameters[22]));
            units.Add(Glossary.Pb, GetUnit(parameters[26]));

            for (int i = 0; i < parameters.Count; i++)
            {
                string name = GeneralParameters[(byte)i];
                if (i == 14 || i == 19 || i == 22 || i == 26) continue;

                if (i == 13 || i == 15 || i == 16 || i == 17)
                    name = string.Format(name, units[Glossary.P(3)]);

                if (i == 18 || i == 20)
                    name = string.Format(name, units[Glossary.dP(3)]);
                if (i == 21 || i == 23)
                    name = string.Format(name, units[Glossary.dP(4)]);
                if (i == 24 || i == 27)
                    name = string.Format(name, units[Glossary.Pb]);

                if (i == 7)
                {
                    int contractHour = 0;
                    if (!int.TryParse(parameters[7], out contractHour))
                    {
                        constant.success = false;
                        constant.error = "не удалось прочитать значение контрактного часа";
                        return constant;
                    }
                    constant.contractHour = contractHour;
                }
                records.Add(MakeConstantRecord(name, parameters[i], date));
            }
            constant.success = true;
            constant.error = string.Empty;
            constant.records = records;
            constant.units = units;
            return constant;
        }

        private dynamic GetPrivateConstants(byte na, byte ch, DateTime date)
        {
            dynamic answer = ReadParameters(na, ch, PrivateParameters.Keys.ToArray());
            if (!answer.success) return answer;

            dynamic constant = new ExpandoObject();
            List<dynamic> records = new List<dynamic>();
            List<dynamic> parameters = answer.parameters;

            Dictionary<string, string> units = new Dictionary<string, string>();

            units.Add(Glossary.P(ch), GetUnit(parameters[10]));
            units.Add(Glossary.dP(ch), GetUnit(parameters[15]));

            for (int i = 0; i < parameters.Count; i++)
            {
                string name = string.Format(PrivateParameters[(byte)i], ch);
                if (i == 10 || i == 15) continue;
                if (i == 8 || i == 11 || i == 12 || i == 13)
                    name = string.Format("{0}({1})", name, units[Glossary.P(ch)]);

                if (i == 14 || i == 16 || i == 17 || i == 18 || i == 19 || i == 21)
                    name = string.Format("{0}({1})", name, units[Glossary.dP(ch)]);

                records.Add(MakeConstantRecord(name, parameters[i], date));
            }

            constant.success = true;
            constant.error = string.Empty;
            constant.records = records;
            constant.units = units;
            return constant;
        }

        private string GetUnit(string code)
        {
            int pcode = 0;
            if (!int.TryParse(code, out pcode))
                return string.Empty;

            switch (pcode)
            {
                case 0: return "кПа";
                case 1: return "МПа";
                case 2: return "кгс/см²";
                case 3: return "кгс/м²";
                default: return string.Empty;
            }
        }

        private dynamic MakeConstantRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        /// <summary>
        /// Настроечные параметры по каналу ОБЩ (Ch=0)
        /// </summary>
        private Dictionary<short, string> GeneralParameters = new Dictionary<short, string>()
        {
            {0, "(СП) Схема потребления"},
            {1, "(МР) Метод расчета"},
            {2, "(ПИ) Период изменений"},
            {3, "(ДО) Дата начала отсчета"},
            {4, "(ТО) Время начала отсчета"},
            {5, "(ПЛ) Автоматический переход на летне/зимнее время"},
            {6, "(СР) Расчетные сутки"},
            {7, "(ЧР) Расчетный час"},
            {8, "(Vд) Норма поставки"},
            {9, "(rc) Плотность сухого газа"},
            {10, "(rв) Процент влаги"},
            {11, "(Ха) Содержание азота в газе"},
            {12, "(Ху) Содержание углерода в газе"},
            {13, "(ВД/Р3) Признак включения датчика давления Р3 ({0})"},
            {14, "Р3"},
            {15, "(ТД/Р3) Тип датчика давления P3 ({0})"},
            {16, "(ВП/Р3) Верхний предел изменения давления P3 ({0})"},
            {17, "(КС/Р3) Поправка на высоту столба разделительной жидкости"},
            {18, "(ВД/∆P3) Признак включения датчика перепада давления ∆Р3"},
            {19, "dР3"},
            {20, "(ВП/∆P3) Верхний предел изменения давления перепада давления ∆P3 ({0})"},
            {21, "(ВД/∆P4) Признак включения датчика перепада давления ∆Р4 ({0})"},
            {22, "dР4"},
            {23, "(ВП/∆P4) Верхний предел изменения перепада давления ∆P4 ({0})"},
            {24, "(ВД/Рб) Признак включения датчика барометрического давления"},
            {25, "(Рбк) Константа барометрического давления"},
            {26, "Рб"},
            {27, "(ВП/Рб) Верхний предел изменения барометрического давления ({0})"},
            {28, "(NT) Сетевой номер корректора"},
            {29, "(ИД) Идентификатор корректора"},
            {30, "(КИ1) Конфигурация магистрального интерфейса"},
            {31, "(КИ2) Конфигурация интерфейса RS232"},
            {32, "(СН) Управление выходным дискретным сигналом"},
            {33, "(КД) Контроль дискретного сигнала на входе"},
            {34, "(ПС) Печать суточных отчетов"},
            {35, "(ПМ) Печать месячных отчетов"},
            {36, "(КУ1) Контроль параметра по уставке 1"},
            {37, "(УВ1) Верхнее значение уставки 1"},
            {38, "(УН1) Нижнее значение уставки 1"},
            {39, "(КУ2) Контроль параметра по уставке 2"},
            {40, "(УВ2) Верхнее значение уставки 2"},
            {41, "(УН2) Нижнее значение уставки 2"},
            {42, "(КУ3) Контроль параметра по уставке 3"},
            {43, "(УВ3) Верхнее значение уставки 3"},
            {44, "(УН3) Нижнее значение уставки 3"},
            {45, "(КУ4) Контроль параметра по уставке 4"},
            {46, "(УВ4) Верхнее значение уставки 4"},
            {47, "(УН4) Нижнее значение уставки 4"},
            {48, "(КУ5) Контроль параметра по уставке 5"},
            {49, "(УВ5) Верхнее значение уставки 5"},
            {50, "(УН5) Нижнее значение уставки 5"}
        };

        /// <summary>
        /// Настроечные параметры по каналам ТР1 и ТР2 (Ch=1,2)
        /// </summary>
        private Dictionary<short, string> PrivateParameters = new Dictionary<short, string>()
        {
            {0, "(ВД/Qр{0}) Признак включения датчика объема газа"},
            {1, "(Qр{0}к) Константа рабочего расхода"},
            {2, "(ВП/Qр{0}) Верхний предел изменения рабочего расхода"},
            {3, "(НП/Qр{0}) Нижний предел изменения рабочего расхода"},
            {4, "(ЦИ/Qp{0}) Цена импульса датчика объема газа"},
            {5, "(Vн/Qр{0}) Начальное значение объема"},
            {6, "(ФС/Qр{0}) Режим фильтрации входного сигнала"},
            {7, "(ОТС/Qp{0}) Отсечка показаний рабочего расхода"},
            {8, "(ВД/Р{0}) Признак включения датчика давления"},
            {9, "(Р{0}к) Константа давления"},
            {10, "Р"},
            {11, "(ТД/Р{0}) Тип датчика давления"},
            {12, "(ВП/Р{0}) Верхний предел изменения давления"},
            {13, "(КС/Р{0}) Поправка на высоту столба разделительной жидкости"},
            {14, "(ВД/∆P{0}) Признак включения датчика перепада давления ∆Р"},
            {15, "dP"},
            {16, "(ВП/∆P{0}) Верхний предел изменения перепада давления ∆P"},
            {17, "(ДК/∆P{0}) Динамический контроль перепада давления ∆P"},
            {18, "(ДП/∆P{0}) Коэффициент предельного превышения расчетного перепада"},
            {19, "(Qн/∆P{0}) Регламентированный расход"},
            {20, "(∆Pн{0}) Регламентированный перепад давления"},
            {21, "(rсн/∆P{0}) Регламентированная плотность газа"},
            {22, "(Pн{0}) Регламентированное давление газа"},
            {23, "(ВД/t{0}) Признак включения датчика температуры"},
            {24, "(t{0}к) Константа температуры"},
            {25, "(ТД/t{0}) Тип датчика температуры"}
        };
    }
}

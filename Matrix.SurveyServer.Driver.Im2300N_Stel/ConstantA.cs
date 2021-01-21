using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private dynamic GetConstantsA(byte na, DateTime date)
        {
            return ParseConstantsA(GetBlocks(na, 0xC4,1, 1, 2807), date);
        }

        private dynamic ParseConstantsA(byte[] bytes, DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            if (bytes == null || bytes.Length < 2807)
            {
                constant.success = false;
                constant.error = "недостаточно данных";
                return constant;
            }

            constant.contractHour = 0;
            constant.records = new List<dynamic>();

            var count = bytes[0];

            for (int offset = 1; offset < 2805; offset += 11)
            {
                var node = bytes[offset + 1] & 0xf0 >> 4;

                var code = BitConverter.ToInt16(new byte[] { bytes[offset], (byte)(bytes[offset + 1] & 0x0f) }, 0);                
                var value = BitConverter.ToSingle(bytes, offset + 2);
                constant.records.Add(MakeConstRecord(GetConstantAName(code), value, date));

                if (code == 0x0072)
                {
                    constant.contractHour = (int)value;
                }
            }

            return constant;
        }

        private string GetConstantAName(short code)
        {
            switch (code)
            {
                case 0x0001: return "расчет по Пр(0)/Обр(1)";
                case 0x0002: return "Температура х.в., °C";
                case 0x0007: return "Температура х.в. лето, °C";
                case 0x0008: return "Дата перехода на лето, дд.мм";
                case 0x0009: return "Температура х.в. зима, °C";
                case 0x000a: return "Дата перехода на зиму, дд.мм";
                case 0x000b: return "Переход зима/лето выкл(0), вкл(1)";
                case 0x0003: return "Барометрическое давление, мм.рт.ст.";
                case 0x0004: return "Считать: 0-всегда, 1-датч. в норме, 2-исп. ДП";
                case 0x0005: return "0-перегрет., 1-насыщен.";
                case 0x0006: return "Степень сухости (70%...100%)";
                case 0x000c: return "Время вычисления tQ, сек";
                case 0x0010: return "Вариант исп. Дог.Парам (0,1)";
                case 0x0011: return "Дог. мин расход, м³/час";
                case 0x0012: return "Дог. макс расход, м³/час";
                case 0x0013: return "Дог. макс расход, н.м³/час";
                case 0x0014: return "Дог. давл. в ед. изм. пасп.";
                case 0x0015: return "Дог. температура, °C";
                case 0x0021: return "Дог. мин. расход, м³/час";
                case 0x0022: return "Дог. макс. расход, м³/час";
                case 0x0025: return "Дог. мин. перепад давл.";
                case 0x002a: return "Дог. макс. расход, н.м³/ч";
                case 0x002b: return "Дог. давл. в ед. изм. пасп.";
                case 0x002c: return "Дог. температура, °C";
                case 0x0030: return "Тип газа (0-чист.;1-попутн.;2-природный)";
                case 0x0031: return "Индекс газа (0-возд.; 1-азот; 2-косл; 3-аргон)";
                case 0x0032: return "Метод расчета Ксж (0-RQ)";
                case 0x0033: return "Метод расчета Ксж (0-NX19,1-GERG, 2-ВНИЦСМВ)";
                case 0x0040: return "Метан, мол.%";
                case 0x0041: return "Этан, мол.%";
                case 0x0042: return "Пропан, мол.%";
                case 0x0043: return "н-Бутан, мол.%";
                case 0x0044: return "и-Бутан, мол.%";
                case 0x0045: return "н-Пентан, мол.%";
                case 0x0046: return "и-Пентан, мол.%";
                case 0x0047: return "н-Гексан, мол.%";
                case 0x0048: return "н-Гептан, мол.%";
                case 0x0049: return "н-Октан, мол.%";
                case 0x004a: return "Ацетилен, мол.%";
                case 0x004b: return "Этилен, мол.%";
                case 0x004c: return "Пропилен, мол.%";
                case 0x004d: return "Бензол, мол.%";
                case 0x004e: return "Толуол, мол.%";
                case 0x004f: return "Водород, мол.%";
                case 0x0050: return "Вод.пар, мол.%";
                case 0x0051: return "Аммиак, мол.%";
                case 0x0052: return "Метанол, мол.%";
                case 0x0053: return "Сероводород, мол.%";
                case 0x0054: return "Метилмеркаптан, мол.%";
                case 0x0055: return "Диоксид серы, мол.%";
                case 0x0056: return "Гелий, мол.%";
                case 0x0057: return "Неон, мол.%";
                case 0x0058: return "Аргон, мол.%";
                case 0x0059: return "Монооксид угл., мол.%";
                case 0x005a: return "Азот, мол.%";
                case 0x005b: return "Кислород, мол.%";
                case 0x005c: return "Диоксид угл., мол.%";
                case 0x005d: return "Прочие, мол.%";
                case 0x0070: return "Плотность при н.у.";
                case 0x0071: return "Влажность при р.у.";
                case 0x0072: return "Расчетный час";
                case 0x0075: return "Ширина лотка, м";
                case 0x0076: return "Длина лотка, м";
                case 0x0077: return "Высота порога, м";
                case 0x0078: return "Ширина канала перед лотком, м";
                case 0x0080: return "Kpr00";
                case 0x0081: return "Kpr01";
                case 0x0082: return "Kpr02";
                case 0x0083: return "Kpr03";
                case 0x0084: return "Kpr10";
                case 0x0085: return "Kpr11";
                case 0x0086: return "Kpr12";
                case 0x0087: return "Kpr13";
                case 0x0088: return "Kpr20";
                case 0x0089: return "Kpr21";
                case 0x008a: return "Kpr22";
                case 0x008b: return "Kpr23";
                case 0x008c: return "Kpr30";
                case 0x008d: return "Kpr31";
                case 0x008e: return "Kpr32";
                case 0x008f: return "Kpr33";
                case 0x0090: return "Kpr40";
                case 0x0091: return "Kpr41";
                case 0x0092: return "Kpr42";
                case 0x0093: return "Kpr43";
                case 0x0100: return "Mu00";
                case 0x0101: return "Mu01";
                case 0x0102: return "Mu02";
                case 0x0103: return "Mu10";
                case 0x0104: return "Mu11";
                case 0x0105: return "Mu12";
                case 0x0106: return "Mu20";
                case 0x0107: return "Mu21";
                case 0x0108: return "Mu22";
                case 0x0110: return "Kappa00";
                case 0x0111: return "Kappa01";
                case 0x0112: return "Kappa02";
                case 0x0113: return "Kappa10";
                case 0x0114: return "Kappa11";
                case 0x0115: return "Kappa12";
                case 0x0116: return "Kappa20";
                case 0x0117: return "Kappa21";
                case 0x0118: return "Kappa22";
                case 0x0120: return "d20, мм";
                case 0x0121: return "Kt_d";
                case 0x0122: return "D20, мм";
                case 0x0123: return "Kt_D";
                case 0x0124: return "Kп";
                case 0x0125: return "Rш, мм";
                case 0x0126: return "dPType";
                case 0x0130: return "dCinf";
                case 0x0131: return "CQ1";
                case 0x0132: return "CQ2";
                case 0x0133: return "CQ3";
                case 0x0134: return "CQ4";
                case 0x0135: return "Remin";
                case 0x0136: return "QshA";
                case 0x0137: return "QshB";
                case 0x0138: return "QshC";
                case 0x0140: return "Плотность при норм.усл., кг/м³";
                case 0x0141: return "Нормальная температура, °С";
                case 0x0142: return "C1";
                case 0x0143: return "C₂";
                case 0x0144: return "C₃";
                case 0x0145: return "C₄";
                case 0x0146: return "C₅";
                case 0x0150: return "Число точек (Nmax=15)";
                case 0x0151: return "Частота 1, Гц";
                case 0x0152: return "Частота 2, Гц";
                case 0x0153: return "Частота 3, Гц";
                case 0x0154: return "Частота 4, Гц";
                case 0x0155: return "Частота 5, Гц";
                case 0x0156: return "Частота 6, Гц";
                case 0x0157: return "Частота 7, Гц";
                case 0x0158: return "Частота 8, Гц";
                case 0x0159: return "Частота 9, Гц";
                case 0x015a: return "Частота 10, Гц";
                case 0x015b: return "Частота 11, Гц";
                case 0x015c: return "Частота 12, Гц";
                case 0x015d: return "Частота 13, Гц";
                case 0x015e: return "Частота 14, Гц";
                case 0x015f: return "Частота 15, Гц";
                case 0x0161: return "Поправка при F1";
                case 0x0162: return "Поправка при F2";
                case 0x0163: return "Поправка при F3";
                case 0x0164: return "Поправка при F4";
                case 0x0165: return "Поправка при F5";
                case 0x0166: return "Поправка при F6";
                case 0x0167: return "Поправка при F7";
                case 0x0168: return "Поправка при F8";
                case 0x0169: return "Поправка при F9";
                case 0x016a: return "Поправка при F10";
                case 0x016b: return "Поправка при F11";
                case 0x016c: return "Поправка при F12";
                case 0x016d: return "Поправка при F13";
                case 0x016e: return "Поправка при F14";
                case 0x016f: return "Поправка при F15";
                default: return "Не определено";
            }
        }
    }
}

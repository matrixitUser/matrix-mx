using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        //public IEnumerable<FracElement> FracElements { get; set; }
        //public IEnumerable<UnitElement> UnitElements { get; set; }
        //public IEnumerable<Element> Elements { get; set; }
        //public ValueType ValueType { get; set; }

        //public static bool JustVersion { get; set; }
        //public int Version { get; private set; }

        //public List<Data> Data { get; private set; }

        ////public static DateTime Date { get; set; }

        private dynamic ParseParameter(dynamic answer, string parameterType, double value, short fracAddress, short unitAddress)
        {
            string measuringUnit = "";
            List<dynamic> ue = answer.UnitElements;
            var unit = ue.FirstOrDefault(u => (short)u.Address == unitAddress);
            if (unit != null)
            {
                measuringUnit = unit.Unit.Trim();
            }

            List<dynamic> fe = answer.FracElements;
            var frac = fe.FirstOrDefault(f => (short)f.Address == fracAddress);
            if (frac != null)
            {
                value /= Math.Pow(10, frac.Frac);
            }

            dynamic result = null;

            switch ((ValueType)answer.ValueType)
            {
                case ValueType.Current:
                case ValueType.TotalCurrent:
                    result = MakeCurrentRecord(parameterType, value, measuringUnit, answer.date);
                    break;

                case ValueType.Hour:
                    result = MakeHourRecord(parameterType, value, measuringUnit, answer.date);
                    break;

                case ValueType.Day:
                    result = MakeDayRecord(parameterType, value, measuringUnit, answer.date);
                    break;
            }

            return result;
        }

        //private DateTime date;

        dynamic ParseReadDataResponse(dynamic answer,
            DateTime date,
            IEnumerable<dynamic> fracElements,
            IEnumerable<dynamic> unitElements,
            IEnumerable<dynamic> elements,
            ValueType valueType)
        {
            if (!answer.success) return answer;

            var read = ParseReadResponse(answer);
            if (!read.success) return read;
            var body = (byte[])read.Body;

            read.date = date;

            read.UnitElements = unitElements;
            read.FracElements = fracElements;
            read.Elements = elements;
            read.ValueType = valueType;

            if (unitElements == null || !unitElements.Any())
            {
                read.success = false;
                read.error = "Единицы измерения не найдены";
                return read;
            }

            if (fracElements == null || !fracElements.Any())
            {
                read.success = false;
                read.error = "Дробные части значений не найдены";
                return read;
            }

            if (elements == null || !elements.Any())
            {
                read.success = false;
                read.error = "Активные элементы не найдены";
                return read;
            }

            var Data = new List<dynamic>();

            var offset = 0;

            foreach (var element in elements)
            {
                if (body.Length < offset + element.Length)
                {
                    continue;
                }

                double value = 0;
                switch ((int)element.Length)
                {
                    case 2:
                        value = BitConverter.ToInt16(body, offset);
                        break;
                    case 4:
                        value = BitConverter.ToInt32(body, offset);
                        break;
                }

                var quality = body[offset + element.Length + 0];
                var ns = body[offset + element.Length + 1];

                if (quality == (byte)Quality.ConfigError || quality == (byte)Quality.DeviceFail)
                {
                    offset += element.Length + 2;
                    continue;
                }

                //log(string.Format("{0:dd.MM.yyyy HH:mm:ss} [RAW={5}] адрес={1} длина={2} смещение={3} значение={4}",
                //    date, element.Address, element.Length, offset, value,
                //    string.Join(",", body.ToList().Skip(offset).Take((int)element.Length + 2).Select(b => b.ToString("X2")))
                //    ));

                dynamic param = null;

                switch ((int)element.Address)
                {
                    case 0://t1 Тв1 Т-Н
                        param = ParseParameter(read, "t1 Тв1", value, 57, 44);
                        break;
                    case 1://t2 Тв1 Т-Н
                        param = ParseParameter(read, "t2 Тв1", value, 57, 44);
                        break;
                    case 2://t3 Тв1 Т-Н
                        param = ParseParameter(read, "t3 Тв1", value, 57, 44);
                        break;
                    case 3://V1 Тв1 -ИН
                        param = ParseParameter(read, "V1 Тв1", value, 59, 46);
                        break;
                    case 4://V2 Тв1 -ИН
                        param = ParseParameter(read, "V2 Тв1", value, 59, 46);
                        break;
                    case 5://V3 Тв1 -ИН
                        param = ParseParameter(read, "V3 Тв1", value, 59, 46);
                        break;
                    case 6://M1 Тв1 -ИН
                        param = ParseParameter(read, "M1 Тв1", value, 60, 47);
                        break;
                    case 7://M2 Тв1 -ИН
                        param = ParseParameter(read, "M2 Тв1", value, 60, 47);
                        break;
                    case 8://M3 Тв1 -ИН
                        param = ParseParameter(read, "M3 Тв1", value, 60, 47);
                        break;
                    case 9://P1 Тв1 Т-Н
                        param = ParseParameter(read, "P1 Тв1", value, 61, 48);
                        break;
                    case 10://P2 Тв1 Т-Н
                        param = ParseParameter(read, "P2 Тв1", value, 61, 48);
                        break;
                    case 11://Mг Тв1 -ИН
                        param = ParseParameter(read, "Mг Тв1", value, 65, 52);
                        break;
                    case 12://Qo Тв1 -ИН
                        param = ParseParameter(read, "Qo Тв1", value, 66, 53);
                        break;
                    case 13://Qг Тв1 -ИН
                        param = ParseParameter(read, "Qг Тв1", value, 66, 54);
                        break;
                    case 14://dt Тв1 Т-Н
                        param = ParseParameter(read, "dt Тв1", value, 72, 62);
                        break;
                    case 15://tx
                        param = ParseParameter(read, "tх", value, 63, 50);
                        break;
                    case 16://ta
                        param = ParseParameter(read, "ta", value, 64, 51);
                        break;
                    case 17://ВНР Тв1 -ИН
                        param = ParseParameter(read, "ВНР Тв1", value, -1, 55);
                        break;
                    case 18://ВОС Тв1 -ИН
                        param = ParseParameter(read, "ВОС Тв1", value, -1, 55);
                        break;
                    case 19://G1 Тв1 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G1 Тв1", value, 58, 45);
                        break;
                    case 20://G2 Тв1 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G2 Тв1", value, 58, 45);
                        break;
                    case 21://G3 Тв1 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G3 Тв1", value, 58, 45);
                        break;

                    case 22://t1 Тв2 Т-Н
                        param = ParseParameter(read, "t1 Тв2", value, 57, 44);
                        break;
                    case 23://t2 Тв2 Т-Н
                        param = ParseParameter(read, "t2 Тв2", value, 57, 44);
                        break;
                    case 24://t3 Тв2 Т-Н
                        param = ParseParameter(read, "t3 Тв2", value, 57, 44);
                        break;
                    case 25://V1 Тв2 -ИН
                        param = ParseParameter(read, "V1 Тв2", value, 59, 46);
                        break;
                    case 26://V2 Тв2 -ИН
                        param = ParseParameter(read, "V2 Тв2", value, 59, 46);
                        break;
                    case 27://V3 Тв2 -ИН
                        param = ParseParameter(read, "V3 Тв2", value, 59, 46);
                        break;
                    case 28://M1 Тв2 -ИН
                        param = ParseParameter(read, "M1 Тв2", value, 60, 47);
                        break;
                    case 29://M2 Тв2 -ИН
                        param = ParseParameter(read, "M2 Тв2", value, 60, 47);
                        break;
                    case 30://M3 Тв2 -ИН
                        param = ParseParameter(read, "M3 Тв2", value, 60, 47);
                        break;
                    case 31://P1 Тв2 Т-Н
                        param = ParseParameter(read, "P1 Тв2", value, 61, 48);
                        break;
                    case 32://P2 Тв2 Т-Н
                        param = ParseParameter(read, "P2 Тв2", value, 61, 48);
                        break;

                    case 33://Mг Тв2 -ИН
                        param = ParseParameter(read, "Mг Тв2", value, 65, 52);
                        break;

                    case 34://Qo Тв2 -ИН
                        param = ParseParameter(read, "Qo Тв2", value, 66, 53);
                        break;
                    case 35://Qг Тв2 -ИН
                        param = ParseParameter(read, "Qг Тв2", value, 66, 54);
                        break;
                    case 36://dt Тв2 Т-Н
                        param = ParseParameter(read, "dt Тв2", value, 72, 62);
                        break;

                    //case 37://резерв
                    //case 38://резерв
                    case 39://ВНР Тв2 -ИН
                        param = ParseParameter(read, "ВНР Тв2", value, -1, 55);
                        break;
                    case 40://ВОС Тв2 -ИН
                        param = ParseParameter(read, "ВОС Тв2", value, -1, 55);
                        break;
                    case 41://G1 Тв2 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G1 Тв2", value, 58, 45);
                        break;
                    case 42://G2 Тв2 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G2 Тв2", value, 58, 45);
                        break;
                    case 43://G3 Тв2 Т--
                        value = BitConverter.ToSingle(body, offset);
                        param = ParseParameter(read, "G3 Тв2", value, 58, 45);
                        break;
                    case 77://НС по Тв1
                        param = ParseParameter(read, "НС Тв1", value, -1, -1);
                        break;
                    case 78://НС по Тв2
                        param = ParseParameter(read, "НС Тв2", value, -1, -1);
                        break;
                    case 79:
                        param = ParseParameter(read, "Длит. НС Тв1", value, -1, -1);
                        break;
                    case 80:
                        param = ParseParameter(read, "Длит. НС Тв2", value, -1, -1);
                        break;
                    case 81://DI
                        param = ParseParameter(read, "DI", value, -1, -1);
                        break;
                    case 82://P3
                        param = ParseParameter(read, "P3", value, -1, -1);
                        break;
                    default:
                        break;
                }

                if (param != null)
                {
                    Data.Add(param);

                    log(string.Format("{4:dd.MM.yyyy HH:mm:ss} прочитано {5} значение {0}({6})={1} {2} [качество={7},НС={8}](от {3:dd.MM.yyyy HH:mm:ss})",
                        param.s1, param.d1, param.s2, param.date, param.dt1, param.type, element.Address,
                        quality == 0xC0 ? "GOOD" : (quality == 0x04 ? "CFGERROR" : (quality == 0x0C ? "FAIL" : (quality == 0x50 ? "NS" : string.Format("0x{0:X2}", quality)))),
                        ns == 0x00 ? "NoNS" : (ns == 0xFF ? "NsOther" : "NS")
                    ));
                }
                

                offset += element.Length + 2;
            }

            read.Data = Data;

            return read;
        }

        enum Quality
        {
            Good = 0xC0,
            ConfigError = 0x04,
            DeviceFail = 0x0C,
            Abnormal = 0x50
        }
    }
}

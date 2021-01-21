using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public class ResponseReadData : ResponseRead
    {
        public IEnumerable<FracElement> FracElements { get; set; }
        public IEnumerable<UnitElement> UnitElements { get; set; }
        public IEnumerable<Element> Elements { get; set; }
        public ValueType ValueType { get; set; }

        public static bool JustVersion { get; set; }
        public int Version { get; private set; }

        public List<Data> Data { get; private set; }

        //public static DateTime Date { get; set; }

        private Data ParseParameter(string parameterType, double value, short fracAddress, short unitAddress)
        {
            var measuringUnit = MeasuringUnitType.Unknown;
            var unit = UnitElements.FirstOrDefault(u => u.Address == unitAddress);
            if (unit != null)
            {
                switch (unit.Unit.Trim())
                {
                    case "°C":
                        measuringUnit = MeasuringUnitType.C;
                        break;
                    case "м3/ч":
                        measuringUnit = MeasuringUnitType.m3_h;
                        break;
                    case "м3":
                        measuringUnit = MeasuringUnitType.m3;
                        break;
                    case "т":
                        measuringUnit = MeasuringUnitType.tonn;
                        break;
                    case "кг/см2":
                        measuringUnit = MeasuringUnitType.kgs_kvSm;
                        break;
                    case "Гкал":
                        measuringUnit = MeasuringUnitType.Gkal;
                        break;
                    case "ч":
                        measuringUnit = MeasuringUnitType.h;
                        break;
                }
            }

            var frac = FracElements.FirstOrDefault(f => f.Address == fracAddress);
            if (frac != null)
            {
                value /= Math.Pow(10, frac.Frac);
            }
            return new Data(parameterType, measuringUnit, date, value);
        }

        private DateTime date;

        public ResponseReadData(byte[] data, DateTime date,
            IEnumerable<FracElement> fracElements,
            IEnumerable<UnitElement> unitElements,
            IEnumerable<Element> elements,
            ValueType valueType)
            : base(data)
        {
            this.date = date;
            Data = new List<Data>();

            UnitElements = unitElements;
            FracElements = fracElements;
            Elements = elements;
            ValueType = valueType;

            if (unitElements == null || !unitElements.Any()) return;
            if (fracElements == null || !fracElements.Any()) return;
            if (elements == null || !elements.Any()) return;

            var offset = 3;

            foreach (var element in elements)
            {
                if (data.Length < offset + element.Length)
                {
                    continue;
                }

                double value = 0;
                switch (element.Length)
                {
                    case 2:
                        value = BitConverter.ToInt16(data, offset);
                        break;
                    case 4:
                        value = BitConverter.ToInt32(data, offset);
                        break;
                }

                switch (element.Address)
                {
                    case 0://t1 Тв1 Т-Н
                        Data.Add(ParseParameter("t1 Тв1", value, 57, 44));
                        break;
                    case 1://t2 Тв1 Т-Н
                        Data.Add(ParseParameter("t2 Тв1", value, 57, 44));
                        break;
                    case 2://t3 Тв1 Т-Н
                        Data.Add(ParseParameter("t3 Тв1", value, 57, 44));
                        break;
                    case 3://V1 Тв1 -ИН
                        Data.Add(ParseParameter("V1 Тв1", value, 59, 46));
                        break;
                    case 4://V2 Тв1 -ИН
                        Data.Add(ParseParameter("V2 Тв1", value, 59, 46));
                        break;
                    case 5://V3 Тв1 -ИН
                        Data.Add(ParseParameter("V3 Тв1", value, 59, 46));
                        break;
                    case 6://M1 Тв1 -ИН
                        Data.Add(ParseParameter("M1 Тв1", value, 60, 47));
                        break;
                    case 7://M2 Тв1 -ИН
                        Data.Add(ParseParameter("M2 Тв1", value, 60, 47));
                        break;
                    case 8://M3 Тв1 -ИН
                        Data.Add(ParseParameter("M3 Тв1", value, 60, 47));
                        break;
                    case 9://P1 Тв1 Т-Н
                        Data.Add(ParseParameter("P1 Тв1", value, 61, 48));
                        break;
                    case 10://P2 Тв1 Т-Н
                        Data.Add(ParseParameter("P2 Тв1", value, 61, 48));
                        break;
                    //case 11://Mг Тв1 -ИН
                    //    Data.Add(Foo(ParameterType.Temperature, value, CalculationType.Total, 1, 57, 44));
                    //    break;
                    case 12://Qo Тв1 -ИН
                        Data.Add(ParseParameter("Qo Тв1", value, 66, 53));
                        break;
                    case 13://Qг Тв1 -ИН
                        Data.Add(ParseParameter("Qг Тв1", value, 66, 54));
                        break;
                    case 14://dt Тв1 Т-Н
                    case 15://tx
                    case 16://ta
                        break;
                    case 17://ВНР Тв1 -ИН
                        Data.Add(ParseParameter("ВНР Тв1", value, -1, 55));
                        break;
                    case 18://ВОС Тв1 -ИН
                        Data.Add(ParseParameter("ВОС Тв1", value, -1, 56));
                        break;
                    case 19://G1 Тв1 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G1 Тв1", value, 58, 45));
                        break;
                    case 20://G2 Тв1 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G2 Тв1", value, 58, 45));
                        break;
                    case 21://G3 Тв1 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G3 Тв1", value, 58, 45));
                        break;

                    case 22://t1 Тв2 Т-Н
                        Data.Add(ParseParameter("t1 Тв2", value, 57, 44));
                        break;
                    case 23://t2 Тв2 Т-Н
                        Data.Add(ParseParameter("t2 Тв2", value, 57, 44));
                        break;
                    case 24://t3 Тв2 Т-Н
                        Data.Add(ParseParameter("t3 Тв2", value, 57, 44));
                        break;
                    case 25://V1 Тв2 -ИН
                        Data.Add(ParseParameter("V1 Тв2", value, 59, 46));
                        break;
                    case 26://V2 Тв2 -ИН
                        Data.Add(ParseParameter("V2 Тв2", value, 59, 46));
                        break;
                    case 27://V3 Тв2 -ИН
                        Data.Add(ParseParameter("V3 Тв2", value, 59, 46));
                        break;
                    case 28://M1 Тв2 -ИН
                        Data.Add(ParseParameter("M1 Тв2", value, 60, 47));
                        break;
                    case 29://M2 Тв2 -ИН
                        Data.Add(ParseParameter("M2 Тв2", value, 60, 47));
                        break;
                    case 30://M3 Тв2 -ИН
                        Data.Add(ParseParameter("M3 Тв2", value, 60, 47));
                        break;
                    case 31://P1 Тв2 Т-Н
                        Data.Add(ParseParameter("P1 Тв2", value, 61, 48));
                        break;
                    case 32://P2 Тв2 Т-Н
                        Data.Add(ParseParameter("P2 Тв2", value, 61, 48));
                        break;
                    //case 33://Mг Тв2 -ИН
                    case 34://Qo Тв2 -ИН
                        Data.Add(ParseParameter("Qo Тв2", value, 66, 53));
                        break;
                    case 35://Qг Тв2 -ИН
                        Data.Add(ParseParameter("Qг Тв2", value, 66, 54));
                        break;
                    //case 36://dt Тв2 Т-Н

                    //case 37://резерв
                    //case 38://резерв
                    case 39://ВНР Тв2 -ИН
                        Data.Add(ParseParameter("ВНР Тв2", value, -1, 55));
                        break;
                    case 40://ВОС Тв2 -ИН
                        Data.Add(ParseParameter("ВОС Тв2", value, -1, 55));
                        break;
                    case 41://G1 Тв2 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G1 Тв2", value, 58, 45));
                        break;
                    case 42://G2 Тв2 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G2 Тв2", value, 58, 45));
                        break;
                    case 43://G3 Тв2 Т--
                        value = BitConverter.ToSingle(data, offset);
                        Data.Add(ParseParameter("G3 Тв2", value, 58, 45));
                        break;
                    case 77://НС по Тв1
                        Data.Add(ParseParameter("НС по Тв1", value, -1, -1));
                        break;
                }

                offset += element.Length + 2;
            }
        }
    }
}

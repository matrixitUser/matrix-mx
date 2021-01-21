using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
        public struct Parameter
        {
            public enum Transform
            {
                None,
                ToFloat,
                ToUInt16
            }

            //public byte condAddress;
            public string name;
            public short fracAddress;
            public short unitAddress;
            public Transform transformType;
            public Parameter(string name, short fracAddress, short unitAddress, Transform transformType = Transform.None)
            {
                //this.condAddress = condAddress;
                this.name = name;
                this.fracAddress = fracAddress;
                this.unitAddress = unitAddress;
                this.transformType = transformType;
            }
        }

        public Dictionary<byte, Parameter> parameterDict = new Dictionary<byte, Parameter>()
        {
            { 0, new Parameter("t1 Тв1", 57, 44) },//t1 Тв1 Т-Н
            { 1, new Parameter("t2 Тв1", 57, 44) },//t2 Тв1 Т-Н
            { 2, new Parameter("t3 Тв1", 57, 44) },//t3 Тв1 Т-Н
            { 3, new Parameter("V1 Тв1", 59, 46) },//V1 Тв1 -ИН
            { 4, new Parameter("V2 Тв1", 59, 46) },//V2 Тв1 -ИН
            { 5, new Parameter("V3 Тв1", 59, 46) },//V3 Тв1 -ИН
            { 6, new Parameter("M1 Тв1", 60, 47) },//M1 Тв1 -ИН
            { 7, new Parameter("M2 Тв1", 60, 47) },//M2 Тв1 -ИН
            { 8, new Parameter("M3 Тв1", 60, 47) },//M3 Тв1 -ИН
            { 9, new Parameter("P1 Тв1", 61, 48) },//P1 Тв1 Т-Н
            { 10, new Parameter("P2 Тв1", 61, 48) },//P2 Тв1 Т-Н
            { 11, new Parameter("Mг Тв1", 65, 52) },//Mг Тв1 -ИН
            { 12, new Parameter("Qo Тв1", 66, 53) },//Qo Тв1 -ИН
            { 13, new Parameter("Qг Тв1", 66, 54) },//Qг Тв1 -ИН
            { 14, new Parameter("dt Тв1", 62, 62) },//dt Тв1 Т-Н
            { 15, new Parameter("tх", 63, 50) },//tx
            { 16, new Parameter("ta", 64, 51) },//ta
            { 17, new Parameter("ВНР Тв1", -1, 55, transformType:Parameter.Transform.ToUInt16) },//ВНР Тв1 -ИН
            { 18, new Parameter("ВОС Тв1", -1, 55, transformType:Parameter.Transform.ToUInt16) },//ВОС Тв1 -ИН
            { 19, new Parameter("G1 Тв1", 58, 45, transformType:Parameter.Transform.ToFloat) },//G1 Тв1 Т--
            { 20, new Parameter("G2 Тв1", 58, 45, transformType:Parameter.Transform.ToFloat) },//G2 Тв1 Т--
            { 21, new Parameter("G3 Тв1", 58, 45, transformType:Parameter.Transform.ToFloat) },//G3 Тв1 Т--
            { 22, new Parameter("t1 Тв2", 57, 44) },//t1 Тв2 Т-Н
            { 23, new Parameter("t2 Тв2", 57, 44) },//t2 Тв2 Т-Н
            { 24, new Parameter("t3 Тв2", 57, 44) },//t3 Тв2 Т-Н
            { 25, new Parameter("V1 Тв2", 69, 46) },//V1 Тв2 -ИН
            { 26, new Parameter("V2 Тв2", 69, 46) },//V2 Тв2 -ИН
            { 27, new Parameter("V3 Тв2", 69, 46) },//V3 Тв2 -ИН
            { 28, new Parameter("M1 Тв2", 70, 47) },//M1 Тв2 -ИН
            { 29, new Parameter("M2 Тв2", 70, 47) },//M2 Тв2 -ИН
            { 30, new Parameter("M3 Тв2", 70, 47) },//M3 Тв2 -ИН
            { 31, new Parameter("P1 Тв2", 71, 48) },//P1 Тв2 Т-Н
            { 32, new Parameter("P2 Тв2", 71, 48) },//P2 Тв2 Т-Н
            { 33, new Parameter("Mг Тв2", 75, 52) },//Mг Тв2 -ИН
            { 34, new Parameter("Qo Тв2", 76, 53) },//Qo Тв2 -ИН
            { 35, new Parameter("Qг Тв2", 76, 54) },//Qг Тв2 -ИН
            { 36, new Parameter("dt Тв2", 72, 62) },//dt Тв2 Т-Н
            { 39, new Parameter("ВНР Тв2", -1, 55, transformType:Parameter.Transform.ToUInt16) },//ВНР Тв2 -ИН
            { 40, new Parameter("ВОС Тв2", -1, 55, transformType:Parameter.Transform.ToUInt16) },//ВОС Тв2 -ИН
            { 41, new Parameter("G1 Тв2", 68, 45, transformType:Parameter.Transform.ToFloat) },//G1 Тв2 Т--
            { 42, new Parameter("G2 Тв2", 68, 45, transformType:Parameter.Transform.ToFloat) },//G2 Тв2 Т--
            { 43, new Parameter("G3 Тв2", 68, 45, transformType:Parameter.Transform.ToFloat) },//G3 Тв2 Т--
            { 77, new Parameter("НС Тв1", -1, -1) },//НС по Тв1
            { 78, new Parameter("НС Тв2", -1, -1) },//НС по Тв2
            { 79, new Parameter("Длит. НС Тв1", -1, -1) },//
            { 80, new Parameter("Длит. НС Тв2", -1, -1) },//
            { 81, new Parameter("DI", -1, -1) },//DI
            { 82, new Parameter("P3", -1, -1) }//P3
        };




        //public IEnumerable<FracElement> FracElements { get; set; }
        //public IEnumerable<UnitElement> UnitElements { get; set; }
        //public IEnumerable<Element> Elements { get; set; }
        //public ValueType ValueType { get; set; }

        //public static bool JustVersion { get; set; }
        //public int Version { get; private set; }

        //public List<Data> Data { get; private set; }

        ////public static DateTime Date { get; set; }

        private List<dynamic> ParseParameter(dynamic answer, string parameterType, double value, byte quality, byte ns, short fracAddress, short unitAddress)
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
                log(string.Format("дробная часть {0}=10^{1}, значение {2}=>{3}", parameterType, frac.Frac, value, value / Math.Pow(10, frac.Frac)), level: 3);
                value /= Math.Pow(10, frac.Frac);
            }
            else if (fracAddress != -1)
            {
                log($"дробная часть {parameterType} не найдена! (адрес={fracAddress})", level: 3);
            }

            var result = new List<dynamic>();

            switch ((ValueType)answer.ValueType)
            {
                case ValueType.Current:
                case ValueType.TotalCurrent:
                    result.Add(MakeCurrentRecord(parameterType, value, measuringUnit, answer.date, reliability: quality, ns: ns));
                    break;

                case ValueType.Hour:
                    result.Add(MakeHourRecord(parameterType, value, measuringUnit, answer.date, reliability: quality, ns: ns));
                    if ((ns > 0) && (ns < 255))
                    {
                        result.Add(MakeAbnormalRecord($"НС {parameterType}", 0, answer.date, weight: 1000));
                    }
                    break;

                case ValueType.Day:
                    result.Add(MakeDayRecord(parameterType, value, measuringUnit, answer.date, reliability: quality, ns: ns));
                    break;

                case ValueType.Month:
                    result.Add(MakeDayRecord("M_" + parameterType, value, measuringUnit, answer.date, reliability: quality, ns: ns));
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
            ValueType valueType,
            bool isTotal = false)
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
                double value = 0;
                switch ((int)element.Length)
                {
                    case 1:
                        value = body[offset];
                        break;
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
                else if (quality == (byte)Quality.Good || quality == (byte)Quality.Abnormal)
                {
                    var parameters = new List<dynamic>();

                    string prefix = isTotal ? "+" : "";

                    byte eaddr = (byte)element.Address;
                    if (parameterDict.ContainsKey(eaddr))
                    {
                        Parameter par = parameterDict[eaddr];
                        switch (par.transformType)
                        {
                            case Parameter.Transform.ToFloat:
                                value = BitConverter.ToSingle(body, offset);
                                break;
                            case Parameter.Transform.ToUInt16:
                                value = BitConverter.ToUInt16(body, offset);
                                break;
                        }

                        parameters.AddRange(ParseParameter(read, prefix + par.name, value, quality, ns, par.fracAddress, par.unitAddress));

                        //if ((int)element.Address == 3)
                        //{
                        log($"{valueType} {par.name} {date:dd.MM.yyyy HH:mm:ss} [RAW={string.Join(", ", body.ToList().Skip(offset).Take((int)element.Length + 2).Select(b => b.ToString("X2")))}] адрес={element.Address} длина={element.Length} смещение={offset} значение={value}", level: 3);
                        //}
                    }


                    if (parameters.Count() > 0)
                    {
                        Data.AddRange(parameters);
                        //log(string.Format("{4:dd.MM.yyyy HH:mm:ss} прочитано {5} значение {0}({6})={1} {2} [{7},НС={8}](от {3:dd.MM.yyyy HH:mm:ss})",
                        //    param.s1, param.d1, param.s2, param.date, param.dt1, param.type, element.Address,
                        //    quality == 0xC0 ? "OK" : (quality == 0x0C ? "Вне диапазона" : (quality == 0x50 ? "Имеется НС" : (quality == 0x04 ? "Элемент отсутствует в расчётной схеме" : (quality.ToString())))),
                        //    //(Quality)quality,// == 0xC0 ? "GOOD" : (quality == 0x04 ? "CFGERROR" : (quality == 0x0C ? "FAIL" : (quality == 0x50 ? "NS" : string.Format("0x{0:X2}", quality)))),
                        //    (ns == 0x00) ? "нет" : ((ns == 0xFF) ? "NsOther" : "NS" + ns.ToString())
                        //));
                    }


                    offset += element.Length + 2;
                }
                else
                {
                    read.success = false;
                    read.error = "Ошибка данных ВКТ-7!!!";
                    break;
                }

                read.Data = Data;
            }

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

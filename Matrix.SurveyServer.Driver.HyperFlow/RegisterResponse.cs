//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class RegisterResponse : Response
//    {
//        protected readonly List<byte> Raw;
//        public byte Id { get; private set; }

//        public RegisterResponse(byte[] data)
//            : base(data)
//        {
//            Id = Body[0];
//            Raw = new List<byte>(Body);
//            /*
//            Registers = new List<RegisterValue>();

//            for (var i = 0; i < Body.Count(); i += 6)
//            {
//                Registers.Add(new RegisterValue(Body.Take(6).ToArray()));
//            }*/
//        }

//        public float GetAsFloat()
//        {
//            return BitConverter.ToSingle(Raw.ToArray(), 2);
//        }

//        public UInt32 GetAsULong()
//        {
//            return BitConverter.ToUInt32(Raw.ToArray(), 2);
//        }

//        public double GetAsULong2()
//        {
//            var hLong = BitConverter.ToUInt32(Raw.ToArray(), 2);
//            var lLong = BitConverter.ToUInt32(Raw.ToArray(), 6+2);
//            return hLong * 1000 + lLong / 100000;
//        }
//        /*
//        public interface IValueFactory
//        {
//            Object Create(byte[] data);
//        }

//        public class ConstantFactory : IValueFactory
//        {
//            private string name;
//            private string measuringUnit;
//            private DataType dataType;
//            public ConstantFactory(string name, string measuringUnit, DataType dataType)
//            {
//                this.name = name;
//                this.measuringUnit = measuringUnit;
//                this.dataType = dataType;
//            }

//            public Object Create(byte[] data)
//            {
//                string value;
//                switch (dataType)
//                {
//                    case DataType.FloatType:
//                        value = string.Format("{0} {1}", BitConverter.ToSingle(data, 2), measuringUnit);
//                        break;
//                    case DataType.ULongType:
//                        value = string.Format("{0} {1}", BitConverter.ToUInt32(data, 2), measuringUnit);
//                        break;
//                    default:
//                        value = "";
//                        break;
//                }
//                return new Constant(name, value);
//            }
//        }

//        public class DataFactory : IValueFactory
//        {
//            private string name;
//            private MeasuringUnitType measuringUnitType;
//            private DataType dataType;
//            public DataFactory(string name, MeasuringUnitType measuringUnitType, DataType dataType)
//            {
//                this.name = name;
//                this.measuringUnitType = measuringUnitType;
//                this.dataType = dataType;
//            }

//            public Object Create(byte[] data)
//            {
//                double value;
//                switch (dataType)
//                {
//                    case DataType.FloatType:
//                        value = BitConverter.ToSingle(data, 2);
//                        break;
//                    case DataType.ULongType:
//                        value = BitConverter.ToUInt32(data, 2);
//                        break;
//                    default:
//                        value = -9999.0;
//                        break;
//                }
//                return new Data(name, measuringUnitType, DateTime.Now, value);
//            }
//        }*/
//        /*
//        public class Register
//        {
//            public MeasuringUnitType MeasuringUnitType { get; private set; }
//            public string ParameterName { get; private set; }
//            public DataType Type { get; private set; }

//            public Register(string pname, MeasuringUnitType mut, DataType type)
//            {
//                ParameterName = pname;
//                MeasuringUnitType = mut;
//                Type = type;
//            }
//        }*/
//        /*
//        public enum DataType { FloatType, ULongHType, ULongLType, ULongType }

//        public static Dictionary<byte, IValueFactory> ValueDictionary = new Dictionary<byte, IValueFactory>()
//        {
//            {0, new DataFactory("Qr", MeasuringUnitType.m3_h, DataType.FloatType)},
//            {1, new DataFactory("P", MeasuringUnitType.kgs_kvSm, DataType.FloatType)},
//            {2, new DataFactory("T", MeasuringUnitType.C, DataType.FloatType)},
//            {3, new DataFactory("Q", MeasuringUnitType.m3_h, DataType.FloatType)},
//            {4, new DataFactory("Wm", MeasuringUnitType.GDj, DataType.FloatType)},
//            {5, new DataFactory("накопленный расход", MeasuringUnitType.Unknown, DataType.ULongHType)},
//            {6, new DataFactory("накопленный расход", MeasuringUnitType.Unknown, DataType.ULongLType)},
//            {7, new ConstantFactory("коммерческий час", "час", DataType.ULongType)},
//            {8, new ConstantFactory("скорость отсечки", "м/с", DataType.FloatType)},
//            {9, new ConstantFactory("плотность н.у.", "кг/м3", DataType.FloatType)},
//            {10, new ConstantFactory("баром.давление", "кгс/см2", DataType.FloatType)},
//            {11, new ConstantFactory("содержание СО2", "молярных долей", DataType.FloatType)},
//            {12, new ConstantFactory("содержание N2", "молярных долей", DataType.FloatType)},
//        };*/
//        /*
//        13 – диаметр трубопровода (мм) н.у., float
//        14 – базовое расстояние в канале А  (мм) при н.у., float
//        15 – материал  трубопровода, unsigned long
//        20 – измеряемая среда, unsigned long (1-природный газ, 4-другая)
//        21 – эмуляция канала P (кгс/см2), float (-800 - выключена)
//        22 – эмуляция канала T (град. Ц), float (-800 - выключена)
//        23 – текущее время, unsigned long (к-во сек, прошедших с 00:00:00 01.01.1997)
//        24 – напряжение литиевой батареи, float (в милливольтах, измеряется ежеминутно)
//        28 – метод расчета коэфф.сжимаемости газа, unsigned long (0-NX19m 1-GERG91)
//        29 – тип термодатчика, unsigned long (0-100М, 1-50М, 2-100П, 3-50П) 
//        30 - эмуляция канала измерения скорости (м/сек), float (-800 - выключена)
//        32 – цикл измерения, unsigned long (2 – 30 сек.)
//        33 – старшая часть накопленной теплоты сгорания, unsigned long (см. формат
//        хранения нак. расхода)
//        34 – младшая часть накопленной теплоты сгорания, unsigned long (см. формат
//        хранения нак. расхода)
//        37 – момент для перехода на летнее время, unsigned long (к-во сек, прошедших с
//        00:00:00 01.01.1997)
//        38 – момент для перехода на зимнее время, unsigned long (к-во сек, прошедших с
//        00:00:00 01.01.1997)
//        40 – время наработки от литиевой батареи, unsigned long (сек.)
//        41 – общее время наработки, unsigned long (сек.)
//        42 – заводской номер прибора, unsigned long
//        64 - Направление потока 0-прямое 1-обратное 2-автовыбор (реверс) unsigned long
//        72 – температура корпуса датчика давления, град.Ц. , float
//        73 – температура корпуса прибора, град.Ц. , float
//        76 -  расход газа за последние коммерческие сутки (м3), float
//        77 -  теплота сгорания за последние коммерческие сутки (ГДж), float
//        80 – скорость газа, м/с
//        90 – нижняя граница скорости (м/сек), float
//        108 – старшая часть накопленного расхода р.у., unsigned long (см. формат хранения
//        нак. расхода)
//        109 – младшая часть накопленного расхода р.у., unsigned long (см. формат
//        хранения нак. расхода)
//        121 - базовое расстояние в канале B  (мм) при н.у., float
//            */


//    }
//}

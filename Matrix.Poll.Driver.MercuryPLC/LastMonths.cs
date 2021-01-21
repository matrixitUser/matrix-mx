using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.MercuryPLC
{
    internal class ParameterValueCurrent1 : ParameterValue
    {
        public ParameterValueCurrent1(string parameter, double value, string unit) : base("Current", parameter, value, unit)
        {

        }
    }



    internal class ParameterValueDay1 : ParameterValue
    {
        public ParameterValueDay1(string parameter, double value, string unit) : base("Day", parameter, value, unit)
        {

        }
    }

    internal class ParameterValue1
    {
        public string Type { get; private set; }
        public string Parameter { get; private set; }
        public double Value { get; private set; }
        public string Unit { get; private set; }
        public ParameterValue1(string type, string parameter, double value, string unit)
        {
            Type = type;
            Parameter = parameter;
            Value = value;
            Unit = unit;
        }

        public const string EE_TARIFF_ALL = "Электроэнергия (все тарифы)";
        public const string EE_TARIFF_FORMAT = "Электроэнергия (тариф {0})";
        public const string EE_TARIFF_PH_A = "Электроэнергия (все тарифы) Фаза A";
        public const string EE_TARIFF_PH_B = "Электроэнергия (все тарифы) Фаза B";
        public const string EE_TARIFF_PH_C = "Электроэнергия (все тарифы) Фаза C";
        public const string EE_UNIT_KWTH = "кВт*ч";

        public static ParameterValue ParseData(byte type, byte[] data)
        {
            switch (type)
            {
                //однофазные счетчики
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                    return new ParameterValueCurrent(string.Format(EE_TARIFF_FORMAT, type + 1), ParseIncValue(data), EE_UNIT_KWTH);

                case 0x0E:
                    return new ParameterValueCurrent(EE_TARIFF_ALL, ParseDecValue(data), EE_UNIT_KWTH);
                case 0x0F:
                    return new ParameterValueCurrent(EE_TARIFF_ALL, ParseIncValue(data), EE_UNIT_KWTH);

                //трехфазные счетчики
                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                    return new ParameterValueCurrent(string.Format(EE_TARIFF_FORMAT, type + 1), ParseIncValue(data), EE_UNIT_KWTH);

                case 0x18:
                    return new ParameterValueCurrent(EE_TARIFF_PH_A, ParseDecValue(data), EE_UNIT_KWTH);
                case 0x19:
                    return new ParameterValueCurrent(EE_TARIFF_PH_B, ParseDecValue(data), EE_UNIT_KWTH);
                case 0x1A:
                    return new ParameterValueCurrent(EE_TARIFF_PH_C, ParseDecValue(data), EE_UNIT_KWTH);

                case 0x1E:
                    return new ParameterValueCurrent(EE_TARIFF_ALL, ParseDecValue(data), EE_UNIT_KWTH);
                case 0x1F:
                    return new ParameterValueCurrent(EE_TARIFF_ALL, ParseIncValue(data), EE_UNIT_KWTH);

                //счётчики электроэнергии, дополнительные параметры
                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                    return new ParameterValueDay(string.Format(EE_TARIFF_FORMAT, type + 1), ParseDecValueExt(data), EE_UNIT_KWTH);

                case 0x44:
                    return new ParameterValueHalfHour(EE_TARIFF_ALL, ParseDecValueHH(data), EE_UNIT_KWTH, data[0]);
            }
            return null;
        }
        public static double ParseIncValue(byte[] data)
        {
            return BitConverter.ToUInt16(data, 0) + data[2];
        }
        public static double ParseDecValue(byte[] data)
        {
            return BitConverter.ToUInt16(data, 0) + (data[2] % 100) / 100.0;
        }
        public static double ParseDecValueHH(byte[] data)
        {
            return BitConverter.ToUInt16(data, 1) + (data[3] % 100) / 100.0;
        }
        public static double ParseDecValueExt(byte[] data)
        {
            return BitConverter.ToUInt32(new byte[] { data[0], data[1], data[2], 0x00 }, 0) + (data[3] % 100) / 100.0;
        }
    }

    internal class Packet1
    {
        public bool Success { get; private set; }
        public byte[] RawData { get; private set; }
        public byte Type { get; private set; }
        public byte[] Data { get; private set; }
        public byte SigLevel { get; private set; }
        public DateTime Date { get; private set; }
        public ParameterValue Parameter { get; private set; }

        public Packet1(byte[] data, int startIndex, int length)
        {
            Success = false;
            if (data.Length < (startIndex + length)) return;
            if (length != 11) return;

            RawData = data.Skip(startIndex).Take(length).ToArray();
            Type = RawData[0];
            Data = RawData.Skip(1).Take(4).ToArray();
            SigLevel = RawData[5];
            Date = new DateTime(hour: RawData[7], minute: RawData[6], second: 0, day: RawData[8] + 1, month: RawData[9] + 1, year: RawData[10] + 2000);
            Parameter = ParameterValue.ParseData(Type, Data);

            Success = true;
        }

        public bool HasParameter()
        {
            return Success && Parameter != null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TEM104
{
    public class SysInt
    {
        SysInt()
        {
            IntV = new RawData("V", 4, MeasuringUnitType.m3);
            IntM = new RawData("M", 4, MeasuringUnitType.tonn);
            IntQ = new RawData("Q", 4, MeasuringUnitType.MWt);

            Trab = new RawData("TimeTurn", 1, MeasuringUnitType.sec);
            Tnar = new RawData("TimeWork", 4, MeasuringUnitType.sec);
            //Tmin = new RawData(ParameterType.TimeWork, 4, MeasuringUnitType.sec);
            //Tmax = new RawData(ParameterType.TimeWork, 4, MeasuringUnitType.sec);
            //Tdt = new RawData(ParameterType.TimeWork, 4, MeasuringUnitType.sec);
            //Ttn = new RawData(ParameterType.TimeWork, 4, MeasuringUnitType.sec);

            Rshv = new RawData("ConsumptionVolume", 4, MeasuringUnitType.m3_h);

            T = new RawData("Temperature", 12, MeasuringUnitType.C);
            P = new RawData("Pressure", 12, MeasuringUnitType.MPa);
        }

        public RawData IntV { get; private set; }
        public RawData IntQ { get; private set; }
        public RawData IntM { get; private set; }

        public RawData Trab { get; private set; }

        public RawData Tnar { get; private set; }
        //public RawData Tmin { get; private set; }
        //public RawData Tmax { get; private set; }
        //public RawData Tdt { get; private set; }
        //public RawData Ttn { get; private set; }

        public RawData Rshv { get; private set; }

        public RawData T { get; private set; }
        public RawData P { get; private set; }

        public DateTime date { get; private set; }

        public static SysInt Parse(byte[] data, int offset)
        {
            if (data == null || data.Length < 0xFF + offset) return null;

            var result = new SysInt();

            DateTime curdate;
            var dateTimeString = string.Format("{0}.{1}.{2} {3}:00:00",
                                ConvertHelper.BinDecToInt(data[offset + 1]), //day
                                ConvertHelper.BinDecToInt(data[offset + 2]), //mon
                                ConvertHelper.BinDecToInt(data[offset + 3]), //yr
                                ConvertHelper.BinDecToInt(data[offset + 0]) //hr
            );
            DateTime.TryParse(dateTimeString, out curdate);
            result.date = curdate;

            result.Trab.Value[0] = BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x68, 4), 0);

            for (var sysch = 0; sysch < 4; sysch++)
            {
                result.IntV.Value[sysch] = 
                    BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0x08 + sysch * 4, 4), 0) +
                    BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x38 + sysch * 4, 4), 0);
                result.IntM.Value[sysch] = 
                    BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0x18 + sysch * 4, 4), 0) +
                    BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x48 + sysch * 4, 4), 0);
                result.IntQ.Value[sysch] = 
                    BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0x28 + sysch * 4, 4), 0) +
                    BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x58 + sysch * 4, 4), 0);

                result.Tnar.Value[sysch] = BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x6c + sysch * 4, 4), 0);
                //result.Tmin.Value[sysch] = BitConverter.ToInt32(GetReversed(data, offset + 0x7c + sysch * 4, 4), 0);
                //result.Tmax.Value[sysch] = BitConverter.ToInt32(GetReversed(data, offset + 0x8c + sysch * 4, 4), 0);
                //result.Tdt.Value[sysch] = BitConverter.ToInt32(GetReversed(data, offset + 0x9c + sysch * 4, 4), 0);
                //result.Ttn.Value[sysch] = BitConverter.ToInt32(GetReversed(data, offset + 0xac + sysch * 4, 4), 0);

                result.Rshv.Value[sysch] = BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0xec + sysch * 4, 4), 0);

                for (int i = 0; i < 3; i++)
                {
                    result.T.Value[sysch * 3 + i] = (double)(BitConverter.ToInt16(ConvertHelper.GetReversed(data, offset + 0xc8 + (sysch * 3 + i) * 2, 2), 0)) / 100;
                    result.P.Value[sysch * 3 + i] = (double)(data[offset + 0xe0 + (sysch * 3 + i)]) / 100;
                }
            }

            return result;
        }
    }
}

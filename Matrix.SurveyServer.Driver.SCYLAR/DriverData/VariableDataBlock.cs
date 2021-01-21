using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SCYLAR.DriverData
{
    internal class VariableDataBlock
    {
        private VariableDataBlock()
        {
        }

        public DataRecordHeader Drh { get; private set; }

        public object Value { get; private set; }
        public int Length { get; private set; }
        public DriverParameter DriverParameter { get; private set; }

        public static VariableDataBlock Parse(byte[] data, int index)
        {
            if (data == null) return null;

            var drh = DataRecordHeader.Parse(data, index);
            if (drh == null) return null;

            var result = new VariableDataBlock { Drh = drh };
            result.ParseValue(data, index + drh.Length);
            return result;
        }

        private void ParseValue(byte[] data, int index)
        {
            if (data == null) return;
            var valueLength = Drh.Dib.Dif.DataType.GetLentgh();
            if (index + valueLength > data.Length) return;
            var quantityMas = data.Skip(index).Take(valueLength).ToArray();
            //if (quantityMas.Length % 2 == 0)
            //{
            //    for (int i = 0; i < quantityMas.Length; i=i+2)
            //    {
            //        var temp = quantityMas[i];
            //        quantityMas[i] = quantityMas[i + 1];
            //        quantityMas[i + 1] = temp;
            //    }
            //}
            double value = 0;
            switch (Drh.Dib.Dif.DataType)
            {
                case DataType.NoData:
                    break;
                case DataType.BitInteger8:
                    value = (int)BitConverter.ToChar(quantityMas, 0);
                    break;
                case DataType.BitInteger16:
                    value = BitConverter.ToInt16(quantityMas, 0);
                    break;
                case DataType.BitInteger24:
                    break;
                case DataType.BitInteger32:
                    value = BitConverter.ToInt32(quantityMas, 0);
                    break;
                case DataType.BitReal32:
                    value = BitConverter.ToSingle(quantityMas, 0);
                    break;
                case DataType.BitInteger48:
                    break;
                case DataType.BitInteger64:
                    value = BitConverter.ToInt64(quantityMas, 0);
                    break;
                case DataType.ReadoutSelection:
                    break;
                case DataType.Bcd2:
                    break;
                case DataType.Bcd4:
                    break;
                case DataType.Bcd6:
                    break;
                case DataType.Bcd8:
                    break;
                case DataType.VariableLength:
                    break;
                case DataType.Bcd12:
                    break;
                case DataType.SpecialFunction:
                    break;
            }
            Length = Drh.Length + valueLength;
            ParseParameter(value, quantityMas);
        }

        private void ParseParameter(double value, byte[] rowData)
        {
            if (!Drh.Vib.Vif.HasExtendedBlock)
                ParseStandardParameter(value, rowData);
            else
            {
                if (Drh.Vib.Vif.UnitAndMultiplier == 0x7d)
                {
                    ParseExtendedParameter1(value, rowData);
                }
                else if (Drh.Vib.Vif.UnitAndMultiplier == 0x7d)
                {
                    ParseExtendedParameter2(value, rowData);
                }
            }

        }



        private void ParseStandardParameter(double value, byte[] rowData)
        {
            var mask = (Drh.Vib.Vif.UnitAndMultiplier >> 3) & 0xf;
            var multiplier = Drh.Vib.Vif.UnitAndMultiplier & 0x07;
            switch (mask)
            {
                case 0:
                    {
                        DriverParameter = DriverParameter.EnergyWh;
                        Value = Math.Pow(10, multiplier - 3) * value;
                    }
                    break;
                case 1:
                    {
                        DriverParameter = DriverParameter.EnergyJ;
                        Value = Math.Pow(10, multiplier) * value;
                    }
                    break;
                case 2:
                    {
                        DriverParameter = DriverParameter.Volume;
                        Value = Math.Pow(10, multiplier - 6) * value;
                    }
                    break;
                case 3:
                    {
                        DriverParameter = DriverParameter.Mass;
                        Value = Math.Pow(10, multiplier - 3) * value;
                    }
                    break;
                case 4:
                    {
                        var localMultiplier = multiplier & 0x03;
                        var type = multiplier >> 2;
                        if (type == 0)
                        {
                            if (localMultiplier == 0)
                                DriverParameter = DriverParameter.OnTimeSeconds;
                            else if (localMultiplier == 1)
                                DriverParameter = DriverParameter.OnTimeMinutes;
                            else if (localMultiplier == 2)
                                DriverParameter = DriverParameter.OnTimeHours;
                            else if (localMultiplier == 3)
                                DriverParameter = DriverParameter.OnTimeDays;
                        }
                        else if (type == 1)
                        {
                            if (localMultiplier == 0)
                                DriverParameter = DriverParameter.OperatingTimeSeconds;
                            else if (localMultiplier == 1)
                                DriverParameter = DriverParameter.OperatingTimeMinutes;
                            else if (localMultiplier == 2)
                                DriverParameter = DriverParameter.OperatingTimeHours;
                            else if (localMultiplier == 3)
                                DriverParameter = DriverParameter.OperatingTimeDays;
                        }

                        Value = value;
                    }
                    break;
                case 5:
                    {
                        DriverParameter = DriverParameter.PowerW;
                        Value = Math.Pow(10, multiplier - 3) * value;
                    }
                    break;
                case 6:
                    {
                        DriverParameter = DriverParameter.PowerJh;
                        Value = Math.Pow(10, multiplier) * value;
                    }
                    break;
                case 7:
                    {
                        DriverParameter = DriverParameter.VolumeFlowm3h;
                        Value = Math.Pow(10, multiplier - 6) * value;
                    }
                    break;
                case 8:
                    {
                        DriverParameter = DriverParameter.VolumeFlowExtm3min;
                        Value = Math.Pow(10, multiplier - 7) * value;
                    }
                    break;
                case 9:
                    {
                        DriverParameter = DriverParameter.VolumeFlowExtm3s;
                        Value = Math.Pow(10, multiplier - 9) * value;
                    }
                    break;
                case 10:
                    {
                        DriverParameter = DriverParameter.MassFlow;
                        Value = Math.Pow(10, multiplier - 3) * value;
                    }
                    break;
                case 11:
                    {
                        var localMultiplier = multiplier & 0x03;
                        var type = multiplier >> 2;

                        if (type == 0)
                        {
                            DriverParameter = DriverParameter.FlowTemperature;
                            Value = Math.Pow(10, localMultiplier - 3) * value;
                        }
                        else if (type == 1)
                        {
                            DriverParameter = DriverParameter.ReturnTemperature;
                            Value = Math.Pow(10, localMultiplier - 3) * value;
                        }
                    }
                    break;
                case 12:
                    {
                        var localMultiplier = multiplier & 0x03;
                        var type = multiplier >> 2;

                        if (type == 0)
                        {
                            DriverParameter = DriverParameter.TemperatureDifferenceK;
                            Value = Math.Pow(10, localMultiplier - 3) * value;
                        }
                        else if (type == 1)
                        {
                            DriverParameter = DriverParameter.ExternalTemperature;
                            Value = Math.Pow(10, localMultiplier - 3) * value;
                        }
                    }
                    break;
                case 13:
                    {
                        var localMultiplier = multiplier & 0x03;
                        var type = multiplier >> 2;

                        if (type == 0)
                        {
                            DriverParameter = DriverParameter.Pressure;
                            Value = Math.Pow(10, localMultiplier - 3) * value;
                        }
                        else if (type == 1)
                        {
                            DriverParameter = DriverParameter.TimePoint;
                            if ((localMultiplier & 0x01) == 0)
                                Value = ParseDate(rowData);
                            else
                                Value = ParseDateTime(rowData);
                        }
                    }
                    break;
                //case 7:
                //    {
                //    }
                //    break;
            }
        }
        private void ParseExtendedParameter1(double value, byte[] rowData)
        {
            if (Drh.Vib.Vife == null || !Drh.Vib.Vife.Any()) return;
            switch (Drh.Vib.Vife.FirstOrDefault().Data)
            {
                case 0x08:
                    {
                        DriverParameter = DriverParameter.AccessNumber;
                        Value = value;
                        return;
                    }
                case 0x09:
                    {
                        DriverParameter = DriverParameter.Medium;
                        Value = 0.1 * value;
                        return;
                    }
                case 0x0a:
                    {
                        DriverParameter = DriverParameter.Manufacturer;
                        Value = value;
                        return;
                    }
                case 0x0b:
                    {
                        DriverParameter = DriverParameter.ParameterSetIdentification;
                        Value = 0.001 * value;
                        return;
                    }
                case 0x0c:
                    {
                        DriverParameter = DriverParameter.Model;
                        Value = value;
                        return;
                    }
                case 0x0d:
                    {
                        DriverParameter = DriverParameter.HardwareVersion;
                        Value = value;
                        return;
                    }
                case 0x0e:
                    {
                        DriverParameter = DriverParameter.FirmwareVersion;
                        Value = value;
                        return;
                    }
                case 0x0f:
                    {
                        DriverParameter = DriverParameter.SoftWareVersion;
                        Value = value;
                        return;
                    }
                case 0x10:
                    {
                        DriverParameter = DriverParameter.CustomerLocation;
                        Value = value;
                        return;
                    }
                case 0x11:
                    {
                        DriverParameter = DriverParameter.Customer;
                        Value = value;
                        return;
                    }
                case 0x12:
                    {
                        DriverParameter = DriverParameter.AccessCodeUser;
                        Value = value;
                        return;
                    }
                case 0x13:
                    {
                        DriverParameter = DriverParameter.AccessCodeOperator;
                        Value = value;
                        return;
                    }
                case 0x14:
                    {
                        DriverParameter = DriverParameter.AccessCodeSystemOperator;
                        Value = value;
                        return;
                    }
                case 0x15:
                    {
                        DriverParameter = DriverParameter.AccessCodeDeveloper;
                        Value = value;
                        return;
                    }
                case 0x16:
                    {
                        DriverParameter = DriverParameter.Password;
                        Value = value;
                        return;
                    }
                case 0x17:
                    {
                        DriverParameter = DriverParameter.ErrorFlags;
                        Value = value;
                        return;
                    }
                case 0x18:
                    {
                        DriverParameter = DriverParameter.ErrorMask;
                        Value = value;
                        return;
                    }
                case 0x1a:
                    {
                        DriverParameter = DriverParameter.DigitalOutput;
                        Value = value;
                        return;
                    }
                case 0x1b:
                    {
                        DriverParameter = DriverParameter.DigitalInput;
                        Value = value;
                        return;
                    }
                case 0x1c:
                    {
                        DriverParameter = DriverParameter.Baudrate;
                        Value = value;
                        return;
                    }
                case 0x1d:
                    {
                        DriverParameter = DriverParameter.ResponseDelayTime;
                        Value = value;
                        return;
                    }
                case 0x1e:
                    {
                        DriverParameter = DriverParameter.Retry;
                        Value = value;
                        return;
                    }
                case 0x61:
                    {
                        DriverParameter = DriverParameter.CumulationCounter;
                        Value = value;
                        return;
                    }
            }
        }
        private void ParseExtendedParameter2(double value, byte[] rowData)
        {
            if (Drh.Vib.Vife==null || !Drh.Vib.Vife.Any())return;
            switch (Drh.Vib.Vife.FirstOrDefault().Data)
            {
                case 0x21:
                    {
                        DriverParameter = DriverParameter.VolumeExFeet;
                        Value = 0.1 * value;
                        return;
                    }
                case 0x22:
                    {
                        DriverParameter = DriverParameter.VolumeExAgDel;
                        Value = 0.1 * value;
                        return;
                    }
                case 0x23:
                    {
                        DriverParameter = DriverParameter.VolumeExAg;
                        Value =  value;
                        return;
                    }
                case 0x24:
                    {
                        DriverParameter = DriverParameter.VolumeFlowExAgMinDel;
                        Value = 0.001 * value;
                        return;
                    }
                case 0x25:
                    {
                        DriverParameter = DriverParameter.VolumeFlowExAgMin;
                        Value = value;
                        return;
                    }
                case 0x26:
                    {
                        DriverParameter = DriverParameter.VolumeFlowExAgH;
                        Value = value;
                        return;
                    }
            }

            var mask = (Drh.Vib.Vife.FirstOrDefault().Data >> 2) & 0x1f;
            var multiplier = Drh.Vib.Vife.FirstOrDefault().Data & 0x03;

            switch (mask)
            {
                case 0x00:
                    {
                        DriverParameter = DriverParameter.EnergyExMwh;
                        Value = Math.Pow(10, multiplier - 1) * value;
                        return;
                    }
                case 0x04:
                    {
                        DriverParameter = DriverParameter.EnergyExGJ;
                        Value = Math.Pow(10, multiplier - 1) * value;
                        return;
                    }
                case 0x08:
                    {
                        DriverParameter = DriverParameter.VolumeExm3;
                        Value = Math.Pow(10, multiplier + 2) * value;
                        return;
                    }
                case 0x0c:
                    {
                        DriverParameter = DriverParameter.MassEx;
                        Value = Math.Pow(10, multiplier + 2) * value;
                        return;
                    }
                case 0x14:
                    {
                        DriverParameter = DriverParameter.PowerExMW;
                        Value = Math.Pow(10, multiplier - 1) * value;
                        return;
                    }
                case 0x18:
                    {
                        DriverParameter = DriverParameter.PowerExGJ;
                        Value = Math.Pow(10, multiplier - 1) * value;
                        return;
                    }
            }

            var doubleMask = (Drh.Vib.Vife.FirstOrDefault().Data >> 2) & 0x1f;
            var doubleMultiplier = Drh.Vib.Vife.FirstOrDefault().Data & 0x03;

            switch (doubleMask)
            {
                case 0x16:
                    {
                        DriverParameter = DriverParameter.FlowTemperatureEx;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
                case 0x17:
                    {
                        DriverParameter = DriverParameter.ReturnTemperatureEx;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
                case 0x18:
                    {
                        DriverParameter = DriverParameter.TemperatureDifferenceEx;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
                case 0x19:
                    {
                        DriverParameter = DriverParameter.ExternalTemperatureEx;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
                case 0x1c:
                    {
                        DriverParameter = DriverParameter.TemperatureLimitF;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
                case 0x1d:
                    {
                        DriverParameter = DriverParameter.TemperatureLimitC;
                        Value = Math.Pow(10, doubleMultiplier - 3) * value;
                        return;
                    }
            }

            var thirdMask = (Drh.Vib.Vife.FirstOrDefault().Data >> 3) & 0xf;
            var thirdMultiplier = Drh.Vib.Vife.FirstOrDefault().Data & 0x07;

            if (thirdMask == 0x0f)
            {
                DriverParameter = DriverParameter.CumulMaxPower;
                Value = Math.Pow(10, thirdMultiplier - 3) * value;
                return;
            }

        }

        private DateTime ParseDate(byte[] data)
        {
            return default(DateTime);
        }
        private DateTime ParseDateTime(byte[] data)
        {
            if (data.Length != 4) return default(DateTime);
            int minute = data[0] & 0x3F;
            int hour = data[1] & 0x1F;
            int day = data[2] & 0x1F;
            int month = data[3] & 0x0F;
            int year = ((data[3] & 0xF0) >> 1) | ((data[2] & 0xE0) >> 5) + 2000;
            try
            {
                var date = new DateTime(year, month, day, hour, minute, 0);
                return date;
            }
            catch { return default(DateTime); }
        }
        public override string ToString()
        {
            return string.Format("{0} - {1}", DriverParameter, Value);
        }
    }
}

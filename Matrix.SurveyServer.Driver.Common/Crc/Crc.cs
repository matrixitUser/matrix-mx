using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common.Crc
{
    public class Crc
    {
        public byte[] CrcData { get; private set; }

        public Crc(byte[] crcData)
        {
            this.CrcData = crcData;
        }

        public static Crc Calc(byte[] buffer, int offset, int length, ICrcCalculator calculator)
        {
            if (calculator != null && buffer.Length > offset && buffer.Length - offset >= length)
            {
                return calculator.Calculate(buffer, offset, length);
            }
            return new Crc(null);
        }

        public static Crc Calc(byte[] buffer, ICrcCalculator calculator)
        {
            return Calc(buffer, 0, buffer.Length, calculator);
        }

        public static bool Check(byte[] buffer, int offset, int length, ICrcCalculator calculator)
        {
            bool success = false;

            if (calculator != null &&
                buffer.Length > offset &&
                buffer.Length - offset >= length &&
                length > calculator.CrcDataLength)
            {
                var crc = Calc(buffer, offset, length - calculator.CrcDataLength, calculator);
                success = crc.CrcData.Length == calculator.CrcDataLength;
                for (int i = 0; i < calculator.CrcDataLength; i++)
                {
                    if (!success) break;
                    success &= buffer[offset + length - calculator.CrcDataLength + i] == crc.CrcData[i];                    
                }
            }

            return success;
        }
        public static bool CheckReverse(byte[] buffer, int offset, int length, ICrcCalculator calculator)
        {
            bool success = false;

            if (calculator != null &&
                buffer.Length > offset &&
                buffer.Length - offset >= length &&
                length > calculator.CrcDataLength)
            {
                var crc = Calc(buffer, offset, length - calculator.CrcDataLength, calculator);
                success = crc.CrcData.Length == calculator.CrcDataLength;
                for (int i = 0; i < calculator.CrcDataLength; i++)
                {
                    if (!success) break;
                    success &= buffer[offset + length - calculator.CrcDataLength + i] == crc.CrcData.Reverse().ToArray()[i];
                }
            }

            return success;
        }
        public static bool Check(byte[] buffer, ICrcCalculator calculator)
        {
            return Check(buffer, 0, buffer.Length, calculator);
        }
        public static bool CheckReverse(byte[] buffer, ICrcCalculator calculator)
        {
            return CheckReverse(buffer, 0, buffer.Length, calculator);
        }
    }
}

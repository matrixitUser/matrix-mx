using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        private byte CalcHartCrc(byte[] buffer)
        {
            return CalcHartCrc(buffer, 0, buffer.Length);
        }

        private byte CalcHartCrc(byte[] buffer, int offset, int length)
        {
            byte crc = buffer[offset];
            for (int i = offset + 1; i < length; i++)
            {
                crc ^= buffer[i];
            }
            return crc;
        }

        private bool CheckHartCrc(byte[] buffer, int offset, int length)
        {
            var crc = CalcHartCrc(buffer, 0, buffer.Length - 1);
            return buffer.Last() == crc;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    partial class Driver
    {
        private byte CalcCrc(byte[] buffer, int offset, int length)
        {
            char bcc = Encoding.ASCII.GetChars(new byte[] { buffer[offset] })[0];

            if (length > 2)
            {
                for (int i = offset + 1; i < length + offset; i++)
                {
                    bcc ^= Encoding.ASCII.GetChars(new byte[] { buffer[i] })[0];
                }
            }

            return Encoding.ASCII.GetBytes(new char[] { bcc })[0];
        }

        private byte CalcCrc(List<byte> bytes )
        {
            return CalcCrc(bytes.ToArray(), 0, bytes.Count);
        }

        private bool CheckCrc(byte[] bytes)
        {
            if (bytes == null) return false;

            var crcClc = CalcCrc(bytes, 1, bytes.Length - 2);
            var crcMsg = bytes[bytes.Length - 2];

            return crcClc == crcMsg;
        }
    }
}

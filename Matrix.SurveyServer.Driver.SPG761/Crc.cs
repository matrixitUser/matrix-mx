using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        public byte[] CrcCalc(byte[] buffer, int offset, int length)
        {
            ushort crc = 0;

            for (ushort i = (ushort)offset; i < offset + length; i++)
            {
                crc = (ushort)(crc ^ (ushort)(buffer[i] << 8));
                for (ushort j = 0; j < 8; j++)
                {
                    if ((ushort)(crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
                    else crc <<= 1;
                }
            }
            return new byte[] { (byte)(crc >> 8), (byte)(crc & 0x00FF) };
        }

        public bool CrcCheck(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;
            var crcMsg = bytes.Skip(bytes.Length - 2).Take(2).ToArray();
            //пропускаем два первых байта, которые не участвуют в подсчете
            var crcClc = CrcCalc(bytes, 2, bytes.Length - 4);
            
            for (int i = 0; i < 2; i++)
            {
                if (crcClc[i] != crcMsg[i]) return false;
            }

            return true;
        }
    }
}

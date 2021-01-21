using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private byte[] CalcCrc16(byte[] bytes)
        {
            ushort polynomial = 0xA001;
            ushort[] table = new ushort[256];
            ushort value;
            ushort temp;

            for (ushort i = 0; i < table.Length; i++)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; j++)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }

            ushort crc = 0xFFFF;

            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            var crcBytes = new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) };
            return crcBytes;
        }

        private bool CheckCrc16(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3) return false;

            var crc = CalcCrc16(bytes.Take(bytes.Length - 2).ToArray());
            var crcMsg = bytes.Skip(bytes.Length - 2).ToArray();

            for (int i = 0; i < 2; i++)
            {
                if (crc[i] != crcMsg[i]) return false;
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.MatrixControllers
{
    static class Crc
    {
        public static bool Check(byte[] buffer, int offset, int length)
        {
            var crc = Calc(buffer, offset, length - 2);
            return buffer[offset + length - 2] == crc[0] && buffer[offset + length - 2 + 1] == crc[1];
        }

        public static byte[] Calc(byte[] buffer, int offset, int length)
        {
            ushort polynomial = 0xA001;
            var table = new ushort[256];
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


            for (int i = offset; i < length; i++)
            {
                byte index = (byte)(crc ^ buffer[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            return new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) };
        }
    }
}

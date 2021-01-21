using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes.Matrix
{
    static class Crc16Modbus
    {
        public static byte[] CrcCalculate(byte[] bytes, bool reverse = false)
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


            if (reverse)
            {
                for (int i = bytes.Length - 1; i >= 0; i--)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
            }
            else
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte index = (byte)(crc ^ bytes[i]);
                    crc = (ushort)((crc >> 8) ^ table[index]);
                }
            }

            return new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) };
        }

        public static bool CrcCheck(IEnumerable<byte> bytes, bool reverse = false)
        {
            int Length = bytes.Count();
            byte[] crcClc = CrcCalculate(bytes.Take(Length - 2).ToArray(), reverse);
            byte[] crcMsg = bytes.Skip(Length - 2).Take(2).ToArray();

            return (crcClc[0] == crcMsg[0] && crcClc[1] == crcMsg[1]);
        }        
    }
}

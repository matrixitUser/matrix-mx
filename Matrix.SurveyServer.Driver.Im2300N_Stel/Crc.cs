using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private byte CalcStelCrc(byte[] buffer, int offset, int length)
        {
            var sum = buffer.Skip(offset).Take(length).Sum(b => b);
            var s = (byte)(258 - sum);
            return s;
        }

        //private byte CrC8(byte[] buffer, int offset, int length)
        //{
        //    int sum = 0;
        //    for (int i = offset; i < offset + length; i++)
        //    {
        //        sum += buffer[i];
        //    }

        //    //return (byte)(sum & 0xFF);
        //    return (byte)(sum % 0xFF);
        //}

        private bool CheckCrC8(byte[] buffer)
        {
            int sum = 0;
            foreach (var b in buffer.Take(buffer.Length - 1))
            {
                sum += b;
            }

            byte crc = buffer.Last();
            return ((byte)(sum & 0xFF) == crc || (byte)(sum % 0xFF) == crc);

            //var crc = CrC8(buffer, 0, buffer.Length - 1);
            //return buffer.Last() == crc;
        }

        private byte[] CalcCrc16Modbus(byte[] buffer)
        {
            return CalcCrc16Modbus(buffer, 0, buffer.Length);
        }

        private byte[] CalcCrc16Modbus(byte[] buffer, int offset, int length)
        {
            ushort crc = 0xFFFF;

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

            for (int i = offset; i < length; i++)
            {
                byte index = (byte)(crc ^ buffer[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            return new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) };
        }

        private bool CheckCrc16Modbus(byte[] buffer)
        {
            return true;
        }
    }
}

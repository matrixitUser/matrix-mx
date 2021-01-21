using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
        private Int16 Crc16Calc(byte[] body)
        {
            UInt16 crc = 0;
            int length = body.Length;

            for (int i = 0; i < body.Length; i++)
            {
                crc = (UInt16)(crc ^ (UInt16)(body[i] << 8));
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (UInt16)((crc << 1) ^ 0x1021);
                    else
                        crc = (UInt16)(crc <<= 1);
                }
            }
            return (Int16)(crc * 256 + crc / 256);
        }

        private bool Crc16Check(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3) return false;

            var crcClc = Crc16Calc(bytes.Skip(1).Take(bytes.Length - 3).ToArray());
            var crcMsg =  BitConverter.ToInt16(bytes, bytes.Length - 2);

            return crcClc == crcMsg;
        }
    }
}

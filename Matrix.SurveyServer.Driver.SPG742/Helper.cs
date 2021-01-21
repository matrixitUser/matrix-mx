using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    static class Helper
    {
        /// <summary>
        /// конвертация в тип float
        /// см. документацию приложение 3 (стр. 22)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static float SpgFloatToIEEE(byte[] data, int offset)
        {
            float result = 0;
            if (data != null && offset >= 0 && offset <= data.Length - 4)
            {
                var spgFloat = new byte[4];
                Array.Copy(data, offset, spgFloat, 0, 4);
                byte sign = (byte)(spgFloat[2] & 0x80);
                byte factorLsb = (byte)(spgFloat[3] & 1);
                spgFloat[2] |= (byte)(factorLsb << 7);
                spgFloat[3] >>= 1;
                spgFloat[3] |= sign;
                result = BitConverter.ToSingle(spgFloat, 0);
            }
            return result;
        }

        public static byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        public static byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }
    }
}

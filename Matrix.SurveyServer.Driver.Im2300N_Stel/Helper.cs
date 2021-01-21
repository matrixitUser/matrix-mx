using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        private byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
        {
            return source.Skip(start).Take(count).Reverse().ToArray();
        }

        public static uint ToUInt32(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
        }

        public static int ToInt32(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToInt32(Reverse(data, startIndex, 4), 0);
        }

        public static uint ToUInt16(byte[] data, int startIndex)
        {
            return BitConverter.ToUInt16(Reverse(data, startIndex, 2), 0);
        }

        public static float ToSingle(byte[] data, int startIndex)
        {
            return BitConverter.ToSingle(Reverse(data, startIndex, 4), 0);
        }

        public static float ToInt16(byte[] data, int startIndex)
        {
            return BitConverter.ToInt16(Reverse(data, startIndex, 2), 0);
        }

        public static double ToLongAndFloat(byte[] data, int offset)
        {
            double result = 0.0;
            result += ToInt32(data, offset);
            result += ToSingle(data, offset + 4);
            return result;
        }

        
        public static byte IntToBinDec(int toBCD)
        {
            byte result = 0xFF;
            if (toBCD < 100)
            {
                result = 0;
                result |= (byte)(toBCD % 10);
                toBCD /= 10;
                result |= (byte)((toBCD % 10) << 4);
            }
            return result;
        }

        private int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }
    }
}

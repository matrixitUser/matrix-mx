using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.WLS
{
	static class Helper
	{
		public static byte GetLowByte(UInt16 b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(UInt16 b)
		{
			return (byte)((b >> 8) & 0xFF);
		}
        
		public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
		{
			return source.Skip(start).Take(count).Reverse().ToArray();
		}
        public static byte[] ReverseFourBytes(byte[] data)
        {
            return new byte[]{ data[2], data[3], data[0], data[1] };
        }
        public static Int32 ToInt32(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToInt32(Reverse(data, startIndex, 4), 0);
		}

		public static Int16 ToInt16(byte[] data, int startIndex)
		{
			return BitConverter.ToInt16(Reverse(data, startIndex, 2), 0);
		}

		public static float ToSingle(byte[] data, int startIndex)
		{
			return BitConverter.ToSingle(Reverse(data, startIndex, 4), 0);
		}
        public static float ToOfterSingle(byte[] data, int startIndex)
        {
            return BitConverter.ToSingle(ReverseFourBytes(data.Skip(startIndex).Take(4).ToArray()).Reverse().ToArray(), 0);
        }
        public static UInt16 ToUInt16(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToUInt16(Reverse(data, startIndex, 2), 0);
        }

        public static UInt32 ToUInt32(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
        }
	}
}

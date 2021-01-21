using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.TSRV024
{
	static class Helper
	{
		public static byte GetLowByte(int b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(int b)
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

		public static UInt16 ToUInt16(byte[] data, int startIndex)
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
	}
}

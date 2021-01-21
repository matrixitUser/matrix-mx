using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
	public partial class Driver
	{
		public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
		{
			return source.Skip(start).Take(count).Reverse().ToArray();
		}

		public static int ToInt32(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToInt32(Reverse(data, startIndex, 4), 0);
		}
		public static uint ToUInt32(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
		}


		public static short ToInt16(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToInt16(Reverse(data, startIndex, 2), 0);
		}

		public static float ToSingle(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToSingle(Reverse(data, startIndex, 4), 0);
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

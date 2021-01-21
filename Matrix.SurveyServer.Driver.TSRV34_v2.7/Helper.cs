using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV34
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

		public static byte[] Reverse(byte[] source, int start, int count)
		{
			return source.Skip(start).Take(count).Reverse().ToArray();
		}

		public static uint ToUInt32(byte[] data, int startIndex)
		{
			return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
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
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public static class Helper
	{
		public static byte GetLowByte(int b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(int b)
		{
			return (byte)((b >> 8) & 0xFF);
		}

		public static int ToInt32(byte[] data, int offset)
		{
			var x = data.Skip(offset).Take(4).Reverse().ToArray();
			return BitConverter.ToInt32(x, 0);
		}

		public static int ToInt16(byte[] data, int offset)
		{
			var x = data.Skip(offset).Take(2).Reverse().ToArray();
			return BitConverter.ToInt16(x, 0);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	public static class Helper
	{
		public static uint MercuryStrange(IEnumerable<byte> data, int startIndex, bool power = false)
		{
            byte mask = power ? (byte)0x3F : (byte)0xFF;
			return ((uint)(data.ElementAt(startIndex) & mask) << 16) +
				(uint)(data.ElementAt(startIndex + 1)) +
				((uint)(data.ElementAt(startIndex + 2)) << 8);
		}

		public static byte[] Reverse(IEnumerable<byte> source, int start, int count)
		{
			return source.Skip(start).Take(count).Reverse().ToArray();
		}

		public static int ToInt32(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToInt32(new byte[]{
				data.ElementAt(startIndex+2),
				data.ElementAt(startIndex+3),
				data.ElementAt(startIndex+0),
				data.ElementAt(startIndex+1)
			}, 0);
		}

		public static short ToInt16(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToInt16(Reverse(data, startIndex, 2), 0);
		}

		public static UInt16 ToUInt16(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToUInt16(Reverse(data, startIndex, 2), 0);
		}

		public static float ToSingle(IEnumerable<byte> data, int startIndex)
		{
			return BitConverter.ToSingle(Reverse(data, startIndex, 4), 0);
		}

        public static byte GetLowByte(UInt32 b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(UInt32 b)
		{
			return (byte)((b >> 8) & 0xFF);
		}

		public static byte FromBCD(byte sourceByte)
		{
			byte left = (byte)(sourceByte >> 4 & 0xf);
			byte right = (byte)(sourceByte & 0xf);
			return (byte)(left * 10 + right);
        }

        public static byte ToBCD1(byte sourceByte)
        {
            byte left = (byte)((sourceByte % 100) / 10);
            byte right = (byte)(sourceByte % 10);
            return (byte)((left << 4) + right);
        }
    }
}

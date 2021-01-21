using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
	public static class Helper
	{
		public static DateTime ParseDateTime(byte[] data, int startIndex = 0)
		{

			if (data == null || data.Length < startIndex + 5) return default(DateTime);

			return new DateTime(2000 + data[4 + startIndex],
									   data[3 + startIndex],
									   data[2 + startIndex],
									   data[1 + startIndex],
									   data[startIndex],
									   0);
		}

		public static int ParseTimeSeconds(byte[] data, int startIndex = 0)
		{
			if (data == null || data.Length < startIndex + 3) return 0;
			var second = data[0 + startIndex];
			var minute = data[1 + startIndex];
			var hour = data[2 + startIndex];
			return second + minute * 60 + hour * 60 * 60;
		}

		public static byte[] GetPassword(string password)
		{
			var data = new byte[]
			{
				0x00,
				0x00			
			};

			if (!string.IsNullOrWhiteSpace(password))
			{
				int pass;
				int.TryParse(password, out pass);
				data[0] = (byte)(pass >> 8);
				data[1] = (byte)(pass & 0x00FF);
			}
			return data;
		}

		public static byte GetLowByte(int b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(int b)
		{
			return (byte)((b >> 8) & 0xFF);
		}

		public static ushort ToUInt16(IEnumerable<byte> data, int offset)
		{
			return BitConverter.ToUInt16(data.Skip(offset).Take(2).Reverse().ToArray(), 0);
		}

		public static uint ToUInt32(IEnumerable<byte> data, int offset)
		{
			var temp = data.Skip(offset).Take(4).Reverse().ToArray();
			return BitConverter.ToUInt32(data.Skip(offset).Take(4).Reverse().ToArray(), 0);
		}
        public static int BinDecToInt(byte binDec)
        {
            return (binDec >> 4) * 10 + (binDec & 0x0f);
        }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.TFG
{
	public static class Helper
	{

        public enum ByteOrder
        {
            LSB,
            MSB,
            BE3412
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

        public static uint ToUInt32(IEnumerable<byte> data, int startIndex, ByteOrder bo = ByteOrder.MSB)
        {
            switch (bo)
            {
                case ByteOrder.LSB:
                    return BitConverter.ToUInt32(data.ToArray(), startIndex);
                case ByteOrder.MSB:
                    return BitConverter.ToUInt32(Reverse(data, startIndex, 4), 0);
                case ByteOrder.BE3412:
                    return BitConverter.ToUInt32(new byte[]{
                        data.ElementAt(startIndex+2),
                        data.ElementAt(startIndex+3),
                        data.ElementAt(startIndex+0),
                        data.ElementAt(startIndex+1)
                    }, 0);
            }
            return 0;
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

        public static double ToDouble(IEnumerable<byte> data, int startIndex)
        {
            return BitConverter.ToDouble(Reverse(data, startIndex, 8), 0);
        }

        public static byte GetLowByte(UInt32 b)
		{
			return (byte)(b & 0xFF);
		}

		public static byte GetHighByte(UInt32 b)
		{
			return (byte)((b >> 8) & 0xFF);
		}
        
        public static byte FromBCD(byte sourceBcd)
        {
            byte left = (byte)(sourceBcd >> 4 & 0xf);
            byte right = (byte)(sourceBcd & 0xf);
            return (byte)(left * 10 + right);
        }

        public static byte ToBCD(byte srcByte)
        {
            byte left = (byte)((byte)(srcByte / 10) % 10);
            byte right = (byte)(srcByte % 10);
            return (byte)((srcByte % 10) | (((srcByte / 10) % 10) << 4));
        }
    }
}

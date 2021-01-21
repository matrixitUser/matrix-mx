using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
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

        //TV7
        public static byte BinByteToBcdByte(byte Src)
        {
            byte res = (byte)(((Src / 10) << 4) | (Src % 10));
            return res;
        }
        public static byte BcdByteToBinByte(byte Src)
        {
            byte res = (byte)(((Src >> 4) * 10) + (Src & 0x0f));
            return res;
        }

        public static byte[] ASCIItoBytes(byte[] buffer)
        {
            List<byte> buf = new List<byte>();
            byte[] bytesToBytes = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15 };
            for (int i = 0; i < buffer.Length - 1; i = i + 2)
            {
                byte high = (byte)(bytesToBytes[buffer[i] - 0x30]);
                byte low = (byte)(bytesToBytes[buffer[i + 1] - 0x30]);
                buf.Add((byte)((high << 4) | low));
            }
            return buf.ToArray();
        }
        public static byte[] ReverseRegister(IEnumerable<byte> buffer)
        {
            List<byte> listBytes = new List<byte>();
            listBytes.AddRange(buffer.Skip(2));
            listBytes.AddRange(buffer.Take(2));
            return listBytes.ToArray();
        }

        public static byte[] Reverse4Bytes(byte[] buffer)
        {
            return new byte[]{ buffer[1], buffer[0], buffer[3], buffer[2] };
        }
        public static byte[] Reverse8Bytes(byte[] buffer)
        {
            return new byte[] { buffer[1], buffer[0], buffer[3], buffer[2], buffer[5], buffer[4], buffer[7], buffer[6] };
        }
        public static byte[] BitsMask(Int16 bytes, byte countBytes)
        {
            List<byte> listBits = new List<byte>();
            for (byte i = 0; i < countBytes; i++)
            {
                if ((bytes & (1 << i)) > 0)
                {
                    listBits.Add(i);
                }
            }
            return listBits.ToArray();
        }
    }
}

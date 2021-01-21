using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using Matrix.SurveyServer.Driver.SCYLAR.DriverData;

namespace Matrix.SurveyServer.Driver.SCYLAR
{
	static class DriverHelper
	{
		#region SND_NKE

		public static byte[] GetSnd_Nke()
		{
			//return new byte[] { 0x10, 0x40, 0xFE, 0x3E, 0x16 };
			var buf = new byte[] { 0x40, 0xFE };
			return NestSimplePacket(buf);
		}

		#endregion

		#region archive
		public static byte[] GetArchiveReadMessage(ArchiveType archiveType)
		{
			byte[] buf = { 0x73, 0xFE, 0x50, (byte)archiveType };
			return NestComplexPacket(buf);
		}
		#endregion

		public static byte[] NestComplexPacket(byte[] packet)
		{
			if (packet == null) return null;
			var length = (byte)packet.Length;
			var result = new List<byte>(packet) { CalcCrc8(packet), 0x16 };
			result.Insert(0, 0x68);
			result.Insert(0, length);//длину надо вставить 2 раза
			result.Insert(0, length);
			result.Insert(0, 0x68);
			return result.ToArray();
		}
		public static byte[] NestSimplePacket(byte[] packet)
		{
			if (packet == null) return null;
			var result = new List<byte>(packet) { CalcCrc8(packet), 0x16 };
			result.Insert(0, 0x10);
			return result.ToArray();
		}
		private static byte CalcCrc8(IEnumerable<byte> buffer)
		{
			byte ret = 0;
			foreach (var b in buffer)
			{
				ret += b;
			}
			return ret;
		}

		internal static byte[] GetNextRecordMessage(byte fcv)
		{
			byte[] buffer = { fcv, 0xFE };
			return NestSimplePacket(buffer);
		}

		public static Tuple<DateTime, IEnumerable<Data>> ParseData(byte[] data)
		{
			if (data == null || data.Length < 6 || !CheckPacket(data))
				return null;

			return Parser.Parse(data.Skip(4).Take(data.Length - 6).ToArray());
		}




		public static bool CheckPacket(byte[] data)
		{
			if (data == null) return false;

			if (data.Length == 1)
			{
				if (data[0] == 0xe5)
					return true;
			}
			else if (data.Length == 5)
			{
				if (data[0] == 0x10 && data[4] == 0x16)
				{
					if (CalcCrc8(new[] { data[1], data[2] }) == data[3])
						return true;
				}
			}
			else if (data.Length == 9)
			{
				if (data[0] == 0x68 && data[3] == 0x68 && data[8] == 0x16 && data[1] == data[2] && data[1] == 3)
				{
					if (CalcCrc8(new byte[] { data[4], data[5], data[6] }) == data[7])
						return true;
				}
			}
			else
			{
				if (data[0] == 0x68 && data[3] == 0x68 && data[data.Length - 1] == 0x16 && data[1] == data[2])
				{

					if (CalcCrc8(data.Skip(4).Take(data.Length - 6)) == data[data.Length - 2])
						return true;
				}
			}
			return false;
		}

		public static IEnumerable<Constant> ParseConstantData(byte[] buf)
		{
			var result = new List<Constant>();
			if (buf == null || buf.Length < 15) return result;

			var cField = buf[4];
			if (cField != 0x08 && cField != 0x18 && cField != 0x28 && cField != 0x38)
				return result;

			var ciField = buf[6];
			if (ciField != 0x72 && ciField != 0x76)
				return result;

			var idNumber = Parser.FromBcdToInt(buf, 7, 4);
			result.Add(new Constant(ConstantType.FactoryNumber, idNumber.ToString()));

			return result;
		}
	}
}

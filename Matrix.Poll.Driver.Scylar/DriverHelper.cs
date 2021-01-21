using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using System.Dynamic;

namespace Matrix.Poll.Driver.Scylar
{
	class DriverHelper
    {
                
        public dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeResult(int code, Driver.DeviceError errorcode, string description)
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case Driver.DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }


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

		public static Tuple<DateTime, IEnumerable<dynamic>> ParseData(byte[] data, ArchiveType archiveType)
		{
			if (data == null || data.Length < 6 || !CheckPacket(data))
				return null;

            int toSkip = 6;
            if(data[data.Length - 1] != 0x16)
            {
                toSkip -= 2;
            }

			return Parser.Parse(data.Skip(4).Take(data.Length - toSkip).ToArray(), archiveType);
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
				if (data[0] == 0x68 && data[3] == 0x68 && data[1] == data[2])
				{
                    if(data[data.Length - 1] == 0x16)
                    {
                        if (CalcCrc8(data.Skip(4).Take(data.Length - 6)) == data[data.Length - 2])
                            return true;
                    }
                    //else
                    //{
                    //    return true;
                    //}
				}
			}
			return false;
		}

		public static IEnumerable<dynamic> ParseConstantData(byte[] buf, DateTime date)
		{
			var result = new List<dynamic>();
			if (buf == null || buf.Length < 15) return result;

			var cField = buf[4];
			if (cField != 0x08 && cField != 0x18 && cField != 0x28 && cField != 0x38)
				return result;

			var ciField = buf[6];
			if (ciField != 0x72 && ciField != 0x76)
				return result;

			var idNumber = Parser.FromBcdToInt(buf, 7, 4);
			result.Add(Instance().MakeConstRecord(ConstantType.FactoryNumber.ToString(), idNumber.ToString(), date));

			return result;
		}

        private DriverHelper() { }
        private static DriverHelper instance = null;

        public static DriverHelper Instance()
        {
            if(instance == null)
            {
                instance = new DriverHelper();
            }
            return instance;
        }
	}
}

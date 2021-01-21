using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.SA94
{
	static class DriverHelper
	{
		public static DateTime ParseDateTime(byte[] buf, int startIndex = 0)
		{
			DateTime archiveTime;
			archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3], buf[startIndex + 5]);
			return archiveTime;
		}
		public static DateTime ParseDateTimeHMS(byte[] buf, int startIndex = 0)
		{
			DateTime archiveTime;
			archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3], buf[startIndex + 5], buf[startIndex + 6], buf[startIndex + 7]);
			return archiveTime;
		}

		public static DateTime ParseDate(byte[] buf, int startIndex = 0)
		{
			DateTime archiveTime;
			archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3]);
			return archiveTime;
		}
		public static IEnumerable<Data> ParseHourArchive(byte[] buf, int startIndex, DateTime date)
		{
			List<Data> result = new List<Data>();
			IEnumerable<Tuple<string, int, MeasuringUnitType, int, double>> mappings = GetHourMapping();
			int currentPosition = startIndex + 8;
			foreach (var map in mappings)
			{
				double value = 0;
				//if (map.Item4 == 2)
				//{
				//    value = BitConverter.ToUInt16(new byte[] { buf[currentPosition + 1], buf[currentPosition] }, 0) * map.Item5;
				//    //value = BitConverter.ToUInt16(buf, currentPosition) * map.Item5;
				//}
				if (map.Item4 == 4)
				{
					value = MakeFloat(buf, currentPosition) * map.Item5;
				}
				currentPosition += map.Item4;
				var data = new Data(map.Item1, map.Item3, date, value);
				data.Channel = map.Item2;
				result.Add(data);
			}
			return result;
		}

		public static IEnumerable<Data> ParseDayArchive(byte[] buf, int startIndex, DateTime date)
		{
			List<Data> result = new List<Data>();
			IEnumerable<Tuple<string, int, MeasuringUnitType, int, double>> mappings = GetDailyMapping();
			int currentPosition = startIndex + 4;
			foreach (var map in mappings)
			{
				double value = 0;
				if (map.Item4 == 2)
				{
					value = BitConverter.ToUInt16(new byte[] { buf[currentPosition + 1], buf[currentPosition] }, 0) * map.Item5;
				}
				else if (map.Item4 == 3)
				{
					value = BitConverter.ToUInt32(new byte[] { 0, buf[currentPosition], buf[currentPosition + 1], buf[currentPosition + 2] }, 0) * map.Item5;
				}
				else if (map.Item4 == 4)
				{
					value = MakeFloat(buf, currentPosition) * map.Item5;
				}
				currentPosition += map.Item4;
				if (map.Item1 == "Unknown") continue;
				var data = new Data(map.Item1, map.Item3, date, value);
				data.Channel = map.Item2;
				result.Add(data);
			}
			return result;
		}

		public static IEnumerable<Tuple<string, int, MeasuringUnitType, int, double>> GetHourMapping()
		{
			return new List<Tuple<string, int, MeasuringUnitType, int, double>> 
            {
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeWork, 0, MeasuringUnitType.sec, 2, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWaterConsumption1", 1, MeasuringUnitType.m3_h, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWaterConsumption2", 2, MeasuringUnitType.m3_h, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater1", 1, MeasuringUnitType.C, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater2", 2, MeasuringUnitType.C, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater3", 3, MeasuringUnitType.C, 4, 1),
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 1, MeasuringUnitType.sec, 2, 1), 
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 2, MeasuringUnitType.sec, 2, 1),
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 3, MeasuringUnitType.sec, 2, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("Energy", 0, MeasuringUnitType.kWt, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("PressureWater1", 1, MeasuringUnitType.MPa, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("PressureWater2", 2, MeasuringUnitType.MPa, 4, 1),                    
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWater1", 1, MeasuringUnitType.m3, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWater2", 2, MeasuringUnitType.m3, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("MassWater1", 1, MeasuringUnitType.tonn, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("MassWater2", 2, MeasuringUnitType.tonn, 4, 1),
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Volume, 0, MeasuringUnitType.m3, 4, 1),
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Mass, 3, MeasuringUnitType.tonn,4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TimeWork", 1, MeasuringUnitType.h, 4, 1),
                    //new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Pressure, 3, MeasuringUnitType.MPa, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("HeatWaterConsumption", 0, MeasuringUnitType.MWtH, 4, 1),
            };
		}

		//ASWega94-2
		public static IEnumerable<Tuple<string, int, MeasuringUnitType, int, double>> GetDailyMapping()
		{
			return new List<Tuple<string, int, MeasuringUnitType, int, double>> 
            {
                    new Tuple<string, int, MeasuringUnitType, int, double>("HeatWaterConsumption", 0, MeasuringUnitType.MWtH, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWaterConsumption1", 1, MeasuringUnitType.m3_h, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWaterConsumption2", 2, MeasuringUnitType.m3_h, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater1", 1, MeasuringUnitType.C, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater2", 2, MeasuringUnitType.C, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TemperatureWater3", 3, MeasuringUnitType.C, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("Energy", 0, MeasuringUnitType.kWt, 4, 1),                    
                    new Tuple<string, int, MeasuringUnitType, int, double>("PressureWater1", 1, MeasuringUnitType.MPa, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("PressureWater2", 2, MeasuringUnitType.MPa, 4, 1),                    
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWater1", 1, MeasuringUnitType.m3, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("VolumeWater2", 2, MeasuringUnitType.m3, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("MassWater1", 1, MeasuringUnitType.tonn,4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("MassWater2", 2, MeasuringUnitType.tonn,4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("TimeWork", 0, MeasuringUnitType.h, 4, 1),
                    new Tuple<string, int, MeasuringUnitType, int, double>("Unknown", 0, MeasuringUnitType.h, 4, 1),

            };
		}

		// ASWega SA94-3
		//public static IEnumerable<Tuple<ParameterType, int, MeasuringUnitType, int, double>> GetDailyMapping()
		//{
		//    return new List<Tuple<ParameterType, int, MeasuringUnitType, int, double>> 
		//    {
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeWork, 0, MeasuringUnitType.h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.MassWater1, 1, MeasuringUnitType.tonn, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.MassWater2, 2, MeasuringUnitType.tonn, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater1, 1, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater2, 2, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater3, 3, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 1, MeasuringUnitType.sec, 2, 2), 
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 2, MeasuringUnitType.sec, 2, 2),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 3, MeasuringUnitType.sec, 2, 2),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.HeatWaterConsumption, 0, MeasuringUnitType.Gkal, 4, 1),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.PressureWater1, 1, MeasuringUnitType.MPa, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.PressureWater2, 2, MeasuringUnitType.MPa, 4, 1),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.VolumeWaterConsumption1, 1, MeasuringUnitType.m3_h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.VolumeWaterConsumption2, 2, MeasuringUnitType.m3_h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.VolumeWater1, 0, MeasuringUnitType.m3, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.MassWater1, 3, MeasuringUnitType.tonn,4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Unknown, 0, MeasuringUnitType.h, 1, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Unknown, 1, MeasuringUnitType.sec, 3, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.PressureWater1, 3, MeasuringUnitType.MPa, 4, 1),
		//    };
		//}

		public static string ByteArrayToString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}

		public static string ConvertHex(String hexString)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < hexString.Length; i += 2)
			{
				string hs = hexString.Substring(i, 2);
				sb.Append(Convert.ToChar(Convert.ToUInt32(hs, 16)));
			}

			String ascii = sb.ToString();
			return ascii;
		}

		public static float MakeFloat(byte[] answer, int startIndex)
		{
			if (answer == null || answer.Length < startIndex + 4) return 0;
			return MakeFloat(new byte[] { answer[startIndex], answer[startIndex + 1], answer[startIndex + 2], answer[startIndex + 3] });
		}

		public static float MakeFloat(byte[] answer)
		{
			if (answer == null || answer.Length != 4) return 0;
			byte[] ieeFormat = new byte[4];

			//result[0] = pv[3];
			//result[1] = pv[2];
			//result[2] = pv[1] & 0x7F;
			//result[3] = pv[1] & 0x80;

			ieeFormat[0] = answer[3];
			ieeFormat[1] = answer[2];
			ieeFormat[2] = (byte)(answer[1] & 0x7F);
			ieeFormat[3] = (byte)(answer[1] & 0x80);

			byte e17 = (byte)(answer[0] >> 1);
			if (e17 > 1)
			{
				ieeFormat[3] |= (byte)(e17 - 1);
			}
			ieeFormat[2] |= (byte)(answer[0] << 7);
			//return result
			return BitConverter.ToSingle(ieeFormat, 0);
		}

		public static DateTime DateTimeConvertion(byte day, byte month, byte year, byte hour = 0, byte min = 0, byte sec = 0)
		{
			DateTime result = DateTime.MinValue;
			int year20 = BinDecToInt(year);
			if (year20 < 95)
			{
				year20 += 2000;
			}
			else
			{
				year20 += 1900;
			};

			var DateTimeString = string.Format("{0}:{1}:{2} {3}.{4}.{5}",
				BinDecToInt(hour), //hh
				BinDecToInt(min), //mm
				BinDecToInt(sec), //sec
				BinDecToInt(day), //day
				BinDecToInt(month), //mon
				year20); //year;

			DateTime curDateTime;
			DateTime.TryParse(DateTimeString, out curDateTime);
			if (curDateTime != null && curDateTime != DateTime.MinValue)
			{
				result = curDateTime;
			}

			return result;
		}

		public static int BinDecToInt(byte binDec)
		{
			return (binDec >> 4) * 10 + (binDec & 0x0f);
		}

		public static bool IsValidRecord(Record newRecord)
		{
			if (newRecord.Block1.IsValid || newRecord.Block2.IsValid)
				return true;
			else
				return false;
		}
	}
}

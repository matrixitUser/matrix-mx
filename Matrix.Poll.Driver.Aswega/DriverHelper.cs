using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public static IEnumerable<dynamic> ParseHourArchive(byte[] buf, int startIndex, DateTime date)
		{
			List<dynamic> result = new List<dynamic>();
			IEnumerable<Tuple<string, int, string, int, double>> mappings = GetHourMapping();
			int currentPosition = startIndex + 8;
            int valueCounter = 0;

            foreach (var map in mappings)
			{
				double? value = null;
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
                if (value.HasValue)
                {
                    var data = Driver.MakeHourRecord(map.Item1, value.Value, map.Item3, date);
                    data.i1 = map.Item2; //data.Channel = map.Item2;//???
                    result.Add(data);
                    valueCounter++;
                }
            }

            int statusCode;
            string statusText;
            if (valueCounter == mappings.Count())
            {
                statusCode = 0;
                statusText = "ошибок нет";
            }
            else if(valueCounter == 0)
            {
                statusCode = 2;
                statusText = "нет данных";
            }
            else
            {
                statusCode = 1;
                statusText = "частичные данные";
            }
            result.Add(Driver.MakeHourRecord("status", statusCode, statusText, date));
            return result;
		}

		public static IEnumerable<dynamic> ParseDayArchive(byte[] buf, int startIndex, DateTime date)
		{
			List<dynamic> result = new List<dynamic>();
			IEnumerable<Tuple<string, int, string, int, double>> mappings = GetDailyMapping();
			int currentPosition = startIndex + 4;
            int valueCounter = 0;

            foreach (var map in mappings)
			{
				double? value = null;
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
                if (value.HasValue)
                {
                    var data = Driver.MakeDayRecord(map.Item1, value.Value, map.Item3, date);
                    data.i1 = map.Item2; //data.Channel = map.Item2;//???
                    result.Add(data);
                    valueCounter++;
                }
            }

            int statusCode;
            string statusText;
            if (valueCounter == mappings.Count())
            {
                statusCode = 0;
                statusText = "ошибок нет";
            }
            else if (valueCounter == 0)
            {
                statusCode = 2;
                statusText = "нет данных";
            }
            else
            {
                statusCode = 1;
                statusText = "частичные данные";
            }
            result.Add(Driver.MakeHourRecord("status", statusCode, statusText, date));
            return result;
		}

		public static IEnumerable<Tuple<string, int, string, int, double>> GetHourMapping()
        {
            //return new List<Tuple<string, int, string, int, double>>
            //{
            //        new Tuple<string, int, string, int, double>("Q1", 1, "м3/ч", 4, 1),
            //        new Tuple<string, int, string, int, double>("Q2", 2, "м3/ч", 4, 1),
            //        new Tuple<string, int, string, int, double>("T1", 1, "град.С", 4, 1),
            //        new Tuple<string, int, string, int, double>("T2", 2, "град.С", 4, 1),
            //        new Tuple<string, int, string, int, double>("T3", 3, "град.С", 4, 1),
            //        new Tuple<string, int, string, int, double>("P", 0, "кВт", 4, 1),
            //        new Tuple<string, int, string, int, double>("p1", 1, "МПа", 4, 1),
            //        new Tuple<string, int, string, int, double>("p2", 2, "МПа", 4, 1),
            //        new Tuple<string, int, string, int, double>("V1", 1, "м3", 4, 1),
            //        new Tuple<string, int, string, int, double>("V2", 2, "м3", 4, 1),
            //        new Tuple<string, int, string, int, double>("V1m", 1, "т", 4, 1),
            //        new Tuple<string, int, string, int, double>("V2m", 2, "т", 4, 1),
            //        new Tuple<string, int, string, int, double>("trab", 1, "ч", 4, 1),
            //        new Tuple<string, int, string, int, double>("E", 0, "МВт*ч", 4, 1),
            //};
            return new List<Tuple<string, int, string, int, double>>
            {
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeWork, 0, string.sec, 2, 1),
                    new Tuple<string, int, string, int, double>("VolumeWaterConsumption1", 1, "м3/ч", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWaterConsumption2", 2, "м3/ч", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater1", 1, "град.С", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater2", 2, "град.С", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater3", 3, "град.С", 4, 1),
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 1, string.sec, 2, 1), 
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 2, string.sec, 2, 1),
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 3, string.sec, 2, 1),
                    new Tuple<string, int, string, int, double>("Energy", 0, "кВт", 4, 1),
                    new Tuple<string, int, string, int, double>("PressureWater1", 1, "МПа", 4, 1),
                    new Tuple<string, int, string, int, double>("PressureWater2", 2, "МПа", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWater1", 1, "м3", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWater2", 2, "м3", 4, 1),
                    new Tuple<string, int, string, int, double>("MassWater1", 1, "т", 4, 1),
                    new Tuple<string, int, string, int, double>("MassWater2", 2, "т", 4, 1),
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.Volume, 0, "м3", 4, 1),
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.Mass, 3, "т",4, 1),
                    new Tuple<string, int, string, int, double>("TimeWork", 1, "ч", 4, 1),
                    //new Tuple<ParameterType, int, string, int, double>(ParameterType.Pressure, 3, "МПа", 4, 1),
                    new Tuple<string, int, string, int, double>("HeatWaterConsumption", 0, "МВт*ч", 4, 1),
            };
        }

		//ASWega94-2
		public static IEnumerable<Tuple<string, int, string, int, double>> GetDailyMapping()
		{
			return new List<Tuple<string, int, string, int, double>> 
            {
                    new Tuple<string, int, string, int, double>("HeatWaterConsumption", 0, "МВт*ч", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWaterConsumption1", 1, "м3/ч", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWaterConsumption2", 2, "м3/ч", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater1", 1, "град.С", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater2", 2, "град.С", 4, 1),
                    new Tuple<string, int, string, int, double>("TemperatureWater3", 3, "град.С", 4, 1),
                    new Tuple<string, int, string, int, double>("Energy", 0, "кВт", 4, 1),                    
                    new Tuple<string, int, string, int, double>("PressureWater1", 1, "МПа", 4, 1),
                    new Tuple<string, int, string, int, double>("PressureWater2", 2, "МПа", 4, 1),                    
                    new Tuple<string, int, string, int, double>("VolumeWater1", 1, "м3", 4, 1),
                    new Tuple<string, int, string, int, double>("VolumeWater2", 2, "м3", 4, 1),
                    new Tuple<string, int, string, int, double>("MassWater1", 1, "т",4, 1),
                    new Tuple<string, int, string, int, double>("MassWater2", 2, "т",4, 1),
                    new Tuple<string, int, string, int, double>("TimeWork", 0, "ч", 4, 1),
                    new Tuple<string, int, string, int, double>("Unknown", 0, "ч", 4, 1),

            };
		}

		// ASWega SA94-3
		//public static IEnumerable<Tuple<ParameterType, int, string, int, double>> GetDailyMapping()
		//{
		//    return new List<Tuple<ParameterType, int, string, int, double>> 
		//    {
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeWork, 0, "ч", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.MassWater1, 1, "т", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.MassWater2, 2, "т", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TemperatureWater1, 1, "град.С", 2, 0.01),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TemperatureWater2, 2, "град.С", 2, 0.01),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TemperatureWater3, 3, "град.С", 2, 0.01),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 1, string.sec, 2, 2), 
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 2, string.sec, 2, 2),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.TimeEmergency, 3, string.sec, 2, 2),                    
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.HeatWaterConsumption, 0, string.Gkal, 4, 1),                    
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.PressureWater1, 1, "МПа", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.PressureWater2, 2, "МПа", 4, 1),                    
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.VolumeWaterConsumption1, 1, "м3/ч", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.VolumeWaterConsumption2, 2, "м3/ч", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.VolumeWater1, 0, "м3", 4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.MassWater1, 3, "т",4, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.Unknown, 0, "ч", 1, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.Unknown, 1, string.sec, 3, 1),
		//            new Tuple<ParameterType, int, string, int, double>(ParameterType.PressureWater1, 3, "МПа", 4, 1),
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

		public static float? MakeFloat(byte[] answer, int startIndex)
		{
            if (answer == null || answer.Length < startIndex + 4) throw new Exception("MakeFloat: входные данные не совпадают по длине или отсутствуют");
			return MakeFloat(new byte[] { answer[startIndex], answer[startIndex + 1], answer[startIndex + 2], answer[startIndex + 3] });
		}

		public static float? MakeFloat(byte[] answer)
		{
			if (answer == null || answer.Length != 4) throw new Exception("MakeFloat: входные данные не совпадают по длине или отсутствуют");
            if ((answer[0] == 0xFF) && (answer[1] == 0xFF) && (answer[2] == 0xFF) && (answer[3] == 0xFF)) return null;

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

        public static byte ByteToBCD(byte dec)
        {
            byte b0 = (byte)(dec % 10);
            byte b1 = (byte)((dec / 10) % 10);
            return (byte)((b1 << 4) | b0);
        }

    }
}

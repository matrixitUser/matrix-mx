using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Matrix.SurveyServer.Driver.Common;
using log4net;
using Matrix.Common.Agreements;

//Device SA94/3; deviceAddress = 27384;
namespace Matrix.SurveyServer.Driver.SA94
{
	public class Driver : BaseDriver
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Driver));

		public Driver()
		{

		}

		public static byte ByteLow(int getLow)
		{
			return (byte)(getLow & 0xFF);
		}
		public static byte ByteHigh(int getHigh)
		{
			return (byte)((getHigh >> 8) & 0xFF);
		}


		private byte[] GetAnswer(byte[] data, int timeOut = 5000)
		{
			isDataReceived = false;
			RaiseDataSended(data);
			Wait(timeOut);
			if (isDataReceived)
			{
				isDataReceived = false;
				return receivedBuffer;
			}
			return null;
		}

		private byte? SelectDevice(int attempts = 1)
		{
			int deviceAddress;
			if (!int.TryParse(Password, out deviceAddress))
				return null;

			int attempt = 0;
			byte send1, send2, send3;

			send3 = (byte)(deviceAddress & 0x7F);
			send2 = (byte)((deviceAddress >> 7) & 0x7F);
			send1 = (byte)(((deviceAddress >> 14) & 0x7F) | 0xC0);

			byte[] send = { 0xFF, send1, send2, send3 };
			//    byte[] send = { 0xC1, 0x55, 0x78 };


			do
			{
				Show(string.Format("SelectDevice 0x{0:X}: попытка {1}", deviceAddress, attempt));

				byte[] buf = GetAnswer(send);

				if (buf != null && buf.Length > 0)
				{
					if (buf.Length == 1)
					{
						Show("SelectDevice: получен ответ");
						byte[] answer = new[] { buf[0] };
						return answer[0];
					}

					Show("SelectDevice: получен неизвестный ответ");
				}
				else
				{
					Show("SelectDevice: таймаут");
				}
			}
			while (++attempt < attempts);

			return null;
		}

		private bool Connect(int attempts = 1)
		{

			for (int i = 0; i < 3; i++)
			{
				byte? status = SelectDevice(attempts);
				if (status.HasValue)
				{
					Show("Connect: OK");
					return true;
				}
			}
			return false;
		}

		private void DeSelectDevice(int timeOut = 5000)
		{
			int deviceAddress;
			if (!int.TryParse(Password, out deviceAddress))
				return;

			byte[] send = { 0xFF };


			Show(string.Format("DeSelectDevice 0x{0:X}", deviceAddress));

			RaiseDataSended(send);

			Wait(timeOut);
		}

		public static int BinDecToInt(byte binDec)
		{
			return (binDec >> 4) * 10 + (binDec & 0x0f);
		}

		private float MakeFloat(byte[] answer, int startIndex)
		{
			if (answer == null || answer.Length < startIndex + 4) return 0;
			return MakeFloat(new byte[] { answer[startIndex], answer[startIndex + 1], answer[startIndex + 2], answer[startIndex + 3] });
		}

		private float MakeFloat(byte[] answer)
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

		public override SurveyResultData ReadCurrentValues()
		{
			if (!Connect()) return null;

			var answer = new List<Data>();

			var commands = new List<Tuple<byte, string, MeasuringUnitType>>
                {
                    new Tuple<byte, string, MeasuringUnitType>(0x00, "VolumeWaterConsumption1", MeasuringUnitType.m3_s),
                    new Tuple<byte, string, MeasuringUnitType>(0x01, "VolumeWaterConsumption2", MeasuringUnitType.m3_s),
                    new Tuple<byte, string, MeasuringUnitType>(0x02, "TemperatureWater1", MeasuringUnitType.C),
                    new Tuple<byte, string, MeasuringUnitType>(0x03, "TemperatureWater2", MeasuringUnitType.C),
                    new Tuple<byte, string, MeasuringUnitType>(0x04, "TemperatureWater3", MeasuringUnitType.C),
                    new Tuple<byte, string, MeasuringUnitType>(0x05, "TemperatureWaterConsumption", MeasuringUnitType.C),
                    new Tuple<byte, string, MeasuringUnitType>(0x06, "Energy", MeasuringUnitType.kWt),
                    new Tuple<byte, string, MeasuringUnitType>(0x07, "HeatWaterConsumption", MeasuringUnitType.MWtH),
                    new Tuple<byte, string, MeasuringUnitType>(0x08, "VolumeWater1", MeasuringUnitType.m3),
                    new Tuple<byte, string, MeasuringUnitType>(0x09, "VolumeWater2", MeasuringUnitType.m3),
                    //new Tuple<byte, ParameterType, MeasuringUnitType>(0x0A, ParameterType., MeasuringUnitType.),//Время
                    //new Tuple<byte, ParameterType, MeasuringUnitType>(0x0B, ParameterType., MeasuringUnitType.),//Дата
                    new Tuple<byte, string, MeasuringUnitType>(0x0C, "TimeWork", MeasuringUnitType.h),
                    //new Tuple<byte, ParameterType, MeasuringUnitType>(0x0D, ParameterType.MassWater3, MeasuringUnitType.tonn),
                    new Tuple<byte, string, MeasuringUnitType>(0x0E, "PressureWater1", MeasuringUnitType.MPa),
                    new Tuple<byte, string, MeasuringUnitType>(0x0F, "PressureWater2", MeasuringUnitType.MPa),
                };


			byte send1, send2;


			foreach (var current in commands)
			{
				send2 = current.Item1;
				send1 = (0x80);
				var summ = send1 + send2;
				byte[] send = { (byte)summ };
				//RaiseDataSended(send);
				byte[] buf = GetAnswer(send);

				if (buf != null && buf.Length > 0)
				{
					double value = 0;
					Show("CurrentValue: получен ответ");
					if (buf.Length == 4)
					{
						value = MakeFloat(buf);
					}

					answer.Add(new Data(current.Item2, current.Item3, DateTime.Now, value));

					//else
					//{
					//    Show("SelectDevice: получен неизвестный ответ");
					//}
				}
				else
				{
					Show("CurrentValue: таймаут");
				}
			}
			DeSelectDevice(2000);
			return new SurveyResultData { Records = answer, State = SurveyResultState.Success };
		}

		public override SurveyResult Ping()
		{
			if (Connect())
			{
				DeSelectDevice(2000);
				ReadDeviceFirmware();
				return new SurveyResult { State = SurveyResultState.Success };
			}
			return new SurveyResult { State = SurveyResultState.NoResponse };
		}

		//protected override SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
		//{
		//    List<AbnormalEvents> result = new List<AbnormalEvents>();
		//    if (!Connect(1)) return null;

		//    var timeOut = 5000;
		//    int attempt = 0;
		//    // List<Data> answer = new List<Data>();

		//    byte[] answer = null;


		//    byte send1, send2, send3;
		//    int i, j;
		//    for (i = 0x70; i <= 0x7F; i++)
		//    {
		//        for (j = 0; j <= 0x3F; j++)
		//        {

		//            send3 = (byte)j;
		//            send2 = (byte)i;
		//            send1 = 0xB7;
		//            byte[] send = { send1, send2, send3 };
		//            //RaiseDataSended(send);
		//            byte[] buf = GetAnswer(send);
		//            if (buf != null && buf.Length > 0)
		//            {
		//                DateTime value /*= DateTime.MinValue*/;
		//                Show("ReadAbnormalEvents: получен ответ");
		//                if (buf.Length == 128)
		//                {
		//                    for (int l = 0; l <= 127; l += 8)
		//                    {
		//                        value = ParseDateTimeHMS(buf, l);
		//                        if (value < dateStart || value > dateEnd) continue;
		//                        switch (buf[l])
		//                        {
		//                            case 01:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Выключение питания" });
		//                                break;
		//                            case 02:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Выбран режим работы <Стоп>" });
		//                                break;
		//                            case 03:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Сбой при записи в память" });
		//                                break;
		//                            case 04:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Неисправность элемента питания таймера" });
		//                                break;
		//                            //case 06:
		//                            //    result.Add(new AbnormalEvents { DateTime = value, Description = "Высокий уровень внешних помех" });
		//                            //    break;
		//                            //case 08:
		//                            //    result.Add(new AbnormalEvents { DateTime = value, Description = "Неисправность в цепи термопреобразователей" });
		//                            //    break;
		//                            case 08:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Обрыв в цепи термопреобразователя Т1 или Т2" });
		//                                break;
		//                            //case 09:
		//                            //    result.Add(new AbnormalEvents { DateTime = value, Description = "Произведена коррекция внутренних часов" });
		//                            //    break;
		//                            case 10:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "-0,01 Q1max > Q1 > 0,01 Q1max" });
		//                                break;
		//                            case 11:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q1 > Q1max" });
		//                                break;
		//                            case 12:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q1 < Q1min" });
		//                                break;
		//                            case 13:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q1 < -Q1min " });
		//                                break;
		//                            case 14:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "T1 > 150 C" });
		//                                break;
		//                            case 15:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "T1 < 0 C" });
		//                                break;
		//                            case 16:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "dT=(Т1-Т2) < dTmin" });
		//                                break;
		//                            case 17:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "dT=(Т1-Т2) <= 0 C" });
		//                                break;
		//                            case 20:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q2min > Q2 > -Q2min" });
		//                                break;
		//                            case 21:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q2 > Q2max" });
		//                                break;
		//                            case 22:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q2 < Q2min" });
		//                                break;
		//                            case 23:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Q2 < -Q2min " });
		//                                break;
		//                            case 24:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "T2 > 150 C" });
		//                                break;
		//                            case 25:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "T2 <= 0 C" });
		//                                break;
		//                            case 26:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "T3 > 150 C" });
		//                                break;
		//                            case 27:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = "Т3 <= 0 C" });
		//                                break;
		//                            default:
		//                                result.Add(new AbnormalEvents { DateTime = value, Description = string.Format("Unknown event, code - {0}", buf[l]) });
		//                                break;
		//                        }

		//                    }
		//                }
		//            }
		//            else
		//            {
		//                Show("ReadAbnormalEvents: таймаут");
		//            }
		//        }
		//    }
		//    DeSelectDevice(2000);
		//    return new SurveyResultAbnormalEvents { Records = result, State = SurveyResultState.Success };
		//}


		//private DateTime ParseDateTime(byte[] buf, int startIndex = 0)
		//{
		//    DateTime archiveTime;
		//    archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3], buf[startIndex + 5]);
		//    return archiveTime;
		//}
		//private DateTime ParseDateTimeHMS(byte[] buf, int startIndex = 0)
		//{
		//    DateTime archiveTime;
		//    archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3], buf[startIndex + 5], buf[startIndex + 6], buf[startIndex + 7]);
		//    return archiveTime;
		//}

		//private DateTime ParseDate(byte[] buf, int startIndex = 0)
		//{
		//    DateTime archiveTime;
		//    archiveTime = DateTimeConvertion(buf[startIndex + 1], buf[startIndex + 2], buf[startIndex + 3]);
		//    return archiveTime;
		//}
		//private IEnumerable<Data> ParseHourArchive(byte[] buf, int startIndex, DateTime date)
		//{
		//    List<Data> result = new List<Data>();
		//    IEnumerable<Tuple<ParameterType, int, MeasuringUnitType, int, double>> mappings = GetHourMapping();
		//    int currentPosition = startIndex + 6;
		//    foreach (var map in mappings)
		//    {
		//        double value = 0;
		//        if (map.Item4 == 2)
		//        {
		//            value = BitConverter.ToUInt16(new byte[] { buf[currentPosition + 1], buf[currentPosition] }, 0) * map.Item5;
		//            //value = BitConverter.ToUInt16(buf, currentPosition) * map.Item5;
		//        }
		//        if (map.Item4 == 4)
		//        {
		//            value = MakeFloat(buf, currentPosition) * map.Item5;
		//        }
		//        currentPosition += map.Item4;
		//        var data = new Data(map.Item1, map.Item3, date, value);
		//        data.Channel = map.Item2;
		//        result.Add(data);
		//    }
		//    return result;
		//}

		//private IEnumerable<Data> ParseDayArchive(byte[] buf, int startIndex, DateTime date)
		//{
		//    List<Data> result = new List<Data>();
		//    IEnumerable<Tuple<ParameterType, int, MeasuringUnitType, int, double>> mappings = GetDailyMapping();
		//    int currentPosition = startIndex + 4;
		//    foreach (var map in mappings)
		//    {
		//        double value = 0;
		//        if (map.Item4 == 2)
		//        {
		//            value = BitConverter.ToUInt16(new byte[] { buf[currentPosition + 1], buf[currentPosition] }, 0) * map.Item5;
		//        }
		//        else if (map.Item4 == 3)
		//        {
		//            value = BitConverter.ToUInt32(new byte[] { 0, buf[currentPosition], buf[currentPosition + 1], buf[currentPosition + 2] }, 0) * map.Item5;
		//        }
		//        else if (map.Item4 == 4)
		//        {
		//            value = MakeFloat(buf, currentPosition) * map.Item5;
		//        }
		//        currentPosition += map.Item4;
		//        if (map.Item1 == ParameterType.Unknown) continue;
		//        var data = new Data(map.Item1, map.Item3, date, value);
		//        data.Channel = map.Item2;
		//        result.Add(data);
		//    }
		//    return result;
		//}

		//private IEnumerable<Tuple<ParameterType, int, MeasuringUnitType, int, double>> GetHourMapping()
		//{
		//    return new List<Tuple<ParameterType, int, MeasuringUnitType, int, double>> 
		//    {
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeWork, 0, MeasuringUnitType.sec, 2, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.MassWater1, 1, MeasuringUnitType.tonn, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.MassWater2, 2, MeasuringUnitType.tonn, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater1, 1, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater2, 2, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TemperatureWater3, 3, MeasuringUnitType.C, 2, 0.01),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 1, MeasuringUnitType.sec, 2, 1), 
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 2, MeasuringUnitType.sec, 2, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeEmergency, 3, MeasuringUnitType.sec, 2, 1),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.HeatWaterConsumption, 0, MeasuringUnitType.Gkal, 4, 1),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.PressureWater1, 1, MeasuringUnitType.MPa, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.PressureWater2, 2, MeasuringUnitType.MPa, 4, 1),                    
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.VolumeWaterConsumption1, 1, MeasuringUnitType.m3_h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.VolumeWaterConsumption2, 2, MeasuringUnitType.m3_h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Volume, 0, MeasuringUnitType.m3, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Mass, 3, MeasuringUnitType.tonn,4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.TimeWork, 1, MeasuringUnitType.h, 4, 1),
		//            new Tuple<ParameterType, int, MeasuringUnitType, int, double>(ParameterType.Pressure, 3, MeasuringUnitType.MPa, 4, 1),
		//    };
		//}

		//private IEnumerable<Tuple<ParameterType, int, MeasuringUnitType, int, double>> GetDailyMapping()
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

		public string ConvertHex(String hexString)
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

		public byte[] ReadNextRecAdr()
		{
			if (!Connect()) return null;

			byte[] send = { 0xB8 };
			//send[0] = 0xB8 ;
			//RaiseDataSended(send);
			byte[] buf = GetAnswer(send);
			if (buf != null && buf.Length > 0)
			{
				Show("GetNextRecAdr: получен ответ");
				if (buf.Length == 6)
				{
					return buf;
				}
			}
			else
			{
				Show("GetNextRecAdr: таймаут");
				buf = null;
			}
			return buf;
		}
		public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			var result = new List<Data>();
			var cache = new RecordCollection();
			if (!Connect()) return null;
			var firstRecord = ReadRecord(Address.GetFirstDayAddress());
			if (firstRecord != null) cache.Add(firstRecord);
			var presentRecord = FindPresentRecordDA(firstRecord);
			if (presentRecord != null) cache.Add(presentRecord);

			foreach (var dateTime in dates)
			{
				var records = ParseDA((FindBlockDA(dateTime, cache)), dateTime);
				if (records != null)
					result.AddRange(records);
			}
			return new SurveyResultData
			{
				Records = result,
				State = result.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse
			};
		}


		private byte[] FindBlockDA(DateTime dateTime, RecordCollection cache)
		{
			byte[] result = null;
			while (true)
			{
				Record nearest = cache.GetNearestRecordDA(dateTime);
				int steps = CalculateStepsDA(dateTime, nearest);
				Address newAddress = nearest.Address.GetNextAddressDA(steps);
				Record newRecord = ReadRecord(newAddress);

				if (newRecord == null || DriverHelper.IsValidRecord(newRecord) == false)
				{
					break;
				}

				if (DriverHelper.IsValidRecord(newRecord))
				{
					cache.Add(newRecord);
				}

				if (newRecord.Block1.Date == dateTime)
				{
					result = newRecord.Block1.Bytes;
					break;
				}
				if (newRecord.Block2.Date == dateTime)
				{
					result = newRecord.Block2.Bytes;
					break;
				}
				if (cache.IsRecordExist(dateTime))
				{
					var record = cache.ExistRecord(dateTime);
					if (dateTime == record.Block1.Date)
					{
						result = newRecord.Block1.Bytes;
						break;
					}
					if (dateTime == record.Block2.Date)
					{
						result = newRecord.Block2.Bytes;
						break;
					}
					break;
				}

			}
			return result;
		}

		private Record FindPresentRecordDA(Record firstRecord)
		{

			DateTime date = DateTime.Today;
			return ReadRecord(firstRecord.Address.GetNextAddressDA(CalculateStepsDA(date, firstRecord) - 0x01));
		}

		private int CalculateStepsDA(DateTime dateTime, Record record)
		{
			var i = ((dateTime - record.Block1.Date).TotalDays);
			if ((i != 1) && (i != -1))
			{
				i = i / 2;
			}

			return ((int)(i));

		}

		public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		{
			var result = new List<Data>();
			var cache = new RecordCollection();

			if (!Connect()) return null;
			var firstRecord = ReadRecord(Address.GetFirstHourAddress());
			if (firstRecord != null) cache.Add(firstRecord);
			var presentRecord = FindPresentRecordHA(firstRecord);
			if (presentRecord != null) cache.Add(presentRecord);

			foreach (var dateTime in dates)
			{
				var records = ParseHA((FindBlockHA(dateTime, cache)), dateTime);
				if (records != null)
					result.AddRange(records);
			}
			return new SurveyResultData
					   {
						   Records = result,
						   State = result.Any() ? SurveyResultState.Success : SurveyResultState.PartialyRead
					   };
		}

		private IEnumerable<Data> ParseHA(byte[] bytes, DateTime dateTime)
		{
			if (bytes == null) return null;
			return DriverHelper.ParseHourArchive(bytes, 0, dateTime);
		}

		private IEnumerable<Data> ParseDA(byte[] bytes, DateTime dateTime)
		{
			if (bytes == null) return null;
			return DriverHelper.ParseDayArchive(bytes, 0, dateTime);
		}

		private byte[] FindBlockHA(DateTime dateTime, RecordCollection cache)
		{
			byte[] result = null;
			while (true)
			{
				Record nearest = cache.GetNearestRecordHA(dateTime);
				int steps = CalculateStepsHA(dateTime, nearest);
				Address newAddress = nearest.Address.GetNextAddressHA(steps);
				Record newRecord = ReadRecord(newAddress);

				if (newRecord == null || DriverHelper.IsValidRecord(newRecord) == false)
				{
					break;
				}

				if (DriverHelper.IsValidRecord(newRecord))
				{
					cache.Add(newRecord);
				}

				if (newRecord.Block1.DateTime == dateTime)
				{
					result = newRecord.Block1.Bytes;
					break;
				}
				if (newRecord.Block2.DateTime == dateTime)
				{
					result = newRecord.Block2.Bytes;
					break;
				}
				if (cache.IsRecordExist(dateTime))
				{
					var record = cache.ExistRecord(dateTime);
					if (dateTime == record.Block1.DateTime)
					{
						result = newRecord.Block1.Bytes;
						break;
					}
					if (dateTime == record.Block2.DateTime)
					{
						result = newRecord.Block2.Bytes;
						break;
					}
					break;
				}
			}
			return result;
		}

		private Record FindPresentRecordHA(Record firstRecord)
		{
			var dateTime = DateTime.Now;
			return ReadRecord(firstRecord.Address.GetNextAddressHA(CalculateStepsHA(dateTime, firstRecord) - 0x01));
		}

		private int CalculateStepsHA(DateTime dateTime, Record record)
		{
			var i = ((dateTime - record.Block1.DateTime).TotalHours);

			if ((i != 1) && (i != -1))
			{
				i = i / 2;
			}

			return ((int)(i));
		}

		private Record ReadRecord(Address address)
		{
			if (!Connect()) return null;
			var request = new byte[] { 0xB7, address.MBytes[0], address.MBytes[1] };
			Show("ReadRecord: запрос блока записей");
			var buf = GetAnswer(request);
			if (buf != null && buf.Length > 0)
			{
				if (buf.Length == 128)
				{
					Show("ReadRecord: получен ответ");
				}
				else
				{
					Show("ReadRecord: получен неизвестный ответ");
					return null;
				}

			}
			else
			{
				Show("ReadRecord: таймаут");
				DeSelectDevice();
				return null;
			}

			return ParserHelper.ParseRecord(buf, address);
		}

		//protected override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		//{
		//    return new SurveyResultData { State = SurveyResultState.NotImplemented };
		//    if (!Connect(2000, 1)) return null;

		//    var timeOut = 5000;
		//    List<Data> result = new List<Data>();
		//    int attempt = 0;
		//    byte[] answer = null;

		//    byte[] address = ReadNextRecAdr();

		//    byte send1, send2, send3;
		//    byte i, j;
		//    for (i = address[0]; i >= 0x50; i--)
		//    {
		//        for (j = address[1]; j >= 0x00; j--)
		//        {

		//            send3 = (byte)j;
		//            send2 = (byte)i;
		//            send1 = 0xB7;
		//            byte[] send = { send1, send2, send3 };
		//            RaiseDataSended(send);
		//            byte[] buf = GetReceivedBuffer(timeOut);
		//            if (buf != null && buf.Length > 0)
		//            {
		//                DateTime value /*= DateTime.MinValue*/;
		//                Show("ReadHourlyArchive: получен ответ");
		//                if (buf.Length == 128)
		//                {
		//                    for (int l = 0; l <= 64; l += 64)
		//                    {
		//                        value = ParseDateTime(buf, l);
		//                        if (!dates.Any(d => d == value)) continue;
		//                        result.AddRange(ParseHourArchive(buf, l, value));
		//                        if (dates.All(d => result.Any(r => r.Date == d)))
		//                            return new SurveyResultData { State = SurveyResultState.Success, Records = result };
		//                    }
		//                }
		//            }
		//            else
		//            {
		//                Show("ReadHourlyArchive: таймаут");
		//            }
		//        }
		//    }
		//    DeSelectDevice(2000, 1);
		//    return new SurveyResultData{State = SurveyResultState.Success, Records = result};
		//}

		//protected override IEnumerable<Data> ReadHourlyArchive(IEnumerable<DateTime> dates)
		//{

		//    if (!Connect(2000, 1)) return null;

		//    var timeOut = 5000;
		//    List<Data> result = new List<Data>();
		//    int attempt = 0;

		//    byte[] answer = null;


		//    byte send1, send2, send3;
		//    int i, j;
		//    for (i = 0x50; i <= 0x5F; i++)
		//    {
		//        for (j = 0; j <= 0x3F; j++)
		//        {

		//            send3 = (byte)j;
		//            send2 = (byte)i;
		//            send1 = 0xB7;
		//            byte[] send = { send1, send2, send3 };
		//            RaiseDataSended(send);
		//            byte[] buf = GetReceivedBuffer(timeOut);
		//            if (buf != null && buf.Length > 0)
		//            {
		//                DateTime value /*= DateTime.MinValue*/;
		//                Show("ReadHourlyArchive: получен ответ");
		//                if (buf.Length == 128)
		//                {
		//                    for (int l = 0; l <= 64; l += 64)
		//                    {
		//                        value = ParseDateTime(buf, l);
		//                        if (!dates.Any(d => d == value)) continue;
		//                        result.AddRange(ParseHourArchive(buf, l, value));
		//                        if (!dates.Any(d => !result.Any(r => r.Date == d)))
		//                            return result;
		//                    }
		//                }
		//            }
		//            else
		//            {
		//                Show("ReadHourlyArchive: таймаут");
		//            }
		//        }
		//    }
		//    DeSelectDevice(2000, 1);
		//    return result;
		//}

		public string ReadDeviceFirmware()
		//Версия прошивки для счетчиков с расширенной статистикой
		{

			if (!Connect(1)) return null;
			string value = string.Empty;
			string valueAscii = string.Empty;
			byte send1, send2, send3;

			send3 = 0x02; //Адрес 128 байтного блока, содержащего версию программы теплосчетчика
			send2 = 0x40;
			send1 = 0xB7;
			byte[] send = { send1, send2, send3 };
			//RaiseDataSended(send);
			byte[] buf = GetAnswer(send);
			if (buf != null && buf.Length > 0)
			{
				Show("DeviceFirmware: получен ответ");
				if (buf.Length == 128)
				{

					Array.Resize(ref buf, buf.Length - 118); // удаляем лишние элементы массива
					buf[0] = 0x20;
					value = ByteArrayToString(buf);
					valueAscii = ConvertHex(value);
					Show(string.Format("DeviceFirmware{0}", valueAscii));
				}
			}
			else
			{
				Show("DeviceFirmware: таймаут");
			}

			return null;
		}

		//protected override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		//{
		//    return new SurveyResultData { State = SurveyResultState.NotImplemented };
		//    if (!Connect(2000, 1)) return null;

		//    var timeOut = 5000;
		//    List<Data> result = new List<Data>();
		//    int attempt = 0;
		//    // List<Data> answer = new List<Data>();

		//    byte[] answer = null;


		//    byte send1, send2, send3;
		//    int i, j;
		//    for (i = 0x60; i <= 0x6F; i++)
		//    {
		//        for (j = 0x00; j <= 0x3F; j++)
		//        {

		//            send3 = (byte)j;
		//            send2 = (byte)i;
		//            send1 = 0xB7;
		//            byte[] send = { send1, send2, send3 };
		//            RaiseDataSended(send);
		//            byte[] buf = GetReceivedBuffer(timeOut);
		//            if (buf != null && buf.Length > 0)
		//            {
		//                DateTime value /*= DateTime.MinValue*/;
		//                Show("ReadDailyArchive: получен ответ");
		//                if (buf.Length == 128)
		//                {
		//                    for (int l = 0; l <= 64; l += 64)
		//                    {
		//                        value = ParseDate(buf, l);
		//                        if (!dates.Any(d => d == value)) continue;
		//                        result.AddRange(ParseDayArchive(buf, l, value));
		//                        if (!dates.Any(d => !result.Any(r => r.Date == d)))
		//                            return new SurveyResultData { Records = result, State = SurveyResultState.Success };
		//                    }
		//                }
		//            }
		//            else
		//            {
		//                Show("ReadDailyArchive: таймаут");
		//            }
		//        }
		//    }
		//    DeSelectDevice(2000, 1);
		//    return new SurveyResultData { Records = result, State = SurveyResultState.Success };
		//}



		#region низы


		private void Show(string msg, MessageType msgtype = MessageType.All)
		{
			//LOG
			switch (msgtype)
			{
				case MessageType.All:
				case MessageType.Debug:
					log.Debug(msg);
					break;
				case MessageType.Info:
					log.Info(msg);
					break;
				case MessageType.Warn:
					log.Warn(msg);
					break;
				case MessageType.Error:
					log.Error(msg);
					break;
				case MessageType.User:
				case MessageType.Tester:
					break;
			}
			//Show to Interface
			switch (msgtype)
			{
				case MessageType.All:
				case MessageType.User:
				case MessageType.Error:
					OnSendMessage(msg);
					break;
				case MessageType.Debug:
				case MessageType.Info:
				case MessageType.Warn:
				case MessageType.Tester:
					Console.WriteLine(msg);
					break;
			}
		}

		private enum MessageType
		{
			All,
			User,   //only user interfaxe
			Tester, //only console
			Debug,
			Info,
			Warn,
			Error
		}
		#endregion

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Timers;
using log4net;


namespace Matrix.SurveyServer.Driver.TSRV34
{
	public class Driver : BaseDriver
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Driver));

		public static byte ByteLow(int getLow)
		{
			return (byte)(getLow & 0xFF);
		}
		public static byte ByteHigh(int getHigh)
		{
			return (byte)((getHigh >> 8) & 0xFF);
		}

		/// <summary>
		/// отправка сообщения прибору
		/// </summary>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="request">запрос</param>		
		/// <returns>ответ</returns>	
		private byte[] SendMessageToDevice(Request request)
		{
			byte[] response = null;
			//var constructor = typeof(TResponse).GetConstructor(new Type[] { typeof(byte[]) });
			//if (constructor == null) throw new Exception("конструктор ответов должен принимать один параметр - данные для парсинга");

			bool success = false;
			int attemtingCount = 0;

			while (!success && attemtingCount < 5)
			{
				attemtingCount++;

				isDataReceived = false;
				receivedBuffer = null;
				var requestBytes = request.GetBytes();
				RaiseDataSended(requestBytes);

				
				Wait(7000);

				if (isDataReceived)
				{
					response = receivedBuffer;					
					success = true;
				}
				else
				{
					OnSendMessage("ответ не получен");
				}
			}
			return response;
		}

		public override SurveyResult Ping()
		{
			try
			{
				var response = new Response17(SendMessageToDevice(new Request17(NetworkAddress)));
				if (response == null) return new SurveyResult { State = SurveyResultState.NoResponse };
				OnSendMessage(response.ToString());
				return new SurveyResult { State = SurveyResultState.Success };
			}
			catch (Exception ex)
			{
				OnSendMessage(string.Format("ошибка: {0}", ex.Message));
				log.Error("ошибка", ex);
			}
			return new SurveyResult { State = SurveyResultState.NotRecognized };
		}

		public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		{
			var data = new List<Data>();

			//читаем регистры 400060..400062 узнаем, а что же сохраняется масса или объем
			var consumptionProperties = new ConsumptionProperties();
			try
			{
				OnSendMessage("чтение настроек");

				var register400060 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400060, 1)));
				var register400061 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400061, 1)));
				var register400062 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400062, 1)));

				consumptionProperties.IsMassByChannel1 = register400060.Value == 0;
				consumptionProperties.IsMassByChannel2 = register400061.Value == 0;
				consumptionProperties.IsMassByChannel3 = register400062.Value == 0;
			}
			catch (Exception ex)
			{
				OnSendMessage("не удалось определить что сохраняется как расход, по умолчанию - масса");
			}

			foreach (var date in dates)
			{
				log.DebugFormat("дата: {0:dd.MM.yyyy HH:mm}", date);

				try
				{
					OnSendMessage(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm}", date));
					var dataResponse = new Response65(SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.HourlySystem1)), consumptionProperties);
					foreach (var d in dataResponse.Data)
					{
						//убираем лишние 59:59
						d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
						log.DebugFormat("данные: {0}", d);
						data.Add(d);
					}
				}
				catch (Exception ex)
				{
					OnSendMessage(string.Format("ошибка: {0}", ex.Message));
				}

			}
			return new SurveyResultData { Records = data, State = SurveyResultState.Success };
		}

		public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			var data = new List<Data>();

			//читаем регистры 400060..400062 узнаем, а что же сохраняется масса или объем
			var consumptionProperties = new ConsumptionProperties();
			try
			{
				OnSendMessage("чтение настроек");

				var register400060 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400060, 1)));
				var register400061 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400061, 1)));
				var register400062 = new Response4AsByte(SendMessageToDevice(new Request4(NetworkAddress, 400062, 1)));

				consumptionProperties.IsMassByChannel1 = register400060.Value == 0;
				consumptionProperties.IsMassByChannel2 = register400061.Value == 0;
				consumptionProperties.IsMassByChannel3 = register400062.Value == 0;
			}
			catch (Exception ex)
			{
				OnSendMessage("не удалось определить что сохраняется как расход, по умолчанию - масса");
			}

			foreach (var date in dates)
			{
				log.DebugFormat("дата: {0:dd.MM.yyyy HH:mm}", date);

				try
				{
					OnSendMessage(string.Format("чтение суточных данных за {0:dd.MM.yyyy}", date));
					var dataResponse = new Response65(SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.DailySystem1)), consumptionProperties);
					foreach (var d in dataResponse.Data)
					{
						//убираем лишние 23:59:59
						d.Date = d.Date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
						log.DebugFormat("данные: {0}", d);
						data.Add(d);
					}
				}
				catch (Exception ex)
				{
					OnSendMessage(string.Format("ошибка: {0}", ex.Message));
					log.Error("ошибка", ex);
				}

			}
			return new SurveyResultData { Records = data, State = SurveyResultState.Success };
		}
	}

	class ConsumptionProperties
	{
		public ConsumptionProperties()
		{
			IsMassByChannel1 = true;
			IsMassByChannel2 = true;
			IsMassByChannel3 = true;
		}

		public bool IsMassByChannel1 { get; set; }
		public bool IsMassByChannel2 { get; set; }
		public bool IsMassByChannel3 { get; set; }
	}
}

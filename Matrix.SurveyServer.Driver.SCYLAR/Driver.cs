using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.SCYLAR
{
	public class Driver : BaseDriver
	{
		#region Сообщения

		private const string NoInitializedSlaveMessage = "Не удалось инициализировать устройство: ";
		private const string TimeoutMessage = "Таймаут";
		private const string NotRecognizedMessage = "Ответ от устройства не распознан";
		private const string InitializedSlaveMessage = "Устройство инициализировано";

		#endregion		

		#region public methods
		public override SurveyResult Ping()
		{
			var answer = SlaveInitialization().ToSurveyResultState();

			//до этого места дойти не должны
			return new SurveyResult { State = answer };
		}

		public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			if (dates == null)
				return new SurveyResultData { State = SurveyResultState.InvalidIncomingParameters };

			var datesList = dates.ToList();
			if (!datesList.Any())
			{
				return new SurveyResultData { State = SurveyResultState.Success };
			}
			var minDate = datesList.Min();
			SurveyResultData result = null;
			try
			{
				result = Survey(ArchiveType.Daily, minDate);
			}
			catch (Exception ex)
			{
				OnSendMessage(string.Format("ошибка в драйвере: {0}", ex));
			}
			return result;
		}
		public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		{
			if (dates == null)
				return new SurveyResultData { State = SurveyResultState.InvalidIncomingParameters };

			var datesList = dates.ToList();
			if (!datesList.Any())
			{
				return new SurveyResultData { State = SurveyResultState.Success };
			}
			var minDate = datesList.Min();
			return Survey(ArchiveType.Hourly, minDate);
		}
		public override SurveyResultConstant ReadConstants()
		{
			return SurveyConstant();
		}
		#region Current

		private int currentValue = 1;
		public override SurveyResultData ReadCurrentValues()
		{
			currentValue++;
			if (currentValue >= 10)
				currentValue = 1;
			return new SurveyResultData
			{
				State = SurveyResultState.Success,
				Records = new List<Data> 
                { 
                    new Data("Volume", MeasuringUnitType.cm, DateTime.Now, currentValue, CalculationType.NotCalculated, 1)
                }
			};
		}
		#endregion
		#endregion

		#region protocol methods
		private AnswerType SlaveInitialization()
		{
			byte[] request = DriverHelper.GetSnd_Nke();

			var result = SendSimpleRequest(request);

			switch (result)
			{
				case AnswerType.Success:
					ShowMessage(InitializedSlaveMessage);
					break;
				case AnswerType.Timeout:
					ShowMessage(NoInitializedSlaveMessage + TimeoutMessage);
					break;
				case AnswerType.Error:
					ShowMessage(NoInitializedSlaveMessage + NotRecognizedMessage);
					break;
			}

			return result;
		}
		private AnswerType InitialReadArchive(ArchiveType archiveType)
		{
			var request = DriverHelper.GetArchiveReadMessage(archiveType);

			return SendSimpleRequest(request);
		}

		private byte[] GetNextRecord(byte fcv)
		{
			byte[] buf = DriverHelper.GetNextRecordMessage(fcv);
			return SendRequest(buf, true);
		}

		private SurveyResultData Survey(ArchiveType archiveType, DateTime dateEnd)
		{
			var result = new SurveyResultData { State = SurveyResultState.Success };

			byte[] fcv = { 0x7B, 0x5B };
			int ifcv = 0;

			var currentDateTime = DateTime.MaxValue;
			int errorTryCounter = 0;

			var records = new List<Data>();

			//инициируем связь со счетчиком
			var slaveInitialziation = SlaveInitialization().ToSurveyResultState();
			if (slaveInitialziation != SurveyResultState.Success)
			{
				result.State = slaveInitialziation;
				return result;
			}

			//инициируем чтение архива
			var archiveReadInitialization = InitialReadArchive(archiveType).ToSurveyResultState();
			if (archiveReadInitialization != SurveyResultState.Success)
			{
				result.State = archiveReadInitialization;
				return result;
			}
			DateTime lastDate = DateTime.MaxValue;

			do
			{

				byte[] buf = null;
				for (int i = 0; i < 5; i++)
				{
					buf = GetNextRecord(fcv[ifcv]);
					if (buf != null)
						break;
				}

				var parsedData = DriverHelper.ParseData(buf);

				if (parsedData == null)
				{
					ShowMessage("Данные не получены");
					result.State = SurveyResultState.PartialyRead;
					errorTryCounter++;
				}
				else
				{
					if (parsedData.Item1 > lastDate)
					{
						break;
					}

					lastDate = parsedData.Item1;
				}


				if (buf == null || parsedData == null)
				{
					ShowMessage("Данные не получены");
					result.State = SurveyResultState.PartialyRead;
					errorTryCounter++;
				}
				else if (parsedData.Item1 == default(DateTime))
				{
					ShowMessage("Окончание чтения архива");
					break;
				}
				else
				{
					errorTryCounter = 0;
					currentDateTime = parsedData.Item1;
					if (parsedData.Item2 != null)
						records.AddRange(parsedData.Item2.OrderByDescending(r => r.Value));
					ShowMessage(string.Format("Данные за {0} получены", currentDateTime.ToString(GetDataFormatString(archiveType))));
				}
				ifcv = 1 - ifcv;
			} while (currentDateTime > dateEnd && errorTryCounter < 5);

			result.Records = records;
			return result;
		}

		private SurveyResultConstant SurveyConstant()
		{
			var result = new SurveyResultConstant { State = SurveyResultState.Success };

			//инициируем связь со счетчиком
			var slaveInitialziation = SlaveInitialization().ToSurveyResultState();
			if (slaveInitialziation != SurveyResultState.Success)
			{
				result.State = slaveInitialziation;
				return result;
			}

			//инициируем чтение архива
			var archiveReadInitialization = InitialReadArchive(ArchiveType.Hourly).ToSurveyResultState();
			if (archiveReadInitialization != SurveyResultState.Success)
			{
				result.State = archiveReadInitialization;
				return result;
			}
			byte[] buf = null;
			for (int i = 0; i < 5; i++)
			{
				buf = GetNextRecord(0x7B);
				if (buf != null)
					break;
			}
			var data = DriverHelper.ParseConstantData(buf);
			result.Records = data;
			return result;
		}

		private string GetDataFormatString(ArchiveType archiveType)
		{
			switch (archiveType)
			{
				case ArchiveType.Daily:
				case ArchiveType.Monthly:
					return "dd.MM.yyyy";
				default:
					return "dd.MM:HH.mm.ss";
			}
		}

		#endregion

		#region internal methods


		/// <summary>
		/// Отправить запрос, на который, в случае успеха, счетчик ответит одним байтом - 0xe5
		/// </summary>
		/// <param name="requst"></param>
		/// <returns></returns>
		private AnswerType SendSimpleRequest(byte[] requst)
		{
			var answer = SendRequest(requst);

			if (answer == null)
			{
				return AnswerType.Timeout;
			}

			if ((answer.Length == 1) && (answer[0] == 0xE5))
			{
				return AnswerType.Success;
			}

			return AnswerType.Error;
		}
		/// <summary>
		/// Отправить запрос и получить на него ответ
		/// </summary>
		/// <param name="data">Данные для отправления</param>
		/// <param name="expectAdditionalData">В случае больших ответов они могут порваться и придти кусками. Параметр
		/// определяет, стоит ли ждать второго куска пакета</param>
		/// <returns></returns>
		private byte[] SendRequest(byte[] data, bool expectAdditionalData = false)
		{
			isDataReceived = false;
			receivedBuffer = null;
			RaiseDataSended(data);
			Wait(5000);
			if (expectAdditionalData && isDataReceived && receivedBuffer != null)
			{
				var rb = new byte[receivedBuffer.Length];
				receivedBuffer.CopyTo(rb, 0);
				receivedBuffer = null;
				isDataReceived = false;
				if (rb.Length > 2 && rb[1] == rb[2])
				{
					if (rb[1] != rb.Length - 6)
					{
						Wait(5000);
						if (isDataReceived && receivedBuffer != null)
						{
							var result = new byte[rb.Length + receivedBuffer.Length];
							rb.CopyTo(result, 0);
							Array.Copy(receivedBuffer, 0, result, rb.Length, receivedBuffer.Length);
							return result;
						}
					}
				}
				return rb;
			}
			if (isDataReceived)
			{
				return receivedBuffer;
			}

			return null;
		}


		private void ShowMessage(string message)
		{
			OnSendMessage(string.Format("<Scylar>: {0}", message));
		}

		#endregion
	}
}

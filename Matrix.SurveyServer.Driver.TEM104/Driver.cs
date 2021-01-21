using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using log4net;
using System.Timers;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.TEM104
{
	public class Driver : BaseDriver
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Driver));

		private T2K t2k;

		public T2K T2k
		{
			get { return t2k ?? (t2k = T2KRead()); }
		}

		private const string DriverVersion = "TEM-104";

		public enum ChecksumType { Normal = 0, Complement1, Complement2 }

		enum ArchiveType
		{
			Hourly = 0,
			Daily,
			Monthly,
		}

		private readonly List<ChecksumType> _getCheckSumType = new List<ChecksumType>() { ChecksumType.Complement1 };

		private byte CheckSum(byte[] buff, int length, ChecksumType type)
		{
			byte CRC1 = 0;
			for (var i = 0; i < length; i++)
			{
				CRC1 += buff[i];
			}
			if (type != ChecksumType.Normal) CRC1 = (byte)(~CRC1);
			if (type == ChecksumType.Complement2) CRC1++;
			return CRC1;
		}

		private byte[] SendRequest(int cmd, byte[] data = null, int timeOut = 3333, int attempts = 5)
		{
			int attempt = 0;
			byte[] answer = null;
			byte[] send_head = { 0x55, NetworkAddress, (byte)(~NetworkAddress), ConvertHelper.ByteHigh(cmd), ConvertHelper.ByteLow(cmd), 0 };

			byte[] send = null;

			if (data != null && data.Length > 0)
			{
				if (data.Length <= 256)
				{
					send = new byte[send_head.Length + data.Length + 1];
					Array.Copy(send_head, send, send_head.Length);

					send[send_head.Length - 1] = (byte)data.Length;
					Array.Copy(data, 0, send, send_head.Length, data.Length);
				}
				else
				{
					Show(string.Format("Ошибка при попытке отправить сообщение длиной {0} байт", data.Length));
				}
			}
			else
			{
				send = new byte[send_head.Length + 1];
				Array.Copy(send_head, send, send_head.Length);
			}

			if (send != null)
			{
				send[send.Length - 1] = CheckSum(send, send.Length - 1, ChecksumType.Complement1);
				do
				{
					Show(string.Format("SendRequest 0x{0:X}: попытка {1}", cmd, attempt + 1));
					RaiseDataSended(send);
					Wait(timeOut);

					if (isDataReceived)
					{
						if (receivedBuffer.Length >= 7 && receivedBuffer[0] == 0xAA && receivedBuffer[5] == receivedBuffer.Length - 7)
						{
							if (receivedBuffer[3] == ConvertHelper.ByteHigh(cmd) && receivedBuffer[4] == ConvertHelper.ByteLow(cmd))
							{
								Show("ОК");
								answer = new byte[1 + (receivedBuffer.Length - 7)];
								Array.Copy(receivedBuffer, 5, answer, 0, 1 + (receivedBuffer.Length - 7));
							}
							else
							{
								Show("Получен неизвестный ответ");
								//answer = new byte[0];
							}
						}
						else
						{
							Show("Формат ответа не распознан");
						}
					}
					else
					{
						Show("Таймаут");
					}
				} while (++attempt < attempts && answer == null);
			}

			return answer;
		}

		private T2K T2KRead()
		{
			var answer = new byte[2048];
			for (int i = 0; i < 32; i++)
			{
				var curAddr = i * 0x40;
				var curanswer = SendRequest(0x0f01, new byte[] { ConvertHelper.ByteHigh(curAddr), ConvertHelper.ByteLow(curAddr), 0x40 });

				if (curanswer == null || curanswer.Length != 65)
				{
					Show("не удалось прочесть память таймера 2к");
					return null;
				}
				Array.Copy(curanswer, 1, answer, 64 * i, 64);
			}
			return T2K.Parse(answer, 0);
		}

		private string GetVersion()
		{
			string result = null;
			var answer = SendRequest(0, null, 1800, 7);

			if (answer != null && answer.Length > 0)
			{
				result = Encoding.ASCII.GetString(answer).Substring(1);
			}
			return result;
		}

		public override SurveyResult Ping()
		{
			var ret = SurveyResultState.NoResponse;

			var version = GetVersion();
			if (version != null)
			{
				Show(version.Equals(DriverVersion) ?
					"Работает!" :
					string.Format("Прибор '{0}' обнаружен", version));
				ret = SurveyResultState.Success;
			}
			else
			{
				Show("Прибор НЕ обнаружен");
			}

			return new SurveyResult { State = ret };
		}

		private IEnumerable<Data> ReadArchive(ArchiveType archiveType, IEnumerable<DateTime> dates)
		{
			var datas = new List<Data>();

			if (T2k == null) return null;

			Show(string.Format("Чтение {0} архива", archiveType));

			if (GetVersion().Equals(DriverVersion))
			{
				foreach (var date in dates)
				{
					Show(string.Format("Запрос даты {0:HH:mm dd.MM.yyyy}", date));
					var answer = SendRequest(0x0d11, new byte[] {(byte) archiveType, 
                        archiveType==ArchiveType.Hourly? IntToBCD(date.Hour):(byte)0x00, 
                        archiveType!=ArchiveType.Monthly? IntToBCD(date.Day):(byte)0x01, 
                        IntToBCD(date.Month), 
                        IntToBCD(date.Year-2000)
                    });

					if (answer != null && answer.Length == 3)
					{
						var num = answer[1] << 8 | answer[2];
						if (num == 0xFFFF)
						{
							Show("запись не обнаружена");
						}
						else
						{
							Show(string.Format("номер записи: {0} ({0:X})", num));
							//Int64 addr = 0;
							//var answer0 = SendRequest(0x0f03, new byte[] { 64, (byte)(addr >> 24), (byte)(addr >> 16), (byte)(addr >> 08), (byte)(addr) });
							answer = new byte[256];
							for (int i = 0; i < 4; i++)
							{
								var curanswer = SendRequest(0x0f03, new byte[] { 64, 0x00, ConvertHelper.ByteHigh(num), ConvertHelper.ByteLow(num) /*answer[1], answer[2]*/, (byte)(i * 0x40) }, 3333, 5);
								if (curanswer == null || curanswer.Length != 65)
								{
									Show("не удалось запросить запись, пропуск");
									answer = null;
									break;
								}
								Array.Copy(curanswer, 1, answer, 64 * i, 64);
							}

							if (answer != null)
							{
								var sysInt = SysInt.Parse(answer, 0);

								if (T2k.Systems < 1 || T2k.Systems > 4)
								{
									Show(string.Format("Некорректное число систем: {0}", T2k.Systems));
									return null;
								}

								datas.Add(new Data(sysInt.Trab.Parameter, sysInt.Trab.MeasuringUnit, sysInt.date,
												   sysInt.Trab.Value[0]));
								for (int sys = 0; sys < T2k.Systems; sys++)
								{
									var systype = T2k.SysConN[sys].sysType;
									datas.Add(new Data(sysInt.IntV.Parameter, sysInt.IntV.MeasuringUnit, sysInt.date,
													   sysInt.IntV.Value[sys]));
									datas.Add(new Data(sysInt.IntM.Parameter, sysInt.IntM.MeasuringUnit, sysInt.date,
													   sysInt.IntM.Value[sys]));
									datas.Add(new Data(sysInt.IntQ.Parameter, sysInt.IntQ.MeasuringUnit, sysInt.date,
													   sysInt.IntQ.Value[sys]));
									datas.Add(new Data(sysInt.Tnar.Parameter, sysInt.Tnar.MeasuringUnit, sysInt.date,
													   sysInt.Tnar.Value[sys]));

									for (int i = 0; i < SysCon.GetChannelsPorT(systype); i++)
									{
										datas.Add(new Data(sysInt.T.Parameter, sysInt.T.MeasuringUnit, sysInt.date,
														   sysInt.T.Value[sys * 3 + i]));
										datas.Add(new Data(sysInt.P.Parameter, sysInt.P.MeasuringUnit, sysInt.date,
														   sysInt.P.Value[sys * 3 + i]));
									}

									datas.Add(new Data(sysInt.Rshv.Parameter, sysInt.Rshv.MeasuringUnit, sysInt.date,
													   sysInt.Rshv.Value[sys]));
								}
							}

							//var answer0 = SendRequest(0x0f03, new byte[] { 64, 0x00, answer[1], answer[2], 0x00 });
							//var answer1 = SendRequest(0x0f03, new byte[] { 64, 0x00, answer[1], answer[2], 0x40 });
							//var answer2 = SendRequest(0x0f03, new byte[] { 64, 0x00, answer[1], answer[2], 0x80 });
							//var answer3 = SendRequest(0x0f03, new byte[] { 64, 0x00, answer[1], answer[2], 0xC0 });

						}
					}
					else
					{
						Show("ответ не получен");
					}

				}
			}

			return datas;
		}

		public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		{
			return new SurveyResultData { Records = ReadArchive(ArchiveType.Hourly, dates), State = SurveyResultState.Success };
		}

		public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			return new SurveyResultData { Records = ReadArchive(ArchiveType.Daily, dates), State = SurveyResultState.Success };
		}

		public static byte IntToBCD(int toBCD)
		{
			byte result = 0xFF;
			if (toBCD < 100)
			{
				result = 0;
				result |= (byte)(toBCD % 10);
				toBCD /= 10;
				result |= (byte)((toBCD % 10) << 4);
			}
			return result;
		}

		#region функция показа сообщения на экран с записью в лог

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Matrix.Common.Agreements;
using log4net;

namespace Matrix.SurveyServer.Driver.Common
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class BaseDriver : IDriver
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(BaseDriver));

		public event Action<byte[], byte> CommandSended;
		public event Action<byte[]> DataSended;

		private readonly object actionLocker = new object();

		public byte Port { get; set; }
		public byte NetworkAddress { get; set; }
		public string Password { get; set; }
		public int Channel { get; set; }

		public bool IsDailyArchiveSupport { get; set; }

		protected void RaiseDataSended(byte[] data)
		{
			if (DataSended != null)
			{
				DataSended(data);
			}
		}

		protected void RaiseCommandSended(byte[] data, byte code)
		{
			if (CommandSended != null)
			{
				CommandSended(data, code);
			}
		}

		public event Action<IDriver, string> SendMessage;

		public virtual SurveyResult Ping()
		{
			return new SurveyResult { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
		{
			return new SurveyResultData { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			IsDailyArchiveSupport = false;
			return new SurveyResultData { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultData ReadMonthlyArchive(IEnumerable<DateTime> dates)
		{
			return new SurveyResultData { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultConstant ReadConstants()
		{
			return new SurveyResultConstant { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
		{
			return new SurveyResultAbnormalEvents { State = SurveyResultState.NotImplemented };
		}

		public virtual SurveyResultData ReadCurrentValues()
		{
			return new SurveyResultData { State = SurveyResultState.NotImplemented };
		}

		public void OnSendMessage(string message)
		{
			if (SendMessage != null)
			{
				try
				{
					log.InfoFormat("{0}", message);
					SendMessage(this, string.Format(message));
				}
				catch (Exception ex)
				{
					log.Error(string.Format("ошибка при обработке сообщения от драйвера ({0})", message), ex);
				}
			}
		}

		protected void OnSendMessage(string message, params object[] args)
		{
			if (SendMessage != null)
			{
				try
				{
					log.InfoFormat(message, args);
					SendMessage(this, string.Format(message, args));
				}
				catch (Exception ex)
				{
					log.Error(string.Format("ошибка при обработке сообщения от драйвера ({0})", message), ex);
				}
			}
		}

		public BaseDriver()
		{
			IsDailyArchiveSupport = true;
			timeoutTimer = new System.Timers.Timer(20000);
			timeoutTimer.Elapsed += TimeOutElapsed;
			timeoutTimer.Stop();
		}

		public virtual void Dispose()
		{
			//переопредели, если хочешь почистить за собой
		}

		#region не вошедшее в BaseDriver
		protected readonly System.Timers.Timer timeoutTimer;
		protected bool isDataReceived;
		protected byte[] receivedBuffer;
		protected bool isTimeout;

		protected void TimeOutElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			timeoutTimer.Stop();
			isTimeout = false;
		}
		public void Receive(byte[] data)
		{
			if (data != null && data.Any())
			{
				receivedBuffer = data;
				isDataReceived = true;
			}
			else
			{

			}
		}
		protected void Wait(int interval)
		{
			isDataReceived = false;
			timeoutTimer.Stop();
			timeoutTimer.Interval = interval;
			timeoutTimer.Start();
			isTimeout = true;
			while (isTimeout && !isDataReceived) System.Threading.Thread.Sleep(100);
			timeoutTimer.Stop();
		}
		#endregion

	}
}

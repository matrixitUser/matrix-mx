using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
	public interface IDriver : IDisposable
	{
		event Action<IDriver, string> SendMessage;
		event Action<byte[]> DataSended;
		event Action<byte[], byte> CommandSended;

		void Receive(byte[] data);

		byte NetworkAddress { get; set; }
		string Password { get; set; }
		int Channel { get; set; }
		byte Port { get; set; }

		bool IsDailyArchiveSupport { get; }

		SurveyResult Ping();
		SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> holes);
		SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates);
		SurveyResultData ReadMonthlyArchive(IEnumerable<DateTime> dates);
		SurveyResultConstant ReadConstants();
		SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd);
		SurveyResultData ReadCurrentValues();
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV34
{
	/// <summary>
	/// запрос архива по дате
	/// </summary>
	class Request65ByDate : Request65
	{
		public Request65ByDate(byte networkAddress, DateTime date, ArchiveType arrayNumber)
			: base(networkAddress, arrayNumber, 1, RequestType.ByDate)
		{
			Data.Add((byte)date.Second);
			Data.Add((byte)date.Minute);
			Data.Add((byte)date.Hour);
			Data.Add((byte)date.Day);
			Data.Add((byte)date.Month);
			Data.Add((byte)(date.Year - 2000));
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class ArchiveRequest : Request
	{
		public ArchiveRequest(ArchiveType archiveType, ArchiveAttribute attribute, string value)
			: base(RequestType.Read, string.Format("{0}:V.{1}", (int)archiveType, (int)attribute), value)
		{

		}
	}

	/// <summary>
	/// 
	/// </summary>
	enum ArchiveAttribute : int
	{
		Value = 0,
		Rights = 1,
		Description = 2,
		MeasuringUnits = 3
	}

	/// <summary>
	/// типы архивов
	/// </summary>
	enum ArchiveType : int
	{
		Month1 = 1,
		Month2 = 2,
		Interval = 3,
		Events = 4,
		Changes = 5
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class MeasuringUnitArchiveRequest : ArchiveRequest
	{
		public MeasuringUnitArchiveRequest()
			: base(ArchiveType.Interval, ArchiveAttribute.MeasuringUnits, "1")
		{

		}
	}
}

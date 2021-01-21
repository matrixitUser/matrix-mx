using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class ParameterArchiveRequest : ArchiveRequest
	{
		public ParameterArchiveRequest()
			: base(ArchiveType.Interval, ArchiveAttribute.Description, "1")
		{

		}
	}
}

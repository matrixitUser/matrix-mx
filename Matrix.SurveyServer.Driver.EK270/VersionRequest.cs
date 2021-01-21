using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class VersionRequest : Request
	{
		public VersionRequest() : base(RequestType.Read, "02:0190.0", "1") { }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class PassRequest : Request
	{
		public PassRequest(string password = "0")
			: base(RequestType.Write, "4:171.0", password)
		{

		}
	}

}

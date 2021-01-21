using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV043
{
	class DebugResponse : Response
	{
		public byte[] Response { get; private set; }

		public DebugResponse(byte[] data)
			: base(data)
		{
			Response = data;
			var x = Helper.ToUInt16(data, 3);
			var y = new DateTime(1970, 1, 1).AddSeconds(x);
		}
	}
}

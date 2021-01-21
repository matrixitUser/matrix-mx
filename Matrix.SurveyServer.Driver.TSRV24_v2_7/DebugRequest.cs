using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV24
{
	class DebugRequest : Request
	{
		public DebugRequest(byte networkAddress, byte function, byte[] data)
			: base(networkAddress, function)
		{
			Data.AddRange(data);
		}
	}
}

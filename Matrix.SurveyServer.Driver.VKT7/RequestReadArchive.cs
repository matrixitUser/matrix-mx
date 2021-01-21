using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	class RequestReadArchive : RequestReadData
	{
		public RequestReadArchive(byte networkAddress)
			: base(networkAddress)
		{
		}
	}
}

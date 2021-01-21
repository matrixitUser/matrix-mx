using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	class RequestReadPropertiesUnits : RequestReadData
	{
		public RequestReadPropertiesUnits(byte networkAddress)
			: base(networkAddress)
		{
		}
	}
}

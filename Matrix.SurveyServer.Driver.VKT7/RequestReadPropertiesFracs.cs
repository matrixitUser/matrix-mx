using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	class RequestReadPropertiesFracs : RequestReadData
	{
		private IEnumerable<Element> elements;

		public RequestReadPropertiesFracs(byte networkAddress)
			: base(networkAddress)
		{
		}
	}
}

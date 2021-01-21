using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV34
{
	class Response4AsByte : Response
	{
		public byte Value { get; private set; }

		public Response4AsByte(byte[] data)
			: base(data)
		{
			Value = data[3];
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	class TestResponse : Response
	{
		public bool Success { get; private set; }

		public TestResponse(byte[] data, byte networkaddress)
            : base(data, networkaddress)
		{
			Success = Body[0] == 0x00 || Body[0] == 0x80;
		}
	}
}

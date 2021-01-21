using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	class TestRequest : Request
	{
		public TestRequest(byte na)
			: base(na, 3)
		{
			Frame.Add(0x3f);
			Frame.Add(0xfc);
			Frame.Add(0x00);
			Frame.Add(0x00);
		}
	}
}

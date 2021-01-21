using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	public class TestRequest : Base
	{
		public TestRequest(byte networkAddress)
			: base(networkAddress, 0x00)
		{
		}

		public override string ToString()
		{
			return string.Format("тест канала");
		}
	}
}

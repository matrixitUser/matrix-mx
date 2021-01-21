using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class Init1Request : Request
	{
		public string Address { get; private set; }

		public Init1Request(string address = "")
			: base(RequestType.Read, "", "")
		{
			Address = address;
		}

		public override byte[] GetBytes()
		{
			return Encoding.ASCII.GetBytes(string.Format("/?{0}!\r\n", Address));
		}
	}
}

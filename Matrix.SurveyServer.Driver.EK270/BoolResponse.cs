using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class BoolResponse
	{
		const byte ACK = 0x06;

		public bool Value { get; private set; }

		public BoolResponse(byte[] data)
		{
			Value = (data != null && data.Length == 1) && data[0] == ACK;
		}
	}
}

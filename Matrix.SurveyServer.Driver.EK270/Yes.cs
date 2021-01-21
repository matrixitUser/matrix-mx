using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class Yes : Request
	{
		const byte ACK = 0x06;

		public Yes() : base(RequestType.Read, "", "") { }

		public override byte[] GetBytes()
		{
			return new byte[] { ACK };
		}
	}
}

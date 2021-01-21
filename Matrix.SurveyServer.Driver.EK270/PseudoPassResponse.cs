using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class PseudoPassResponse
	{
        public string PseudoPass { get; private set; }
		public PseudoPassResponse(byte[] data)
		{
			PseudoPass = Encoding.ASCII.GetString(data);
		}
	}
}

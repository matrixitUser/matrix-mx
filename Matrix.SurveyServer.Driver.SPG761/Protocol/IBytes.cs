using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761.Protocol
{
	interface IBytes
	{		
		IEnumerable<byte> GetBytes();
	}
}

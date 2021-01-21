using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
	/// <summary>
	/// 
	/// </summary>
	public enum Mode : byte
	{
		Start = 0x00,
		Continue = 0x01,
		RepeatLast = 0x02
	}
}

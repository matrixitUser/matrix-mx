﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	class Request17 : Request
	{
		public Request17(byte networkAddress)
			: base(networkAddress, 17)
		{
		}

		public override string ToString()
		{
			return string.Format("запрос информации о приборе");
		}
	}
}
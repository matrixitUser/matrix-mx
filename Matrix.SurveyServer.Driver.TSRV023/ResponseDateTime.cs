﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	class ResponseDateTime : Response3
	{
		public DateTime Date { get; private set; }

		public ResponseDateTime(byte[] data)
			: base(data)
		{
			var s = Helper.ToUInt32(RegisterData, 0);
			Date = new DateTime(1970, 1, 1).AddSeconds(s);
		}
	}
}

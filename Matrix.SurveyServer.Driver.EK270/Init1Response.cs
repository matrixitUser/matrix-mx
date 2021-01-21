using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class Init1Response
	{
		public string XXX { get; private set; }
		public int SpeedCode { get; private set; }
		public string Ident { get; private set; }
        public string Raw { get; private set; }

		public Init1Response(byte[] data)
		{
            var str = Encoding.GetEncoding(1252).GetString(data);
            Raw = str;
			XXX = str.Substring(1, 3);
			int z = 0;
			int.TryParse(str.Substring(4, 1), out z);
			SpeedCode = z;
			Ident = new string(str.Substring(5).TakeWhile(c => c != '\r' || c != '\n').ToArray());
		}
	}
}

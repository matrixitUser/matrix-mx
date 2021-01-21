using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	class Response17 : Response
	{
		public string Version { get; private set; }

		public Response17(byte[] data)
			: base(data)
		{
			var dataLength = data[2];
			var x = Encoding.ASCII.GetString(data, 3, data.Length - 3 - 2);
			var regex = new Regex(@"VZLJOT (..\.?){4}");
			var match = regex.Match(x);
			if (match.Success)
			{
				Version = match.Value;
			}
		}

		public override string ToString()
		{
			return string.Format("тсрв023, версия={0}", Version);
		}
	}
}

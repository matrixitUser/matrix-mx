using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.TSRV34
{
	class Response17 : Response
	{
		public string Version { get; private set; }

		public List<byte> Bytes { get; set; }

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
			return string.Format("ВЗЛЕТ, версия={0}", Version);
		}
	}

	/// <summary>
	/// todo
	/// победить лень...
	/// </summary>
	class Version
	{
		public int First { get; private set; }
		public int Second { get; private set; }
		public int Third { get; private set; }
		public int Fourth { get; private set; }

		public Version(int first, int second, int third, int fourth)
		{
			First = first;
			Second = second;
			Third = third;
			Fourth = fourth;
		}

		public override string ToString()
		{
			return string.Format("{0}.{1}.{2}.{3}", First, Second, Third, Fourth);
		}
	}
}

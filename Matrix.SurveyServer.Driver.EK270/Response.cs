using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
	class Response
	{
		public string Parameter { get; private set; }
		public IEnumerable<string> Values { get; private set; }

        public string Raw { get; private set; }

		public Response(byte[] data)
		{
			if (!Crc.Check(data, 1, data.Length - 1, new BccCalculator())) throw new Exception("Не сошлась контрольная сумма");

			var rawString = Encoding.ASCII.GetString(data, 1, data.Length - 3);

            Raw = rawString;

			rawString = rawString.Replace("\r\n", "").Replace(")(", "\n").Replace("(", "\n").Replace(")", "\n");
			var elements = rawString.Split('\n');
			Parameter = elements.FirstOrDefault();
			Values = elements.Skip(1).Take(elements.Count() - 2).ToList();
		}
	}
}

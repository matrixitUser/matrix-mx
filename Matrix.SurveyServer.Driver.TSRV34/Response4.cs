using System.Collections.Generic;
using System.Linq;

namespace Matrix.SurveyServer.Driver.TSRV34
{
	class Response4 : Response
	{
		public int RegisterCount { get; private set; }
		public List<byte> RegisterData { get; private set; }
		public Response4(byte[] data)
			: base(data)
		{
			if (data.Length > 3)
				RegisterCount = data[2];

			RegisterData = new List<byte>(RegisterCount);

			if (RegisterCount + 3 == data.Length && RegisterCount > 0)
			{
				RegisterData.AddRange(data.Skip(3));
			}
		}
	}
}

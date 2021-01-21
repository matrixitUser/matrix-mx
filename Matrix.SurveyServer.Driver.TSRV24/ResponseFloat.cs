using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV24
{
	class ResponseFloat : Response4
	{
		private List<float> values = new List<float>();
		public IEnumerable<float> Values
		{
			get
			{
				return values;
			}
		}

		public ResponseFloat(byte[] data)
			: base(data)
		{
			for (var offset = 0; offset < RegisterCount; offset += 4)
			{
				values.Add(Helper.ToSingle(RegisterData.ToArray(), offset));
			}
		}
	}
}

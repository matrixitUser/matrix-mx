using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV24
{
	class ResponseFloat : Response4
	{
		public ResponseFloat(byte[] data)
			: base(data)
		{
			for (var offset = 0; offset < RegisterCount; offset += 4)
			{
				values.Add(Helper.ToSingle(RegisterData.ToArray(), offset));
			}
		}
	}

    class ResponseWord : Response4
    {
        public ResponseWord(byte[] data)
            : base(data)
        {
            for (var offset = 0; offset < RegisterCount; offset += 4)
            {
                values.Add(Helper.ToUInt32(RegisterData.ToArray(), offset));
            }
        }
    }

    class ResponseLongFloat : Response4
    {
        public ResponseLongFloat(byte[] data)
            : base(data)
        {
            for (var offset = 0; offset < RegisterCount; offset += 8)
            {
                values.Add(Helper.ToLongAndFloat(RegisterData.ToArray(), offset));
            }
        }
    }
   
}

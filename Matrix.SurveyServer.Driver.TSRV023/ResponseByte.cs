using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV023
{
    class ResponseByte : Response3

    {
        private List<byte> values = new List<byte>();
        public IEnumerable<byte> Values
        {
            get
            {
                return values;
            }
        }

        public ResponseByte(byte[] data)
            : base(data)
        {
            for (var offset = 0; offset < RegisterCount; offset += 2)
             values.Add(Helper.ToByte(RegisterData.ToArray()));
        }

        public byte OneValue
        {
            get
            {
                return values.FirstOrDefault();
            }
        }
    }
}

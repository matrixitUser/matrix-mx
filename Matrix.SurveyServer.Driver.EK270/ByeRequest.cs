using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.EK270
{
    class ByeRequest : Request
    {
        public ByeRequest()
            : base(RequestType.Read, "", "")
        {

        }

        public override byte[] GetBytes()
        {            
            var bytes = new byte[]
            {
                Request.SOH,
                0x42,0x30, //B0
                Request.ETX,
                0x00
            };
            var crc = Crc.Calc(bytes, 1, 3, new BccCalculator()).CrcData;
            bytes[4] = crc[0];
            return bytes;
        }
    }
}

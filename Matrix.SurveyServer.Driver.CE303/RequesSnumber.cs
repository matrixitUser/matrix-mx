using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.CE303
{
    class RequestSnumber : Request
    {

        public RequestSnumber(string identName)
            : base(identName)
        {
        }

        public override byte[] GetBytes()
        {
             return new byte[] { SOH, 0x52, 0x31, STX, 0x53, 0x4E, 0x55, 0x4D, 0x42, 0x28, 0x29, 0x03, 0x5E };
        }

        public override string ToString()
        {
            return string.Format("Режим считывания данных");
        }
    }
}

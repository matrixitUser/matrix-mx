using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.CE303
{
    class RequestASKandOption : Request
    {
        private byte Regim;
        public RequestASKandOption(string identName, byte regim)
            : base(identName)
        {
            Regim = regim;
        }

        public override byte[] GetBytes()
        {
            List<byte> Data = new List<byte> { ASK, 0x30 , Z , Regim, CR, LF};
            return Data.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Режим считывания данных");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.CE303
{
    class RequestData : Request
    {
        private List<byte> NAMEParameter;
        public RequestData(string identName, string NameParameter)
            : base(identName)
        {
            NAMEParameter = new List<byte>(Encoding.Default.GetBytes(NameParameter));
        }

        public override byte[] GetBytes()
        {
            List<byte> Data = new List<byte> { SOH};
            int begВcc = Data.Count;
            Data.AddRange(R1);
            Data.Add(STX);
            Data.AddRange(NAMEParameter);
            Data.Add(ETX);
            Data.Add(ComputeВCC(Data, begВcc));
            return Data.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Режим считывания данных");
        }
    }
}

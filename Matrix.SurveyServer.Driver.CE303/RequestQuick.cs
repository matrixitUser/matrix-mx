using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.CE303
{
    class RequestQuick : Request
    {
        private List<byte> NAMEParameter;
        //private byte[] NetworkAddress = new byte[];
        public RequestQuick(string identName, string NameParameter)
            : base(identName)
        {
            NAMEParameter = new List<byte>(Encoding.Default.GetBytes(NameParameter));
        }

        private byte ComputeВCC(IList<byte> bytes,int begBCC)
        {
            byte bcc = 0;
            for (int i = begBCC; i < bytes.Count; i++)
            {
                bcc = (byte)(bcc + bytes[i]);
            }

            return bcc;
        }

        public override byte[] GetBytes()
        {
            List<byte> Data =  new List<byte> { NAKL, VOPROS, VOSKL };
            //if (address != null) Data.AddRange(address);
            Data.Add(SOH);
            int begВcc = Data.Count;
            Data.AddRange(R1);
            Data.Add(STX);
            //int begВcc = Data.Count;
            Data.AddRange(NAMEParameter);
            Data.Add(ETX);
            Data.Add(ComputeВCC(Data, begВcc));

            return Data.ToArray();
        }

        public override string ToString()
        {
            return string.Format("запрос информации о приборе");
        }
    }
}

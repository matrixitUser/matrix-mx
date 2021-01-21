using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.EK270
{
    class CustomRequest : Request
    {
        public const byte SOH = 0x01;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;

        public string Str { get; private set; }

        public CustomRequest(string str)
            : base(RequestType.Read, "", "")
        {
            Str = str;
        }

        public override byte[] GetBytes()
        {
            var e = Encoding.ASCII;

            var bytes = new List<byte>();
            bytes.Add(SOH);
            bytes.AddRange(e.GetBytes("R1"));
            bytes.Add(STX);
            bytes.AddRange(e.GetBytes(Str));
            bytes.Add(ETX);
            bytes.AddRange(Crc.Calc(bytes.ToArray(), 1, bytes.Count - 1, new BccCalculator()).CrcData);
            return bytes.ToArray();
        }
    }
}

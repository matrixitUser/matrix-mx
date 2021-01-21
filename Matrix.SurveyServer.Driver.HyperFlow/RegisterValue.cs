using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    class RegisterValue
    {
        protected readonly List<byte> Raw;
        public byte Id { get; private set; }

        public RegisterValue(byte[] raw)
        {
            Id = raw[0];
            Raw = new List<byte>(raw);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        public byte[] MakeParametersRequest(byte parameter, byte? count = null)
        {
            var Data = new List<byte>();
            Data.Add(parameter);

            if (count != null) Data.Add(count.Value);

            return MakeBaseRequest(0x08, Data);
        }
    }
}

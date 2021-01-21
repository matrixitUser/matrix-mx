using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        byte[] MakeTestRequest()
        {
            var Data = new List<byte>();
            Data.Add(0x00);
            return MakeBaseRequest(Data);
        }
    }
}

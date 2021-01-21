using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    class JournalRequest : Base
    {
        public JournalRequest(byte networkAddress, byte number)
            : base(networkAddress, 0x04)
        {
            Data.Add(0x01);
            Data.Add(number);
        }
    }
}

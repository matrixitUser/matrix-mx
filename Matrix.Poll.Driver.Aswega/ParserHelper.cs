using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SA94
{
    static class ParserHelper
    {
        public static Record ParseRecord(byte[] data, Address address)
        {
            //data 0 -63
            //data 64-127

            return new Record { Block1 = new Block(data, 0), Block2 = new Block(data, 64), Address = address };
            
        }
    }
}

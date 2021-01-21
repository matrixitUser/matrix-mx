using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private float FromBCD(byte[] bytes, int offset)
        {
            return BitConverter.ToSingle(new byte[] 
            { 
                bytes[0],
                bytes[1],
                bytes[2],
                (byte)(bytes[3]& 0xfe)
            }, 0);
        }
    }
}

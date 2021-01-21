using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common.Crc
{
    public interface ICrcCalculator
    {
        int CrcDataLength { get; }
        Crc Calculate(byte[] buffer, int offset, int length);
    }
}

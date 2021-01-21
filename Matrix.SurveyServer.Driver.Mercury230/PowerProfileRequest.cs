using Matrix.SurveyServer.Driver.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    class PowerProfileRequest : Base
    {
        public PowerProfileRequest(byte networkAddress, UInt32 address, byte count, byte profile)
            : base(networkAddress, 0x06)
        {
            byte strange = 1;
            //byte addressBit = isByte17 ? (byte)0 : (byte)1;
            byte energyType = 0;
            byte memory = 3;

            profile = (profile & 0x01) > 0 ? (byte)(address >> 16) : (byte)0;

            strange = (byte)(((profile & 0x01) << 7) | ((energyType & 0x07) << 4) | (memory & 0xf));

            Data.Add(strange);
            Data.Add(Helper.GetHighByte(address));
            Data.Add(Helper.GetLowByte(address));
            Data.Add(count);
        }
    }
}

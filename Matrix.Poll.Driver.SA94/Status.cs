using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public struct Status
    {
        public bool b6_isQ2ComputeByT3;
        public bool b5_isNotSA94;
        public bool b4_isModeCount;
        public bool b3_isSA94;
        public bool b2_isTwoChannels;
        public bool b1_SA94_1_isT2Programmed;
        public bool b1_SA94_2_2M_isT3Measured;
        public bool b0_SA94_1_2M_isPrn1OnBackTube;
        public bool b0_SA94_2_isComputeT3Programmed;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        dynamic MakeChipidRequest(UInt16 devid)
        {
            return MakeRegisterRequest((UInt32)GetRegisterSet(devid).Chipid, 12);
        }
        

        public dynamic ParseChipidResponse(dynamic answer)
        {
            var chipid = ParseRegisterResponse(answer);
            if (!chipid.success) return chipid;
            
            chipid.text = string.Format("{0:X2}{1:X2}{2:X2}{3:X2} {4:X2}{5:X2}{6:X2}{7:X2} {8:X2}{9:X2}{10:X2}{11:X2}", 
                chipid.Register[0], chipid.Register[1], chipid.Register[2], chipid.Register[3], 
                chipid.Register[4], chipid.Register[5], chipid.Register[6], chipid.Register[7], 
                chipid.Register[8], chipid.Register[9], chipid.Register[10], chipid.Register[11]
            );

            chipid.enabled = (chipid.text != "00000000 00000000 00000000");
            return chipid;
        }

    }

}

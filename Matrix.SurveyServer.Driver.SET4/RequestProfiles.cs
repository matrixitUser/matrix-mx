using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
	{
        byte[] MakeRequestProfiles(byte ident, byte nMemory, byte addrHigh, byte addrLow, byte count)
        {
            var Data = new List<byte>();
			Data.Add(0x0C);//код запроса (чтение параметров)		
            Data.Add(ident);
            Data.Add(nMemory);
            Data.Add(addrHigh);
            Data.Add(addrLow);
            Data.Add(count);

            return MakeBaseRequest(Data);
		}
	}
}

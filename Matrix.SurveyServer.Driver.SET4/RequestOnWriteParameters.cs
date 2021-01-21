using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
	{
		byte[] MakeRequestOnWriteParameters (byte numberParam, byte[] Params)
        {
            var Data = new List<byte>();
			Data.Add(0x03);//код запроса (запись параметров)		
            Data.Add(numberParam);
            if (Params != null) Data.AddRange(Params);

            return MakeBaseRequest(Data);
		}
	}
}

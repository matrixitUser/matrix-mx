using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        byte[] MakeRequestLogsExt(byte numberLog, byte nRecord)
        {
            var Data = new List<byte>();
            Data.Add(0x09);//код запроса (чтение журналов расширенный)		
            Data.Add(numberLog);
            Data.Add(nRecord);
            return MakeBaseRequest(Data);
        }
    }
}

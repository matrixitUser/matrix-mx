using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MatrixTerminal
{
    public partial class Driver
    {
        private const int MODBUS_USER_FUNCTION_WRITE_BKP = 80;

        byte[] MakeWriteBkpRequest(DateTime time)
        {
            var Data = new List<byte>();

            //timestamp
            UInt32 ts;
            if(time == DateTime.MinValue || time == DateTime.MaxValue)
            {
                ts = 0xFFFFFFFF;
            }
            else
            {
                var span = (time.ToLocalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0));
                ts = (UInt32)span.TotalSeconds;
            }

            //channels
            Data.AddRange(BitConverter.GetBytes(ts).Reverse());
            return MakeWriteHoldingRegisterRequest(0x32000, 4, Data);     
        }
        
    }
}

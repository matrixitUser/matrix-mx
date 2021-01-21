using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        private const int MODBUS_USER_FUNCTION_WRITE_BKP = 80;

        byte[] MakeWriteBkpRequest(DateTime time, UInt16 devid, UInt32[] channels = null)
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

            if(GetRegisterSet(devid).name == "new")
            {
                Data.AddRange(BitConverter.GetBytes(ts).Reverse());
                if (channels != null)
                {
                    foreach(var channel in channels)
                    {
                        Data.AddRange(BitConverter.GetBytes(channel).Reverse());
                    }
                }
                return MakeWriteHoldingRegisterRequest((UInt32)GetRegisterSet(devid).Timestamp, 4, Data);
            }

            Data.AddRange(BitConverter.GetBytes(ts));
            if (channels != null)
            {
                foreach (var channel in channels)
                {
                    Data.AddRange(BitConverter.GetBytes(channel));
                }
            }
            return MakeBaseRequest(MODBUS_USER_FUNCTION_WRITE_BKP, Data);            
        }
        
    }
}

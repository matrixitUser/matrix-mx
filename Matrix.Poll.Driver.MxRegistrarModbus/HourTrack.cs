using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus3
{
    class HourTrack : ITrack
    {
        public byte[] GetRequest(byte networkAddress, int inx)
        {
            return Make65Request(networkAddress, ArchiveType.Hourly, (UInt16)inx);//Direction.MasterToSlave, networkAddress, 140, BitConverter.GetBytes(inx));
        }

        public DateTime GetDate(byte[] rsp, dynamic passport)
        {
            return Parse65Response(rsp, passport).Date;
        }

        public List<Data> GetData(byte[] rsp, dynamic passport)
        {
            return Parse65Response(rsp, passport).Data;
        }

        public int GetOffset(DateTime a, DateTime b)
        {
            return (int)(a - b).TotalHours;
        }

        public int GetCapacity(dynamic passport)
        {
            return 5842;
        }
    }
}

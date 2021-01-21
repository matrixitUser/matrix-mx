//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class MemoryRequest : Request
//    {
//        public MemoryRequest(string serialNumber, UInt16 start, UInt16 count)
//            : base(serialNumber, 0x02)
//        {
//            body.AddRange(BitConverter.GetBytes(start));
//            body.AddRange(BitConverter.GetBytes(count));
//        }
//    }
//}

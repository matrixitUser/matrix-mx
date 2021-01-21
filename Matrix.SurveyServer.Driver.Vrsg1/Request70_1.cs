//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Vrsg1
//{
//    class Request70_1 : Request
//    {
//        public Request70_1(byte networkAddress, byte channel, byte regim, DateTime day)
//            : base(networkAddress, 70, 1)
//        {
//            Data.Add(channel);
//            Data.Add(regim);
//            Data.Add((byte)day.Day);
//            Data.Add((byte)day.Month);
//            Data.Add((byte)(day.Year - 2000));
//            Data.Add(0x00);
//            Data.Add(0x00);
//        }
//    }
//}

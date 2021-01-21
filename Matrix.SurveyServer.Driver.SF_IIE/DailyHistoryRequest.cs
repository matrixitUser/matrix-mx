//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    class DailyHistoryRequest : Request
//    {
//        public DailyHistoryRequest(byte networkAddress, byte channel, byte seq, DateTime start, DateTime end)
//            : base(networkAddress, 20)
//        {
//            Data.Add(channel);
//            Data.Add(seq);
//            Data.Add((byte)start.Month);
//            Data.Add((byte)start.Day);
//            Data.Add((byte)(start.Year % 100));
//            Data.Add((byte)end.Month);
//            Data.Add((byte)end.Day);
//            Data.Add((byte)(end.Year % 100));
//        }
//    }
//}

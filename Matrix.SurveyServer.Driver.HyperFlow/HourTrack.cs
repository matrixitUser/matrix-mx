//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class HourTrack : ITrack
//    {
//        public Request GetRequest(byte networkAddress, int inx)
//        {
//            return new Request(Direction.MasterToSlave, networkAddress, 140, BitConverter.GetBytes(inx));
//        }

//        public DateTime GetDate(byte[] rsp)
//        {
//            return new HourResponse(rsp).Hour;
//        }

//        public List<Data> GetData(byte[] rsp)
//        {
//            return new HourResponse(rsp).Hours;
//        }

//        public int GetOffset(DateTime a, DateTime b)
//        {
//            return (int)(a - b).TotalHours;
//        }
//    }
//}

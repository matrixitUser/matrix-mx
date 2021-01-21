//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class DayTrack : ITrack
//    {
//        public Request GetRequest(byte networkAddress, int inx)
//        {
//            return new Request(Direction.MasterToSlave, networkAddress, 142, BitConverter.GetBytes(inx));
//        }

//        public DateTime GetDate(byte[] rsp)
//        {
//            var ret = new DayResponse(rsp).Day;
//            return ret.AddHours(-ret.Hour);
//        }

//        public List<Data> GetData(byte[] rsp)
//        {
//            return new DayResponse(rsp).Days;
//        }

//        public int GetOffset(DateTime a, DateTime b)
//        {
//            /*var aNormal = a;//.AddHours(-a.Hour);
//            var bNormal = b;//.AddHours(-b.Hour);*/
//            return (int)(a - b).TotalDays;
//        }
//    }
//}

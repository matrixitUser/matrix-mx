//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class RaccordRequest : Request
//    {
//        public int Length { get; private set; }

//        public RaccordRequest(int length) : base("", 0x00) 
//        {
//            Length = length;
//        }

//        public override byte[] GetBytes()
//        {            
//            var bytes=new byte[Length];
//            for(var i=0;i<Length;i++)
//            {
//                bytes[i] = 0x55;
//            }
//            return bytes;
//        }
//    }
//}

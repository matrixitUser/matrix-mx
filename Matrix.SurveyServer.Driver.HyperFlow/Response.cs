//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class Response
//    {
//        public Direction Direction { get; private set; }
//        public byte NetworkAddress { get; private set; }
//        public byte Command { get; private set; }
//        public byte Length { get; private set; }
//        public byte[] Body { get; private set; }
//        public UInt16 Status { get; private set; }

//        public Response(byte[] data)
//        {
//            //убираем преамбулу
//            var clearData = data.SkipWhile(b => b == 0xff).ToArray();
//            Direction = (Direction)clearData[0];
//            NetworkAddress = clearData[1];
//            Command = clearData[2];
//            Length = (byte)(clearData[3] - (byte)2);
//            Status = BitConverter.ToUInt16(clearData, 4);
//            Body = clearData.Skip(6).Take(clearData.Length - (6 + 1)).ToArray();
//        }
//    }
//}

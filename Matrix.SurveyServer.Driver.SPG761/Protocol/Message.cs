//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.SPG761.Protocol
//{
//    class Message : IBytes
//    {
//        public const byte DLE = 0x10;

//        public Head Head { get; set; }
//        public Body Body { get; set; }

//        public Message()
//        {
//            Head = new Head();
//            Body = new Body();
//        }

//        public IEnumerable<byte> GetBytes()
//        {
//            List<byte> bytes = new List<byte>();

//            bytes.AddRange(Head.GetBytes());
//            bytes.AddRange(Body.GetBytes());

//            var crc = Crc.Calc(bytes.ToArray(), 2, bytes.Count - 2, new CrcSpg());

//            bytes.AddRange(crc.CrcData);


//            return bytes;
//        }

//        public static Message Parse(byte[] data)
//        {
//            int stxPosition = 0;
//            for (int i = 1; i < data.Length; i++)
//            {
//                if (data[i] == Body.STX && data[i - 1] == DLE)
//                {
//                    stxPosition = i;
//                    break;
//                }
//            }

//            var headLength = stxPosition - 1;
//            var bodyStart = stxPosition;
//            var bodyLength = data.Length - bodyStart - 2;

//            var head = Head.Parse(data, 0, headLength);
//            if (head == null) return null;

//            var body = Body.Parse(data, bodyStart, bodyLength);
//            if (body == null) return null;

//            return new Message()
//            {
//                Head = head,
//                Body = body
//            };
//        }
//    }
//}

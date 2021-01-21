//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SPG761.Protocol
//{
//    class Body : IBytes
//    {
//        public const byte STX = 0x02;
//        public const byte ETX = 0x03;
//        public const byte DLE = 0x10;

//        public List<Category> Categories { get; set; }

//        public Body()
//        {
//            Categories = new List<Category>();
//        }

//        public IEnumerable<byte> GetBytes()
//        {
//            List<byte> bytes = new List<byte>();

//            bytes.Add(Message.DLE);
//            bytes.Add(STX);

//            var rawBytes =new List<byte>();
//            Categories.ToList().ForEach(c => rawBytes.AddRange(c.GetBytes()));

//            foreach(var rb in rawBytes)
//            {
//                bytes.Add(rb);
//                if (rb == Message.DLE) bytes.Add(rb);
//            }

//            bytes.Add(Message.DLE);
//            bytes.Add(ETX);

//            return bytes;
//        }

//        public override string ToString()
//        {
//            return string.Format("{0}", string.Join(",", Categories));
//        }

//        public static Body Parse(byte[] data, int offset, int lenght)
//        {
//            var body = new Body();
//            int categoryStart = offset + 2;



//            for (int ffPosition = offset + 2; ffPosition < offset + lenght; ffPosition++)
//            {
//                if (data[ffPosition] == Category.FF)
//                {
//                    var category = Category.Parse(data, categoryStart, ffPosition - categoryStart);
//                    body.Categories.Add(category);
//                    categoryStart = ffPosition + 1;
//                }
//            }
//            return body;
//        }
//    }
//}

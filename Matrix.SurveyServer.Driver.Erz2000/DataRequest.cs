//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    class DataRequest : Request
//    {
//        public byte Group { get; private set; }
//        public byte Channel { get; private set; }
//        public long Number { get; private set; }

//        public DataRequest(byte networkAddress, byte group, byte channel, int number)
//            : base(networkAddress, 65)
//        {
//            Group = group;
//            Channel = channel;
//            Number = number;

//            Data.Add(Group);
//            Data.Add(Channel);
//            Data.AddRange(BitConverter.GetBytes(number).Reverse());
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос архивной записи №{0}, группа {1}, параметр {2}", Number, Group, Channel);
//        }
//    }
//}

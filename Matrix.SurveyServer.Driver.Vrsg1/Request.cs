//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Vrsg1
//{
//    class Request
//    {
//        public byte NetworkAddress { get; private set; }
//        public byte Function { get; private set; }
//        public byte Command { get; private set; }

//        protected readonly List<byte> Data = new List<byte>();


//        public Request(byte networkAddress, byte function, byte command)
//        {
//            NetworkAddress = networkAddress;
//            Function = function;
//            Command = command;
//        }


//        public byte[] GetBytes()
//        {
//            var bytes = new List<byte>();
//            bytes.Add(NetworkAddress);
//            bytes.Add(Function);
//            bytes.Add(Command);

//            bytes.AddRange(Data);

//            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
//            bytes.Add(crc.CrcData[0]);
//            bytes.Add(crc.CrcData[1]);
//            return bytes.ToArray();
//        }

//    }
//}

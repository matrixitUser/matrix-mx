//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    /// <summary>
//    /// базовый modbus запрос
//    /// </summary>
//    class Request
//    {
//        public byte NetworkAddress { get; private set; }
//        public byte Function { get; private set; }
//        public List<byte> Data { get; protected set; }

//        public Request(byte networkAddress, byte function)
//        {
//            NetworkAddress = networkAddress;
//            Function = function;
//            Data = new List<byte>();
//        }

//        public byte[] GetBytes()
//        {
//            var bytes = new List<byte>();
//            bytes.Add(NetworkAddress);
//            bytes.Add(Function);

//            bytes.AddRange(Data);

//            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
//            bytes.Add(crc.CrcData[0]);
//            bytes.Add(crc.CrcData[1]);
//            return bytes.ToArray();
//        }
//    }
//}

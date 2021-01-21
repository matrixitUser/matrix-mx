//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    class Request
//    {
//        public byte Address { get; private set; }
//        public byte Function { get; private set; }
//        public byte Sync { get; private set; }

//        protected readonly List<byte> Data = new List<byte>();

//        public Request(byte address, byte function)
//        {
//            Address = address;
//            Function = function;
//            Sync = 0xaa;
//        }

//        public virtual byte[] GetBytes()
//        {
//            var bytes = new List<byte>();
//            bytes.Add(Sync);
//            bytes.Add(Address);
//            bytes.Add((byte)(4 + Data.Count + 2));
//            bytes.Add(Function);

//            bytes.AddRange(Data);

//            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());

//            bytes.AddRange(crc.CrcData);

//            return bytes.ToArray();
//        }
//    }
//}

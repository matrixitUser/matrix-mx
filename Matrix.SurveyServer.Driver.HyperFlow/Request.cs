//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class Request
//    {
//        public Direction Direction { get; private set; }
//        public byte NetworkAddress { get; private set; }
//        public byte Command { get; private set; }
//        public IEnumerable<byte> Data { get; private set; }

//        public Request(Direction direction, byte networkAddress, byte command, IEnumerable<byte> data)
//        {
//            Direction = direction;
//            NetworkAddress = networkAddress;
//            Command = command;
//            Data = data;
//        }

//        public byte[] GetBytes()
//        {
//            var bytes = new List<byte>();

//            bytes.Add((byte)Direction);
//            bytes.Add(NetworkAddress);
//            bytes.Add(Command);
//            bytes.Add((byte)Data.Count());
//            bytes.AddRange(Data);

//            var crc = Crc.Calc(bytes.ToArray(), new HartCrc());
//            bytes.AddRange(crc.CrcData);

//            for (int i = 0; i < 8; i++)
//            {
//                bytes.Insert(0, 0xff);
//            }

//            return bytes.ToArray();
//        }
//    }

//    enum Direction : byte
//    {
//        MasterToSlave = 0x02,
//        SlaveToMaster = 0x06
//    }
//}

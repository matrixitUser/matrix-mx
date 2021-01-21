//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class Request
//    {
//        public string SerialNumber { get; private set; }
//        public byte Command { get; private set; }

//        protected readonly List<byte> body;

//        public Request(string serialNumber, byte command)
//        {
//            body = new List<byte>();
//            Command = command;
//            SerialNumber = serialNumber;
//        }

//        private byte[] GetSerialNumberBytes(string serialNumber)
//        {            
//            int num = 0;
//            int.TryParse(serialNumber, out num);            
//            return BitConverter.GetBytes(num);
//        }

//        public virtual byte[] GetBytes()
//        {
//            var bytes = new List<byte> { 0xa5, 0x01 };
//            bytes.AddRange(GetSerialNumberBytes(SerialNumber));
//            bytes.Add(Command);

//            bytes.Add(Helper.GetLowByte(body.Count));
//            bytes.Add(Helper.GetHighByte(body.Count));
//            bytes.AddRange(body);
//            var crc = Crc.Calc(bytes.ToArray(), new GoboyCrcCalculator());
//            bytes.AddRange(crc.CrcData);

//            return bytes.ToArray();
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MatrixTerminal
{
    public partial class Driver
    {
         public byte[] MakeBaseRequest(byte Function, List<byte> Data)// = null
        {
            var bytes = new List<byte>();
            if(NetworkAddress.Count > 1)
            {
                bytes.Add(251);
            }
            bytes.AddRange(NetworkAddress);
            bytes.Add(Function);

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.Add(crc.CrcData[0]);
            bytes.Add(crc.CrcData[1]);

            return bytes.ToArray();
        }
        public byte[] MakeBaseRequest(byte networkAddress, byte Function, List<byte> Data)// = null
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(networkAddress);
            bytes.Add(Function);

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.Add(crc.CrcData[0]);
            bytes.Add(crc.CrcData[1]);

            return bytes.ToArray();
        }
    }
}

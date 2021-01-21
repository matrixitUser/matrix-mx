using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        public byte[] MakeBaseRequest(byte RequestCode, List<byte> Data)// = null
        {
            var bytes = new List<byte>();
            bytes.Add(NetworkAddress);
            bytes.Add(RequestCode);

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

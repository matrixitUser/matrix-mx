using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        public byte[] CalcGoboiCrc(byte[] buffer)
        {
            Int16 sum = (Int16)buffer.Skip(0).Take(buffer.Length).Sum(d => d);
            return new byte[]
			{
                GetLowByte(sum),
                GetHighByte(sum)																
			};
        }

        private bool CheckGoboiCrc(byte[] data)
        {
            var crc = CalcGoboiCrc(data.Take(data.Length - 2).ToArray());
            var supposeCrc = data.Skip(data.Length - 2).ToArray();
            for (var i = 0; i < 2; i++)
            {
                if (crc[i] != data[data.Length - 2 + i]) return false;
            }
            return true;
        }
    }
}

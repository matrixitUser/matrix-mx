using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class Init2Request : Request
	{
		const byte ACK = 0x06;

		public int SpeedCode { get; private set; }
		public string Regim { get; private set; }

		public Init2Request(int speedCode, string regim)
			: base(RequestType.Read, "", "")
		{
			SpeedCode = speedCode;
			Regim = regim;
		}

		public override byte[] GetBytes()
		{
			var bytes = new List<byte>();
			bytes.Add(ACK);
			bytes.AddRange(Encoding.ASCII.GetBytes(string.Format("0{0}{1}\r\n", SpeedCode, Regim)));
			return bytes.ToArray();
		}
	}
}

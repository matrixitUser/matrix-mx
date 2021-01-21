using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	abstract class Request
	{
		public byte NetworkAddress { get; private set; }
		public byte Function { get; private set; }

		public List<byte> Data { get; private set; }

		public Request(byte networkAddress, byte function)
		{
			Data = new List<byte>();
			NetworkAddress = networkAddress;
			Function = function;
		}

		public byte[] GetBytes()
		{
			var data = new List<byte>();
			data.Add(NetworkAddress);
			data.Add(Function);

			data.AddRange(Data);

			var crc = Crc.Calc(data.ToArray(), new Crc16Modbus());
			data.Add(crc.CrcData[0]);
			data.Add(crc.CrcData[1]);
			return data.ToArray();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class Response
	{
		public byte NetworkAddress { get; private set; }
		public byte Function { get; private set; }

		public Response(byte[] data)
		{
			if (data.Length <= 4) throw new Exception("слишком короткий ответ");
			if (!Crc.Check(data.ToArray(), new Crc16Modbus())) throw new Exception("не сошлась контрольная сумма");

			NetworkAddress = data[0];
			Function = data[1];
		}

		public override string ToString()
		{
			return string.Format("необрабатываемый ответ на функцию {0:X}", Function);
		}
	}
}

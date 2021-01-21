using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	abstract class Response
	{
		public byte NetworkAddress { get; private set; }
		public byte Function { get; private set; }

		public Response(byte[] data)
		{
			if (data.Length < 5) throw new Exception("в кадре ответа не может содежаться менее 5 байт");
			if (!Crc.Check(data, new Crc16Modbus())) throw new Exception("контрольная сумма кадра не сошлась");

			NetworkAddress = data[0];
			Function = data[1];

			//modbus error
			if (Function > 0x80)
			{
				var exceptionCode = (ModbusExceptionCode)data[2];
				throw new Exception(string.Format("устройство вернуло ошибку: {0}", exceptionCode));
			}
		}
	}

	enum ModbusExceptionCode : byte
	{
		ILLEGAL_FUNCTION = 0x01,
		ILLEGAL_DATA_ADDRESS = 0x02,
		ILLEGAL_DATA_VALUE = 0x03,
		SLAVE_DEVICE_FAILURE = 0x04,
		ACKNOWLEDGE = 0x05,
		SLAVE_DEVICE_BUSY = 0x06,
		MEMORY_PARITY_ERROR = 0x07,
		GATEWAY_PATH_UNAVAILABLE = 0x0a,
		GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
	}
}

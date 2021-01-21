using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;

namespace Matrix.SurveyServer.Driver.SET4
{
	abstract class Response
	{
		public byte NetworkAddress { get; private set; }
		public string ErrorMessage { get; private set; }

		public byte[] Body { get; private set; }

		public Response(byte[] data)
		{
			data = data.SkipWhile(b => b == 0xff).ToArray();
			Body = data.Skip(1).Take(data.Count() - 3).ToArray();
			if (data.Length < 4) throw new Exception("в кадре ответа не может содержаться менее 4 байт");
			if (!Crc.Check(data, new Crc16Modbus())) throw new Exception("контрольная сумма кадра не сошлась");

			NetworkAddress = data[0];

			//modbus error
			if (data.Length == 4)
			{
				switch (data[1])
				{
					case 0x00:
						ErrorMessage = "все нормально"; break;
					case 0x01:
						throw new Exception("недопустимая команда или параметр");
					case 0x02:
						throw new Exception("внутренняя ошибка счетчика");
					case 0x03:
						throw new Exception("не достаточен уровень доступа для удовлетворения запроса");
					case 0x04:
						throw new Exception("внутренние часы счетчика уже корректировались в течении текущих суток");
					case 0x05:
						throw new Exception("не открыт канал связи");
					default:
						throw new Exception("неизвестная ошибка");
				}
			}
		}
	}
}

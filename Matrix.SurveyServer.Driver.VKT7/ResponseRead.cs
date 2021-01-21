using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseRead : Response
	{
		public byte Length { get; private set; }

		public ResponseRead(byte[] data)
			: base(data)
		{
			if (data.Length <= 5) throw new Exception("пакет короток");
			if (NetworkAddress == 0x83)
			{
				throw new Exception(string.Format("вычислитель вернул ошибку; код={0}", data[2]));
			}
			Length = data[2];
			if (data.Length < Length + 4) throw new Exception("полученное число байт не соответствует заявленной в байте КБ");
		}
	}
}

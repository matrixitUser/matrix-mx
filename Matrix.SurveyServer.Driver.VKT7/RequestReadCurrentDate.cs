using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на чтение текущей даты и времени
	/// см. док. п. 4.14 стр. 22
	/// </summary>
	public class RequestReadCurrentDate : RequestRead
	{
		public RequestReadCurrentDate(byte networkAddress)
			: base(networkAddress, 0x3ffb, 0x0000)
		{
		}

		

		public override string ToString()
		{
			return string.Format("чтение текущей даты");
		}
	}
}

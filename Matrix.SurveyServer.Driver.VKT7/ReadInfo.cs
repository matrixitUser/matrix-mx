using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на чтение служебной информации
	/// см. док. п. 4.6 стр. 17
	/// </summary>
	public class RequestReadInfo : RequestRead
	{
		public RequestReadInfo(byte networkAddress)
			: base(networkAddress, 0x3ff9, 0x0000)
		{
		}

		public override string ToString()
		{
			return string.Format("служебная информация");
		}
	}
}

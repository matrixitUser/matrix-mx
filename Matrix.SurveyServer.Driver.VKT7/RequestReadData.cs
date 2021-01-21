using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на чтение данных 
	/// см. док. п. 4.5 стр. 16
	/// </summary>
	public abstract class RequestReadData : RequestRead
	{
		public RequestReadData(byte networkAddress)
			: base(networkAddress, 0x3ffe, 0x0000)
		{
		}

		public override string ToString()
		{
			return string.Format("чтение данных");
		}
	}
}

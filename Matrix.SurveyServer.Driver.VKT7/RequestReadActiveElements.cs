using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на чтение перечня активных элементов данных
	/// см. док. п. 4.1 стр. 13
	/// </summary>
	public class RequestReadActiveElements : RequestRead
	{
		public RequestReadActiveElements(byte networkAddress)
			: base(networkAddress, 0x3ffc, 0x0000)
		{
		}		

		public override string ToString()
		{
			return string.Format("чтение перечня активных элементов");
		}
	}
}

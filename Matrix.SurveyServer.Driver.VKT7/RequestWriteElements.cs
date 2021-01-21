using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на запись перечня элементов для чтения
	/// см. док. п. 4.2 стр. 13
	/// </summary>
	public class RequestWriteElements : RequestWrite
	{
		public RequestWriteElements(byte networkAddress, IEnumerable<Element> elements) :
			base(networkAddress, 0x3fff, 0x0000, elements.GetBytes())
		{
		}

		public override string ToString()
		{
			return string.Format("запись перечня элементов");
		}
	}
}

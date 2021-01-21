using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запись регистров
	/// см. док. п. 3.2. стр. 12
	/// </summary>
	public class ResponseWrite : Response
	{
		public ResponseWrite(byte[] data)
			: base(data)
		{
			if (NetworkAddress == 0x90)
			{
				throw new Exception(string.Format("вычислитель вернул ошибку; код={0}", data[2]));
			}


		}
	}
}

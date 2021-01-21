using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	/// <summary>
	/// запрос на запись даты
	/// см. док. п. 4.4 стр. 15
	/// </summary>
	public class RequestWriteDate : RequestWrite
	{
		private DateTime date;
		public RequestWriteDate(byte networkAddress, DateTime date)
			: base(networkAddress, 0x3ffb, 0x0000, new byte[]
			{
				(byte)date.Day,
				(byte)date.Month,
				(byte)(date.Year-2000),
				(byte)date.Hour,
			})
		{
			this.date = date;
		}



		public override string ToString()
		{
			return string.Format("запись даты: {0:dd.MM.yyyy HH:mm}", date);
		}
	}
}

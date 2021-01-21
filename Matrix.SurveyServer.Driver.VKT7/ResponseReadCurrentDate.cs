using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseReadCurrentDate : ResponseRead
	{
		public DateTime Date { get; private set; }

		public ResponseReadCurrentDate(byte[] data)
			: base(data)
		{
			int day = data[3];
			int month = data[4];
			int year = data[5] + 2000;
			int hour = data[6];
			int minute = data[7];
			int second = data[8];

			Date = new DateTime(year, month, day, hour, minute, second);
		}

		public override string ToString()
		{
			return string.Format("текущая дата: {0:dd.MM.yyyy HH:mm:ss}", Date);
		}
	}
}

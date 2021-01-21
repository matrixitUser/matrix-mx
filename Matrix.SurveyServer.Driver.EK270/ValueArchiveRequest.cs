using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
	class ValueArchiveRequest : ArchiveRequest
	{
		public ValueArchiveRequest(int column, DateTime dateStart, DateTime dateEnd, int count)
			: base(ArchiveType.Interval, ArchiveAttribute.Value, string.Format("{0};{1:yyyy-MM-dd,HH:mm:ss};{2:yyyy-MM-dd,HH:mm:ss};{3}", column, dateStart, dateEnd, count))
		{

		}

        public ValueArchiveRequest(int column, DateTime dateStart, int count)
            : base(ArchiveType.Interval, ArchiveAttribute.Value, string.Format("{0};{1:yyyy-MM-dd,HH:mm:ss};;{2}", column, dateStart, count))
        {

        }
	}
}

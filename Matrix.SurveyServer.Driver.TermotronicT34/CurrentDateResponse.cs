using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	class CurrentDateResponse : Response
	{
		public DateTime Date { get; private set; }
		public bool IsWinter { get; private set; }


        public CurrentDateResponse(byte[] data, byte networkaddress)
			: base(data, networkaddress)
		{			
			var second = Helper.ToBCD(Body[0]);
			var minute = Helper.ToBCD(Body[1]);
			var hour = Helper.ToBCD(Body[2]);
			var unknown = Helper.ToBCD(Body[3]);
			var day = Helper.ToBCD(Body[4]);
			var month = Helper.ToBCD(Body[5]);
			var year = Helper.ToBCD(Body[6]);
			IsWinter = Body[7] == 1;
			Date = new DateTime(2000 + year, month, day, hour, minute, second);
		}
	}
}

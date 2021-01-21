using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	class PowerResponse : Response
	{
		public List<Data> Data { get; private set; }

		public PowerResponse(byte[] data, DateTime date, byte networkaddress)
			: base(data, networkaddress)
		{
			Data = new List<Common.Data>();

			try
			{
				var value1 = Helper.MercuryStrange(data, 1) / 100.0;
				Data.Add(new Data("Мощность (фаза 1)", Matrix.Common.Agreements.MeasuringUnitType.Wt, date, value1, Matrix.Common.Agreements.CalculationType.Average, 0));

				var value2 = Helper.MercuryStrange(data, 4) / 100.0;
				Data.Add(new Data("Мощность (фаза 2)", Matrix.Common.Agreements.MeasuringUnitType.Wt, date, value2, Matrix.Common.Agreements.CalculationType.Average, 0));

				var value3 = Helper.MercuryStrange(data, 7) / 100.0;
				Data.Add(new Data("Мощность (фаза 3)", Matrix.Common.Agreements.MeasuringUnitType.Wt, date, value3, Matrix.Common.Agreements.CalculationType.Average, 0));
			}
			catch (Exception ex)
			{

			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TSRV023
{
	/// <summary>
	/// часовой, суточный, месячный архив нарастающим итогом (в тепловычислителях с версией старше 76.30.03.00)
	/// </summary>
	class Response65Totals : Response
	{

		public List<Data> Data { get; private set; }

		public Response65Totals(byte[] data)
			: base(data)
		{
			Data = new List<Data>();

			int offset = 3;

			var seconds = Helper.ToUInt32(data, offset + 0);
			var date = new DateTime(1970, 1, 1).AddSeconds(seconds);

			var heat1 = Helper.ToLongAndFloat(data, offset + 4);
			Data.Add(new Data("Тепло ТС1", MeasuringUnitType.Gkal, date, heat1));

			var heat2 = Helper.ToLongAndFloat(data, offset + 12);
			Data.Add(new Data("Тепло ТС2", MeasuringUnitType.Gkal, date, heat2));

			var heat3 = Helper.ToLongAndFloat(data, offset + 20);
			Data.Add(new Data("Тепло ТС3", MeasuringUnitType.Gkal, date, heat3));

			var mass1 = Helper.ToLongAndFloat(data, offset + 28);
			Data.Add(new Data("Масса ТС1", MeasuringUnitType.tonn, date, mass1));

			var mass2 = Helper.ToLongAndFloat(data, offset + 36);
			Data.Add(new Data("Масса ТС2", MeasuringUnitType.tonn, date, mass2));

			var mass3 = Helper.ToLongAndFloat(data, offset + 44);
			Data.Add(new Data("Масса ТС3", MeasuringUnitType.tonn, date, mass3));
		}
	}
}

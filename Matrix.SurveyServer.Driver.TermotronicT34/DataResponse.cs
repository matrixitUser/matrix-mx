using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	class DataResponse : Response
	{
		public List<Data> Data { get; private set; }

		public DataResponse(byte[] data, DateTime date, byte tariff, int A, byte networkaddress)
			: base(data, networkaddress)
		{
			Data = new List<Data>();

			/// Если поле данных ответа содержит 16 байт, то отводится по четыре двоичных байта
			/// на каждый вид энергии в последовательности: активная прямая (А+), активная обратная
			/// (А-), реактивная прямая (R+), реактивная обратная (R-).
			if (data.Length == 16 + 3)
			{
				var offset = 1;
				var ap = (double)Helper.ToInt32(data, offset + 0) / (2.0 * A);
				var am = (double)Helper.ToInt32(data, offset + 4) / (2.0 * A);
				var pp = (double)Helper.ToInt32(data, offset + 8) / (2.0 * A);
				var pm = (double)Helper.ToInt32(data, offset + 12) / (2.0 * A);

				Data.Add(new Data(string.Format("A+ (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, ap));
				Data.Add(new Data(string.Format("A- (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, am));
				Data.Add(new Data(string.Format("R+ (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, pp));
				Data.Add(new Data(string.Format("R- (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, pm));
			}

			/// Если поле данных ответа содержит 12 байт, то отводится по четыре двоичных байта
			/// на каждую фазу энергии А+ в последовательности: активная прямая по 1 фазе, активная
			/// прямая по 2 фазе, активная прямая по 3 фазе.
			if (data.Length == 12 + 3)
			{
				var offset = 1;
				var ap1 = Helper.ToInt32(data, offset + 0);
				var ap2 = Helper.ToInt32(data, offset + 4);
				var ap3 = Helper.ToInt32(data, offset + 8);

				Data.Add(new Data(string.Format("A+ (фаза 1) (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, ap1));
				Data.Add(new Data(string.Format("A+ (фаза 2) (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, ap2));
				Data.Add(new Data(string.Format("A+ (фаза 3) (тариф {0})", tariff), Matrix.Common.Agreements.MeasuringUnitType.WtH, date, ap3));
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.TSRV34
{
	/// <summary>
	/// 
	/// </summary>
	class Response65 : Response
	{
		public List<Data> Data { get; private set; }

		public List<byte> RawData { get; set; }

		public Response65(byte[] data, ConsumptionProperties properties)
			: base(data)
		{
			RawData = new List<byte>(data);

			Data = new List<Common.Data>();

			var length = data[2];
			//скипаем три байта (сетевой адрес, функция и длина)
			int start = 3;

			var seconds = Helper.ToUInt32(data, start + 0);

			//если данные нулевые, игнорим их
			if (seconds == 0) return;

			var date = new DateTime(1970, 1, 1).AddSeconds(seconds);

			var heat1 = Helper.ToUInt32(data, start + 4) / 4.184 * 0.001;
			Data.Add(new Data(Glossary.W4, MeasuringUnitType.Gkal, date, heat1));

			var heat2 = Helper.ToUInt32(data, start + 8) / 4.184 * 0.001;
			Data.Add(new Data(Glossary.W5, MeasuringUnitType.Gkal, date, heat2));

			var heat3 = Helper.ToUInt32(data, start + 12) / 4.184 * 0.001;
			Data.Add(new Data(Glossary.W6, MeasuringUnitType.Gkal, date, heat3));

			var cons1 = Helper.ToUInt32(data, start + 16) * 0.001;
			if (properties.IsMassByChannel1)
				Data.Add(new Data(Glossary.M1, MeasuringUnitType.tonn, date, cons1));
			else
				Data.Add(new Data(Glossary.V1, MeasuringUnitType.m3, date, cons1));

			var cons2 = Helper.ToUInt32(data, start + 20) * 0.001;
			if (properties.IsMassByChannel2)
				Data.Add(new Data(Glossary.M2, MeasuringUnitType.tonn, date, cons2));
			else
				Data.Add(new Data(Glossary.V2, MeasuringUnitType.m3, date, cons2));

			var cons3 = Helper.ToUInt32(data, start + 24) * 0.001;
			if (properties.IsMassByChannel3)
				Data.Add(new Data(Glossary.M3, MeasuringUnitType.tonn, date, cons3));
			else
				Data.Add(new Data(Glossary.V3, MeasuringUnitType.m3, date, cons3));

			var temperature1 = Helper.ToInt16(data, start + 28) * 0.01;
			Data.Add(new Data(Glossary.T1, MeasuringUnitType.C, date, temperature1));

			var temperature2 = Helper.ToInt16(data, start + 30) * 0.01;
			Data.Add(new Data(Glossary.T2, MeasuringUnitType.C, date, temperature2));

			var temperature3 = Helper.ToInt16(data, start + 32) * 0.01;
			Data.Add(new Data(Glossary.T3, MeasuringUnitType.C, date, temperature3));

			var timeWork = Helper.ToUInt32(data, start + 36);
			Data.Add(new Data(Glossary.Tw, MeasuringUnitType.sec, date, timeWork));

			var timeOff = Helper.ToUInt32(data, start + 40);
			Data.Add(new Data(Glossary.Ts, MeasuringUnitType.sec, date, timeOff));
		}
	}
}

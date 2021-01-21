using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.Mercury230
{
	class PowerProfileResponse : Response
	{
		public List<Data> Data { get; private set; }

		public DateTime Date { get; private set; }

        public string text { get; private set; }

        public bool IsEmpty { get; private set; }

        public PowerProfileResponse(byte[] data, byte networkAddress, int A, byte networkaddress)
			: base(data,  networkaddress)
		{
			Data = new List<Data>();

            text = "";

			var start = 0;

			var state = Body[start];
			
            if (state == 0)
            {
                text = "Пустая запись";
                IsEmpty = true;                
            }
            else
            {
                IsEmpty = false;
                var hour = Helper.ToBCD(Body[start + 1]);
                var minute = Helper.ToBCD(Body[start + 2]);
                var day = Helper.ToBCD(Body[start + 3]);
                var month = Helper.ToBCD(Body[start + 4]);
                var year = 2000 + Helper.ToBCD(Body[start + 5]);

                Date = new DateTime(year, month, day, hour, minute, 0);

                if (45 < minute && minute <= 59)
                {
                    Date = Date.AddHours(1);
                    Date = Date.AddMinutes(-Date.Minute);
                }
                else if (0 < minute && minute <= 15)
                {
                    Date = Date.AddMinutes(-Date.Minute);
                }
                else if (15 < minute && minute <= 45)
                {
                    Date = Date.AddMinutes(-Date.Minute);
                    Date = Date.AddMinutes(30);
                }

                var pp = (double)BitConverter.ToInt16(Body, start + 7); pp = pp == -1.0 ? 0.0 : pp;
                var PPlus1 = pp / (double)A;
                var pm = (double)BitConverter.ToInt16(Body, start + 9); pm = pm == -1.0 ? 0.0 : pm;
                var PMinus1 = pm / (double)A;
                var ap = (double)BitConverter.ToInt16(Body, start + 11); ap = ap == -1.0 ? 0.0 : ap;
                var APlus1 = ap / (double)A;
                var am = (double)BitConverter.ToInt16(Body, start + 13); am = am == -1.0 ? 0.0 : am;
                var AMinus1 = am / (double)A;

                text = string.Format("{0:dd.MM.yyyy HH:mm} P+={1:F4} P-={2:F4} A+={3:F4} A-={4:F4}", Date, pp, pm, ap, am);

                //var next = start + 14;
                //var PPlus2 = (double)BitConverter.ToInt16(data, next + 8) / (double)A;
                //var PMinus2 = (double)BitConverter.ToInt16(data, next + 10) / (double)A;
                //var APlus2 = (double)BitConverter.ToInt16(data, next + 12) / (double)A;
                //var AMinus2 = (double)BitConverter.ToInt16(data, next + 14) / (double)A;

                Data.Add(new Data(Glossary.Pp, Matrix.Common.Agreements.MeasuringUnitType.kWt, Date, APlus1));
                Data.Add(new Data(Glossary.Pm, Matrix.Common.Agreements.MeasuringUnitType.kWt, Date, AMinus1));
                Data.Add(new Data(Glossary.Qp, Matrix.Common.Agreements.MeasuringUnitType.kWt, Date, PPlus1));
                Data.Add(new Data(Glossary.Qm, Matrix.Common.Agreements.MeasuringUnitType.kWt, Date, PMinus1));
            }
		}
	}
}

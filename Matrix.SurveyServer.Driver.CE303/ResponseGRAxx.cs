using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.CE303
{
	/// <summary>
	/// 
	/// </summary>
	class ResponseGRAXX : Response
	{
		public List<Data> Data { get; private set; }

        public ResponseGRAXX(string nameParameter,byte[] data, DateTime date, int tAver)
			: base(data)
		{
            int nDiapasone = (int)(60.0/tAver); // число дипазонов внутри часа и он же коэфициент на который надо разделить мощности по диапазонам

            Data = new List<Common.Data>();
            var lData = DriverHelper.responceToParameters(nameParameter, data, 0);

            double summa = 0;

            foreach (var item in lData)
            {
                try
                {
                    summa = summa + System.Double.Parse(item.Replace(".",","));
                }
                catch (Exception)
                {
                    summa = -1;
                }
            }
            switch (nameParameter)
	        {
                   case "GRAPE": nameParameter=  "Мощность активная потребленная";break;
                   case "GRAPI": nameParameter = "Мощность активная отпущенная"; break;
                   case "GRAQE": nameParameter = "Мощность реактивная потребленная"; break;
                   case "GRAQI": nameParameter = "Мощность реактивная отпущенная"; break;
	        }

            Data.Add(new Data(nameParameter, MeasuringUnitType.kWt, date, (float)(summa / nDiapasone)));
 		}
	}
}

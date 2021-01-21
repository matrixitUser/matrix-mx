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
	class ResponseENDxx : Response
	{
		public List<Data> Data { get; private set; }

        public ResponseENDxx(string nameParameter,byte[] data, DateTime date)
			: base(data)
		{

            Data = new List<Common.Data>();
            var lData = DriverHelper.responceToParameters(nameParameter, data, 0);

            double summa = System.Double.Parse(lData[0].Replace(".",","));

            switch (nameParameter)
	        {
                   case "ENDPE": nameParameter=  "Энергия активная потребленная на конец суток";break;
                   case "ENDPI": nameParameter = "Энергия активная отпущенная на конец суток"; break;
                   case "ENDQE": nameParameter = "Энергия реактивная потребленная на конец суток"; break;
                   case "ENDQI": nameParameter = "Энергия реактивная отпущенная на конец суток"; break;
	        }

            Data.Add(new Data(nameParameter, MeasuringUnitType.kWt, date, (float)summa));
 		}
	}
}

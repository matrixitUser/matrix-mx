using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
	{
		public Int16 Kt { get; private set; }
		public Int16 Kn { get; private set; }

        dynamic ParseTransformationResponse(dynamic answer)
		{
            if(!answer.success) return answer;

            answer.Kn = Helper.ToInt16(answer.Body, 0);
            answer.Kt = Helper.ToInt16(answer.Body, 2);
            return answer;
		}
	}
}

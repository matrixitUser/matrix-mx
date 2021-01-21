using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    class RequestConstant : RequestParameters
	{
        public RequestConstant(byte networkAddress, byte ParameterCode)
            : base(networkAddress, ParameterCode, null)
		{

        }

		public override string ToString()
		{
			return string.Format("чтение констант");
		}
	}
}

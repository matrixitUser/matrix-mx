using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
	public class DriverFactory : IDriverFactory
	{
		public IDriver Create(byte networkAddress, byte port, string password, byte channel)
		{
			throw new NotImplementedException();
		}
	}
}

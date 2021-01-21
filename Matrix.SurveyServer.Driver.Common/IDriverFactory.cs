using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
	public interface IDriverFactory
	{
		IDriver Create(byte networkAddress, byte port, string password, byte channel);
	}
}

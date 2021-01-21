using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.VKT7
{
	public class ResponseReadInfo : ResponseRead
	{
		public List<Constant> Constants { get; private set; }

		public string FactoryNumber { get; private set; }
		public int Version { get; private set; }
		public int TotalDay { get; private set; }

		public ResponseReadInfo(byte[] data)
			: base(data)
		{
			Constants = new List<Constant>();
			if (data[2] > 0x03)
			{
				Version = data[0x03];
				TotalDay = data[0x11];
				FactoryNumber = Encoding.ASCII.GetString(data, 8, 8);
				Constants.Add(new Constant(ConstantType.FactoryNumber, FactoryNumber));
			}
			else
			{
				Version = 0x10;
				TotalDay = data[0x03];
			}
		}

		public override string ToString()
		{
			return string.Format("службная информация; заводской номер: {0}", FactoryNumber);
		}
	}
}

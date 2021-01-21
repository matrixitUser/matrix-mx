using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Common.Agreements
{
	class ParameterAttribute : Attribute
	{
		public string Value { get; private set; }
		public ParameterAttribute(string value)
		{
			this.Value = value;
		}
	}
}

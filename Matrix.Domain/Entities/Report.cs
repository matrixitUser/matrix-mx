using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{
	[DataContract]
	public class Report : Entity
	{        
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string Template { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}

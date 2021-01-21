using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Matrix.Domain.Events
{
	/// <summary>
	/// событие - авария
	/// </summary>
	[DataContract]
	public class AlarmEvent : BaseEvent
	{
		
	}
}

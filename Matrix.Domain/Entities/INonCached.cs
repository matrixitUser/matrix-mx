using System;
using System.Runtime.Serialization;

namespace Matrix.Domain.Entities
{	
	/// <summary>
	/// некешируемые сущности
	/// вероятно они хранятся в другой бд
	/// </summary>
	public interface INonCached
	{		
		Guid Id { get; set; }
	}
}

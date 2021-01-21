using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	/// <summary>
	/// описывает имена типов данных в замапленных базах данных
	/// примечание: возможно это уже есть в проваидерах данных, но 
	/// тотальный контроль важнее
	/// </summary>
	public interface ITypes
	{
		string GuidType { get; }
		string StringType { get; }
		string DateTimeType { get; }
		string IntType { get; }
		string DoubleType { get; }
		string BoolType { get; }
		string BytesType { get; }
		string TextType { get; }
	}
}

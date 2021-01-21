using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	/// <summary>
	/// имена типов данных при маппинге на Postgresql 9+
	/// </summary>
	public class PostgresqlTypes : ITypes
	{
		public string StringType
		{
			get { return "varchar"; }
		}

		public string GuidType
		{
			get { return "uuid"; }
		}


		public string DateTimeType
		{
			get { return "timestamp"; }
		}


		public string IntType
		{
			get { return "int4"; }
		}


		public string DoubleType
		{
			get { return "float8"; }
		}


		public string BoolType
		{
			get { return "bool"; }
		}


		public string BytesType
		{
			get { return "bytea"; }
		}

		public string TextType
		{
			get { return "text"; }
		}		
	}
}

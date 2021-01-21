using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	/// <summary>
	/// имена типов данных при маппинге на MS Sql Server 2005+
	/// </summary>
	public static class MssqlTypes
	{
		public static string GuidType
		{
			get { return "uniqueidentifier"; }
		}

        public static string StringType
		{
			get { return "nvarchar"; }
		}

        public static string DateTimeType
		{
			get { return "datetime2"; }
		}

        public static string IntType
		{
			get { return "int"; }
		}

        public static string DoubleType
		{
			get { return "float"; }
		}

        public static string BoolType
		{
			get { return "bit"; }
		}

        public static string BytesType
		{
			get { return "varbinary"; }
		}

        public static string TextType
		{
			get { return "text"; }
		}
	}
}

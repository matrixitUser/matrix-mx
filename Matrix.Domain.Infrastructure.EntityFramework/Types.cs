using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	/// <summary>
	/// синглтон определяющий имена типов для маппинга к бд
	/// todo заюзать какойнибудь DI
	/// </summary>
	public static class Types
	{
		private static ITypes instance = null;
		public static ITypes Instance
		{
			get
			{
				if (instance == null)
				{
					//instance = new MssqlTypes();
					instance = new PostgresqlTypes();
				}
				return instance;
			}
		}
	}
}

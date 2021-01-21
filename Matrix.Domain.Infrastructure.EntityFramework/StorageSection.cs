using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	public class StorageSection : ConfigurationSection
	{
		public const string STORAGE_SECTION = "storage";

		private const string MAPPING = "mapping";
		[ConfigurationProperty(MAPPING, IsRequired = false, DefaultValue = MappingType.MsSql)]
		public MappingType MappingType
		{
			get
			{
				return (MappingType)this[MAPPING];
			}
			set
			{
				this[MAPPING] = value;
			}
		}

		private static Configuration config = null;
		public static StorageSection GetSection()
		{
			if (config == null)
			{
				config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			}

			var section = (StorageSection)config.GetSection(STORAGE_SECTION);
			if (section == null)
			{
				section = new StorageSection();
				config.Sections.Add(STORAGE_SECTION, section);
				Save();
			}
			return section;
		}

		public static void Save()
		{
			if (config == null) return;
			config.Save();
		}
	}
}

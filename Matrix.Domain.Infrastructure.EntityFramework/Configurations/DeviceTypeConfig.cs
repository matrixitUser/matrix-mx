using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	class DeviceTypeConfig : EntityTypeConfiguration<DeviceType>
	{
		public DeviceTypeConfig()
		{
			ToTable("DeviceType");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.DisplayName).HasColumnName("DisplayName").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Driver).HasColumnName("Driver").HasColumnType(MssqlTypes.BytesType).IsMaxLength();
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}
	}
}

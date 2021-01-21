using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;
using System.Data.Entity.ModelConfiguration;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	public class TagConfig : EntityTypeConfiguration<Tag>
	{
		public TagConfig()
		{
			ToTable("Tag");
			HasKey(t => t.Id);
            Property(t => t.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(t => t.TaggedId).HasColumnName("TaggedId").HasColumnType(MssqlTypes.GuidType);
            Property(t => t.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(t => t.Value).HasColumnName("Value").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(t => t.IsSpecial).HasColumnName("IsSpecial").HasColumnType(MssqlTypes.BoolType);			
		}
	}
}

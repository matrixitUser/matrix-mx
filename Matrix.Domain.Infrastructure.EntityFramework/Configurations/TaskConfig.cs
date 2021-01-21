using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	class TaskConfig : EntityTypeConfiguration<Task>
	{
		public TaskConfig()
		{
			ToTable("Task");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Cron).HasColumnName("Cron").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Type).HasColumnName("Type").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.IsEnabled).HasColumnName("IsEnabled").HasColumnType(MssqlTypes.BoolType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}

	}
}

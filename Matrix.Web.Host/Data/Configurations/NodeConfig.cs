using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    class NodeConfig : EntityTypeConfiguration<Node>
    {
        public NodeConfig()
        {
            ToTable("Node");
            HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Type).HasColumnName("Type").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
        }
    }
}

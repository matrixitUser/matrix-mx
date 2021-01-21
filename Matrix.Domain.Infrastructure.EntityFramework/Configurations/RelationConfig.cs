using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    class RelationConfig : EntityTypeConfiguration<Relation>
    {
        public RelationConfig()
        {
            ToTable("Relation");
            HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.StartNodeId).HasColumnName("StartNodeId").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.EndNodeId).HasColumnName("EndNodeId").HasColumnType(MssqlTypes.GuidType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
        }
    }
}

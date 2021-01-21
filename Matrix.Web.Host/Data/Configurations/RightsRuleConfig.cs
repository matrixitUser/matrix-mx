using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    class RightsRuleConfig : EntityTypeConfiguration<RightsRule>
    {
        public RightsRuleConfig()
        {
            ToTable("RightsRule");
            HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.GroupId).HasColumnName("GroupId").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.ObjectId).HasColumnName("ObjectId").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.RelyId).HasColumnName("RelyId").HasColumnType(MssqlTypes.GuidType).IsOptional();
        }
    }
}

using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    public class GroupConfig : EntityTypeConfiguration<Group>
    {
		public GroupConfig()
        {
            ToTable("Group");
            HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.ParentId).HasColumnName("ParentId").HasColumnType(MssqlTypes.GuidType).IsOptional();
            Property(s => s.Code).HasColumnName("Code").HasColumnType(MssqlTypes.StringType).IsRequired();            
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
        }
    }
}

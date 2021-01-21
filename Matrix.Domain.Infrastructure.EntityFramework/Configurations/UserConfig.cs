using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    public class UserConfig : EntityTypeConfiguration<User>
    {
        public UserConfig()
        {
            ToTable("User");
            HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Login).HasColumnName("Login").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Patronymic).HasColumnName("Patronymic").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Surname).HasColumnName("Surname").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.GroupId).HasColumnName("GroupId").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Password).HasColumnName("Password").HasColumnType(MssqlTypes.StringType).IsMaxLength();
            Property(s => s.IsDomainUser).HasColumnName("IsDomainUser").HasColumnType(MssqlTypes.BoolType);
            Property(s => s.Password).HasColumnName("Password").HasColumnType(MssqlTypes.StringType).IsMaxLength().IsOptional();
            Property(s => s.IsAdmin).HasColumnName("IsAdmin").HasColumnType(MssqlTypes.BoolType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
        }
    }
}

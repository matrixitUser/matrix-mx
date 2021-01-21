using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	class GsmModemConfig : EntityTypeConfiguration<GsmModem>
	{
		public GsmModemConfig()
		{
			ToTable("GsmModem");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.ComPort).HasColumnName("ComPort").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.BaudRate).HasColumnName("BaudRate").HasColumnType(MssqlTypes.IntType);
            Property(s => s.CsdPortId).HasColumnName("CsdPortId").HasColumnType(MssqlTypes.GuidType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}
	}
}

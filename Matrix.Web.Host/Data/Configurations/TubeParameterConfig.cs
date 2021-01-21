using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	class TubeParameterConfig : EntityTypeConfiguration<TubeParameter>
	{
		public TubeParameterConfig()
		{
			ToTable("TubeParameter");
			HasKey(s => s.Id);
			Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.TubeId).HasColumnName("TubeId").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.SystemParameterId).HasColumnName("SystemParameterId").HasColumnType(MssqlTypes.StringType).HasMaxLength(50);
            Property(s => s.CalculationTypeId).HasColumnName("CalculationTypeId").HasColumnType(MssqlTypes.StringType).HasMaxLength(50);

            Property(s => s.MeasuringUnitId).HasColumnName("MeasuringUnitId").HasColumnType(MssqlTypes.StringType).HasMaxLength(50);

            Property(s => s.IsVirtual).HasColumnName("IsVirtual").HasColumnType(MssqlTypes.BoolType);

            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}
	}
}

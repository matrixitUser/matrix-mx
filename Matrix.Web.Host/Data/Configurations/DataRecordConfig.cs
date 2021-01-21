using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
    class DataRecordConfig : EntityTypeConfiguration<DataRecord>
	{
		public DataRecordConfig()
		{
			ToTable("DataRecordView");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(d => d.ObjectId).HasColumnName("ObjectId").HasColumnType(MssqlTypes.GuidType);
			Property(d => d.Type).HasColumnName("Type").HasColumnType(MssqlTypes.StringType);
			Property(d => d.Date).HasColumnName("Date").HasColumnType(MssqlTypes.DateTimeType);

			Property(d => d.D1).HasColumnName("D1").HasColumnType(MssqlTypes.DoubleType).IsOptional();
			Property(d => d.D2).HasColumnName("D2").HasColumnType(MssqlTypes.DoubleType).IsOptional();
			Property(d => d.D3).HasColumnName("D3").HasColumnType(MssqlTypes.DoubleType).IsOptional();

			Property(d => d.I1).HasColumnName("I1").HasColumnType(MssqlTypes.IntType).IsOptional();
			Property(d => d.I2).HasColumnName("I2").HasColumnType(MssqlTypes.IntType).IsOptional();
			Property(d => d.I3).HasColumnName("I3").HasColumnType(MssqlTypes.IntType).IsOptional();

			Property(d => d.S1).HasColumnName("S1").HasColumnType(MssqlTypes.StringType).IsOptional().IsMaxLength();
			Property(d => d.S2).HasColumnName("S2").HasColumnType(MssqlTypes.StringType).IsOptional().IsMaxLength();
			Property(d => d.S3).HasColumnName("S3").HasColumnType(MssqlTypes.StringType).IsOptional().IsMaxLength();

			Property(d => d.Dt1).HasColumnName("Dt1").HasColumnType(MssqlTypes.DateTimeType).IsOptional();
			Property(d => d.Dt2).HasColumnName("Dt2").HasColumnType(MssqlTypes.DateTimeType).IsOptional();
			Property(d => d.Dt3).HasColumnName("Dt3").HasColumnType(MssqlTypes.DateTimeType).IsOptional();

			Property(d => d.G1).HasColumnName("G1").HasColumnType(MssqlTypes.GuidType).IsOptional();
			Property(d => d.G2).HasColumnName("G2").HasColumnType(MssqlTypes.GuidType).IsOptional();
			Property(d => d.G3).HasColumnName("G3").HasColumnType(MssqlTypes.GuidType).IsOptional();
		}
	}
}

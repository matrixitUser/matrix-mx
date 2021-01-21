using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	public class ReportConfig : EntityTypeConfiguration<Report>
	{
		public ReportConfig()
		{
			ToTable("Report");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType).HasMaxLength(255);
            Property(s => s.Template).HasColumnName("Template").HasColumnType(MssqlTypes.TextType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}
	}
}

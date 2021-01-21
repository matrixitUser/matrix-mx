using System.Data.Entity.ModelConfiguration;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Configurations
{
	public class Maquette80020Config : EntityTypeConfiguration<Maquette80020>
	{
		public Maquette80020Config()
		{
			ToTable("Maquette80020");
			HasKey(s => s.Id);
            Property(s => s.Id).HasColumnName("Id").HasColumnType(MssqlTypes.GuidType);
            Property(s => s.Name).HasColumnName("Name").HasColumnType(MssqlTypes.StringType);
            HasMany(s => s.Tags).WithOptional().HasForeignKey(t => t.TaggedId);
		}
	}
}

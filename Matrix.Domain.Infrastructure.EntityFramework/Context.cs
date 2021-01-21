using System.Data.Entity;
using Matrix.Domain.Infrastructure.EntityFramework.Configurations;
using System.Collections.Generic;


namespace Matrix.Domain.Infrastructure.EntityFramework
{
    public class Context : DbContext
    {
        private ITypes types;

        /// <summary>
        /// настройка параметров контекста
        /// </summary>
        private void Initialize(ITypes types)
        {
            this.types = types;
            Database.SetInitializer<Context>(null);
            this.Configuration.ProxyCreationEnabled = false;

            //отключаем ленивую загрузку, для принудительной загрузки тегов
            this.Configuration.LazyLoadingEnabled = false;
            //this.Configuration.AutoDetectChangesEnabled = false;
        }

        public Context(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            Database.SetInitializer<Context>(null);
            this.Configuration.ProxyCreationEnabled = false;

            //отключаем ленивую загрузку, для принудительной загрузки тегов
            this.Configuration.LazyLoadingEnabled = false;
            //this.Configuration.AutoDetectChangesEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new TubeParameterConfig());
            modelBuilder.Configurations.Add(new DeviceTypeConfig());
            modelBuilder.Configurations.Add(new UserConfig());
            modelBuilder.Configurations.Add(new GroupConfig());

            modelBuilder.Configurations.Add(new TagConfig());
            modelBuilder.Configurations.Add(new GsmModemConfig());
            modelBuilder.Configurations.Add(new TaskConfig());
            modelBuilder.Configurations.Add(new ReportConfig());
            modelBuilder.Configurations.Add(new Maquette80020Config());
            modelBuilder.Configurations.Add(new DataRecordConfig());
            modelBuilder.Configurations.Add(new NodeConfig());
            modelBuilder.Configurations.Add(new RelationConfig());
            modelBuilder.Configurations.Add(new RightsRuleConfig());
        }
    }
}

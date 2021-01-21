using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Matrix.ExportData
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(host =>
            {
                host.Service<Service>(service =>
                {
                    service.ConstructUsing(() => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                host.RunAsLocalSystem();
                host.SetDisplayName("Матрикс.Экспорт");
                host.SetDescription("Экспорт данных во внешние системы");
                host.SetServiceName("Matrix.ExportData");
            });
        }
    }
}

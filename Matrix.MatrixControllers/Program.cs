using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace Matrix.MatrixControllers
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(host =>
            {
                host.Service<Service>(svc =>
                {
                    svc.ConstructUsing(() => new Service());
                    svc.WhenStarted(s => s.Start());
                    svc.WhenStopped(s=>s.Stop());
                });
                host.RunAsLocalSystem();
                host.SetDescription("Работает с контроллерами Матрикс (принимает соединения, держит соединения открытыми, обеспечивает опрос приборов через контроллеры матрикс");
                host.SetDisplayName("Матрикс.КонтроллерыМатрикс");
                host.SetServiceName("Matrix.MatrixControllers");
            });
        }
    }
}

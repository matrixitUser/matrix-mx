using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Topshelf;

namespace Matrix.LogManager
{
    class Program
    {
        static void Main(string[] args)
        {
            var name = ConfigurationManager.AppSettings["service-name"];
            HostFactory.Run(host =>
            {
                host.Service<Service>(service =>
                {
                    service.ConstructUsing(() => new Service());
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                host.RunAsLocalSystem();
                host.SetDisplayName(string.Format("Матрикс.МенеджерЛогов.{0}", name));
                host.SetDescription("Управление логами");
                host.SetServiceName(string.Format("Matrix.LogManager.{0}", name));
            });
        }
    }
}

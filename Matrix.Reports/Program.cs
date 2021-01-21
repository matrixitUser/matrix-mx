using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Topshelf;

namespace Matrix.Reports
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceName = ConfigurationManager.AppSettings["service-name"].Replace(" ", "");
            HostFactory.Run(host =>
            {
                host.Service<Service>(svc =>
                {
                    svc.ConstructUsing(() => new Service());
                    svc.WhenStarted(s => s.Start());
                    svc.WhenStopped(s => s.Stop());
                });
                host.RunAsLocalSystem();
                host.SetDescription("Строит отчеты");
                host.SetDisplayName(string.Format("Матрикс.Отчеты.{0}", serviceName));
                host.SetServiceName(string.Format("Matrix.Reports.{0}", serviceName));
            });
        }
    }
}

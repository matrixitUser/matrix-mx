using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Topshelf;

namespace Matrix.Scheduler
{
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            HostFactory.Run(h =>
            {
                h.UseLinuxIfAvailable();
                h.Service<Service>(s =>
                {
                    s.ConstructUsing(name => new Service());
                    s.WhenStarted(os => os.Start());
                    s.WhenStopped(os => os.Stop());
                });
                h.RunAsLocalSystem();
                h.SetDescription("Микросервис, планировщик задач на опрос");
                h.SetDisplayName("Матрикс.Планировщик");
                h.SetServiceName("Matrix.Scheduler");
            });   
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using NLog;
using Owin;

namespace Matrix.Spotter
{
    class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable webHost;
        private Bus bus;

        public void Start()
        {
            var uc = new UnityContainer();

            var om = new ObjectManager();
            uc.RegisterInstance(om);

            bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                logger.Debug("поступило сообщение {0}", ea.Message.head.what);
            };
            bus.Start();
            uc.RegisterInstance(bus);

            var url = ConfigurationManager.AppSettings["url"];
            webHost = WebApp.Start(url, app =>
            {
                app.UseNancy(opt => opt.Bootstrapper = new Bootstrapper(uc));
            });
            logger.Info("сервис запущен, url: {0}", url);
        }

        public void Stop()
        {
            bus.Stop();
            webHost.Dispose();
            logger.Info("сервис остановлен");
        }
    }
}

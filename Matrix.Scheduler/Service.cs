using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.AspNet.SignalR;
using NLog;
using Owin;

namespace Matrix.Scheduler
{
    class Service
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private IDisposable nancyHost;
        Bus bus;

        public void Start()
        {
            var uc = new UnityContainer();

            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(uc));

            bus = new Bus();
            bus.Start();
            uc.RegisterInstance(bus);

            var tm = new TaskManager();
            tm.Start();
            uc.RegisterInstance(tm);

            var bootstrap = new NancyBootstrapper(uc);
            var url = ConfigurationManager.AppSettings["url"];
            nancyHost = WebApp.Start(url, app =>
            {
                app.UseNancy(config => config.Bootstrapper = bootstrap).
                    UseCors(CorsOptions.AllowAll).
                    MapSignalR();
            });

            logger.Info("сервис запущен");
        }

        public void Stop()
        {
            nancyHost.Dispose();
            bus.Stop();
            logger.Info("сервис остановлен");
        }
    }

}

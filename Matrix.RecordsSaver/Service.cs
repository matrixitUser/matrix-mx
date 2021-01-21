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
using System.Timers;

namespace Matrix.RecordsSaver
{
    public class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable webHost;
        private Bus bus;
        private Timer timer;

        public void Start()
        {
            var uc = new UnityContainer();
            var saver = new Saver();
            bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                var records = ea.Message.body.records;
                saver.Push(records);
            };
            bus.Start();

            int interval = int.Parse(ConfigurationManager.AppSettings["save-interval-seconds"]);
            timer = new Timer();
            timer.Interval = interval * 1000;
            timer.Elapsed += (se, ea) =>
            {
                saver.Save();
            };
            timer.Start();

            uc.RegisterInstance(bus);
            uc.RegisterInstance(saver);

            var url = ConfigurationManager.AppSettings["url"];
            webHost = WebApp.Start(url, app =>
            {
                app.UseNancy(opt => opt.Bootstrapper = new Bootstrapper(uc));
            });
            logger.Info("сервис запущен, url: {0}", url);
        }

        public void Stop()
        {
            timer.Stop();
            bus.Stop();
            webHost.Dispose();
            logger.Info("сервис остановлен");
        }
    }
}

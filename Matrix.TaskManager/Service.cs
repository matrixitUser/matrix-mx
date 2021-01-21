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

namespace Matrix.TaskManager
{
    class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable webHost;

        private Bus bus;

        public void Start()
        {
            var uc = new UnityContainer();

            var tm = new TaskManager();

            bus = new Bus();
            bus.Start();

            bus.OnMessageReceived += (se, ea) =>
            {
                if (ea.Message.head.what == "poll-reject")
                {
                    logger.Debug("опрос на порту провалился: {0}", ea.Message.body.reason);
                    Guid target = Guid.Parse(ea.Message.body.targetId);
                    tm.RouteReject(target);
                }

                if (ea.Message.head.what == "poll-finish")
                {
                    logger.Debug("опрос на порту завершился");
                    tm.CloseTask(Guid.Parse(ea.Message.body.targetId));                    
                }               
            };

            bus.OnMessageRollback += (se, ea) => {
                logger.Debug("сообщение откатилось...");
                Guid target = Guid.Parse(ea.Message.body.targetId);
                tm.RouteReject(target);
            };

            uc.RegisterInstance(bus);
            uc.BuildUp(tm);
            uc.RegisterInstance(tm);

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

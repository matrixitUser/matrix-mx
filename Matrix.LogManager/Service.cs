using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using NLog;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.LogManager
{
    class Service
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private IDisposable webHost;
        private Bus bus;
        private ParameterAnalizer parameterAnalizer;

        public void Start()
        {
            var uc = new UnityContainer();

            var log = new LogAnalizer();
            parameterAnalizer = new ParameterAnalizer(); 

            bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                var records = ea.Message.body.records.ToArray();
                try
                {
                    log.Analize(records);
                }
                catch (Exception ex)
                {
                    logger.Error("не обработались логером");
                }    
				
                try
                {
                    parameterAnalizer.Analize(records);
                }
                catch (Exception ex)
                {
                    logger.Error("не обработались параметры");
                }                
            };

            bus.OnSubscribeMessageReceived += (se, ea) =>
            {

                Guid sessionId = Guid.Parse(ea.Message.body.sessionId);
                var ids = new List<Guid>();
                foreach (var id in ea.Message.body.ids)
                {
                    ids.Add(Guid.Parse(id));
                }
                log.Subscribe(sessionId, ids);
            };

            bus.Start();
            uc.RegisterInstance(bus);
            uc.BuildUp(log);
            uc.RegisterInstance(log);

            uc.BuildUp(parameterAnalizer);
            uc.RegisterInstance(parameterAnalizer);
			
			var url = ConfigurationManager.AppSettings["service-url"];
            webHost = WebApp.Start(url, app =>
            {
                app.UseNancy(opt => opt.Bootstrapper = new Bootstrapper(uc));
            });
            logger.Info("сервис запущен, url: {0}", url);
        }

        public void Stop()
        {
            bus.Stop();
            parameterAnalizer.Dispose();
            webHost.Dispose();
            logger.Info("сервис остановлен");
        }
    }
}

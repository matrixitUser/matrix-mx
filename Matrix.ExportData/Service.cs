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

namespace Matrix.ExportData
{
    public class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable webHost;
        private Bus bus;

        public void Start()
        {
            var uc = new UnityContainer();

            var pvt = new Pivoter();

            var om = new ObjectManager();

            bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                logger.Debug("поступило сообщение {0}", ea.Message.head.what);
                if(ea.Message.head.what=="export-data")
                {
                    DateTime start = ea.Message.body.start;
                    DateTime end = ea.Message.body.end;

                    string type = ea.Message.body.type;

                    var ids = (ea.Message.body.ids as IEnumerable<object>).Select(s => Guid.Parse(s.ToString())).ToArray();

                    var rcs = pvt.Pivot(ids, start, end, type);
                    //pvt.Pivot()
                    var ans = bus.MakeMessageStub("export");
                    ans.head.id = ea.Message.head.id;
                    ans.body.data = rcs;

                    bus.SendToSession(ans);
                }
                if(ea.Message.head.what=="export-names")
                {
                    var ans = bus.MakeMessageStub("export");
                    ans.head.id = ea.Message.head.id;
                    ans.body.data = om.GetObjects();

                    bus.SendToSession(ans);
                }
            };
            bus.Start();

            uc.RegisterInstance(pvt);
            uc.RegisterInstance(om);
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

//#define NEWREPORTS

#if (NEWREPORTS)
using System.Linq;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using System.Web.Http;
using Owin;
using System.Configuration;
using System;
using log4net.Config;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using System.Threading;
using Microsoft.Practices.Unity;
using Microsoft.Practices.ServiceLocation;
using NLog;
using Matrix.Web.Host.Common;
using System.Collections.Generic;
using System.Dynamic;

namespace Matrix.Web.Host
{
    class Service
    {
        const int MIN_THREAD_COUNT = 500;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable host;
        private Bus bus;

        public void Start()
        {
            XmlConfigurator.Configure();

            var uc = new UnityContainer();
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(uc));
            //временное решение (todo испльзовать IUnityContainer+NancyFX)

            //var sm = new SessionManager();
            //uc.RegisterInstance(sm);

            bus = new Bus();
            bus.Start();

            bus.OnMessageReceived += (se, ea) =>
            {
                //1. filter
                //2. signalR notifications?
                logger.Debug("получено сообщение из шины");
            };

            bus.OnHandlerRegister += (se, ea) =>
            {
                string what = ea.Message.body.what;
                bool adminOnly = ea.Message.body.adminOnly;
                string exchange = ea.Message.body.exchange;


            };

            bus.OnNotifyMessageReceived += (se, ea) =>
            {
                try
                {
                    if (ea.Message.head.what.StartsWith("mailer"))
                    {
                        logger.Debug(string.Format("получена задача {0} для {1} объектов", ea.Message.head.what, ea.Message.body.objectIds.Count));
                        var ids = new List<Guid>();
                        foreach (dynamic id in ea.Message.body.objectIds)
                        {
                            Guid nodeId = Guid.Empty;
                            if (Guid.TryParse((string)id, out nodeId))
                            {
                                ids.Add(nodeId);
                            }
                        }

                        Sender.Instance.SendMail(ids);

                        //var nodes = NodeManager.Instance.GetByIds(ids);

                        //var priority = PollTask.PRIORITY_USER;

                        //var darg = ea.Message.body.arg as IDictionary<string, object>;
                        //if (darg.ContainsKey("auto") && ea.Message.body.arg.auto == true)
                        //    priority = PollTask.PRIORITY_AUTO;

                        //PollTaskManager.Instance.CreateTasks(ea.Message.body.what, nodes, ea.Message.body.arg, priority);

                    }
                    else if (ea.Message.head.what.StartsWith("maquette"))
                    {
                        logger.Debug(string.Format("получена задача {0}", ea.Message.head.what));//для {1} объектов, ea.Message.body.objectIds.Count));
                        //var ids = new List<Guid>();
                        //foreach (dynamic id in ea.Message.body.objectIds)
                        //{
                        //    Guid nodeId = Guid.Empty;
                        //    if (Guid.TryParse((string)id, out nodeId))
                        //    {
                        //        ids.Add(nodeId);
                        //    }
                        //}

                        Sender.Instance.SendMaquette();

                        //var nodes = NodeManager.Instance.GetByIds(ids);

                        //var priority = PollTask.PRIORITY_USER;

                        //var darg = ea.Message.body.arg as IDictionary<string, object>;
                        //if (darg.ContainsKey("auto") && ea.Message.body.arg.auto == true)
                        //    priority = PollTask.PRIORITY_AUTO;

                        //PollTaskManager.Instance.CreateTasks(ea.Message.body.what, nodes, ea.Message.body.arg, priority);
                    }
                    else
                    {
                        Guid sessionId = Guid.Parse(ea.Message.body.sessionId);
                        logger.Debug("уведомление для сессии {0} сообщений {1}", sessionId, ea.Message.body.messages.Count());
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "уведомление сломало");
                }
                //});
            };

            uc.RegisterInstance(bus);

            //настройка ограничений по потокам
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(MIN_THREAD_COUNT, completionPortThreads);

            //старт кэша полноты данных
            try
            {
                FulnessCache.Instance.Start();
            }
            catch(Exception e)
            {

            }

            HandlerManager.Instance.Register(new AuthHandler());
            //HandlerManager.Instance.Register(new ObjectsHandler());
            HandlerManager.Instance.Register(new SignalHandler());
            HandlerManager.Instance.Register(new PollHandler());
            HandlerManager.Instance.Register(new HelperHandler());
            HandlerManager.Instance.Register(new FolderHandler());
            HandlerManager.Instance.Register(new DriverHandler());
            HandlerManager.Instance.Register(new MaquetteHandler());
            HandlerManager.Instance.Register(new MailerHandler());
            HandlerManager.Instance.Register(new TaskHandler());
            HandlerManager.Instance.Register(new SetpointHandler());
            HandlerManager.Instance.Register(new CacheHandler());
            var rh = new RecordsHandler();
            HandlerManager.Instance.Register(rh);

            //фоновый пост-обработчик записей

            //rh.AddHandler(new LogRecordsHandler());
            //rh.AddHandler(new ParametersHandler());
            //rh.AddHandler(new LastRecordCacheHandler());            

            HandlerManager.Instance.Register(new LogHandler(bus));
            HandlerManager.Instance.Register(new ModemsHandler());
            ReportHandler.Init(bus);
            HandlerManager.Instance.Register(ReportHandler.Instance);
            //HandlerManager.Instance.Register(new ReportHandler());
            HandlerManager.Instance.Register(new UsersHandler());
            HandlerManager.Instance.Register(new RowsHandler());
            HandlerManager.Instance.Register(new ManagmentHandler());
            HandlerManager.Instance.Register(new NodesHandler());
            HandlerManager.Instance.Register(new ParameterHandler());
            HandlerManager.Instance.Register(new ExportHandler());
            HandlerManager.Instance.Register(new EditHandler());

            HandlerManager.Instance.Register(new BusHandler(bus));

            //RecordsBackgroundProccessor.Instance.ReSet();

            //Sender.Instance.Load();

            //2. запуск веб-сервера
            var binding = ConfigurationManager.AppSettings["binding"];
            var root = ConfigurationManager.AppSettings["root-folder"];
            host = WebApp.Start(url: binding, startup: b =>
            {
                Newtonsoft.Json.JsonConvert.DefaultSettings = () =>
                new JsonSerializerSettings()
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc
                };

                var fso = new FileServerOptions
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = new PhysicalFileSystem(root)
                };
                fso.StaticFileOptions.ServeUnknownFileTypes = true;
                b.UseFileServer(fso);

                var config = new HttpConfiguration();
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );
                var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
                config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

                b.UseWebApi(config);

                b.MapSignalR<SignalRConnection>("/messageacceptor", new HubConfiguration());
            });

            logger.Info("сервер запущен (привязка {0}, корневая папка {1})", binding, root);


            var cmd = "";
            while ((cmd = Console.ReadLine()) != "exit")
            {
                switch (cmd)
                {
                    case "dump":
                        break;
                }
                logger.Info("для выхода введите exit");
            }
            CacheRepository.Instance.Dispose();
            bus.Stop();
        }

        public void Stop()
        {
            CacheRepository.Instance.Dispose();
            bus.Stop();
        }
    }
}
#endif
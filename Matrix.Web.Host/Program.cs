//#define NEWREPORTS

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
using System.Text;

#if (NEWREPORTS)
using Topshelf;
#endif

namespace Matrix.Web.Host
{
#if (NEWREPORTS)
    class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            try
            {
                var name = ConfigurationManager.AppSettings["service-name"].Replace(" ", "").Replace("/", "").Replace("\\", "");
                HostFactory.Run(host =>
                {
                    host.Service<Service>(svc =>
                    {
                        svc.ConstructUsing(() => new Service());
                        svc.WhenStarted(s => s.Start());
                        svc.WhenStopped(s => s.Stop());
                    });
                    host.RunAsLocalSystem();
                    host.SetDescription("Веб-клиент");
                    host.SetDisplayName(string.Format("Матрикс.ВебКлиент.{0}", name));
                    host.SetServiceName(string.Format("Matrix.Web.{0}", name));
                });
            }
            catch (Exception ex)
            {

            }
        }
    }

#else
    /// <summary>
    /// Программа.
    /// </summary>
    class Program
    {
        const int MIN_THREAD_COUNT = 500;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable host;
        //private Bus bus;
    
        /// <summary>
        /// Точка входа.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var uc = new UnityContainer();
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(uc));
            //временное решение (todo испльзовать IUnityContainer+NancyFX)
            //var sm = new SessionManager();
            //uc.RegisterInstance(sm);

            #region RabbitMQ
            var bus = new Bus();
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
                    }
                    else if (ea.Message.head.what.StartsWith("maquette"))
                    {
                        logger.Debug(string.Format("получена задача {0}", ea.Message.head.what));
                        Sender.Instance.SendMaquette();
                    }
                    else
                    {
                        var dicBody = (IDictionary<string, object>)ea.Message.body;
                        if (dicBody.ContainsKey("sessionId"))
                        {
                            Guid sessionId = Guid.Parse(ea.Message.body.sessionId);
                            logger.Debug("уведомление для сессии {0} сообщений {1}", sessionId, ea.Message.body.messages.Count());
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "уведомление сломало");
                }
                //});
            };

            uc.RegisterInstance(bus);
            #endregion

            //настройка ограничений по потокам
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(MIN_THREAD_COUNT, completionPortThreads);

            #region Обработчики API
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
            HandlerManager.Instance.Register(new CalculatorHandler());
            var rh = new RecordsHandler();
            HandlerManager.Instance.Register(rh);

            //фоновый пост-обработчик записей

            //rh.AddHandler(new LogRecordsHandler());
            //rh.AddHandler(new ParametersHandler());
            //rh.AddHandler(new LastRecordCacheHandler());           

            HandlerManager.Instance.Register(new LogHandler(bus));
            HandlerManager.Instance.Register(new ModemsHandler());
            HandlerManager.Instance.Register(new ReportHandler());
            HandlerManager.Instance.Register(new UsersHandler());
            HandlerManager.Instance.Register(new RowsHandler());
            HandlerManager.Instance.Register(new ManagmentHandler());
            HandlerManager.Instance.Register(new NodesHandler());
            HandlerManager.Instance.Register(new ParseHandler());
            HandlerManager.Instance.Register(new ParameterHandler());
            HandlerManager.Instance.Register(new ExportHandler());
            HandlerManager.Instance.Register(EditHandler.Instance()); 

            HandlerManager.Instance.Register(new BusHandler(bus));

            //RecordsBackgroundProccessor.Instance.ReSet();
            #endregion

            Watchdog.Instance();
            AbnormalsCache.Instance.Load();
            //Sender.Instance.Load();

            #region Web API
            //2. запуск веб-сервера
            var binding = ConfigurationManager.AppSettings["binding"];
            var root = ConfigurationManager.AppSettings["root-folder"];
            WebApp.Start(url: binding, startup: b =>
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
                config.EnableCors();
                //config.Filters.Add(new CustomRequireHttpsAttribute());
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
            #endregion

            #region Команды console, ожидание exit
            var cmd = "";
            while ((cmd = Console.ReadLine()) != "exit")
            {
                switch (cmd)
                {
                    case "session":
                        {
                            try
                            {
                                IEnumerable<dynamic> sessions = CacheRepository.Instance.GetSessions();
                                var sessionList = sessions.Select(s =>
                                {
                                    dynamic u = (s as IDictionary<string, object>).ContainsKey("user") ? s.user : null;
                                    var user = new
                                    {
                                        Id = (u != null) ? (Guid?)Guid.Parse(u.id.ToString()) : null,
                                        Login = (u != null) && (u as IDictionary<string, object>).ContainsKey("login") ? (string)u.login : null,
                                        Name = (u != null) && (u as IDictionary<string, object>).ContainsKey("name") ? (string)u.name : null
                                    };
                                    IEnumerable<dynamic> ls = (s as IDictionary<string, object>).ContainsKey("logSubscriber") ? s.logSubscriber : null;
                                    var logSubscriber = ls?.Select(t => new { TubeId = t.tubeId, Neighbours = t.neighbours as IEnumerable<Guid> });
                                    dynamic bag = (s as IDictionary<string, object>).ContainsKey("bag") ? s.bag : null;
                                    Guid? signalConnectionId = (s as IDictionary<string, object>).ContainsKey("signalConnectionId") ? (Guid?)Guid.Parse(s.signalConnectionId.ToString()) : null;
                                    return new
                                    {
                                        Id = (Guid)Guid.Parse(s.id.ToString()),
                                        Date = s.date as DateTime?,
                                        UserId = (Guid)Guid.Parse(s.userId.ToString()),
                                        Bag = bag,
                                        LogSubscriber = logSubscriber,
                                        User = user,
                                        SignalConnectionId = signalConnectionId
                                    };
                                });

                                //

                                StringBuilder text = new StringBuilder();
                                text.AppendLine($"Sessions ({sessionList.Count()}):");
                                foreach (var s in sessionList)
                                {
                                    text.AppendLine($"Id = {s.Id}, UserId = {s.UserId}, Date = {(s.Date?.ToString() ?? "NULL")},");
                                    text.AppendLine($"User = {{ Id = {(s.User.Id?.ToString() ?? "NULL")}, Login = {(s.User.Login ?? "NULL")}, Name = {(s.User.Name ?? "NULL")}}}");
                                    text.AppendLine("LogSubscriber = [");
                                    if (s.LogSubscriber != null)
                                    {
                                        foreach (var ls in s.LogSubscriber)
                                        {
                                            text.AppendLine($" TubeId = {(ls?.TubeId.ToString() ?? "NULL")}, Neighbours = [{String.Join(", ", ls.Neighbours?.Select(n => n.ToString()) ?? new[] { "NULL" })}]");
                                        }
                                    }
                                    text.AppendLine("]").AppendLine("Bag = ");
                                    if (s.Bag != null)
                                    {
                                        foreach (var key in (s.Bag as IDictionary<string, object>).Keys)
                                        {
                                            try
                                            {
                                                text.AppendLine($" {key} = {s.Bag[key].ToString()}");
                                            }
                                            catch (Exception ex)
                                            {
                                                text.AppendLine($" {key} = ??? ({ex.Message})");
                                            }
                                        }
                                    }
                                    text.AppendLine($"SignalConnectionId = {s.SignalConnectionId.ToString() ?? "<NULL>"}");
                                    text.AppendLine("").AppendLine("");
                                }
                                string fileName = string.Format("sessions_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
                                System.IO.File.WriteAllText(fileName, text.ToString());
                                System.Diagnostics.Process.Start(fileName);
                            }
                            catch (Exception ex)
                            {
                                logger.Info($"при выводе списка сессий произошла ошибка: {ex.Message}");
                            }
                            break;
                        }

                    case "dump":
                        break;

                    default:
                        logger.Info("для выхода введите exit");
                        break;
                }
            }
            #endregion
            #region Освобождение ресурсов
            CacheRepository.Instance.Dispose();
            bus.Stop();
            #endregion
        }
    }

#endif
}

using log4net.Config;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using System.Net.Http.Formatting;
using Owin;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Matrix.SignalingServer.Fill;
using System.Configuration;
using NLog;
using Matrix.SignalingServer.Handlers;
using Newtonsoft.Json;
using System.Web.Http;

namespace Matrix.SignalingServer
{
    class Program
    {
        const int MIN_THREAD_COUNT = 200;
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //Старт программы
        static void Main(string[] args)
        {
            //конфигурация логгера
            XmlConfigurator.Configure();

            //настройка ограничений по потокам
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(MIN_THREAD_COUNT, completionPortThreads);
            
            HandlerManager.Instance.Register(new SignalHandler());
            
            #region API - соединение с Api
            var connector = UnityManager.Instance.Resolve<IConnector>();
            while (!connector.Relogin())
            {
                logger.Warn(string.Format("не удалось соединиться с сервером, повтор через 5 сек."));
                Thread.Sleep(5000);
            }
            connector.Subscribe();
            var autoEvent = new AutoResetEvent(false);
            ApiConnector.Instance.Initial();
            //Timer timer = new Timer(Calc.Instance.Calculate, autoEvent, 0, 1000 * 60 * 60 * 1);
            
            #endregion
            #region RabbitMQ - соединение с шиной
            var bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (ea.Message.head.what.StartsWith("signaling"))
                        {
                            if(ea.Message.body.what == "scheduler")
                            {
                                logger.Debug(string.Format("пришел пинг от scheduler", ea.Message.head.what));
                                countSchedulerCheck = 0;
                            }
                            if (ea.Message.body.what == "poll")
                            {
                                logger.Debug(string.Format("пришел пинг от poll", ea.Message.head.what));
                                countPollCheck = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "ВНИМАНИЕ ПРОБЛЕМА ПРИ ПРИЕМЕ ЗАЯВОК");
                    }
                });
            };
            bus.Start();
            #endregion

            //2. запуск веб-сервера
            var binding = ConfigurationManager.AppSettings["binding"];
            var root = ConfigurationManager.AppSettings["root-folder"];

            try
            {

                WebApp.Start(url: binding, startup: b =>
                {
                    Newtonsoft.Json.JsonConvert.DefaultSettings = () =>
                    new JsonSerializerSettings()
                    {
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    };

                    b.UseFileServer(new FileServerOptions
                    {
                        EnableDirectoryBrowsing = false,
                        FileSystem = new PhysicalFileSystem(root)
                    });

                    var config = new HttpConfiguration();
                    config.Routes.MapHttpRoute(
                        name: "DefaultApi",
                        routeTemplate: "api/{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional }
                    );
                    var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
                    config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

                    b.UseWebApi(config);
                });

                logger.Debug("сервер запущен (привязка {0}, корневая папка {1})", binding, root);
            }
            catch (Exception e)
            {
                logger.Error("Не удалось запустить сервер (привязка {0}, корневая папка {1})! {2}", binding, root, e);
            }

            #region Команды console, ожидание exit
            var cmd = "";
            while (!(cmd = Console.ReadLine()).Contains("exit"))
            {
                switch (cmd)
                {
                    case "status":
                        {
                            break;
                        }
                    case "info":
                        {
                            break;
                        }
                    case "dump":
                        {
                            string fileName = string.Format("dump_{0:dd.HH-mm-ss}.txt", DateTime.Now);
                            break;
                        }
                    case "sys":
                        {
                            var sys = "";
                            foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                            {
                                sys += string.Format("{0} {1}\n", thread.StartTime, thread.TotalProcessorTime);
                            }
                            string fileName = string.Format("sys_{0:dd.HH-mm-ss}.txt", DateTime.Now);
                            File.WriteAllText(fileName, sys);
                            break;
                        }
                    case "cancel":
                        {
                            break;
                        }
                    case "connect":
                        {
                            connector.Subscribe();
                            break;
                        }
                }
            }
            #endregion
            #region освобождение ресурсов
            bus.Stop();
            connector.Dispose();
            #endregion
        }
        static int countSchedulerCheck = 0;
        static int countPollCheck = 0;
        public static void RestartProgram(string name)
        {
            string operationAction = ConfigurationManager.AppSettings[$"{name}Server"];
            Process.Start(operationAction);
        }
       
    }
}
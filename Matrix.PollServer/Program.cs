using log4net.Config;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.PollServer.Nodes;
using Matrix.PollServer.Nodes.Csd;
using System.IO;
using Matrix.PollServer.Api;
using System.Diagnostics;
using Matrix.PollServer.Fill;
using Matrix.PollServer.Nodes.Tube;
using Matrix.PollServer.Routes;
using Matrix.PollServer.Storage;
using Matrix.PollServer.Handlers;
using System.Configuration;
using NLog;
using Microsoft.Practices.ServiceLocation;

namespace Matrix.PollServer
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

            #region API - соединение с Api
            var connector = UnityManager.Instance.Resolve<IConnector>();
            while (!connector.Relogin())
            {
                logger.Warn(string.Format("не удалось соединиться с сервером, повтор через 5 сек."));
                Thread.Sleep(5000);
            }
            connector.Subscribe();
            #endregion
            #region RabbitMQ - соединение с шиной
            var bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        logger.Debug("получено сообщение из шины, {0}", ea.Message.head.what);
                        if (ea.Message.head.what.StartsWith("poll"))
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

                            var nodes = NodeManager.Instance.GetByIds(ids);

                            var priority = PollTask.PRIORITY_USER;

                            var darg = ea.Message.body.arg as IDictionary<string, object>;
                            if (darg.ContainsKey("auto") && ea.Message.body.arg.auto == true)
                            {
                                priority = PollTask.PRIORITY_AUTO;
                            }

                            if (darg.ContainsKey("all") && ea.Message.body.arg.all == true)
                            {
                                ids = NodeManager.Instance.GetNodes<TubeNode>().Select(t => t.GetId()).ToList();
                            }

                            PollTaskManager.Instance.CreateTasks(ea.Message.body.what, nodes, ea.Message.body.arg, priority);
                        }
                        if (ea.Message.head.what.StartsWith("check-poll"))
                        {
                            var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "check");
                            msg.body.objectIds = new Guid[] { };
                            msg.body.arg = new ExpandoObject();
                            msg.body.what = "poll";
                            bus.SendCheck(msg);
                            logger.Debug("сработал check");
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

            //загрузка драйверов и нодов
            DriverManager.Instance.Load();
            NodeManager.Instance.Load();

            #region Обработчики API
            //регистрация обработчиков сообщений api
            HandlerManager.Instance.Register(new Handlers.PollHandler());
            HandlerManager.Instance.Register(new Handlers.DriverHandler());
            HandlerManager.Instance.Register(new Handlers.ManagmentHandler());
            HandlerManager.Instance.Register(new Handlers.ChangesHandler());
            #endregion

            #region Команды console, ожидание exit
            var cmd = "";
            while (!(cmd = Console.ReadLine()).Contains("exit"))
            {
                switch (cmd)
                {
                    case "modems":
                        {
                            var modems = NodeManager.Instance.GetNodes<PoolModem>();
                            var report = string.Format("---модемы в пуле {0:dd.MM.yy HH:mm:ss.fff}---\r\n{1}", DateTime.Now, string.Join("\r\n", modems.Select(m => m.GetInfo())));

                            string fileName = string.Format("{1}_{0:dd.HH-mm-ss}.txt", DateTime.Now, cmd);
                            File.WriteAllText(fileName, report);
                            Process.Start(fileName);
                            break;
                        }
                    case "nodes":
                        {
                            var nodes = NodeManager.Instance.GetNodes<TubeNode>();
                            var report = string.Format("---тюбы в пуле {0:dd.MM.yy HH:mm:ss.fff}---\r\n{1}", DateTime.Now, string.Join("\r\n", nodes.Select(n => n.GetInfo())));
                            File.WriteAllText("reports.txt", report);
                            Process.Start("reports.txt");
                            break;
                        }
                    case "status":
                        {
                            string fileName = "status.txt";
                            StringBuilder text = new StringBuilder();
                            text.AppendLine("---Poll Task Manager---").Append(PollTaskManager.Instance.GetInfo()).AppendLine("").AppendLine("");
                            text.AppendLine("---Records Acceptor---").Append(RecordsAcceptor.Instance.GetStatus()).AppendLine("").AppendLine("");
                            text.AppendLine("---Node Paths---");
                            var nodes = NodeManager.Instance.GetNodes<TubeNode>();
                            foreach (var node in nodes.Where(n => !n.IsDisabled()))
                            {
                                text.AppendLine($"id:{node.GetId()} description:{node.ToString()}");
                                text.AppendLine($"name:{GetParam(node.GetArguments(), "name")}; networkAddress:{GetParam(node.GetArguments(), "networkAddress")}; ");
                                text.AppendLine("paths:");
                                if (node.GetPaths().Where(p => p.Where(n => n.Node.IsDisabled()).Any()).Any())
                                {
                                    text.Append("[DISABLED] ");
                                }
                                foreach (var path in node.GetPaths())
                                {
                                    text.AppendLine(string.Join(" -> ", path.Select(p => $"{p.Node.ToString()}{(p.Node.IsDisabled() ? " [X]" : "")}")));
                                }
                                text.AppendLine("");
                            }
                            text.AppendLine("").AppendLine("");
                            text.AppendLine("---Bus---").Append(bus.GetStatus()).AppendLine("").AppendLine("");
                            text.AppendLine("---Api Connector---").Append(connector.ToString()).AppendLine("").AppendLine("");

                            File.WriteAllText(fileName, text.ToString());
                            Process.Start(fileName);
                            break;
                        }
                    case "info":
                        {
                            var nodes = NodeManager.Instance.GetNodes<PollNode>();
                            var report = new StringBuilder(string.Format("---статистика {0:dd.MM.yy HH:mm:ss.fff}\r\n---", DateTime.Now));
                            report.AppendLine(string.Format("мин потоков в пуле: {0}", workerThreads));
                            report.AppendLine(string.Format("всего нодов: {0}", nodes.Count()));

                            string fileName = string.Format("{1}_{0:dd.HH-mm-ss}.txt", DateTime.Now, cmd);
                            File.WriteAllText(fileName, report.ToString());
                            Process.Start(fileName);
                            break;
                        }
                    case "dump":
                        {
                            string fileName = string.Format("dump_{0:dd.HH-mm-ss}.txt", DateTime.Now);
                            File.WriteAllText(fileName, PollTaskManager.Instance.Dump());
                            Process.Start(fileName);
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
                    case "all":
                        {
                            //var tubes = new List<TubeNode>();
                            //tubes.Add((TubeNode)NodeManager.Instance.GetById(Guid.Parse("bdb15ed9-bbda-41d3-918d-e915b31764ac")));

                            //var tubes = NodeManager.Instance.GetNodes<TubeNode>();
                            //string what = "all";
                            //dynamic arg = new ExpandoObject();
                            //arg.components = "Current;Hour;Day;";
                            //PollTaskManager.Instance.CreateTasks(what, tubes, arg, PollTask.PRIORITY_AUTO);

                            logger.Info("сработал стимулятор");
                            var tubes = NodeManager.Instance.GetNodes<TubeNode>();
                            string what = "all";
                            dynamic arg = new ExpandoObject();
                            arg.components = "Day:3:60;Hour:3:3";
                            PollTaskManager.Instance.CreateTasks(what, tubes, arg, PollTask.PRIORITY_AUTO);
                            break;
                        }
                    case "cancel":
                        {
                            PollTaskManager.Instance.RemoveTasks();
                            break;
                        }
                    case "connect":
                        {
                            connector.Subscribe();
                            break;
                        }
                    //case "bus start":
                    //    {
                    //        bus.Start();
                    //        break;
                    //    }
                    //case "bus stop":
                    //    {
                    //        bus.Stop();
                    //        break;
                    //    }
                    case "bus restart":
                        {
                            bus.Stop();
                            bus.Start();
                            break;
                        }
                    //case "api restart":
                    //    {
                    //        Console.WriteLine("Перезапуск API");
                    //        connector.Restart();
                    //        break;
                    //    }
                    case "reset":
                        {
                            ManagmentHelper.ResetServer(ConfigurationManager.AppSettings["name"]);
                            break;
                        }
                    case "killcom":
                        {
                            Console.WriteLine("введите порт: ");
                            var port = Console.ReadLine();
                            ManagmentHelper.KillCom(port);
                            break;
                        }
                    case "startcom":
                        {
                            Console.WriteLine("введите порт: ");
                            var port = Console.ReadLine();
                            ManagmentHelper.StartCom(port);
                            break;
                        }
                    case "jobs":
                        {
                            //Stimulator.Instance.Reload();
                            break;
                        }
                    case "test":
                        {
                            dynamic request = Helper.BuildMessage("managment-test");
                            dynamic answer = connector.SendMessage(request);
                            break;
                        }
                }
            }
            #endregion
            #region освобождение ресурсов
            bus.Stop();
            PollTaskManager.Instance.Dispose();
            //Synchronizer.Instance.Dispose();
            RecordsAcceptor.Instance.Dispose();
            StateSaveCollector.Instance.Dispose();
            //RecordsRepository2.Instance.Dispose();
            connector.Dispose();
            NodeManager.Instance.Dispose();
            #endregion
        }

        private static object GetParam(dynamic dyn, string[] param, int inx = 0)
        {
            if ((inx < param.Length) && dyn is IDictionary<string, object> && (dyn as IDictionary<string, object>).ContainsKey(param[inx]))
            {
                dynamic p = (dyn as IDictionary<string, object>)[param[inx]];
                if (p is IDictionary<string, object>)
                {
                    return GetParam(p, param, inx + 1);
                }
                else if ((inx + 1) == param.Length)
                {
                    return p;
                }
            }
            return null;
        }

        private static string GetParam(dynamic dyn, string param, string nullStr = "<NULL>")
        {
            dynamic res = GetParam(dyn, param.Split('.'));
            if (res == null)
            {
                return nullStr;
            }
            else if (res is string)
            {
                return (string)res;
            }
            else
            {
                return res.ToString();
            }
        }
    }
}
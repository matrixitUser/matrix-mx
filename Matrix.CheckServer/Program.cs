using log4net.Config;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Matrix.CheckServer.Fill;
using System.Configuration;
using NLog;
using Matrix.CheckServer.Handlers;

namespace Matrix.CheckServer
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

            Thread threadWhileChecking = new Thread(new ThreadStart(whileChecking));
            threadWhileChecking.Start();
            #endregion
            #region RabbitMQ - соединение с шиной
            var bus = new Bus();
            bus.OnMessageReceived += (se, ea) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (ea.Message.head.what.StartsWith("check"))
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
        public static void whileChecking()
        {
            while (true)
            {
                if (countSchedulerCheck > 2 && countSchedulerCheck < 5)
                {
                    logger.Debug(string.Format("restart scheduler"));
                    RestartProgram("scheduler");
                }
                if (countPollCheck > 2 && countPollCheck < 5)
                {
                    logger.Debug(string.Format("restart poll"));
                    RestartProgram("poll");
                }
                
                countSchedulerCheck++;
                countPollCheck++;
                Thread.Sleep(1000 * 60 * 5);
            }
        }
    }
}
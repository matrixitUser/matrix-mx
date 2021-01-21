using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using NLog;
using Owin;
using SuperSocket.SocketBase;

namespace Matrix.MatrixControllers
{
    class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable host;
        private MatrixSocketServer ss;
        private Bus bus;
        private HandlerManager hm;

        public void Start()
        {
            var uc = new UnityContainer();
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(uc));

            hm = new HandlerManager();
            bus = new Bus();
            ss = new MatrixSocketServer();

            uc.RegisterInstance(bus);
            uc.RegisterInstance(ss);

            bus.Start();
            bus.OnMessageReceived +=async (se, ea) =>
            {
                try
                {
                    logger.Debug("получено сообщение на опрос");

                    //при поступлениии сообщения из шины 
                    //ищем обработчик, если он есть
                    //смотрим есть ли сессия,
                    //запускаем опрос

                    string what = ea.Message.head.what;
                    var handler = hm.Get(what);
                    if (handler == null)
                    {
                        logger.Warn("обработчик команды {0} не найден, команда проигнорирована", what);
                        return;
                    }

                    //find session
                    var ids = new List<Guid>();
                    foreach(var id in ea.Message.body.path)
                    {
                        ids.Add(Guid.Parse(id));
                    }

                    var executor = ids.Skip(ids.Count() - 2).FirstOrDefault();
                    var target = ids.FirstOrDefault();

                    var session = ss.GetAllSessions().FirstOrDefault(s => s.Id == executor);
                    if (session == null)
                    {
                        logger.Warn("сессия контроллера {0} не найдена", executor);
                        bus.SendRejectPoll(target, "не на связи");
                        return;
                    }

                    if (!session.CurrentState.CanChange())
                    {
                        logger.Warn("текущее состояние {0} не может быть заменено", session.CurrentState);
                        bus.SendRejectPoll(target, "заблокирован");
                        return;
                    }

                    bus.SendBeginPoll(target, "");
                    session.ChangeState(handler);

                    var result = await session.CurrentState.Start(ea.Message.body.path, ea.Message.body.details);

                    bus.SendCompletePoll(target, 1, "");
                    //todo ans here
                    session.ChangeState(hm.Get("idle"));
                }catch(Exception ex)
                {
                    logger.Error(ex,"при получении заявки на опрос");
                }
                //if (ea.Message.head.what == "poll")
                //{
                //    Guid matrixId = Guid.Parse(ea.Message.body.matrixId);
                //    Guid tubeId = Guid.Parse(ea.Message.body.tubeId);                    
                //    logger.Debug("опрос начался: {0}", result.Success ? "да" : "нет");

                //    if (result.Success)
                //    {
                //        var answer = bus.MakeMessageStub("", "poll-started");
                //        answer.body.taskId = ea.Message.body.taskId;
                //        answer.body.reason = result.Reason;
                //        answer.body.portName = ea.Message.body.portName;
                //        bus.Send(answer);
                //    }
                //    else
                //    {
                //        var answer = bus.MakeMessageStub("", "poll-rejected");
                //        answer.body.taskId = ea.Message.body.taskId;
                //        answer.body.reason = result.Reason;
                //        answer.body.portName = ea.Message.body.portName;
                //        bus.Send(answer);
                //    }
                //}
                //ea.Message;
            };

            ss.NewSessionConnected += (session) =>
            {
                //todo notify task manager here
                logger.Debug("контроллер {0} вышел на связь ({1})", session.Imei, session.RemoteEndPoint.Address.ToString());
            };

            ss.SessionClosed += (session, reason) =>
            {
                logger.Debug("соединение с {0} закрыто, причина {1}", session.Imei, reason);
            };

            ss.NewRequestReceived += (session, info) =>
            {
                logger.Debug("пришла посылка от {0} а именно {1}", session.Imei, string.Join(",", info.Body.Select(b => b.ToString("X2"))));
                session.CurrentState.AcceptFrame(info);
            };

            var port = int.Parse(ConfigurationManager.AppSettings["port-port"]);
            if (!ss.Setup(port) || !ss.Start())
            {
                logger.Warn("не удалось запустить порт матрикс на {0} порту", port);
                return;
            }
            logger.Info("сокет сервер запущен на порту {0}", port);

            var url = ConfigurationManager.AppSettings["url"];
            host = WebApp.Start(url, app =>
            {
                app.UseNancy(n => n.Bootstrapper = new Bootstrapper(uc));
            });

            logger.Info("сервис запущен, url: {0}", url);
        }

        public void Stop()
        {
            bus.Stop();
            ss.Stop();
            host.Dispose();
            logger.Info("сервис остановлен");
        }
    }
}

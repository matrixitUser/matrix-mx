using log4net;
using Matrix.PollServer.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Dynamic;
using System.Timers;

namespace Matrix.PollServer.Nodes.Tcp
{
    /// <summary>
    /// Финальный нод, представляющий TCP-клиент, работающий на уровне данных (байты)
    /// Должны быть заданы host и port (ip и порт сервера соответсвенно)
    /// При загрузке устанавливает связь с сервером и постоянно держит её (TODO флаг "Инициатива сверху"),
    /// тем самым обеспечивает постоянную прослушку сообщений
    /// При получении входящих данных нод в любом случае рассылает их дальше
    /// </summary>

    class TcpClient : PollNode
    {
        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(TcpClient));

        private bool isKeepAlive = true;

        private bool isStarted = false;

        private bool isReceiver = false;

        private bool tcpIsReady = false;

        public TcpClient(dynamic content)
        {
            this.content = content;
            isReceiver = GetReceiver();

            if (isReceiver)
            {
                TcpClientStart();
            }
        }

        public int GetPort()
        {
            var dcontent = content as IDictionary<string, object>;
            int port = 0;
            if (dcontent.ContainsKey("port"))
                int.TryParse(content.port.ToString(), out port);
            return port;
        }

        private string GetHost()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("host")) return "127.0.0.1";
            return content.host.ToString();
        }

        private int GetKeepAliveTime()
        {
            var dcontent = content as IDictionary<string, object>;
            int keepalive = 0;
            if (dcontent.ContainsKey("keepalive"))
                int.TryParse(content.keepalive.ToString(), out keepalive);
            return keepalive;
        }
        
        public bool GetReceiver()
        {
            var dcontent = content as IDictionary<string, object>;
            bool receiver = false;
            if (dcontent.ContainsKey("receiver"))
                bool.TryParse(content.receiver.ToString(), out receiver);
            return receiver;
        }

        public override int GetFinalisePriority()
        {
            return 10;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        public override bool IsFinalNode()
        {
            return true;
        }

        private Socket socket = null;
        private bool loop = false;
        private Thread worker;

        private Dictionary<Route, Action<byte[]>> subscriber = new Dictionary<Route, Action<byte[]>>();

        #region Tcp client part

        private System.Timers.Timer tmrKeepAlive = null;

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //log.Debug(string.Format("[{0}] keep alive", this));
            foreach (var inp in RelationManager.Instance.GetInputs(GetId(), "contains"))
            {
                var id = inp.GetStartId();
                var node = NodeManager.Instance.GetById(id);
                TcpClientSend(node.GetKeepAlive());
            }
            TimerTouch();
        }

        private void TimerStop()
        {
            if (tmrKeepAlive != null)
            {
                tmrKeepAlive.Stop();
                tmrKeepAlive.Dispose();
                tmrKeepAlive = null;
            }
        }

        private void TimerStart()
        {
            var keepAlive = GetKeepAliveTime();
            if ((keepAlive > 0) && (tmrKeepAlive == null))
            {
                tmrKeepAlive = new System.Timers.Timer();
                tmrKeepAlive.Interval = TimeSpan.FromSeconds(keepAlive).TotalMilliseconds;
                tmrKeepAlive.Start();
                tmrKeepAlive.Elapsed += OnTimedEvent;
            }
        }

        private void TimerTouch()
        {
            TimerStop();
            TimerStart();
        }
        /// <summary>
        /// Запускает процесс приема входящих соединений
        /// </summary>		
        public void TcpClientStart()
        {
            isStarted = true;
            try
            {
                TimerStop();
                worker = new Thread(TcpClientIdle);
                worker.IsBackground = true;
                worker.Name = string.Format("поток {0}", this);
                worker.Start();

                log.Info(string.Format("[{0}] порт опроса запущен", this));
            }
            catch (Exception ex)
            {
                log.Error(string.Format("[{0}] порт опроса НЕ запущен: {1}", this, ex.Message));
            }
        }
        
        /// <summary>
        /// Останавливает сервер, перестает принимать новые соединения, но
        /// все ранее установленные соединения продолжают жить
        /// </summary>
        public void TcpClientStop()
        {
            isStarted = false;
            //TimerStop();
            //Stop();
            log.InfoFormat("[{0}] порт опроса остановлен", this);
            //
        }

        private bool Start()
        {
            try
            {
                Stop();

                if (IsDisabled())
                {
                    log.InfoFormat("[{0}] соединение отключено", this);
                    TcpClientStop();
                    return false;
                }

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(GetHost(), GetPort());
                tcpIsReady = true;
            }
            catch (Exception ex)
            {
                socket = null;
                log.Error(string.Format("приготовление {0}", this), ex);
                return false;
            }

            return true;
        }

        private void Stop()
        {
            try
            {
                if(socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("закрытие {0}: {1}", this, ex.Message);
            }
        }

        /// <summary>
        /// Цикл ожидания входящих сообщений
        /// </summary>
        /// <param name="parameter"></param>
        private void TcpClientIdle()
        {
            for (; isStarted; )
            {
                log.InfoFormat("[{0}] запуск цикла ожидания сообщений", this);

                loop = true;
                if (Start())
                {
                    try
                    {
                        TimerTouch();
                        var buffer = new byte[1024];
                        while (isStarted && loop)
                        {
                            if (IsDisabled())
                            {
                                log.InfoFormat("[{0}] обнаружено отключение соединения", this);
                                TcpClientStop();
                                continue;
                            }

                            //входящие байты
                            var readed = socket.Receive(buffer);
                            if (readed == 0) continue;

                            TimerTouch();

                            var bytes = buffer.Take(readed).ToArray();

                            //if (isKeepAlive)
                            {
                                //отправка всем "входящим"
                                foreach (var inp in RelationManager.Instance.GetInputs(GetId(), "contains"))
                                {
                                    var id = inp.GetStartId();
                                    var node = NodeManager.Instance.GetById(id);
                                    node.Receive(bytes);
                                }
                            }
                            //else
                            {
                                //отправка подписчикам
                                if (subscriber.Count() > 0)
                                {
                                    foreach (var sub in subscriber)
                                    {
                                        sub.Value(bytes);
                                    }
                                }
                            }
                        }
                    }
                    catch(SocketException ex)
                    {
                        log.Error(string.Format("ошибка сокета {0}:{1}, код {2}", GetHost(), GetPort(), ex.ErrorCode), ex);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("поток соединения {0}:{1} закрыт с ошибкой", GetHost(), GetPort()), ex);
                    }
                }

                Stop();
                TimerStop();

                Thread.Sleep(1000);
            }
            worker.Join();
        }

        private void TcpClientSend(byte[] bytes)
        {
            if (bytes.Length == 0) return;

            try
            {
                //исходящие байты
                if (socket != null)
                {
                    socket.Send(bytes);
                }
                else
                {
                    log.Debug(string.Format("сокет у {0} занулен", this));
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("не удалось отправить сообщение {0}", ex));
                loop = false;
            }
        }

        #endregion
        
        public override string ToString()
        {
            return string.Format("порт опроса {0}:{1}", GetHost(), GetPort());
        }
        
        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            if (!isReceiver)
            {
                tcpIsReady = false;
                TcpClientStart();

                int timeout = 20000;
                while ((timeout > 0) && (!tcpIsReady))
                {

                    timeout -= 100;
                    Thread.Sleep(100);
                }

                if (!tcpIsReady)
                {
                    TcpClientStop();
                    return Codes.RESOURCE_BUSY;
                }
            }

            subscriber[route] = (bytes) =>
            {
                route.Send(this, bytes, Routes.Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, dir) =>
            {
                if (dir == Routes.Direction.FromInitiator)// К к.у. ОТ драйвера через ZigbeeConnection
                {
                    TcpClientSend(bytes);
                }
                else // ОТ к.у. К драйверу
                {
                    //в idle
                }
            });
            
            return 0;
        }


        protected override void OnRelease(Routes.Route route, int port)
        {
            if (!isReceiver)
            {
                TcpClientStop();
            }

            subscriber.Remove(route);
        }

        protected override bool OnLock(Route route, PollTask initiator)
        {
            return true;
        }

        protected override bool OnUnlock(Route route)
        {
            return true;
        }

        public override void Dispose()
        {
            subscriber.Clear();
            subscriber = null;

            TcpClientStop();
        }
    }
}

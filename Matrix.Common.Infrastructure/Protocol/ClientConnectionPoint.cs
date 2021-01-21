using System;
using System.Net.Sockets;
using System.Threading;
using log4net;
using System.Timers;
using Timer = System.Timers.Timer;
using Matrix.Common.Infrastructure.Protocol.Messages;
using System.Text;
using System.Collections.Generic;

namespace Matrix.Common.Infrastructure.Protocol
{
    /// <summary>
    /// точка соединения на клиентской стороне
    /// активно пытается соединится,
    /// восстанавливает соединение в случае обрыва
    /// </summary>
    public class ClientConnectionPoint : ConnectionPoint
    {
        const string VERSION = "2.6.1";

        private static readonly ILog log = LogManager.GetLogger(typeof(ClientConnectionPoint));

        /// <summary>
        /// Задержка между попытками соединения
        /// </summary>
        private const int CONNECT_ATTEMPT_DELAY = 5000;

        private Timer pingTimer;

        /// <summary>
        /// срабатывает при переподключении
        /// </summary>
        public event Action<ClientConnectionPoint> Reconnected;
        public bool IsConnected
        {
            get { return socket != null && socket.Connected; }
        }
        private void RaiseReconnected()
        {
            if (Reconnected != null)
                Reconnected(this);
        }

        private ConnectionInfo connectionInfo;

        /// <summary>
        /// Переподключаться при разрыве связи
        /// </summary>
        public bool NeedReconnect { get; set; }


        public ClientConnectionPoint(string idleThreadName)
            : base(new JsonSerilizer(), idleThreadName)
        {
            Disconnected += SocketClientDisconnected;

            pingTimer = new Timer();
            //pingTimer.Interval = PingRequest.PING_FREQUENCY;
            pingTimer.Elapsed += new ElapsedEventHandler(OnPingTimerElapsed);
        }

        private void OnPingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //SendMessage(new PingRequest(Guid.NewGuid()), response =>
            //{
            //    if (response == null)
            //    {
            //        CloseConnection();
            //        DoReconnect();
            //    }
            //});
        }

        private void SocketClientDisconnected(object sender, EventArgs e)
        {
            pingTimer.Stop();
            DoReconnect();
        }

        /// <summary>
        /// переподключение
        /// </summary>
        private void DoReconnect()
        {
            if (NeedReconnect)
            {
                var res = Connect(connectionInfo, true);
                RaiseReconnected();
            }
        }

        /// <summary>
        /// Соедениться с удаленной точкой
        /// </summary>
        /// <param name="connectionInfo">Инфо для соединения</param>
        /// <param name="repeat">если true, то в случае, если соединиться не получиться, то будет долбиться, пока не упадет или не подключиться.
        /// если false, то попробует только один раз</param>
        /// <param name="callback"> </param>
        /// <returns></returns>
        public ConnectingStatus Connect(ConnectionInfo connectionInfo, bool repeat)
        {
            ConnectionId = Guid.NewGuid();
            this.connectionInfo = connectionInfo;
            string reason = null;
            //соединяем сокет
            do
            {
                try
                {
                    CloseConnection();
                    socket = Socks.CreateSocks(connectionInfo);
                    (socket as Socks).Connect();
                }
                catch (SocksException se)
                {
                    log.Error(string.Format("[{0}] ошибка при попытке соединения c прокси сервером {1}:{2}", this, connectionInfo.ProxyServerHost, connectionInfo.ProxyServerPort), se);
                    reason = string.Format("Не удалось подключиться к прокси-серверу. {0}", se.Message);
                    if (repeat)
                        Thread.Sleep(CONNECT_ATTEMPT_DELAY);
                }
                catch (Exception e)
                {
                    log.Error(string.Format("[{0}] ошибка при попытке соединения сокета по адресу {1}:{2}", this, connectionInfo.Host, connectionInfo.Port), e);
                    if (repeat)
                        Thread.Sleep(CONNECT_ATTEMPT_DELAY);
                }

            } while (repeat && !socket.Connected);

            if (socket == null || !socket.Connected)
            {
                log.Error(reason ?? string.Format("[{0}] не удалось соединиться с узлом {1}:{2}", this, connectionInfo.Host, connectionInfo.Port));
                //callback(false, reason);
                return new ConnectingStatus(false, reason);
            }

            OpenConnection();

            //шлем сообщение "коннект" (на случай, если сервак не соответствует ожиданиям)
            var response = SendSyncMessage(new DoMessage(Guid.NewGuid(), "connect", new Dictionary<string, object> { { "connection-id", ConnectionId }, { "version", VERSION } }, null));// ConnectRequest(Guid.NewGuid(), ConnectionId, VERSION));

            if (response == null || !(response is DoMessage))
            {
                log.Error(string.Format("[{0}] сервер не подтверждает подключение {1}:{2}", this, connectionInfo.Host, connectionInfo.Port));
                return new ConnectingStatus(false, reason);
            }
            else
            {
                var conResp = response as DoMessage;
                var arg = conResp.Argument;

                DateTime serverDate = DateTime.Now;
                if (arg.ContainsKey("server-date"))
                {
                    serverDate = (DateTime)arg["server-date"];
                }

                var isVersionAcceptable = (bool)arg["is-version-acceptable"];
                if (!isVersionAcceptable)
                {
                    reason = "Версия клиента не соответствует версии сервера";
                    return new ConnectingStatus(false, reason);
                }
                else
                {
                    return new ConnectingStatus(true, reason, serverDate);
                }
            }
        }
    }

    public class ConnectingStatus
    {
        public string Message { get; private set; }
        public bool Success { get; private set; }

        public DateTime ServerDate { get; private set; }

        public ConnectingStatus(bool success, string message)
            : this(success, message, DateTime.Now)
        {
        }

        public ConnectingStatus(bool success, string message, DateTime serverDate)
        {
            ServerDate = serverDate;
            Message = message;
            Success = success;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Matrix
{
    class SimpleMatrixNode : PollNode
    {
        /// <summary>
        /// минимальный интервал между реконнектами сокета
        /// </summary>
        private const int MIN_SOCKET_RECONNECT_INTERVAL = 10000;

        private static readonly ILog log = LogManager.GetLogger(typeof(SimpleMatrixNode));

        private Thread worker;

        public SimpleMatrixNode(dynamic content)
        {
            this.content = content;
        }

        public override bool HasChance(PollTask task)
        {
            return true;
        }

        public string GetImei()
        {
            var docntent = content as IDictionary<string, object>;
            if (!docntent.ContainsKey("imei")) return "";
            return content.imei.ToString();
        }

        public override int GetFinalisePriority()
        {
            return 5;
        }

        public override int GetPollPriority()
        {
            return 10;
        }

        private const int BUFFER_SIZE = 1024 * 64;
        private Socket socket;

        public override bool IsFinalNode()
        {
            return true;
        }

        private DateTime dateStart = DateTime.Now;
        public void OpenSocket(Socket socket)
        {
            if (socket == null)
            {
                log.Error("сокет не может быть пустым");
                return;
            }

            var now = DateTime.Now;
            if ((this.socket != null && this.socket.Connected) && (now - dateStart).TotalMilliseconds < MIN_SOCKET_RECONNECT_INTERVAL)
            {
                log.Warn(string.Format("[{0}] слишком быстрое обновление соединенного сокета, возможно все сокеты контроллера выведены на один порт, второй сокет проигнорирован", GetImei()));
                return;
            }

            CloseSocket();
            this.socket = socket;
            this.socket.ReceiveBufferSize = BUFFER_SIZE;

            //запуск потока ожидания данных от сокета
            worker = new Thread(Idle);
            worker.IsBackground = true;
            worker.Start();

            IsConnected = true;
            dateStart = DateTime.Now;
            Notify();
        }

        public void CloseSocket()
        {
            if (socket != null)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                socket.Dispose();
                socket = null;
                log.Debug(string.Format("[{0}] закрытие сокета...", GetImei()));
            }
            IsConnected = false;
        }

        public void SendDataToSocket(byte[] data)
        {
            if (data == null || !data.Any()) return;

            try
            {
                lock (socket)
                {
                    socket.Send(data.ToArray());
                    log.Debug(string.Format("[{0}]; данные отправлены на сокет: [{1}]", this, string.Join(",", data.Select(d => d.ToString("X2")))));
                }
            }
            catch (Exception ex)
            {
                log.Warn(string.Format("[{0}]; данные не были отправлены", this), ex);
                CloseSocket();
            }
        }

        protected override void OnRelease(Route route, int port)
        {
            Log(string.Format("закрытие порта"));
            subscriber = null;
        }

        public override string ToString()
        {
            return GetImei();
        }

        protected override bool OnLock(Route route, PollTask initiator)
        {
            return IsConnected && base.OnLock(route, initiator);
        }

        public override bool IsLocked()
        {
            return !IsConnected || base.IsLocked();
        }

        private Action<byte[]> subscriber = null;

        private bool isConnected = false;

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            log.Debug(string.Format("начата подготовка контроллера матрикс {0}", GetImei()));
            if (socket == null)
            {
                Log("контроллер не на связи");
                log.Debug(string.Format("завершена подготовка контроллера матрикс {0} (неудача)", GetImei()));
                return Codes.MATRIX_NOT_CONNECTED;
            }

            subscriber = (package) =>
            {
                route.Send(this, package, Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, forward) =>
            {
                if (bytes == null) return;
                SendDataToSocket(bytes);
            });

            return Codes.SUCCESS;
        }

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                if (isConnected != value && value)
                {
                    Notify();
                }
                isConnected = value;
            }
        }

        private void Idle()
        {
            try
            {
                while (true)
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var readed = socket.Receive(buffer);

                    log.Debug(string.Format("[{0}]; данные получены от сокета: [{1}]", this, string.Join(",", buffer.Take(readed).Select(d => d.ToString("X2")))));

                    if (readed == 0)
                    {
                        throw new Exception("пришло 0 байт");
                    }

                    if (subscriber != null)
                    {
                        subscriber(buffer.Take(readed).ToArray());
                    }
                }
            }
            catch (Exception oex)
            {
                log.Error(string.Format("[{0}] поток слушающий сокет остановлен", GetImei()), oex);
                //Stop(true);
                CloseSocket();
            }
        }

        public override void Dispose()
        {
            CloseSocket();
            subscriber = null;
        }

        protected override bool IsAlive()
        {
            return socket != null;
        }
    }
}

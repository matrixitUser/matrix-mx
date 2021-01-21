using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Lan
{
    class LanConnection : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LanConnection));



        public LanConnection(dynamic content)
        {
            this.content = content;
        }

        private string GetHost()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("host")) return "127.0.0.1";
            return content.host.ToString();
        }

        public override bool IsFinalNode()
        {
            return true;
        }

        public override int GetPollPriority()
        {
            return 15;
        }

        private int GetPort()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("port")) return 0;
            return int.Parse(content.port.ToString());
        }

        private Thread worker;
        private bool loop = true;
        private Socket socket;

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(GetHost(), GetPort());

                loop = true;
                worker = new Thread(() =>
                {
                    try
                    {
                        var buffer = new byte[1024];
                        while (loop)
                        {
                            var readed = socket.Receive(buffer);
                            route.Send(this, buffer.Take(readed).ToArray(), Direction.ToInitiator);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("остановлен поток lan соединения {0}:{1}", GetHost(), GetPort()), ex);
                    }
                });
                worker.IsBackground = true;
                worker.Name = string.Format("поток {0}", this);
                worker.Start();

                route.Subscribe(this, (bytes, dir) =>
                {
                    socket.Send(bytes);
                });
            }
            catch (Exception ex)
            {
                log.Error(string.Format("приготовление {0}", this), ex);
                Log(string.Format("сокет НЕ соединен {0}", this));
                return Codes.NO_SOCKET;
            }

            Log(string.Format("сокет соединен {0}", this));
            return Codes.SUCCESS;
        }

        protected override void OnRelease(Route route, int port)
        {
            try
            {
                loop = false;
                socket.Shutdown(SocketShutdown.Both);
                worker.Join();
            }
            catch (Exception ex)
            {
            }
        }

        public override string ToString()
        {
            return string.Format("lan соединение {0}:{1}", GetHost(), GetPort());
        }
    }
}

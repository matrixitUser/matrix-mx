using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Http
{
    class HttpConnection : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(HttpConnection));

        private dynamic content;

        public HttpConnection(dynamic content)
        {
            this.content = content;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        public override bool IsFinalNode()
        {
            return true;
        }

        private Thread worker;
        private bool loop = true;

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            //socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                //socket.Connect(GetHost(), GetPort());

                //loop = true;
                //worker = new Thread(() =>
                //{
                //    try
                //    {
                //        var buffer = new byte[1024];
                //        while (loop)
                //        {
                //            var readed = socket.Receive(buffer);
                //            route.Send(this, buffer.Take(readed).ToArray(), Direction.ToInitiator);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        log.Error(string.Format("остановлен поток lan соединения {0}:{1}", GetHost(), GetPort()), ex);
                //    }
                //});
                //worker.IsBackground = true;
                //worker.Name = string.Format("поток {0}", this);
                //worker.Start();

                route.Subscribe(this, (bytes, dir) =>
                {
                    //socket.Send(bytes);
                    var addr = Encoding.ASCII.GetString(bytes);
                    using (var client = new HttpClient())
                    {
                        var response = client.GetAsync(addr).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            // by calling .Result you are performing a synchronous call
                            var responseContent = response.Content;

                            // by calling .Result you are synchronously reading the result
                            string responseString = responseContent.ReadAsStringAsync().Result;

                            var rsp = Encoding.ASCII.GetBytes(responseString);

                            route.Send(this, rsp, Direction.ToInitiator);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                log.Error(string.Format("приготовление {0}", this), ex);
                return Codes.NO_HTTP_CONNECTION;
            }

            return Codes.SUCCESS;
        }

        protected override void OnRelease(Route route, int port)
        {
        }

        public override string ToString()
        {
            return string.Format("http соединение");
        }
    }
}

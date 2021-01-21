﻿using log4net;
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

namespace Matrix.PollServer.Nodes.TeleofisWrx
{
    class TeleofisWrxPortNode : PollNode
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(TeleofisWrxPortNode));

        public TeleofisWrxPortNode(dynamic content)
        {
            this.content = content;
            Start();
        }

        public int GetPort()
        {         
            var dcontent = content as IDictionary<string, object>;
            int port = 0;
            if (dcontent.ContainsKey("port"))
                int.TryParse(content.port.ToString(), out port);
            return port;
        }

        public override int GetFinalisePriority()
        {
            return 10;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        private Socket listenSocket = null;

        private Thread worker;

        /// <summary>
        /// Запускает процесс приема входящих соединений
        /// </summary>		
        public void Start()
        {
            if (listenSocket != null) return;

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Blocking = true;
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, GetPort()));
            listenSocket.Listen(100);

            worker = new Thread(Idle);
            worker.IsBackground = true;
            worker.Start();

            log.Info(string.Format("[{0}] порт опроса запущен", GetPort()));

        }

        /// <summary>
        /// Останавливает сервер, перестает принимать новые соединения, но
        /// все ранее установленные соединения продолжают жить
        /// </summary>
        public void Stop()
        {
            if (listenSocket != null)
            {
                listenSocket.Close();
                listenSocket = null;

                worker.Join();
            }
            log.InfoFormat("[{0}] порт опроса остановлен", this);
        }

        public override string ToString()
        {
            return string.Format("teleofisWrx порт {0}", GetPort());
        }

        #region Tcp server part

        /// <summary>
        /// Цикл ожидания входящих соединений
        /// </summary>
        /// <param name="parameter"></param>
        private void Idle()
        {
            try
            {
                //ожидаем загрузки всех нодов, для избежания дубликатов
                Thread.Sleep(20000);
                while (true)
                {
                    var clientSocket = listenSocket.Accept();
                    Task.Factory.StartNew(() =>
                    {
                        CheckConnection(clientSocket);
                    });
                    //ThreadPool.QueueUserWorkItem(CheckConnection, clientSocket);
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("[{0}], прекращен прием сокетов", this), ex);
            }
        }

        private readonly byte[] TELEOFIS_TCP_AUTH_START = new byte[] { 0xC0, 0x00, 0x06, 0x00, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE7, 0x48, 0xC2 };
        private readonly byte[] TELEOFIS_TCP_AUTH_END = new byte[] { 0xC0, 0x00, 0x06, 0x01, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x25, 0xC2 };

        private void CheckConnection(object arg)
        {
            try
            {
                Thread.CurrentThread.IsBackground = true;

                var socket = arg as Socket;

                socket.Send(TELEOFIS_TCP_AUTH_START);

                var buffer = new byte[64 * 1024];
                var readed = socket.Receive(buffer);
                string imei = "";

                if(readed == 71 && buffer[0] == 0xC0 && buffer[1] == 0x00 && buffer[2] == 0x07 && buffer[3] == 0x00 && buffer[4] == 0x3F && buffer[70] == 0xC2)
                {
                    byte[] data = buffer.Skip(5).Take(63).ToArray();
                    byte authCount = data[0];
                    imei = Encoding.ASCII.GetString(data.Skip(1).Take(15).ToArray());
                }
                
                var regex = new Regex(@"[0-9]{15}");
                var match = regex.Match(imei);
                var remote = socket.RemoteEndPoint.ToString();

                //проверяем имей ли это
                if (match.Success)
                {
                    socket.Send(TELEOFIS_TCP_AUTH_END);

                    var terminal = NodeManager.Instance.GetNodes<TeleofisWrxConnectionNode>().FirstOrDefault(m => m.GetImei() == imei);
                    if (terminal == null)
                    {
                        log.Warn(string.Format("контроллер с imei '{0}' не зарегистрирован на сервере, идет сохранение", imei));
                        var api = UnityManager.Instance.Resolve<IConnector>();
                        dynamic message = Helper.BuildMessage("edit");
                        var id = Guid.NewGuid();
                        dynamic rule1 = new ExpandoObject();
                        rule1.action = "add";
                        rule1.target = "node";
                        rule1.content = new ExpandoObject();
                        rule1.content.id = id;
                        rule1.content.type = "TeleofisWrxConnection";
                        rule1.content.body = new ExpandoObject();
                        rule1.content.body.id = id;
                        rule1.content.body.imei = imei;
                        rule1.content.body.type = "TeleofisWrxConnection";
                        rule1.content.body.created = DateTime.Now;

                        dynamic rule2 = new ExpandoObject();
                        rule2.action = "add";
                        rule2.target = "relation";
                        rule2.content = new ExpandoObject();
                        rule2.content.start = id;
                        rule2.content.end = GetId();
                        rule2.content.type = "contains";
                        rule2.content.body = new ExpandoObject();
                        message.body.rules = new List<dynamic>();
                        message.body.rules.Add(rule1);
                        message.body.rules.Add(rule2);
                        api.SendMessage(message);
                        //socket.Close();
                        return;
                    }
                    terminal.OpenSocket(socket);
                    log.InfoFormat("[{0}] получено соединение контроллера: IMEI={1}, IP={2}", this, imei, remote);

                    return;
                }
                else
                {
                    log.WarnFormat("[{0}] соединение не соответствующее протоколу ({1}); соединение разорвано", this, imei);
                    socket.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("[{0}] ошибка при приеме соединения", this), ex);
            }
        }

        #endregion

        public override void Dispose()
        {
            Stop();
        }
    }
}

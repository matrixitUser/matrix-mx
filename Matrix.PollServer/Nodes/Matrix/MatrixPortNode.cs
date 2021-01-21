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

namespace Matrix.PollServer.Nodes.Matrix
{
    class MatrixPortNode : PollNode
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(MatrixPortNode));

        public MatrixPortNode(dynamic content)
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
            return string.Format("порт опроса матрикс {0}", GetPort());
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

        private void CheckConnection(object arg)
        {
            try
            {
                Thread.CurrentThread.IsBackground = true;

                var socket = arg as Socket;
                var buffer = new byte[64 * 1024];
                var readed = socket.Receive(buffer);
                var helloMessage = Encoding.ASCII.GetString(buffer, 0, readed);

                var regex = new Regex(@"\*(?<imei>[0-9A-Za-z]{15})(\*(?<port>\d+))?#");
                var match = regex.Match(helloMessage);

                var regex2 = new Regex(@"\*(?<version>.+)\*(?<code>.+)#");
                var match2 = regex2.Match(helloMessage);

                var remote = socket.RemoteEndPoint.ToString();

                //проверяем имей ли это
                if (match.Success)
                {
                    //убираем * и #
                    string imei = match.Groups["imei"].Value;

                    var matrix = NodeManager.Instance.GetNodes<MatrixConnectionNode>().FirstOrDefault(m => m.GetImei() == imei);
                    if (matrix == null)
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
                        rule1.content.type = "MatrixConnection";
                        rule1.content.body = new ExpandoObject();
                        rule1.content.body.id = id;
                        rule1.content.body.imei = imei;
                        rule1.content.body.type = "MatrixConnection";
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
                    matrix.OpenSocket(socket);
                    log.InfoFormat("[{0}] получено соединение контроллера: IMEI={1}, IP={2}", this, imei, remote);

                    return;
                }
                else if (match2.Success)
                {
                    var code = match2.Groups["code"].Value;
                    var matrix = NodeManager.Instance.GetNodes<SimpleMatrixNode>().FirstOrDefault(m => m.GetImei() == code);
                    if (matrix == null)
                    {
                        log.Warn(string.Format("контроллер с imei '{0}' не зарегистрирован на сервере, идет сохранение", code));
                        var api = UnityManager.Instance.Resolve<IConnector>();
                        dynamic message = Helper.BuildMessage("edit");
                        var id = Guid.NewGuid();
                        dynamic rule1 = new ExpandoObject();
                        rule1.action = "add";
                        rule1.target = "node";
                        rule1.content = new ExpandoObject();
                        rule1.content.id = id;
                        rule1.content.type = "SimpleMatrixConnection";
                        rule1.content.body = new ExpandoObject();
                        rule1.content.body.id = id;
                        rule1.content.body.imei = code;
                        rule1.content.body.type = "SimpleMatrixConnection";
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
                    matrix.OpenSocket(socket);
                    log.InfoFormat("[{0}] получено соединение контроллера: IMEI={1}, IP={2}", this, code, remote);
                }
                else if(readed == 0)
                {
                    //убираем * и #
                    string imei = "123456789101112";

                    var matrix = NodeManager.Instance.GetNodes<MatrixConnectionNode>().FirstOrDefault(m => m.GetImei() == imei);
                    if (matrix == null)
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
                        rule1.content.type = "MatrixConnection";
                        rule1.content.body = new ExpandoObject();
                        rule1.content.body.id = id;
                        rule1.content.body.imei = imei;
                        rule1.content.body.type = "MatrixConnection";
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
                    matrix.OpenSocket(socket);
                    log.InfoFormat("[{0}] получено соединение контроллера: IMEI={1}, IP={2}", this, imei, remote);
                   
                }
                else
                {
                    log.WarnFormat("[{0}] соединение не соответствующее протоколу ({1}); соединение разорвано", this, helloMessage);
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

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

namespace Matrix.PollServer.Nodes.Zigbee
{
    class ZigbeeTcpPort : PollNode
    {
        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(ZigbeeTcpPort));

        public ZigbeeTcpPort(dynamic content)
        {
            this.content = content;
            TcpClientStart();
        }

        public int GetPort()
        {
            // return 9013;            
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

        #region Data processing part

        private void DataProcess(IEnumerable<byte> readed, int dataLength, Action<byte[]> frameProcess)
        {
            // DATA = {MESSAGE,MESSAGE,HALF-OF-MESSAGE}

            //сообщений может быть много
            //сообщения могут быть неполноценными
            //формат: АА длина_2_байта полезная_нагрузка КС

            if (dataLength > readed.Count()) return;

            //простой вариант - сообщения следуют друг за другом; макс. длина сообщения - 1024 или dataLength
            var maxLength = Math.Min(dataLength, 1024);

            var bytes = readed.Take(dataLength).ToList();

            while (true)
            {
                var target = bytes.SkipWhile(b => b != 0xAA);
                if (target.Count() < 5) break; //Length too small

                var messageLength = target.ElementAt(1) | ((int)target.ElementAt(2) << 8);

                if ((messageLength > maxLength) || (messageLength > target.Count()))
                {
                    bytes = bytes.Skip(1).ToList();
                    continue;
                }

                //MESSAGE

                var message = target.Take(messageLength).ToArray();

                byte sum = 0;

                for (var i = 0; i < message.Length - 1; i++)
                {
                    var s = message[i];
                    sum += s;
                }

                if (sum == message[message.Length - 1])
                {
                    var payLoad = message.Skip(3).Take(message.Length - 4);
                    if (frameProcess != null)
                    {
                        frameProcess(payLoad.ToArray());
                    }
                    bytes = bytes.Skip(message.Length).ToList();
                }
                else //CS mismatch
                {
                    bytes = bytes.Skip(1).ToList();
                }
            }
        }

        private void FrameProcess(byte[] message)
        {
            if (message.Length == 0) return;

            var cmd = message[0];
            var body = message.Skip(1).ToArray();

            switch (cmd)
            {
                ////статус соединения устройства mac
                case 0x01://FRAME_MAC_CONNECTION_STATUS => body = mac[8],status,volt,tempL,uartcs,sleepcs,tempH
                    if (body.Length >= 9)
                    {
                        var macArr = body.Take(8).ToArray();
                        var status = body[8]; //0- ping, 1- connected, 2- disconnected, 3- no ds ed
                        //
                        UInt64 mac64 = 0;
                        for (var i = 0; i < 8; i++)
                        {
                            mac64 <<= 8;
                            mac64 |= macArr[i];
                        }
                        var macStr = string.Format("{0:X16}", mac64);
                        //
                        var ed = NodeManager.Instance.GetNodes<ZigbeeConnection>().FirstOrDefault(z => z.GetMac().ToUpper() == macStr);

                        if (ed == null) return;//не найден

                        ed.ConnectionStatusUpdate(status);

                        if (status == 0x01)//CONNECTED
                        {
                            Notify();
                        }
                    }
                    break;

                //case 0x04://FRAME_NWK_RADIOACK_STATUS
                //    if (body.Length >= 10)
                //    {
                //        var macArr = body.Take(8).ToArray();
                //        var seq = body[8]; //sequence
                //        var ack = body[9]; //ack=0 is OK
                //        //
                //        UInt64 mac64 = 0;
                //        for (var i = 0; i < 8; i++)
                //        {
                //            mac64 <<= 8;
                //            mac64 |= macArr[i];
                //        }
                //        var macStr = string.Format("{0:X16}", mac64);
                //        //
                //        var ed = NodeManager2.Instance.GetNodes<ZigbeeConnection>().FirstOrDefault(z => z.GetMac().ToUpper() == macStr);

                //        if (ed == null) return;//не найден

                //        ed.AckStatusUpdate(ack);
                //    }
                //    break;

                //данные (UART) с устройства с MAC-адресом mac
                case 0x20://FRAME_MAC_UART_RECEIVE_DATA => body = mac[8],data[1...]
                    if (body.Length >= 9)
                    {
                        if (subscriber.Count() > 0)
                        {
                            foreach (var sub in subscriber)
                            {
                                sub.Value(body);
                            }
                        }
                        //var macArr = body.Take(8).ToArray();
                        //var data = body.Skip(8).ToArray();
                        ////
                        //UInt64 mac64 = 0;
                        //for (var i = 0; i < 8; i++)
                        //{
                        //    mac64 <<= 8;
                        //    mac64 |= macArr[i];
                        //}
                        //var macStr = string.Format("{0:X16}", mac64);
                        ////
                        //var ed = NodeManager2.Instance.GetNodes<ZigbeeConnection>().FirstOrDefault(z => z.GetMac().ToUpper() == macStr);

                        //if (ed == null) return;//не найден

                        //ed.DataReceive(data);
                    }
                    break;

                default: break;
            }
        }

        #endregion


        #region Tcp client part

        public bool TcpClientPrepare()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(GetHost(), GetPort());
            }
            catch (Exception ex)
            {
                log.Error(string.Format("приготовление {0}", this), ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Запускает процесс приема входящих соединений
        /// </summary>		
        public void TcpClientStart()
        {
            loop = true;

            worker = new Thread(TcpClientIdle);
            worker.IsBackground = true;
            worker.Name = string.Format("поток {0}", this);
            worker.Start();

            log.Info(string.Format("[{0}] порт опроса запущен", this));
        }

        /// <summary>
        /// Останавливает сервер, перестает принимать новые соединения, но
        /// все ранее установленные соединения продолжают жить
        /// </summary>
        public void TcpClientStop()
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

            log.InfoFormat("[{0}] порт опроса остановлен", this);
        }

        /// <summary>
        /// Цикл ожидания входящих сообщений
        /// </summary>
        /// <param name="parameter"></param>
        private void TcpClientIdle()
        {
            if (TcpClientPrepare())
            {
                try
                {

                    var buffer = new byte[1024];
                    while (loop)
                    {
                        var readed = socket.Receive(buffer);
                        DataProcess(buffer, readed, FrameProcess);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("поток zb соединения {0}:{1} закрыт с ошибкой", GetHost(), GetPort()), ex);
                }
            }

            if (loop)
            {
                TcpClientStop();
                TcpClientStart();
            }
        }

        private void TcpClientSend(byte[] message)
        {
            try
            {
                socket.Send(message);
            }
            catch (Exception ex)
            {
                log.Error(string.Format("не удалось отправить сообщение zb {0}", ex));
            }
        }

        //private void CheckConnection(object arg)
        //{
        //    try
        //    {
        //        Thread.CurrentThread.IsBackground = true;

        //        var socket = arg as Socket;
        //        var buffer = new byte[64 * 1024];
        //        var readed = socket.Receive(buffer);
        //        var helloMessage = Encoding.ASCII.GetString(buffer, 0, readed);

        //        var regex = new Regex(@"\*(?<imei>[0-9A-Za-z]{15})(\*(?<port>\d+))?#");
        //        var match = regex.Match(helloMessage);
        //        //проверяем имей ли это
        //        if (!match.Success)
        //        {
        //            log.WarnFormat("[{0}] соединение не соответствующее протоколу ({1}); соединение разорвано", this, helloMessage);
        //            socket.Close();
        //            return;
        //        }

        //        //убираем * и #
        //        string imei = match.Groups["imei"].Value;

        //        var remote = socket.RemoteEndPoint.ToString();

        //        var matrix = NodeManager2.Instance.GetNodes<MatrixConnectionNode2>().FirstOrDefault(m => m.GetImei() == imei);
        //        if (matrix == null)
        //        {
        //            log.Warn(string.Format("контроллер с imei '{0}' не зарегистрирован на сервере, идет сохранение", imei));
        //            var api = UnityManager.Instance.Resolve<IConnector>();
        //            dynamic message = Helper.BuildMessage("edit");
        //            var id = Guid.NewGuid();

        //            dynamic rule1 = new ExpandoObject();
        //            rule1.action = "add";
        //            rule1.target = "node";
        //            rule1.content = new ExpandoObject();
        //            rule1.content.id = id;
        //            rule1.content.type = "MatrixConnection";
        //            rule1.content.body = new ExpandoObject();
        //            rule1.content.body.id = id;
        //            rule1.content.body.imei = imei;
        //            rule1.content.body.type = "MatrixConnection";
        //            rule1.content.body.created = DateTime.Now;

        //            dynamic rule2 = new ExpandoObject();
        //            rule2.action = "add";
        //            rule2.target = "relation";
        //            rule2.content = new ExpandoObject();
        //            rule2.content.start = id;
        //            rule2.content.end = GetId();
        //            rule2.content.type = "contains";
        //            rule2.content.body = new ExpandoObject();

        //            message.body.rules = new List<dynamic>();
        //            message.body.rules.Add(rule1);
        //            message.body.rules.Add(rule2);

        //            api.SendMessage(message);
        //            //socket.Close();
        //            return;
        //        }

        //        matrix.OpenSocket(socket);
        //        log.InfoFormat("[{0}] получено соединение контроллера: IMEI={1}, IP={2}", this, imei, remote);
        //    }
        //    catch (Exception ex)
        //    {
        //        log.Error(string.Format("[{0}] ошибка при приеме соединения", this), ex);
        //    }
        //}

        #endregion


        public override string ToString()
        {
            return string.Format("порт опроса zb {0}:{1}", GetHost(), GetPort());
        }

        private byte[] MakePackage(byte[] bytes)
        {
            var pack = new List<byte>();
            var packLength = bytes.Length + 5;

            pack.Add(0xAA);
            pack.Add((byte)packLength);
            pack.Add((byte)(packLength >> 8));
            pack.Add(0x10);
            pack.AddRange(bytes);
            pack.Add((byte)pack.Sum(s => s));

            return pack.ToArray();
        }


        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            subscriber[route] = (bytes) =>
            {
                route.Send(this, bytes, Routes.Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, dir) =>
            {
                if (dir == Routes.Direction.FromInitiator)//К конечному устройству ОТ драйвера через ZigbeeConnection
                {
                    var pack = MakePackage(bytes);
                    TcpClientSend(pack);
                }
                else //сюда не должен зайти
                {
                    route.Send(this, bytes, dir);
                }
            });

            ////команда на открытие порта
            //route.Send(this, MakePackage((byte)port, 0x01, new byte[] { }), Routes.Direction.FromInitiator);

            return 0;
        }


        protected override void OnRelease(Routes.Route route, int port)
        {
            subscriber.Remove(route);
        }

        protected override bool OnLock(Route route, PollTask initiator)
        {
            return true;
        }

        protected override bool OnUnlock(Route route)
        {
            return true;//base.OnUnlock(route);
        }

        public override void Dispose()
        {
            subscriber.Clear();
            subscriber = null;

            TcpClientStop();
        }
    }
}

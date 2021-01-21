using Matrix.PollServer.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Dynamic;
using System.Timers;
using System.IO.Ports;
using log4net;

namespace Matrix.PollServer.Nodes.Com
{
    class ComConnection : PollNode
    {
        private const int IDLE_TIMEOUT = 300;

        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(ComConnection));

        //private bool isKeepAlive = true;

        private bool isStarted = false;

        private bool isReceiver = false;

        private bool serialIsReady = false;
        protected ManualResetEvent threadStop = new ManualResetEvent(false);
        WaitHandle waithandler;

        public ComConnection(dynamic content)
        {
            this.content = content;

            isReceiver = GetReceiver();

            if (isReceiver)
            {
                ComPortStart();
            }
        }

        public string GetPort()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("port")) return "";
            return content.port.ToString();
        }

        public int GetBaudRate()
        {
            var d = content as IDictionary<string, object>;
            var baudRate = 9600;
            if (d.ContainsKey("baudRate"))
                int.TryParse(content.baudRate.ToString(), out baudRate);
            return baudRate;
        }

        public Handshake GetHandshake()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("handshake"))
            {
                switch ((string)content.handshake)
                {
                    case "rts": return Handshake.RequestToSend;
                    default: return Handshake.None;
                }
            }
            return Handshake.None;
        }


        public Parity GetParity()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("parity"))
            {
                switch ((string)content.parity)
                {
                    case "none":
                        return Parity.None;
                    case "odd":
                        return Parity.Odd;
                    case "even":
                        return Parity.Even;
                    case "space":
                        return Parity.Space;
                    case "mark":
                        return Parity.Mark;
                    default:
                        return Parity.None;
                }
            }
            return Parity.None;
        }

        public bool GetReceiver()
        {
            var dcontent = content as IDictionary<string, object>;
            bool receiver = false;
            if (dcontent.ContainsKey("receiver"))
                bool.TryParse(content.receiver.ToString(), out receiver);
            return receiver;
        }

        //public bool IsDisabled()
        //{
        //    var d = content as IDictionary<string, object>;
        //    if (d.ContainsKey("isDisabled"))
        //    {
        //        if ((string)content.isDisabled == "false" || (string)content.isDisabled == "0") return false;
        //        return true;
        //    }
        //    return false;
        //}

        private StopBits GetStopBits()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("stopBits"))
            {
                switch ((string)content.stopBits)
                {
                    case "one": return StopBits.One;
                    case "none": return StopBits.None;
                    case "two": return StopBits.Two;
                    case "1.5": return StopBits.OnePointFive;
                    default: return StopBits.One;
                }
            }
            return StopBits.One;
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

        private SerialPort serial = null;
        private bool loop = false;
        private Thread worker;

        private Dictionary<Route, Action<byte[]>> subscriber = new Dictionary<Route, Action<byte[]>>();

        #region Serial port part

        /// <summary>
        /// Запускает процесс приема входящих соединений
        /// </summary>		
        public void ComPortStart()
        {
            isStarted = true;
            try
            {
                worker = new Thread(ComPortIdle);
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
        public void ComPortStop()
        {
            isStarted = false;
            //Stop();
            log.InfoFormat("[{0}] порт опроса остановлен", this);
            //worker.Join();
        }

        private bool Start()
        {
            try
            {
                Stop();

                if (IsDisabled())
                {
                    log.InfoFormat("[{0}] соединение отключено", this);
                    ComPortStop();
                    return false;
                }

                var sleepTimeout = 10 * 1000;
                while ((sleepTimeout -= 100) > 0 && loop) Thread.Sleep(100);

                serial = new SerialPort();
                serial.PortName = GetPort();
                serial.BaudRate = GetBaudRate();
                serial.Handshake = GetHandshake();
                serial.StopBits = GetStopBits();
                serial.DataBits = 8;
                serial.Parity = GetParity();
                serial.RtsEnable = false;
                serial.DtrEnable = false;
                serial.WriteTimeout = 10000;
                serial.ReadTimeout = 10000;
                serial.Open();
                serial.DiscardInBuffer();
                serial.DiscardOutBuffer();
                serialIsReady = true;

                return true;
            }
            catch (Exception ex)
            {
                log.Error(string.Format("приготовление {0}", this), ex);
                Stop();
                return false;
            }

        }

        private void Stop()
        {
            try
            {
                if (serial != null)
                {
                    if (serial.IsOpen)
                    {
                        serial.DiscardInBuffer();
                        serial.DiscardOutBuffer();
                        serial.Close();
                    }
                    serial = null;
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
        private void ComPortIdle()
        {
            lock (locker)
            {
                while (isStarted)
                {
                    log.InfoFormat("[{0}] запуск цикла ожидания сообщений", this);

                    loop = true;
                    if (Start())
                    {
                        try
                        {
                            var allBuffer = new List<byte>();
                            while (isStarted && loop)
                            {
                                if (IsDisabled())
                                {
                                    log.InfoFormat("[{0}] обнаружено отключение соединения", this);
                                    Log("COM-порт отключен");
                                    ComPortStop();
                                    continue;
                                }

                                //входящие байты
                                try
                                {
                                    var readed = 0;
                                    if (serial != null)
                                    {
                                        var bytesCount = serial.BytesToRead;
                                        if (bytesCount > 0)
                                        {
                                            do
                                            {
                                                var buffer = new byte[bytesCount];
                                                readed += serial.Read(buffer, 0, bytesCount);

                                                //SuperLog(buffer, false);

                                                allBuffer.AddRange(buffer);
                                                Thread.Sleep(IDLE_TIMEOUT);
                                                bytesCount = serial.BytesToRead;
                                            } while (bytesCount > 0);

                                            var bytes = allBuffer.Take(readed).ToArray();

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

                                            allBuffer.Clear();
                                        }
                                    }
                                    Thread.Sleep(100);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(string.Format("цикл приема данных {0}: {1}", this, ex));
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("поток соединения {0} закрыт с ошибкой", GetPort()), ex);
                        }
                    }

                    Stop();
                    Thread.Sleep(200);
                }
            }
            
            //worker.Join();
            Stop();
            threadStop.WaitOne(); 
        }

        private void ComPortSend(byte[] bytes)
        {
            if (bytes.Length == 0) return;

            try
            {
                //исходящие байты
                if (serial != null)
                {
                    serial.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    log.Debug(string.Format("сериал порт у {0} занулен", this));
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
            return string.Format("последовательный порт {0}", GetPort());
        }

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            if (!isReceiver)
            {
                Log("открытие COM-порта");

                serialIsReady = false;
                ComPortStart();

                int timeout = 20000;
                while ((timeout > 0) && (!serialIsReady))
                {

                    timeout -= 100;
                    Thread.Sleep(100);
                }

                if (!serialIsReady)
                {
                    Log("COM-порт занят другим приложением");
                    ComPortStop();
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
                    ComPortSend(bytes);
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
                Log("закрытие COM-порта");
                ComPortStop();
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

            //Log("COM-порт уничтожается");
            ComPortStop();
        }
    }
}

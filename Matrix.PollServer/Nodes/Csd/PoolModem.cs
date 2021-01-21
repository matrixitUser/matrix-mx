using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.PollServer.Routes;
using NLog;

namespace Matrix.PollServer.Nodes.Csd
{
    /// <summary>
    /// настройки модема 
    /// {
    ///     handshake:"rts"|"none" ["none"]
    ///     baudRate:N [9600]
    ///     isRtsEnable:true|false [false]
    ///     isDtrEnable:true|false [true]
    ///     parity:"none" ["none"]
    ///     stopBits:"none"|"one"|"two"|"1.5" ["one"]
    /// }
    /// </summary>
    class PoolModem : PollNode
    {
        private const int IDLE_TIMEOUT = 300;
        
        private readonly static Logger log = LogManager.GetCurrentClassLogger();

        private Thread worker;
        private bool isBroken = true;

        public override bool IsFinalNode()
        {
            return true;
        }

        public PoolModem(dynamic raw)
        {
            content = raw;
            //if (!InitSerialPort()) Lock(new Route());
            ReInit();
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
            if (d.ContainsKey("baudRate")) return (int)content.baudRate;
            return 9600;
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

        private bool IsRtsEnable()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("isRtsEnable"))
            {
                return (bool)content.isRtsEnable;
            }
            return false;
        }

        private bool IsDtrEnable()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("isDtrEnable"))
            {
                return (bool)content.isDtrEnable;
            }
            return true;
        }

        public Parity GetParity()
        {
            var d = content as IDictionary<string, object>;
            if (d.ContainsKey("parity"))
            {
                switch ((string)content.parity)
                {
                    case "none": return Parity.None;
                    default: return Parity.None;
                }
            }
            return Parity.None;
        }

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

        private readonly object locker = new object();
        protected override bool OnLock(Route route, PollTask initiator)
        {
            lock (locker)
            {
                if (isBroken) return false;
                return base.OnLock(route, initiator);
            }
        }

        public override bool IsLocked()
        {
            return isBroken || base.IsLocked();
        }

        private Action<bool> turnDtr;
        private Action clearBuffer;

        public void ReInit()
        {
            Dispose();

            isBroken = true;
            loop = true;
            worker = new Thread(Idle);
            worker.IsBackground = true;
            worker.Name = string.Format("{0}", this);
            worker.Start();
        }

        private IEnumerable<string> GetInit()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("init"))
            {
                yield return "ATE0";
                yield return "AT&D2";
                //yield return "AT&F&C1&D2&H3";
                yield break;
            }
            foreach (string cmd in content.commands.Split('\n'))
            {
                yield return cmd;
            }
        }

        private string SendAt(string at, int timeout = 2000)
        {
            var answer = string.Empty;
            callback = (bytes) =>
            {
                answer += Encoding.ASCII.GetString(bytes);
            };
            Thread.Sleep(200);
            answer = string.Empty;

            clearBuffer();

            log.Debug(string.Format("на [{0}] отправлена команда '{1}'", this, at));
            var request = Encoding.ASCII.GetBytes(string.Format("{0}\r\n", at));

            write(request);

            while ((timeout -= 330) > 0 && string.IsNullOrEmpty(answer))
                Thread.Sleep(330);

            log.Debug(string.Format("от [{0}] получен ответ '{1}'", this, answer));
            return answer;
        }

        private bool IsRegistred()
        {
            return SendAt("AT+CREG?").Contains("CREG: 0,1");
        }

        private string SendAt(SerialPort port, string at)
        {
            var buffer = new byte[1024];
            //var count = port.Read(buffer, 0, 1024);
            //if (count > 0)
            //{
            //    log.Debug(string.Format("[{0}] в буфере содержался мусор '{1}'", this, Encoding.ASCII.GetString(buffer, 0, count)));
            //}

            port.WriteLine(at);
            // log.Debug(string.Format("[{0}] отправлена команда '{1}'", this, at));
            Thread.Sleep(300);
            var readed = port.Read(buffer, 0, 1024);
            string answer = Encoding.ASCII.GetString(buffer, 0, readed);
            // log.Debug(string.Format("[{0}] на команду '{1}' получен ответ '{2}'", this, at, answer));
            return answer;
        }

        /// <summary>
        /// 1. создаем ком порт, 
        /// 2. настраиваем его
        /// 3. пытаемся открыть
        /// 4. даем некоторые at
        /// 5. слушаем порт
        /// </summary>
        private void Idle()
        {
            try
            {
                SerialPort serialPort = null;
                do
                {
                    try
                    {
                        isBroken = true;

                        if (serialPort != null)
                        {
                            if (serialPort.IsOpen)
                            {
                                serialPort.DiscardInBuffer();
                                serialPort.DiscardOutBuffer();
                                serialPort.Close();
                            }
                            serialPort = null;
                        }

                        var sleepTimeout = 20 * 1000;
                        while ((sleepTimeout -= 100) > 0 && loop) Thread.Sleep(100);

                        serialPort = new SerialPort();
                        serialPort.PortName = GetPort();
                        serialPort.BaudRate = GetBaudRate();
                        serialPort.Handshake = GetHandshake();
                        serialPort.StopBits = GetStopBits();
                        serialPort.DataBits = 8;
                        serialPort.Parity = GetParity();
                        serialPort.NewLine = "\r\n";
                        serialPort.RtsEnable = true;//IsRtsEnable();
                        serialPort.DtrEnable = true;//IsDtrEnable();
                        serialPort.WriteTimeout = 10000;
                        serialPort.ReadTimeout = 10000;
                        serialPort.Open();

                        serialPort.DiscardInBuffer();
                        serialPort.DiscardOutBuffer();

                        //Thread.Sleep(30 * 1000); // ожидание после перезагрузки модема

                        //SendAt(serialPort, "at+cfun=1,1");

                        Thread.Sleep(30 * 1000); // ожидание после перезагрузки модема

                        turnDtr = (enable) => serialPort.DtrEnable = enable;


                        write = (bytes) =>
                        {
                            try
                            {
                                if (serialPort != null)
                                {
                                    serialPort.Write(bytes, 0, bytes.Length);
                                    SuperLog(bytes, true);
                                }
                                else
                                {
                                    log.Debug(string.Format("сериал порт у {0} занулен", this));
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error(string.Format("при записи в {0}", this), ex);
                            }
                        };

                        clearBuffer = () =>
                        {
                            if (serialPort != null)
                            {
                                serialPort.DiscardInBuffer();
                                serialPort.DiscardOutBuffer();
                            }
                            else
                            {
                                log.Debug(string.Format("сериал порт у {0} занулен", this));
                            }
                        };

                        foreach (var ini in GetInit())
                        {
                            SendAt(serialPort, ini);
                            //log.Debug(string.Format("{0}->{1}='{2}'", this, ini, SendAt(serialPort, ini)));
                        }

                        //log.Debug(string.Format("{0}->AT&D2='{1}'", this, SendAt(serialPort, "AT&D2")));
                        //log.Debug(string.Format("{0}->ATE0='{1}'", this, SendAt(serialPort, "ATE0")));

                        var creg = SendAt(serialPort, "AT+CREG?");
                        if (!creg.Contains("CREG: 0,1"))
                        {
                            log.Warn(string.Format("модем {0} не зарегестрирован в сети ({1})", this, creg));
                            continue;
                        }
                        log.Debug(string.Format("{0}->AT+CREG?='{1}'", this, creg));

                        isBroken = false;
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is TimeoutException) && !(ex is TimeoutException))
                            log.Error(string.Format("{0}: ", this), ex);
                    }
                } while (loop);

                Notify();

                var allBuffer = new List<byte>();
                while (loop)
                {
                    try
                    {
                        var bytesCount = serialPort.BytesToRead;
                        if (bytesCount > 0)
                        {
                            do
                            {
                                var buffer = new byte[bytesCount];
                                serialPort.Read(buffer, 0, bytesCount);

                                SuperLog(buffer, false);

                                allBuffer.AddRange(buffer);
                                Thread.Sleep(IDLE_TIMEOUT);
                                bytesCount = serialPort.BytesToRead;
                            } while (bytesCount > 0);

                            if (callback != null)
                            {
                                callback(allBuffer.ToArray());
                            }
                            allBuffer.Clear();
                        }

                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("цикл приема данных {0}: {1}", this, ex));
                    }
                }

                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.Close();
                serialPort = null;
                log.Debug(string.Format("[{0}] модем остановил процесс получения данных", this));
            }
            catch (Exception ex)
            {
                log.Error(string.Format("[{0}] поток работы с ком портом остановлен", this), ex);
            }
        }

        private void SuperLog(IEnumerable<byte> data, bool from)
        {
            var fp = string.Format(@"d:\LOG-{0}.txt", GetPort());
            //System.IO.File.AppendAllText(fp, string.Format("---{0} {1:dd.MM.yy HH:mm:ss.fff}---\r\n{2}\r\n", from ? "от нас" : "к нам", DateTime.Now, string.Join(",", data.Select(b => b.ToString("X2")))));
            //log.Trace("---{0} {1}---\r\n{2}\r\n", GetPort(), from ? "от нас" : "к нам", string.Join(",", data.Select(b => b.ToString("X2"))));
        }

        private Action<byte[]> callback;
        private Action<byte[]> write;

        private bool loop = true;

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            log.Info(string.Format("начата подготовка модема из пула {0}", GetPort()));
            turnDtr(true);
            Thread.Sleep(2000);
            //  SendAt("AT&D2");
            callback = (bytes) =>
                route.Send(this, bytes, Direction.ToInitiator);

            route.Subscribe(this, (bytes, forward) =>
            {
                log.Trace(string.Format("[{0}] -> [{1}]", GetPort(), string.Join(",", bytes.Select(b => b.ToString("X2")))));
                if (write != null)
                    write(bytes);
            });

            log.Debug(string.Format("завершена подготовка модема из пула {0}", GetPort()));
            return Codes.SUCCESS;
        }

        protected override void OnRelease(Route route, int port)
        {
            log.Debug(string.Format("[{0}] релиз", this));
            callback = null;
            turnDtr(false);
            Thread.Sleep(500);
            turnDtr(true);
            Thread.Sleep(2000);
            //var answer = "";
            //callback = (bytes) =>
            //{
            //    answer += Encoding.ASCII.GetString(bytes);
            //};
            //var timeout = 1000;
            //while ((timeout -= 330) > 0 && string.IsNullOrEmpty(answer))
            //    Thread.Sleep(330);

            //log.Debug(string.Format("от {0} получен ответ на смену DTR '{1}'", this, answer));
        }

        public override string ToString()
        {
            return string.Format("модем из пула {0}", GetPort());
        }

        public override void Dispose()
        {
            isBroken = true;
            loop = false;
            if (worker != null)
            {
                worker.Join();
            }
        }

        public string GetInfo()
        {
            return string.Format("порт:{0},скорость:{1},сломан:{2}, в работе:{3}, залочен {4}", GetPort(), GetBaudRate(), isBroken, callback != null, IsLocked());
        }

        public override void Update(dynamic content)
        {
            if (HasChange(content))
            {
                Dispose();
                this.content = content;
                //if (!InitSerialPort()) Lock(new Route());
                ReInit();
            }
        }

        private bool HasChange(dynamic content)
        {
            var d = content as IDictionary<string, object>;
            return (d.ContainsKey("port") && (GetPort() != d["port"].ToString()));
        }

        protected override bool IsAlive()
        {
            return !IsLocked();

            return true;
            var isRegistred = IsRegistred();
            if (!isRegistred)
            {
                ReInit();
            }
            return isRegistred;
            //return true;
        }
    }
}

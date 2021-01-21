using log4net;
using Matrix.PollServer.Routes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.PollServer.Storage;

namespace Matrix.PollServer.Nodes.Matrix
{
    class MatrixConnectionNode :ConnectionNode
    {
        /// <summary>
        /// минимальный интервал между реконнектами сокета
        /// </summary>
        private const int MIN_SOCKET_RECONNECT_INTERVAL = 10000;

        private static readonly ILog log = LogManager.GetLogger(typeof(MatrixConnectionNode));

        private Thread worker;

        override protected dynamic GetDefaultPeriod()
        {
            //значение по-умолчанию
            dynamic period = new ExpandoObject();
            period.type = PollType.Hour;
            period.value = 1;

            return period;
        }

        public MatrixConnectionNode(dynamic content)
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

        //protected override dynamic CheckAndAppendTask(dynamic task)
        //{
        //    var tube = content as IDictionary<string, object>;
        //    var args = task.arg as IDictionary<string, object>;
        //    dynamic newArgs = new ExpandoObject();
        //    var newArgsDic = newArgs as IDictionary<string, object>;
        //    foreach (var key in tube.Keys)
        //    {
        //        newArgsDic.Add(key, tube[key]);
        //    }
        //    foreach (var key in args.Keys)
        //    {
        //        if (newArgsDic.ContainsKey(key))
        //            newArgsDic[key] = args[key];
        //        else
        //            newArgsDic.Add(key, args[key]);
        //    }

        //    task.arg = newArgs;
        //    return task;
        //}

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
                log.Debug(string.Format("[{0}] закрытие сокета...", GetImei()));
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch(SocketException ex) { log.Debug("Ошибка при закрытие сокета" + ex.ErrorCode); }
                finally
                {
                    socket.Close();
                    socket.Dispose();
                    socket = null;
                }
            }
            IsConnected = false;
        }

        public byte[] MakePackage(byte code, byte[] data)
        {
            ////
            byte[] frame = new byte[data.Length + 4];
            frame[0] = (byte)(frame.Count());
            frame[1] = code;

            //содержимое (со 2 по предпоследний байты)
            Array.Copy(data, 0, frame, 2, data.Length);

            //контрольная сумма
            var reverse = code != 1 && code != 2;
            var crc = Crc16Modbus.CrcCalculate(frame.Take(frame.Length - 2).ToArray(), reverse);
            frame[frame.Length - 2] = crc[0];
            frame[frame.Length - 1] = crc[1];

            return frame;
        }

        public void SendDataToSocket(byte[] data)
        {
            if (data == null || !data.Any()) return;

            try
            {
                lock (socket)
                {
                    socket.Send(data.ToArray());
                    //log.Debug(string.Format("[{0}]; данные отправлены на сокет: [{1}]", this, string.Join(",", data.Select(d => d.ToString("X2")))));
                }
            }
            catch (Exception ex)
            {
                log.Warn(string.Format("[{0}]; данные не были отправлены", this), ex);
                CloseSocket();
            }
        }

        private dynamic MakeSignal(float level)
        {
            dynamic signal = new ExpandoObject();

            signal.Id = Guid.NewGuid();
            signal.ObjectId = GetId();
            signal.Type = "MatrixSignal";
            signal.Date = DateTime.Now;
            signal.D1 = level;
            return signal;
        }

        protected override void OnRelease(Route route, int port)
        {
            ClosePort((byte)port);
            Log(string.Format("закрытие порта"));
            //DeleteSubscriber(port);
            subscriber = null;
        }

        #region commands

        private void OpenPort(byte port)
        {
            var pack = MakePackage(17, new byte[] { port });
            SendDataToSocket(pack);
            log.Debug(string.Format("байты открытия порта {0}", string.Join(",", pack.Select(b => b.ToString("X2")))));
            log.Debug(string.Format("[матрикс {0}]: открытие порта", GetImei()));
        }

        private void ClosePort(byte port)
        {
            var pack = MakePackage(16, new byte[] { port });
            SendDataToSocket(pack);
            log.Debug(string.Format("[матрикс {0}]: закрытие порта", GetImei()));
        }

        private void SendAt(string at)
        {
            var pack = MakePackage(8, Encoding.UTF8.GetBytes(at));
            SendDataToSocket(pack);
            log.Debug(string.Format(@"[матрикс {0}]: AT команда ""{1}""", GetImei(), at));
        }

        private void ChangeServer(string newServer)
        {
            var pack = MakePackage(3, Encoding.UTF8.GetBytes(newServer));
            SendDataToSocket(pack);
            log.Debug(string.Format(@"[матрикс {0}]: смена сервера на ""{1}""", GetImei(), newServer));
        }

        private void CheckVersion()
        {
            var pack = MakePackage(11, new byte[] { });
            SendDataToSocket(pack);
            log.Debug(string.Format(@"[матрикс {0}]: чтение версии контроллера", GetImei()));
        }

        #endregion

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

        private Action<dynamic> subscriber = null;

        private void DefaultSubscriber(dynamic package)
        {
            IsConnected = true;
            switch ((byte)package.code)
            {
                case 1:
                    {
                        break;
                    }
                case 2:
                    if (package.body.Length < 2) break;
                    var perc = (float)package.body[1] * 100f / 127f;
                    if (perc > 100) perc = 100;
                    //log.Debug(string.Format("[{0}] изменение уровня сигнала {1:0.00} %", this, perc));
                    Log(string.Format("уровень сигнала {0:0.00} %", perc));
                    dynamic record = new ExpandoObject();
                    record.id = Guid.NewGuid();
                    record.type = "MatrixSignal";
                    record.date = DateTime.Now;
                    record.d1 = perc;
                    record.d2 = package.body[1];
                    record.objectId = GetId();
                    RecordsAcceptor.Instance.Save(new dynamic[] { record });
                    break;
                case 11:
                    var msg = Encoding.UTF8.GetString(package.body);
                    Log(msg);
                    break;
                case 24:
                    {
                        if (package.body.Length < 1) break;
                        //log.Debug(string.Format("[{0}] ответ на 24 команду", this));
                        SendDataToSocket(MakePackage(24, new byte[] { package.body[0] }));
                        break;
                    }

                default: log.Warn(string.Format("[{0}] необработанный пакет код: {1} первые 100 байт {2}", this, package.code, string.Join(",", (package.body as byte[]).Take(100).Select(b => b.ToString("X2"))))); break;
            }
        }

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

            if (initiator.Owner == this)
            {
                var what = initiator.What;
                switch (what.ToLower())
                {
                    case "at":
                        {
                            var dcom = initiator.Arg as IDictionary<string, object>;
                            var cmd = "at";
                            if (dcom.ContainsKey("command")) cmd = initiator.Arg.command.ToString();
                            Log(string.Format("команда контроллеру: '{0}'", cmd));
                            SendAt(cmd);
                            break;
                        }
                    case "change":
                        {
                            var dcom = initiator.Arg as IDictionary<string, object>;
                            var server = "";
                            if (dcom.ContainsKey("server")) server = initiator.Arg.server.ToString();
                            if (server != "")
                            {
                                Log(string.Format("смена сервера: '{0}'", server));
                                ChangeServer(server);
                            }
                            break;
                        }
                    case "version":
                        Log(string.Format("запрос версии котроллера"));
                        CheckVersion();
                        break;
                }
                initiator.Destroy();
                return Codes.SUCCESS;
            }

            bool isPortOpen = false;

            subscriber = (package) =>
            {
                isPortOpen = true;
                switch ((byte)package.code)
                {
                    case 1:
                        {
                            var bts = (package.body as IEnumerable<byte>);
                            log.Trace(string.Format("[{0}] <- [{1}]", this, string.Join(",", bts.Select(b => b.ToString("X2")))));
                            route.Send(this, bts.ToArray(), Direction.ToInitiator);
                            return;
                        }
                    case 2:
                        {
                            return;
                        }
                }
            };

            route.Subscribe(this, (bytes, forward) =>
            {
                if (bytes == null) return;

                log.Trace(string.Format("[{0}] -> [{1}]", this, string.Join(",", bytes.Select(b => b.ToString("X2")))));

                if (bytes.Length >= 6 && Encoding.ASCII.GetString(bytes, 0, 6) == "matrix")
                {
                    var cmd = bytes[6];
                    var body = bytes.Skip(7).ToArray();
                    SendDataToSocket(MakePackage(cmd, body));
                }
                else
                {
                    SendDataToSocket(MakePackage((byte)port, bytes));
                }
            });

            CheckVersion();
            Log(string.Format("открытие порта"));
            OpenPort((byte)port);
            var timeout = 0;
            var period = 100;
            while (!isPortOpen && timeout < 20000)
            {
                Thread.Sleep(period);
                timeout += period;
            }

            // isConnected = isPortOpen;

            if (isPortOpen)
            {
                Log(string.Format("контроллер на связи"));
                log.Debug(string.Format("[{0}] сокет соединен", this));
            }
            else
            {
                Log("контроллер не на связи, ожидание");
                log.Debug(string.Format("[{0}] сокет не соединен, опрос не возможен", this));
            }

            log.Debug(string.Format("[{0}] завершена подготовка контроллера матрикс ({1})", this, isPortOpen ? "успех" : "неудача"));
            return isPortOpen ? Codes.SUCCESS : Codes.MATRIX_NOT_CONNECTED;
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
                var glue = new List<byte>();
                while (true)
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var readed = socket.Receive(buffer);

                    //log.Debug(string.Format("[{0}]; данные получены от сокета: [{1}]", this, string.Join(",", buffer.Take(readed).Select(d => d.ToString("X2")))));

                    //поддержка старых прошивок, где был 1 байт пинга
                    if (readed == 1)
                    {
                        continue;
                    }
                    if (readed == 0)
                    {
                        throw new Exception("пришло 0 байт");
                    }

                    //log.Trace(string.Format("[{0}]; на сокет поступили данные; [{1}]", GetImei(), string.Join(",", buffer.Take(readed).Select(d => d.ToString("X2")))));

                    glue.AddRange(buffer.Take(readed));
                    glue = new List<byte>(GetPackage(glue.ToArray()));
                }
            }
            catch (Exception oex)
            {
                log.Error(string.Format("[{0}] поток слушающий сокет остановлен", GetImei()), oex);
                //Stop(true);
                CloseSocket();
            }
        }

        private byte[] GetPackage(byte[] bytes)
        {
            try
            {
                if (bytes == null) return new byte[] { };
                if (bytes.Length < 4) return bytes;
                //определение длины пакета (либо 1 первый байт, либо 2 байта (2 и 3))
                ushort len = bytes[0];
                if (len == 0)
                {
                    len = BitConverter.ToUInt16(bytes, 1);
                }

                if (bytes.Length < len) return bytes;

                var packageData = bytes.Take(len).ToArray();

                dynamic package = PackageParse(packageData.ToArray());

                if (!package.success)
                {
                    log.Error(string.Format("ошибка при разборе посылки {0}", string.Join(",", (package.body as byte[]).Select(d => d.ToString("X2")))));
                }
                else
                {
                    if (subscriber != null)
                    {
                        subscriber(package);
                    }

                    DefaultSubscriber(package);
                }

                return GetPackage(bytes.Skip(len).ToArray());
            }
            catch (Exception ex)
            {
                log.Error("ошибка при обработке пакета", ex);
            }
            return new byte[] { };
        }

        private dynamic PackageParse(byte[] bytes)
        {
            dynamic package = new ExpandoObject();

            if (bytes == null || bytes.Length < 4)
            {
                package.success = false;
                package.error = "не достаточно данных";
                return package;
            }

            if (Crc16Modbus.CrcCheck(bytes))
            {
                var skip = bytes[0] == 0 ? 3 : 1;
                package.body = bytes.Skip(skip).Take(bytes.Length - (skip + 2)).ToArray();
                package.code = 1;
            }
            else if (Crc16Modbus.CrcCheck(bytes, true))
            {
                var skip = bytes[0] == 0 ? 3 : 1;
                package.body = bytes.Skip(skip).Take(bytes.Length - (skip + 2)).ToArray();
                package.code = bytes[skip];
            }
            else
            {
                package.success = false;
                package.error = "не сошлась контрольная сумма";
                return package;
            }

            package.success = true;
            package.error = string.Empty;
            return package;
        }

        public override void Dispose()
        {
            //Stop();
            CloseSocket();

            subscriber = null;
        }

        protected override bool IsAlive()
        {
            // CheckVersion();
            return socket != null;
        }
    }
}
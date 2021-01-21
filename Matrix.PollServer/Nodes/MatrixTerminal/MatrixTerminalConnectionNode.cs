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
using System.Net;
using Newtonsoft.Json;
using System.Configuration;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Runtime.InteropServices;

namespace Matrix.PollServer.Nodes.MatrixTerminal
{
    class MatrixTerminalConnectionNode : ConnectionNode
    {
        /// <summary>
        /// минимальный интервал между реконнектами сокета
        /// </summary>
        private const int MIN_SOCKET_RECONNECT_INTERVAL = 10000;

        private static readonly ILog log = LogManager.GetLogger(typeof(MatrixTerminalConnectionNode));

        private Thread worker;

        override protected dynamic GetDefaultPeriod()
        {
            //значение по-умолчанию
            dynamic period = new ExpandoObject();
            period.type = PollType.Hour;
            period.value = 1;

            return period;
        }
        public void EditNode(string config)
        {
            var api = UnityManager.Instance.Resolve<IConnector>();
            dynamic message = Helper.BuildMessage("edit");
            dynamic rule1 = new ExpandoObject();
            rule1.action = "upd";
            rule1.target = "node";
            rule1.content = new ExpandoObject();
            rule1.content.id = GetId();
            rule1.content.type = "MatrixTerminalConnection";
            rule1.content.body = new ExpandoObject();
            rule1.content.body.imei = GetImei();
            rule1.content.body.id = GetId();
            rule1.content.body.type = "MatrixTerminalConnection";
            rule1.content.body.config = config;
            message.body.rules = new List<dynamic>();
            message.body.rules.Add(rule1);
            api.SendMessage(message);
        }
        public MatrixTerminalConnectionNode(dynamic content)
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
            //ClosePort((byte)port);
            Log(string.Format("закрытие порта"));
            //DeleteSubscriber(port);
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
            log.Debug(string.Format("начата подготовка терминала T/O WRX {0}", GetImei()));
            if (socket == null)
            {
                Log("контроллер не на связи");
                log.Debug(string.Format("завершена подготовка контроллера T/O WRX {0} (неудача)", GetImei()));
                return Codes.MATRIX_NOT_CONNECTED;
            }

            bool isPortOpen = true;

            subscriber = (bytes) =>
            {
                isPortOpen = true;
                log.Trace(string.Format("[{0}] <- [{1}]", this, string.Join(",", bytes.Select(b => b.ToString("X2")))));
                route.Send(this, bytes, Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, forward) =>
            {
                if (bytes == null) return;

                log.Trace(string.Format("[{0}] -> [{1}]", this, string.Join(",", bytes.Select(b => b.ToString("X2")))));
                
                SendDataToSocket(bytes);// MakePackage((byte)port, bytes));
            });

            //CheckVersion();
            //Log(string.Format("открытие порта"));
            //OpenPort((byte)port);
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

            log.Debug(string.Format("[{0}] завершена подготовка контроллера TeleofisWrx ({1})", this, isPortOpen ? "успех" : "неудача"));
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
        //public Guid GetObjectId(string imei, UInt32 networkAddress)
        //{
        //    dynamic msg = Helper.BuildMessage("poll-get-objectid-imeina-matrixterminal");
        //    msg.body.imei = imei;
        //    msg.body.networkaddress = networkAddress;
        //    Guid objectId;
        //    var connector = UnityManager.Instance.Resolve<IConnector>();
        //    var answer = connector.SendMessage(msg);
        //    if (answer == null || answer.head.what == "error")
        //    {
        //        log.Error($"не удалось получить objectId по imei={imei}; ca={networkAddress}");
        //        objectId = new Guid();
        //    }
        //    else
        //    {
        //        objectId = Guid.Parse((string)answer.body.objectId);
        //    }
        //    return objectId;
        //}
        public Guid GetObjectId<T>(string imei, T networkAddress)
        {
            dynamic msg = Helper.BuildMessage("poll-get-objectid-imeina-matrixterminal");
            msg.body.imei = imei;
            msg.body.networkaddress = networkAddress;
            Guid objectId;
            var connector = UnityManager.Instance.Resolve<IConnector>();
            var answer = connector.SendMessage(msg);
            if (answer == null || answer.head.what == "error")
            {
                log.Error($"не удалось получить objectId по imei={imei}; ca={networkAddress}");
                objectId = new Guid();
            }
            else
            {
                objectId = Guid.Parse((string)answer.body.objectId);
            }
            return objectId;
        }
        //public Guid GetObjectId(string imei, byte networkAddress)
        //{
        //    dynamic msg = Helper.BuildMessage("poll-get-objectid-imeina-matrixterminal");
        //    msg.body.imei = imei;
        //    msg.body.networkaddress = networkAddress;
        //    Guid objectId;
        //    var connector = UnityManager.Instance.Resolve<IConnector>();
        //    var answer = connector.SendMessage(msg);
        //    if (answer == null || answer.head.what == "error")
        //    {
        //        log.Error($"не удалось получить objectId по imei={imei}; ca={networkAddress}");
        //        objectId = new Guid();
        //    }
        //    else
        //    {
        //        objectId = Guid.Parse((string)answer.body.objectId);
        //    }
        //    return objectId;
        //}
        public Guid GetObjectId<T>(byte[] bytesObjectId, string imei, T networkAddress)
        {
            Guid nullGuid = new Guid();
            Guid objectId = new Guid(bytesObjectId);
            if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF))
            {
                objectId = GetObjectId<T>(imei, networkAddress);
            }
            return objectId;
        }
        
        //public Guid GetObjectId(byte[] bytesObjectId, string imei, UInt32 networkAddress)
        //{
        //    Guid nullGuid = new Guid();
        //    Guid objectId = new Guid(bytesObjectId);
        //    if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF))
        //    {
        //        objectId = GetObjectId(imei, networkAddress);
        //    }
        //    return objectId;
        //}
        private void Idle()
        {
            try
            {
                while (true)
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var readed = socket.Receive(buffer);

                    if (readed == 0)
                    {
                        throw new Exception("пришло 0 байт");
                        //log.Debug(string.Format("[{0}] пришло 0 байт", GetImei()));
                    }
                    else
                    {
                        byte[] bytes = buffer.Take(readed).ToArray();
                        subscriber?.Invoke(bytes);
                        if(subscriber == null) // 12.03.2019 
                        {
                           
                            if(bytes.Length < 5)
                            {
                                if (bytes.Length == 2 && (bytes[0] + bytes[1] == 0xFF))
                                {
                                    SendDataToSocket(bytes);
                                }
                                else if (bytes.Length == 3 && ((byte)((bytes[1] >> 4) | (bytes[1] << 4)) == bytes[2]))
                                {
                                    try
                                    {
                                        Guid objectId = GetObjectId<byte>(GetImei(), bytes[0]);
                                        ModbusControl.Instance.Request(new Guid[] { objectId }, bytes[1].ToString(), GetImei(), "Abnormal");
                                    }
                                    catch(Exception ex)
                                    {

                                    }
                                    log.Error("событие");
                                }
                                
                                log.Error($"количество байтов пришло {bytes.Length} < 5");
                            }

                            else if (Crc.Check(bytes, new Crc16Modbus()))
                            {
                                if (bytes[0] == 0xFB) //вместо СА используется CID
                                {
                                    //if (bytes[13] == 0x60) // не работает еще
                                    //{
                                    //    byte[] byteConfig = bytes.Skip(1).Take(bytes.Length - 4).ToArray();
                                    //    //tsConfig conf = setBytes(byteConfig);
                                    //    EditNode(BitConverter.ToString(byteConfig));
                                    //}
                                    if (bytes[13] == 0x4D) //длинный СА
                                    {
                                        DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                        byte[] bytesCId = bytes.Skip(1).Take(12).ToArray();
                                        string strCId = BitConverter.ToString(bytesCId).Replace("-", "");
                                        byte func = bytes[13];
                                        byte[] bytesObjectId = bytes.Skip(14).Take(16).ToArray();
                                        UInt32 networkAddress = Helper.ToUInt32(bytes, 30);
                                        //byte networkAddress = bytes[30];
                                        Guid objectId = GetObjectId<UInt32>(bytesObjectId, GetImei(), networkAddress);
                                        byte[] byteTime = bytes.Skip(34).Take(4).ToArray(); //31
                                        UInt32 uInt32Time = (UInt32)(byteTime[3] << 24) | (UInt32)(byteTime[2] << 16) | (UInt32)(byteTime[1] << 8) | byteTime[0];
                                        DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                                        UInt16 code = Helper.ToUInt16(bytes, 38);
                                        DateTime[] dtEvent = new DateTime[16];
                                        for (int i = 0; i < 16; i++)
                                        {
                                            if (i == 7)
                                            {
                                                byte[] byteTime1 = bytes.Skip(40 + i * 4).Take(4).ToArray(); //31
                                                UInt32 uInt32Time1 = (UInt32)(byteTime1[3] << 24) | (UInt32)(byteTime1[2] << 16) | (UInt32)(byteTime1[1] << 8) | byteTime1[0];
                                                dtEvent[i] = dt1970.AddSeconds(uInt32Time1);
                                            }
                                            else
                                            {
                                                dtEvent[i] = u32ToBytes(bytes.Skip(40 + i * 4).Take(4).ToArray());
                                            }
                                            //byte[] byteTime1 = bytes.Skip(37 + i*4 ).Take(4).ToArray();
                                            //UInt32 uInt32Time1 = (UInt32)(byteTime1[3] << 24) | (UInt32)(byteTime1[2] << 16) | (UInt32)(byteTime1[1] << 8) | byteTime1[0];
                                            //dtEvent[i] = dt1970.AddSeconds(uInt32Time1);
                                        }
                                        byte[] arrParams = new byte[] { 0x01, 0x02, 0x07, 0x08, 0x0A, 0x12, 0x13, 0x00 };
                                        List<dynamic> records = new List<dynamic>();
                                        for (int i = 0; i < arrParams.Length; i++)
                                        {
                                            if (((byte)(code >> i) & 1) == 1 && dtEvent[i] != DateTime.MinValue)
                                            {
                                                records.Add(MakeAbnormalRecord(objectId, ParamName(arrParams[i]), MessageParam(arrParams[i]), dtEvent[i], 1));
                                            }
                                        }
                                        if (records.Any()) RecordsAcceptor.Instance.Save(records);
                                    }
                                }
                                else
                                {
                                    if (bytes[1] == 0x60)
                                    {
                                        byte[] byteConfig = bytes.Skip(2).Take(bytes.Length - 4).ToArray();
                                        EditNode(BitConverter.ToString(byteConfig));
                                    }
                                }
                                //tsConfig conf = setBytes(bytes);
                                //SendDataToSocket(ModbusControl.Instance.TeleofisWrx(bytes, GetImei()));
                            }
                            else
                            {
                                if ((bytes[0] == 0x0A && bytes[1] == 0x0D) || (bytes[bytes.Length - 2] == 0x0A && bytes[bytes.Length - 1] == 0x0D))
                                {
                                    List<byte> bufferTmp = bytes.ToList();
                                    while (bufferTmp.Count > 5 && (bufferTmp[0] == 0x0A && bufferTmp[1] == 0x0D))
                                    {
                                        bufferTmp.RemoveRange(0, 2);
                                    }
                                    while (bufferTmp.Count > 5 && (bufferTmp[bufferTmp.Count - 2] == 0x0A && bufferTmp[bufferTmp.Count - 1] == 0x0D))
                                    {
                                        bufferTmp.RemoveRange(bufferTmp.Count - 2, 2);
                                    }
                                    bytes = null;
                                    bytes = bufferTmp.ToArray();
                                    if (!Crc.Check(bytes, new Crc16Modbus()))
                                    {
                                        log.Error("контрольная сумма кадра не сошлась");
                                    }
                                    else
                                    {
                                        SendDataToSocket(ModbusControl.Instance.TeleofisWrx(bytes, GetImei()));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception oex)
            {
                log.Error(string.Format("[{0}] поток слушающий сокет остановлен", GetImei()), oex);
                CloseSocket();
            }
        }
       
        public string MessageParam(byte param)
        {
            switch (param)
            {
                case 0x00:
                    return "вскрытие шкафа";
                case 0x01:
                    return "включение/выключение прибора";
                case 0x02:
                    return "коррекция часов прибора";
                case 0x07:
                    return "коррекция тарифного расписания";
                case 0x08:
                    return "коррекция расписания праздничных дней";
                case 0x0A:
                    return "инициализация массива средних мощностей";
                case 0x12:
                    return "вскрытие/закрытие прибора";
                case 0x13:
                    return "перепрограммирование прибора";
                default:
                    return $"Неизвестный параметер: {param}";
            }
        }

        private dynamic MakeAbnormalRecord(Guid objectId, string name, string message, DateTime date, int output)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.id = Guid.NewGuid();
            record.objectId = objectId;
            record.s1 = name;
            record.d1 = output;
            record.s2 = message;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    
        public string ParamName(byte param)
        {
            switch (param)
            {
                case 0x00:
                    return "openBox";
                case 0x01:
                    return "turnOffOn";
                case 0x02:
                    return "beforeAfterCurrectTime";
                case 0x07:
                    return "currectTariff";
                case 0x08:
                    return "currectScheduleHoliday";
                case 0x0A:
                    return "mediumPowerArray";
                case 0x12:
                    return "timeOpenCloseDevice";
                case 0x13:
                    return "timePereprogramming";
                default:
                    return $"параметер 0x:{param:x}";
            }
        }
       
        public DateTime u32ToBytes(byte[] bytes)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFF && bytes[2] == 0xFF && bytes[3] == 0xFF) return DateTime.MinValue;
            UInt32 u32Date = BitConverter.ToUInt32(bytes, 0);
            int year = 2000 + (int)((u32Date >> 26) & 0x3F);//year 6
            int month = (int)((u32Date >> 22) & 0x0F);//mon  4
            //u32Date = 1364791907;
            int day = (int)((u32Date >> 17) & 0x1F);//day   5
            int hour = (int)((u32Date >> 12) & 0x1F);//hour  5
            int minute = (int)((u32Date >> 6) & 0x3F);//min	 6
            int second = (int)((u32Date >> 0) & 0x3F);//sec	 6
            return new DateTime(year, month, day, hour, minute, second);
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
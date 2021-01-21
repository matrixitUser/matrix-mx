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
    class ZigbeePort : PollNode
    {
        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(ZigbeePort));

        public ZigbeePort(dynamic content)
        {
            this.content = content;
        }

        //public int GetPort()
        //{
        //    // return 9013;            
        //    var dcontent = content as IDictionary<string, object>;
        //    int port = 0;
        //    if (dcontent.ContainsKey("port"))
        //        int.TryParse(content.port.ToString(), out port);
        //    return port;
        //}

        //private string GetHost()
        //{
        //    var dcontent = content as IDictionary<string, object>;
        //    if (!dcontent.ContainsKey("host")) return "127.0.0.1";
        //    return content.host.ToString();
        //}

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
            return false;
        }

        //private Socket socket = null;
        //private bool loop = false;
        //private Thread worker;

        private Dictionary<Route, Action<byte[]>> subscriber = new Dictionary<Route, Action<byte[]>>();

        #region Data processing part

        private void DataProcess(IEnumerable<byte> bytes, Action<byte[]> frameProcess)
        {
            while (true)
            {
                var target = bytes.SkipWhile(b => b != 0xAA);
                if (target.Count() < 5) break; //Length too small

                var messageLength = target.ElementAt(1) | ((int)target.ElementAt(2) << 8);

                if ((messageLength > bytes.Count()) || (messageLength > target.Count()))
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

                        if (status == 0x01 || status == 0x03)//CONNECTED
                        {
                            NotifyAll();
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
                    }
                    break;

                default: break;
            }
        }

        #endregion


        private void NotifyAll()
        {
            foreach (var output in RelationManager.Instance.GetOutputs(GetId(), "contains"))
            {
                try
                {
                    NodeManager.Instance.GetById(output.GetEndId()).Notify();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public override string ToString()
        {
            return "порт опроса zb";
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


        //парсинг сообщений от Координатора ИЛИ тут, ИЛИ в subscribe
        public override void Receive(byte[] bytes)
        {
            DataProcess(bytes, FrameProcess);
        }

        public override byte[] GetKeepAlive()
        {
            return new byte[] { 0xAA, 0x05, 0x00, 0x00, 0xAF };
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
                    route.Send(this, pack, dir);
                }
                else //парсинг сообщений от Координатора
                {
                    DataProcess(bytes, FrameProcess);
                }
            });

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
            return true;
        }

        public override void Dispose()
        {
            subscriber.Clear();
            subscriber = null;
        }
    }
}

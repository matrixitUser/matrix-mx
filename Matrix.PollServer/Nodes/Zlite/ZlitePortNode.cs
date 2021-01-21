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

namespace Matrix.PollServer.Nodes.Zlite
{
    /// <summary>
    /// Хранит в себе маршруты Zlite для связи со всеми устройствами
    /// </summary>
    class ZlitePort : PollNode
    {
        private Dictionary<string, dynamic> network;

        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(ZlitePort));

        private Dictionary<byte, List<byte>> ZLRoute = new Dictionary<byte, List<byte>>();

        public ZlitePort(dynamic content)
        {
            this.content = content;
            network = new Dictionary<string, dynamic>();

            var nwk = GetNetwork();
            foreach (var n in nwk)
            {
                network[n.addr] = n;
            }
        }

        public override int GetFinalisePriority()
        {
            return 10;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        //"0058523D|0058523A:0058523D,0058523C,0058523B"
        public List<dynamic> GetNetwork()
        {
            List<dynamic> nwk = new List<dynamic>();
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("network")) return nwk;

            var paths = (content.network.ToString() as string).Split('|');
            foreach (var path in paths)
            {
                var parameters = path.Split(':');

                if (parameters.Length < 1) continue;

                var addr = parameters[0];
                if (addr.Length != (4 * 2)) continue;

                List<UInt32> route = new List<UInt32>();

                if (parameters.Length > 1)
                {
                    try
                    {
                        var hopsArr = parameters[1].Split(',');

                        for (var i = 0; i < hopsArr.Length; i++)
                        {
                            route.Add(UInt32.Parse(hopsArr[i], System.Globalization.NumberStyles.HexNumber));
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
                
                dynamic node = new ExpandoObject();
                node.addr = addr;
                node.route = route;
                nwk.Add(node);
            }

            return nwk;
        }

        public override bool IsFinalNode()
        {
            return false;
        }

        //private Socket socket = null;
        //private bool loop = false;
        //private Thread worker;

        private Dictionary<Route, Action<byte[]>> subscriber = new Dictionary<Route, Action<byte[]>>();

        /*
        private byte GetRouteByChipid(string chipId, out byte[] pipe)
        {
            switch (chipId)
            {
                case "003D00525833570520393131":
                    pipe = new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    return 0x01;

                case "003B00365833570520393131":
                    pipe = new byte[] { 0x02, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00 };
                    return 0x02;

                case "003B004F5833570520393131":
                    pipe = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    return 0x03;

                default:
                    break;
            }
            pipe = new byte[] { };
            return 0x00;
        }*/


        private enum FrameAction
        {
            SendRemote = 1,
            RemoteReceive 
        }

        private enum DestType
        {
            Unicast = 0,
            Multcast,
            Unicast_Routed
        }

        private enum FrameProto
        {
            UDP = 0,
            ICMP
        }
        
        private enum UdpPort
        {
            Uart1 = 1,
            VirtualUart1 = 21
        }

        private byte[] MakePackage(byte[] addr, byte[] body)
        {
            //byte routeN = 0x01;
            //ZLRoute[routeN] = addr.ToList();

            var strAddr = string.Join("", addr.Take(4).Select(s => { return string.Format("{0:X02}", s); }));
            if (!network.ContainsKey(strAddr)) return null;
            var n = network[strAddr];
            List<UInt32> route = n.route;
            byte routeSize = route.Count > 15 ? (byte)15 : (byte)route.Count;

            var pack = new List<byte>();
            pack.Add((byte)FrameAction.SendRemote);
            pack.Add((byte)DestType.Unicast_Routed);
            pack.AddRange(addr);
            pack.Add(routeSize);

            for(int i = 0; i < routeSize; i++)
            {
                pack.AddRange(BitConverter.GetBytes(route[i]).Reverse());
            }
            
            pack.Add((byte)FrameProto.UDP);
            pack.Add((byte)UdpPort.VirtualUart1);//src
            pack.Add((byte)UdpPort.Uart1);//dst

            pack.AddRange(body);        // функция и данные для точки назначения
            
            return pack.ToArray();
        }


        public override byte[] GetKeepAlive()
        {
            return new byte[] { 0x00 };
        }


        #region Data processing part

        private void DataProcess(IEnumerable<byte> bytes, Action<byte[]> frameProcess)
        {
            frameProcess(bytes.ToArray());

            /*while (true)
            {
                if (bytes.Count() < 5) break; //Length too small



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
            }*/
        }

        private void FrameProcess(byte[] message)
        {
            if (message.Length == 0) return;

            FrameAction cmd = (FrameAction)message[0];
            var body = message.Skip(1).ToArray();

            switch (cmd)
            {
                case FrameAction.RemoteReceive:
                    if (body.Length > 1)
                    {
                        DestType dstType = (DestType)body[0];
                        switch(dstType)
                        {
                            case DestType.Unicast:
                                if(body.Length > (1 + 4 + 1))
                                {
                                    byte[] fromAddr = body.Skip(1).Take(4).ToArray();
                                    var strAddr = string.Join("", fromAddr.Select(s => { return string.Format("{0:X02}", s); }));
                                    FrameProto frameProto = (FrameProto)body.Skip(5).FirstOrDefault();
                                    byte[] frameData = body.Skip(6).ToArray();

                                    switch (frameProto)
                                    {
                                        case FrameProto.ICMP:
                                            break;

                                        case FrameProto.UDP:
                                            if(frameData.Length > 2)
                                            {
                                                byte srcPort = frameData[0];
                                                byte dstPort = frameData[1];
                                                byte[] data = frameData.Skip(2).ToArray();

                                                if (!network.ContainsKey(strAddr) || (dstPort != (byte)UdpPort.VirtualUart1)) break;
                                                //var n = network[strAddr];
                                                List<byte> concat = fromAddr.ToList();
                                                concat.AddRange(data);

                                                if (subscriber.Count() > 0)
                                                {
                                                    foreach (var sub in subscriber)
                                                    {
                                                        sub.Value(concat.ToArray());
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                                break;

                            case DestType.Multcast:
                                break;
                        }
                    }
                    break;

                default: break;
            }
        }

        #endregion


        /*
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
        }*/


        public override string ToString()
        {
            return "порт опроса zlite";
        }


        //парсинг сообщений ИЛИ тут, ИЛИ в subscribe
        /*public override void Receive(byte[] bytes)
        {
            DataProcess(bytes, FrameProcess);
        }*/


        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            subscriber[route] = (bytes) =>
            {
                route.Send(this, bytes, Routes.Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, dir) =>
            {
                // ОТ драйвера
                // ДО конечного устройства
                // Driver->Tube->ZLC-> !ZLP! -> ... -> Device
                // Приходит в формате Addr[4] + bytes[...]
                // Необходимо подготовить маршрут и сохранить (на время сессии) в массив
                // ЛИБО каждый раз передавать этот маршрут в составе сообщения
                if (dir == Routes.Direction.FromInitiator)
                {
                    var addr = bytes.Take(4).ToArray();
                    var body = bytes.Skip(4);
                    //var packed = new List<byte> { 0x02 };
                    //packed.AddRange(body);
                    var pack = MakePackage(addr, body.ToArray());
                    if(pack != null)
                    {
                        route.Send(this, pack, dir);
                    }
                }
                // ОТ конечного устройства
                // ДО драйвера
                // Device -> ... -> !ZLP! -> ZLC -> ... -> Driver
                // Ответ от устройства необходимо обработать в соответствии с номером маршрута
                // и отправить в формате ChipID[12] + bytes[...] на ZLC
                else
                {
                    DataProcess(bytes, FrameProcess);
                }
            });

            /*{
                Log(string.Format("открытие порта"));
                var pack = MakePackage(chipId, new byte[] { 0x01 });
                route.Send(this, pack, Routes.Direction.FromInitiator);
            }*/

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

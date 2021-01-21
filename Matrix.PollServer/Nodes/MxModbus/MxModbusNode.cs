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
using Matrix.SurveyServer.Driver.Common.Crc;
using Matrix.PollServer.Nodes.Tube;

namespace Matrix.PollServer.Nodes.MxModbus
{
    class MxModbus : PollNode
    {
        private readonly object locker = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(MxModbus));

        public MxModbus(dynamic content)
        {
            this.content = content;
        }

        public override int GetFinalisePriority()
        {
            return 10;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }

        public bool GetReceiver()
        {
            var dcontent = content as IDictionary<string, object>;
            bool receiver = false;
            if (dcontent.ContainsKey("receiver"))
                bool.TryParse(content.receiver.ToString(), out receiver);
            return receiver;
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
            if (bytes.Count() < 3) return; // 1+ byte(s) and CRC 2 bytes

            if (!Crc.Check(bytes.ToArray(), new Crc16Modbus())) return;//CRC mismatch

            //MESSAGE
            if (frameProcess != null)
            {
                frameProcess(bytes.ToArray());
            }
        }

        private void FrameProcess(byte[] message)
        {
            if (message.Length < 4) return;

            var na = message[0];
            var func = message[1];

            switch (func)
            {
                case 99:
                    var tubes = new List<PollNode>();
                    foreach (var input in RelationManager.Instance.GetInputs(GetId(), "contains"))
                    {
                        var tube = NodeManager.Instance.GetById(input.GetStartId());
                        if (tube is TubeNode)
                        {
                            var networkAddress = (tube as TubeNode).GetNetworkAddress();
                            if (networkAddress != null && networkAddress == na)
                            {
                                tubes.Add(tube);
                            }
                        }
                    }

                    string what = "current";
                    dynamic arg = new ExpandoObject();
                    arg.components = string.Format("{0}", na);//"Current:3;Hour:3:60;Day:3:60;";
                    PollTaskManager.Instance.CreateTasks(what, tubes, arg, PollTask.PRIORITY_AUTO);
                    break;

                default:
                    if (subscriber.Count() > 0)
                    {
                        foreach (var sub in subscriber)
                        {
                            sub.Value(message);
                        }
                    }
                    break;
            }
        }

        #endregion

        //-->(MxModbus)-->(TcpClient etc FINAL)
        private void NotifyAll()
        {
            foreach (var output in RelationManager.Instance.GetOutputs(GetId(), "contains"))
            {
                try
                {
                    var final = NodeManager.Instance.GetById(output.GetEndId());
                    final.Notify();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public override string ToString()
        {
            return "порт опроса mxmodbus";
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
            if (GetReceiver())
                DataProcess(bytes, FrameProcess);
        }

        public override byte[] GetKeepAlive()
        {
            return new byte[] { 0xff, 0x00, 0x40, 0x40 };
        }


        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            subscriber[route] = (bytes) =>
            {
                route.Send(this, bytes, Routes.Direction.ToInitiator);
            };

            route.Subscribe(this, (bytes, dir) =>
            {
                if (dir == Routes.Direction.FromInitiator)//К конечному устройству ОТ драйвера 
                {
                    route.Send(this, bytes, dir);
                }
                else //парсинг сообщений
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

using log4net;
using Matrix.PollServer.Routes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.PollServer.Nodes.Switch
{
    class SwitchNode2 : PollNode
    {
        private readonly ILog log = LogManager.GetLogger(typeof(SwitchNode2));

        public SwitchNode2(dynamic content)
        {
            this.content = content;
        }

        public byte GetNetworkAddress()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("networkAddress")) return 1;

            return (byte)content.networkAddress;
        }

        private Dictionary<int, Action<dynamic>> subscribers = new Dictionary<int, Action<dynamic>>();

        private void DefaultSubscriber(dynamic package)
        {
            switch ((byte)package.code)
            {
                case 1:
                    {
                        break;
                    }
                case 2:
                    var perc = (float)package.body[1] * 100f / 127f;
                    if (perc > 100) perc = 100;
                    log.Debug(string.Format("[{0}] изменение уровня сигнала {1:0.00} %", this, perc));
                    break;
                case 11:
                    var msg = Encoding.UTF8.GetString(package.body);
                    break;

                default: log.Warn(string.Format("[{0}] необработанный пакет код: {1} первые 100 байт {2}", this, package.code, string.Join(",", (package.body as byte[]).Take(100).Select(b => b.ToString("X2"))))); break;
            }
        }

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            bool isPortOpen = false;

            route.Subscribe(this, (bytes, forward) =>
            {
                // log.Debug(string.Format("[{0}] -> [{1}]", this, string.Join(",", bytes.Select(b => b.ToString("X2")))));

                if (forward == Direction.ToInitiator)
                {
                    route.Send(this, MakePackage((byte)port, bytes), forward);
                }
                else
                {
                    dynamic package = PackageParse(bytes);
                    if (!package.success) return;
                }
            });
            return isPortOpen ? Codes.SUCCESS : Codes.MATRIX_NOT_CONNECTED;
        }

        private dynamic PackageParse(byte[] bytes)
        {
            dynamic package = new ExpandoObject();
            package.success = false;
            package.error = "не достаточно данных";
            package.body = bytes;

            if (bytes == null) return package;
            if (bytes.Length < 4) return package;
            if (!CrcCheck(bytes))
            {
                package.error = "контрольная сумма не сошлась";
                return package;
            }

            package.networkAddress = bytes[0];
            package.channel = bytes[3];
            package.command = bytes[4];
            package.body = bytes.Skip(5).Take(bytes.Count() - 5 - 1).ToArray();

            package.success = true;
            package.error = string.Empty;
            return package;
        }

        private byte[] MakeRequest(byte networkAddress, byte channel, byte command, byte[] body)
        {
            var frame = new List<byte>();
            frame.Add(networkAddress);

            var len = 1 + 2 + 2 + body.Length + 1;
            frame.Add(GetLowByte(len));
            frame.Add(GetHighByte(len));
            frame.Add(channel);
            frame.Add(command);
            frame.AddRange(body);
            frame.Add(CrcCalculate(frame));
            return frame.ToArray();
        }

        private byte[] MakePackage(byte channel, byte[] bytes)
        {
            return MakeRequest(GetNetworkAddress(), channel, 0x02, bytes);
        }

        private byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        private byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        private byte CrcCalculate(IEnumerable<byte> bytes)
        {
            byte chck = (byte)bytes.Sum(d => d);
            chck = (byte)((byte)~chck + 1);
            return chck;
        }

        private bool CrcCheck(IEnumerable<byte> bytes)
        {
            int Length = bytes.Count();
            byte crcClc = CrcCalculate(bytes.Take(Length - 1));
            byte crcMsg = bytes.ElementAt(Length - 1);
            return crcClc == crcMsg;
        }

        public override string ToString()
        {
            return string.Format("Switch");
        }
    }
}

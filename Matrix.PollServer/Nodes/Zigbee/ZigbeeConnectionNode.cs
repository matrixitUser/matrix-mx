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
using System.Timers;
using Matrix.PollServer.Storage;

namespace Matrix.PollServer.Nodes.Zigbee
{
    class ZigbeeConnection : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ZigbeeConnection));

        //

        private bool isAsleep = false;
        private System.Timers.Timer tmrAsleep = null;

        private void tmrAsleep_Elapsed(Object source, ElapsedEventArgs e)
        {
            Log(string.Format("{0} заснул по таймауту", GetMac()));
            IsAsleep = true;
            tmrAsleep_Stop();
        }

        private void tmrAsleep_Start()
        {
            if (tmrAsleep == null)
            {
                tmrAsleep = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
                tmrAsleep.Elapsed += tmrAsleep_Elapsed;
                tmrAsleep.Start();
            }
        }

        private void tmrAsleep_Stop()
        {
            if (tmrAsleep != null)
            {
                tmrAsleep.Stop();
                tmrAsleep.Dispose();
                tmrAsleep = null;
            }
        }

        public bool IsAsleep
        {
            get
            {
                return isAsleep;
            }
            set
            {
                if (isAsleep && isAsleep != value)
                {
                    //Notify();
                }
                isAsleep = value;
            }
        }

        //

        public ZigbeeConnection(dynamic content)
        {
            this.content = content;
            isAsleep = IsSleeping();
        }

        //

        public string GetMac()
        {
            var docntent = content as IDictionary<string, object>;
            if (!docntent.ContainsKey("mac")) return "";
            return content.mac.ToString();
        }

        public bool IsSleeping()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("kind")) return false;
            return (content.kind == "ED");
        }

        //

        public override int GetFinalisePriority()
        {
            return 5;
        }

        public override int GetPollPriority()
        {
            return 10;
        }

        public override bool IsFinalNode()
        {
            return false;
        }

        protected override bool OnLock(Route route, PollTask initiator)
        {
            //return (!IsSleeping() || !IsAsleep) && base.OnLock(route);
            return !IsAsleep && base.OnLock(route, initiator);
        }

        public override bool IsLocked()
        {
            //return (IsSleeping() && IsAsleep) || base.IsLocked();
            return IsAsleep || base.IsLocked();
        }

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            log.Debug(string.Format("начата подготовка zb {0}", GetMac()));

            ////сначала таски соединения            
            while (PollTaskManager.Instance.HasTaskForStarter(this))
            {
                var task = PollTaskManager.Instance.GetTaskForStarter(this);
                var what = task.What;
                switch (what.ToLower())
                {
                    default: break;
                }
                task.Destroy();
            }

            if (initiator.Owner == this)
            {
                log.Debug(string.Format("завершена подготовка zb {0} (успех)", GetMac()));
                return 0;
            }

            route.Subscribe(this, (bytes, dir) =>
            {
                if (bytes == null) return;

                log.Trace(string.Format("[{0}] -> [{1}]", this, string.Join(",", bytes.Select(b => b.ToString("X2")))));

                if (dir == Routes.Direction.ToInitiator)//К драйверу ОТ конечного устройства
                {
                    var body = ParsePackage(bytes);
                    if (body != null)
                    {
                        route.Send(this, body, dir);
                    }
                }
                else
                {
                    var pack = MakePackage(bytes);
                    route.Send(this, pack, dir);
                }
            });

            return 0;
        }

        protected override void OnRelease(Route route, int port)
        {

        }


        public override string ToString()
        {
            return GetMac();
        }

        public override void Dispose()
        {

        }

        //

        public void ConnectionStatusUpdate(byte newStatus)
        {
            IsAsleep = (newStatus == 0x02);

            if (newStatus == 0x01)
            {
                log.Debug(string.Format("вышел на связь zb {0}", GetMac()));
                Log(string.Format("{0} вышел на связь", GetMac()));
                tmrAsleep_Start();
            }
            else if (newStatus == 0x02)
            {
                log.Debug(string.Format("разорвал соединение zb {0}", GetMac()));
                Log(string.Format("{0} разорвал соединение", GetMac()));
                tmrAsleep_Stop();
            }
            else
            {
                log.Debug(string.Format("на связи zb {0} статус={1}", GetMac(), newStatus));
                Log(string.Format("{0} на связи", GetMac()));
            }
        }

        //pack = MAC + body
        private byte[] MakePackage(byte[] body)
        {
            var macStr = GetMac();
            var macArr = new byte[8];

            if (macStr.Length != 16) throw new Exception("Неправильный MAC-адрес");

            for (var i = 0; i < 8; i++)
            {
                macArr[i] = byte.Parse(string.Join("", macStr.Skip(i * 2).Take(2)), System.Globalization.NumberStyles.HexNumber);
            }

            var pack = new List<byte>();
            pack.AddRange(macArr);
            pack.AddRange(body);
            return pack.ToArray();
        }

        //возвращает body от pack, если совпадает mac
        private byte[] ParsePackage(byte[] pack)
        {
            if (pack.Length > 8)
            {
                var macArr = pack.Take(8).ToArray();
                var body = pack.Skip(8).ToArray();
                //
                UInt64 mac64 = 0;
                for (var i = 0; i < 8; i++)
                {
                    mac64 <<= 8;
                    mac64 |= macArr[i];
                }
                var macStr = string.Format("{0:X16}", mac64);
                //
                if (GetMac().ToUpper() == macStr)
                {
                    return body;
                }
            }

            return null;
        }
    }
}
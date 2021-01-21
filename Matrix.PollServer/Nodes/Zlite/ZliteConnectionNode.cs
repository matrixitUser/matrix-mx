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

namespace Matrix.PollServer.Nodes.Zlite
{
    /// <summary>
    /// Хранит в себе Addr конечного устройства Zlite 
    /// </summary>
    class ZliteConnection : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ZliteConnection));

        //

        public ZliteConnection(dynamic content)
        {
            this.content = content;
            //isAsleep = IsSleeping();
        }

        //

        public string GetAddr()
        {
            var docntent = content as IDictionary<string, object>;
            if (!docntent.ContainsKey("addr")) return "";
            return content.addr.ToString();
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

        protected override int OnPrepare(Route route, int port, PollTask initiator)
        {
            log.Debug(string.Format("начата подготовка zlite {0}", GetAddr()));

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
                log.Debug(string.Format("завершена подготовка zlite {0} (успех)", GetAddr()));
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
            return GetAddr();
        }

        public override void Dispose()
        {

        }
        
        //pack = MAC + body
        private byte[] MakePackage(byte[] body)
        {
            var ciStr = GetAddr();
            var ciArr = new byte[4];

            if (ciStr.Length != (4 * 2)) throw new Exception("Неправильный адрес");

            for (var i = 0; i < 4; i++)
            {
                ciArr[i] = byte.Parse(string.Join("", ciStr.Skip(i * 2).Take(2)), System.Globalization.NumberStyles.HexNumber);
            }

            var pack = new List<byte>();
            pack.AddRange(ciArr);
            pack.AddRange(body);
            return pack.ToArray();
        }

        //возвращает body от pack, если совпадает addr
        private byte[] ParsePackage(byte[] pack)
        {
            if (pack.Length > 8)
            {
                var ciArr = pack.Take(4).ToArray();
                var body = pack.Skip(4).ToArray();
                //
                var ciStr = string.Join("", ciArr.Select(s =>
                {
                    return string.Format("{0:X02}", s);
                }));
                //
                if (GetAddr().ToUpper() == ciStr)
                {
                    return body;
                }
            }

            return null;
        }
    }
}
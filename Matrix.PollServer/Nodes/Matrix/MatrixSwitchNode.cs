using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;

namespace Matrix.PollServer.Nodes.Matrix
{
    class MatrixSwitchNode : PollNode
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MatrixSwitchNode));

        public MatrixSwitchNode(dynamic content)
        {
            this.content = content;
        }

        public override Guid GetId()
        {
            return Guid.Parse(content.id.ToString());
        }

        protected override int OnPrepare(Routes.Route route, int port, PollTask initiator)
        {
            var isOpen = false;

            route.Subscribe(this, (bytes, dir) =>
            {
                if (dir == Routes.Direction.ToInitiator)
                {
                    ParsePackage(bytes, pack =>
                    {
                        if (!pack.success)
                        {
                            log.Debug(string.Format("ошибка при обработке данных в свитче: {0}", pack.error));
                            return;
                        }

                        if (pack.channel != port) return;

                        if (pack.cmd == 3)
                        {
                            byte[] clear = pack.body;
                            log.Debug(string.Format("[Switch] {0} {1}", dir == Routes.Direction.FromInitiator ? "->" : "<-", string.Join(",", clear.Select(b => b.ToString("X2")))));
                            route.Send(this, clear, dir);
                        }

                        //0x00 - OK; 0x82 - другой порт открыт?; 0x83 - порт УЖЕ открыт - OK
                        if (pack.cmd == 1 && (pack.body[0] == 0x00 || pack.body[0] == 0x83 || pack.body[0] == 0x82))
                        {
                            log.Debug(string.Format("[Switch] порт {0} открыт", port));
                            isOpen = true;
                        }
                    });
                }
                else
                {
                    byte[] pack = MakePackage(GetNA(), (byte)port, 0x02, bytes);
                    route.Send(this, pack, dir);
                    log.Trace(string.Format("[Switch] {0} {1}", dir == Routes.Direction.FromInitiator ? "->" : "<-", string.Join(",", pack.Select(b => b.ToString("X2")))));
                }
            });

            //команда на открытие порта
            route.Send(this, MakePackage(GetNA(), (byte)port, 0x01, new byte[] { }), Routes.Direction.FromInitiator);
            var tmt = 5000;
            while (!isOpen && tmt > 0)
            {
                tmt -= 100;
                Thread.Sleep(100);
            }

            if (isOpen)
            {
                log.Debug("свитч порт открыт");
            }
            else
            {
                log.Debug("свитч порт закрыт");
            }

            return isOpen ? Codes.SUCCESS : Codes.MATRIX_NOT_CONNECTED;
        }

        protected override void OnRelease(Routes.Route route, int port)
        {
            route.Send(this, MakePackage(GetNA(), (byte)port, 0x00, new byte[] { }), Routes.Direction.FromInitiator);
        }

        private byte GetNA()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("networkAddress")) return 1;
            return (byte)content.networkAddress;
        }

        private byte CalcCrc(byte[] bytes)
        {
            return (byte)(0 - bytes.Sum(b => b));
        }

        private bool CheckCrc(byte[] bytes)
        {
            return (byte)(bytes.Sum(b => b)) == 0;
        }

        private byte[] MakePackage(byte na, byte channel, byte cmd, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(na);
            bytes.Add(channel);
            bytes.Add(cmd);
            bytes.AddRange(body);
            bytes.InsertRange(1, BitConverter.GetBytes((short)(bytes.Count + 2 + 1)));
            bytes.Add(CalcCrc(bytes.ToArray()));
            return bytes.ToArray();
        }

        private void ParsePackage(byte[] bytes, Action<dynamic> packProcessor)
        {
            var rest = bytes;

            do
            {
                dynamic package = new ExpandoObject();
                package.success = true;
                if (rest == null || rest.Length < 5)
                {
                    package.success = false;
                    package.error = "не достаточно данных";
                    return;
                }

                package.na = rest[0];
                package.len = BitConverter.ToInt16(rest, 1);
                package.channel = rest[3];
                package.cmd = rest[4];

                package.body = rest.Skip(5).Take((int)package.len - (5 + 1)).ToArray();
                packProcessor(package);

                rest = rest.Skip((int)package.len).ToArray();
            } while (rest.Any());

            //if (!CheckCrc(bytes))
            //{
            //    package.success = false;
            //    package.error = "не сошлась контрольная сумма";
            //    return package;
            //}            
        }
    }
}

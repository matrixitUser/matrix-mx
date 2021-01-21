using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.PollServer.Routes;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Matrix.PollServer.Nodes.Csd
{
    class CsdConnectionNode : ConnectionNode
    {
        private const int CALL_TIMEOUT = 40000;
        private const int NO_CARRIER_TIMEOUT = 500;
        private const int IDLE_TIMEOUT = 1000;
        private const int COMMAND_MODE_TIMEOUT = 3000;
        private const int CALL_ATTEMPTS = 3;
        private const int REST_TIME = 1 * 60 * 1000;

        private static readonly ILog log = LogManager.GetLogger(typeof(CsdConnectionNode));

        public CsdConnectionNode(dynamic content)
        {
            this.content = content;
        }

        public override Guid GetId()
        {
            return Guid.Parse((string)content.id);
        }


        ///// <summary>
        ///// получает окна - часы, в которые возможен опрос
        ///// </summary>
        ///// <returns></returns>
        //private int[] GetWindows()
        //{
        //    var dcontent = content as IDictionary<string, object>;
        //    if (!dcontent.ContainsKey("windows")) return new int[] { 13 };
        //    var arr = (content.windows as IEnumerable<object>).Select(i => int.Parse(i.ToString())).ToArray();
        //    return arr;
        //}

        public override int GetPollPriority()
        {
            return 5;
        }

        public string GetPhone()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("phone")) return "";
            return content.phone.ToString();
        }

        private IEnumerable<string> GetCommands()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("commands") || string.IsNullOrEmpty(dcontent["commands"].ToString()))
            {
                yield return "AT+CBST=71,0,1";
                yield break;
            }
            foreach (string cmd in content.commands.Split('\n'))
            {
                yield return cmd;
            }
            //  return new string[] { "AT" };
        }

        private int GetCallTimeout()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("callTimeout"))
            {
                return 30 * 1000;
            }
            return int.Parse(content.callTimeout.ToString()) * 1000;
        }

        protected override int OnPrepare(Route route, int port, PollTask task)
        {
            //string phone = GetPhone();
            //var regex = new Regex(@"");
            //if (regex.IsMatch(phone))
            //{

            //}

            ////проверка по приоритету на попадание в окно
            //if (initiator.Priority < PollTask.PRIORITY_USER)
            //{
            //    //var now = DateTime.Now.Hour;
            //    if (!InWindow(initiator.CreationDate))// GetWindows().Contains(initiator.CreationDate.Hour))
            //    {
            //        Log(string.Format("окно для опроса закрыто (окна: {0})", string.Join(",", GetWindows())));
            //        log.Debug(string.Format("окно для опроса закрыто (окна: {0})", string.Join(",", GetWindows())));
            //        return Codes.CANT_CALL_WINDOW_CLOSED;
            //    }
            //    else
            //    {
            //        Log(string.Format("окно для опроса открыто (окна: {0})", string.Join(",", GetWindows())));
            //    }
            //}

            //звонок, если автоопрос, то ждем после неудачного звонка
            var res = Call(route, port);

            //  CheckAvailability(res == 0, task.Priority);
            return res;
        }

        //public bool InWindow(DateTime date)
        //{
        //    return GetWindows().Contains(date.Hour);
        //}

        private int Call(Route route, int port)
        {
            log.Debug(string.Format("начался звонок на номер {0}", GetPhone()));

            var commandMode = true;

            var buffer = new List<byte>();

            var com = route.GetLast();
            var comName = "";
            if (com != null)
            {
                comName = com.ToString();
            }

            route.Subscribe(this, (bytes, dir) =>
            {
                log.Trace(string.Format("[{0}] {1} [{2}]", GetPhone(), dir == Direction.FromInitiator ? "->" : "<-", string.Join(",", bytes.Select(b => b.ToString("X2")))));
                if (commandMode && dir == Direction.ToInitiator)
                {
                    buffer.AddRange(bytes);
                    return;
                }
                route.Send(this, bytes, dir);
            });

            //цепочка at
            Func<string, long, string> at = (command, initialTimeout) =>
            {
                Thread.Sleep(300);
                log.Debug(string.Format("[{0}] AT->{1}", GetPhone(), command));
                Log(string.Format("AT>{0}", command));
                commandMode = true;
                //string wrap = "";
                //if (command == "+++")
                //    wrap = command;
                //else
                //    wrap = string.Format("{0}\r\n", command);

                string wrap = string.Format("{0}\r\n", command);

                var req = Encoding.ASCII.GetBytes(wrap);
                buffer.Clear();
                route.Send(this, req, Direction.FromInitiator);

                var answer = "";
                var step = 100;
                var timeout = initialTimeout;
                while (timeout > 0)
                {
                    if (buffer.Count > 0)
                    {
                        var copy = buffer.ToArray();
                        buffer.Clear();
                        answer = Encoding.ASCII.GetString(copy);
                        break;
                    }
                    timeout -= step;
                    Thread.Sleep(step);
                }

                //  if (command == "+++") Thread.Sleep(1000);

                if (answer.ToUpper().Contains("CONNECT")) commandMode = false;
                log.Debug(string.Format("[{0}] AT<-{1}", GetPhone(), answer.Replace('\r', ' ').Replace('\n', ' ')));
                Log(string.Format("{0}>{1}", comName, answer.Replace('\r', ' ').Replace('\n', ' ')));
                return answer;
            };

            foreach (var command in GetCommands())
            {
                var a1 = at(command, 1000);
                if (!a1.ToUpper().Contains("OK"))
                {
                    // log.Debug(string.Format("завершен звонок на номер {0} (неудача)", GetPhone()));
                    return Codes.CANT_CALL_NO_AT_RESPONSE;
                }
            }

            int retriesCount = CALL_ATTEMPTS;
            for (int i = 0; i < retriesCount; i++)
            {
                var tm = new Stopwatch();
                tm.Start();
                var a2 = at(string.Format("ATD{0}", GetPhone()), GetCallTimeout());
                if (a2.ToUpper().Contains("CONNECT"))
                {
                    log.Debug(string.Format("завершен звонок на номер {0} (успех)", GetPhone()));
                    return Codes.SUCCESS;
                }
                if (a2.ToUpper().Contains("BUSY"))
                {
                    log.Debug(string.Format("завершен звонок на номер {0} (неудача - Busy)", GetPhone()));
                    return Codes.CANT_CALL_BUSY;
                }
                //if (a2.ToUpper().Contains("NO CARRIER"))
                //{
                //    tm.Stop();
                //    //var err = at("AT+CEER", 2000);
                //    //if (killCodes.Any(k => err.ToUpper().Contains(k)))
                //    if (tm.ElapsedMilliseconds < NO_CARRIER_TIMEOUT)
                //    {
                //        log.Warn(string.Format("перезагрузка модема для {0}", GetPhone()));
                //        at("AT+CFUN=1,1", 2000);
                //        Log("рестарт модема");
                //        Thread.Sleep(30000);
                //        at("ATE0", 500);
                //    }
                //}
                Thread.Sleep(1000);
            }

            SetEnabled(false);
            //Api.ApiProxy.Instance.SaveNode(content);

            log.Debug(string.Format("завершен звонок на номер {0} (неудача)", GetPhone()));
            return Codes.CANT_CALL_NO_CARRIER;
        }

        private string[] killCodes = new string[] { "8,41,", "8,1," };

        public void SetEnabled(bool enabled)
        {
            content.enabled = enabled;
        }
        public override bool IsEnabled()
        {
            var dcontent = content as IDictionary<string, object>;
            if (!dcontent.ContainsKey("enabled")) return true;
            return (bool)content.enabled;
        }

        protected override void OnRelease(Route route, int port)
        {

            var commandMode = true;

            var buffer = new List<byte>();

            route.Subscribe(this, (bytes, dir) =>
            {
                if (commandMode && dir == Direction.ToInitiator)
                {
                    lock (buffer)
                    {
                        buffer.AddRange(bytes);
                    }
                    return;
                }
                route.Send(this, bytes, dir);
            });

            //цепочка at
            Func<string, long, string> at = (command, timeout) =>
            {
                commandMode = true;
                Log(string.Format("AT>{0}", command));
                string wrap = "";
                if (command == "+++")
                    wrap = command;
                else
                    wrap = string.Format("{0}\r\n", command);

                var req = Encoding.ASCII.GetBytes(wrap);
                route.Send(this, req, Direction.FromInitiator);

                var answer = "";
                var step = 100;
                var initialTimeout = timeout;
                while (timeout > 0)
                {
                    lock (buffer)
                    {
                        if (buffer.Any())
                        {
                            var copy = buffer.ToArray();
                            buffer.Clear();
                            answer = Encoding.ASCII.GetString(copy);
                            break;
                        }
                    }
                    timeout -= step;
                    Thread.Sleep(step);
                }

                if (command == "+++") Thread.Sleep(1000);
                Log(string.Format("AT>{0}", answer.Replace('\r', ' ').Replace('\n', ' ')));
                return answer;
            };

            var counter = 0;
            while (!at("+++", 2000).Contains("OK") && counter++ < 3) Thread.Sleep(500);
            counter = 0;
            while (!at("ATH", 1000).Contains("OK") && counter++ < 3) Thread.Sleep(500);
            Log("звонок завершен");
        }

        public override string ToString()
        {
            return GetPhone();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using Matrix.Common.Infrastructure.Protocol.Messages;
using System.Threading;

namespace Matrix.Common.Infrastructure.Protocol
{
    /// <summary>
    /// точка соединения на серверной стороне
    /// </summary>
    public class ServerConnectionPoint : ConnectionPoint
    {
        const string VERSION = "2.6.1";

        private readonly System.Timers.Timer pingTimer;

        public ServerConnectionPoint(Socket socket, string idleThreadName)
            : base(new JsonSerilizer(), idleThreadName)
        {
            this.socket = socket;
            base.MessageRecieved += OnMessageRecieved;
            pingTimer = new System.Timers.Timer();
            //pingTimer.Interval = PingRequest.PING_TIMEOUT;
            pingTimer.Elapsed += OnPingTimerElapsed;
            //pingTimer.Start();
        }

        private void OnPingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!lastPingSuccess)
            {
                CloseConnection();
            }
            lastPingSuccess = false;
        }

        /// <summary>
        /// общение между точками соединения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
            if (e.Message is DoMessage)
            {
                var message = e.Message as DoMessage;
                switch (message.What)
                {
                    case "connect":
                        {
                            var arg = (IDictionary<string, object>)message.Argument;
                            ConnectionId = Guid.Parse(arg["connection-id"].ToString());
                            var version = (string)arg["version"];
                            var response = new DoMessage(message.Id, "connect", new Dictionary<string, object> 
                            { 
                                { "connection-id", ConnectionId }, 
                                { "is-version-acceptable", IsClientVersionAcceptable(version) },
                                { "server-date", DateTime.Now }
                            }, new Guid[] { });
                            //var response = new ConnectResponse(e.Message.Id, request.ConnectionId, IsClientVersionAcceptable(request.Version));
                            SendMessage(response);
                            break;
                        }
                }
            }
            //хендшейк между точками
            //if (e.Message is ConnectRequest)
            //{
            //    var request = e.Message as ConnectRequest;
            //    ConnectionId = request.ConnectionId;
            //    var response = new ConnectResponse(e.Message.Id, request.ConnectionId, IsClientVersionAcceptable(request.Version));
            //    SendMessage(response);
            //}
            //if (e.Message is PingRequest)
            //{
            //    lastPingSuccess = true;
            //    var response = new PingResponse(e.Message.Id);
            //    SendMessage(response);
            //}
        }

        private bool IsClientVersionAcceptable(string clientVersion)
        {
            var normalClientVersion = VersionParse(clientVersion);
            var normalServerVersion = VersionParse(VERSION);

            return normalClientVersion.Item1 == normalServerVersion.Item1 &&
                normalClientVersion.Item2 == normalServerVersion.Item2 &&
                normalClientVersion.Item3 == normalServerVersion.Item3;
        }

        private Tuple<int, int, int> VersionParse(string version)
        {
            var parts = version.Split('.');
            int a = 0, b = 0, c = 0;
            if (parts.Length >= 3)
            {
                int.TryParse(parts[0], out a);
                int.TryParse(parts[1], out b);
                int.TryParse(parts[2], out c);
            }
            return new Tuple<int, int, int>(a, b, c);
        }

        private bool lastPingSuccess = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Matrix.Web.Host
{
    public class BrowserConnectionPoint : PersistentConnection
    {
        public event EventHandler<WebMessageEventArgs> MessageReceived;
        public void RaiseMessageReceived(WebMessage message, IRequest request)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, new WebMessageEventArgs(message, request));
            }
        }

        public void SendMessage(WebMessage message)
        {
            BrowserConnectionPointAcceptor.SendMessage(message, connectionId);
        }

        public event EventHandler Disconnected;
        public void RaiseDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        private readonly string connectionId;

        public string ConnectionId
        {
            get { return connectionId; }
        }

        public BrowserConnectionPoint(string connectionId)
        {
            this.connectionId = connectionId;
        }
    }

    public class WebMessageEventArgs : EventArgs
    {
        public WebMessage Message { get; private set; }
        public IRequest Request { get; private set; }

        public WebMessageEventArgs(WebMessage message, IRequest request)
        {
            Message = message;
            Request = request;
        }
    }

    public class RawWebMessageEventArgs : EventArgs
    {
        public WebMessage Message { get; private set; }
        public IRequest Request { get; private set; }
        public string ConnectionId { get; private set; }

        public RawWebMessageEventArgs(WebMessage message, IRequest request, string connectionId)
        {
            Message = message;
            ConnectionId = connectionId;
            Request = request;
        }
    }
}

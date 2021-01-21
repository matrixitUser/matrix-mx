using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Matrix.Web.Host.Transport
{
    /// <summary>
    /// служит для уведомления клиентов о наступлении событий
    /// </summary>
    public class SignalRConnection : PersistentConnection
    {
        public SignalRConnection()
        {
        }

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return base.OnConnected(request, connectionId);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var context = GlobalHost.ConnectionManager.GetConnectionContext<SignalRConnection>();
            context.Connection.Send(connectionId, data);            
            return base.OnReceived(request, connectionId, data);
        }

        /// <summary>
        /// имя ключа для хранения например в сессии
        /// </summary>
        public const string SIGNAL_CONNECTION_ID = "signalConnectionId";

        public static void RaiseEvent(object message, string connectionId)
        {
            var context = GlobalHost.ConnectionManager.GetConnectionContext<SignalRConnection>();
            context.Connection.Send(connectionId, message);
        }
    }
}

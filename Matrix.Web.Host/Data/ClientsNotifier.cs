using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Data
{
    class ClientsNotifier
    {
        public void Notify(string what, IEnumerable<Guid> objectIds, dynamic message)
        {
            var sessions = CacheRepository.Instance.GetSessions();
            foreach (var session in sessions)
            {
                var dsess = session as IDictionary<string, object>;
                if (dsess.ContainsKey("subscribers"))
                {

                }

                var subs = session.subscribers;
                foreach (var sub in subs)
                {
                    if (sub.what == what)
                    {
                        if (objectIds.Any(o => sub.ids.Contains(o)))
                        {

                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        public void NotifyAll(dynamic message)
        {
            var sessions = CacheRepository.Instance.GetSessions();
            foreach (var session in sessions)
            {
                var bag = session as IDictionary<string, object>;
                if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID))
                {
                    //log.Debug(string.Format("сессия {0} не содержит сигналр подписки", session.id));
                    continue;
                }
                //log.Debug(string.Format("отправка логов {0} шт, сессия {1}", filtered.Count, session.id));

                var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                SignalRConnection.RaiseEvent(message, connectionId);
            }
        }

        static ClientsNotifier() { }
        private ClientsNotifier() { }

        private static readonly ClientsNotifier instance = new ClientsNotifier();
        public static ClientsNotifier Instance
        {
            get
            {
                return instance;
            }
        }
    }
}

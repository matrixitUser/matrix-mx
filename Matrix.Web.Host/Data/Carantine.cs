using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Data
{
    class Carantine
    {
        private const int WAITER_TIMEOUT = 3000;

        private static readonly ILog log = LogManager.GetLogger(typeof(Carantine));
        private readonly HashSet<Guid> waiters = new HashSet<Guid>();
        Dictionary<Guid, int> waitersState = new Dictionary<Guid, int>();
        private readonly System.Timers.Timer timer = new System.Timers.Timer();

        public void Push(Guid id)
        {
            Push(new Guid[] { id });
        }
        public void Push(Guid id, int state)
        {
            Push(new Guid[] { id }, state);
        }
        public void Push(IEnumerable<Guid> ids)
        {
            if (ids == null || !ids.Any()) return;
            lock (waiters)
            {
                foreach (var id in ids)
                {
                    waiters.Add(id);
                }
            }
        }
        public void Push(IEnumerable<Guid> ids, int state)
        {
            if (ids == null || !ids.Any()) return;
            lock (waiters)
            {
                foreach (var id in ids)
                {
                    waiters.Add(id);
                    if (waitersState.ContainsKey(id)) waitersState[id] = state;
                    else waitersState.Add(id, state);
                }
            }
        }
        private void NotifyAll(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (waiters)
            {
                if (waiters.Count == 0) return;

                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var session in sessions)
                {
                    var dsession = session as IDictionary<string, object>;
                    if (!dsession.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var filtered = StructureGraph.Instance.Filter(waiters, Guid.Parse((string)session.userId));
                    
                    if (filtered.Any())
                    {
                        dynamic notifyMsg = Helper.BuildMessage("ListUpdate");
                        Dictionary<Guid, int> filteredWhithState = new Dictionary<Guid, int>();
                        foreach(var id in filtered)
                        {
                            if(waitersState.ContainsKey(id))
                                filteredWhithState.Add(id, waitersState[id]);
                        }
                        notifyMsg.body.ids = filtered.ToArray();
                        notifyMsg.body.idswithstate = filteredWhithState.ToArray();
                        //log.Debug(string.Format("уведомление по {0} объектам для сессии {1}", filtered.Count(), session.id));    
                        try
                        {
                            var connectionId = dsession[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                            SignalRConnection.RaiseEvent(notifyMsg, connectionId);
                        }
                        catch(Exception ex)
                        {
                            log.Error("Не смог отправить по signalR", ex);
                        }
                    }
                }
                waiters.Clear();
                waitersState.Clear();
            }
        }

        private Carantine()
        {
            timer.Elapsed += NotifyAll;
            timer.Interval = WAITER_TIMEOUT;
            timer.Start();
        }

        static Carantine() { }
        private static readonly Carantine instance = new Carantine();
        public static Carantine Instance
        {
            get
            {
                return instance;
            }
        }
    }
}

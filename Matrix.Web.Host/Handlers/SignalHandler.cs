using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Handlers
{
    class SignalHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SignalHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("signal");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            var bag = session as IDictionary<string, object>;
            if (bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID))
            {
                bag[SignalRConnection.SIGNAL_CONNECTION_ID] = message.body.connectionId;
            }
            else
            {
                bag.Add(SignalRConnection.SIGNAL_CONNECTION_ID, message.body.connectionId);
            }
            Data.CacheRepository.Instance.SaveSession(session, Guid.Parse((string)session.userId));
            log.Debug(string.Format("подписка на события {0}", message.body.connectionId));

            var answer = Helper.BuildMessage("signal-binded");
            return answer;
        }
    }
}

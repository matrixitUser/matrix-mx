using Matrix.Web.Host.Data;
using Matrix.Web.Host.Transport;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Matrix.Web.Host
{
    class Watchdog
    {
        private const int WATCHDOG_INTERVAL_SEC = 4 * 60;
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private Timer intervalTimer;
        private Watchdog()
        {
            int watchdogIntervalSec;
            if (!int.TryParse(ConfigurationManager.AppSettings["watchdog-interval"]?.ToLower().Trim(), out watchdogIntervalSec) || watchdogIntervalSec < 0)
            {
                watchdogIntervalSec = WATCHDOG_INTERVAL_SEC;
            }
            if (watchdogIntervalSec == 0) return;

            intervalTimer = new Timer(watchdogIntervalSec * 1000);
            intervalTimer.Elapsed += new ElapsedEventHandler(OnWatchdogReset);
            intervalTimer.Enabled = true;
        }

        private static Watchdog instance = null;
        public static Watchdog Instance()
        {
            if (instance == null)
            {
                instance = new Watchdog();
            }
            return instance;
        }

        private void OnWatchdogReset(object sender, ElapsedEventArgs e)
        {
            try
            {
                dynamic head = new ExpandoObject();
                head.what = "ping";
                dynamic body = new ExpandoObject();
                dynamic message = new ExpandoObject();
                message.head = head;
                message.body = body;
                //
                var sessions = CacheRepository.Instance.GetSessions();
                if (sessions == null)
                {
                    throw new Exception("не удалось получить список сессий");
                }

                int count = 0;
                foreach (var sess in sessions)
                {
                    var bag = sess as IDictionary<string, object>;
                    if (bag == null)
                    {
                        continue;
                    }
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID) || bag[SignalRConnection.SIGNAL_CONNECTION_ID] == null) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);
                    count++;
                }
                logger.Trace("Пинг отправлен по {0} из {1} сессий", count, sessions.Count());
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Ошибка при отправке пинга: {0}", ex.Message);
            }
        }
    }
}

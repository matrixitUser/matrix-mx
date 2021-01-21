using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Handlers;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Transport
{
    /// <summary>
    /// обработчик поступающих сообщений.
    /// здесь проходит обработка спец сообщений (например авторизация)
    /// сообщение - JSON объект, состоящий из заголовка и тела
    /// заголовок состоит из идентификатора сесии (sessionId) и имени операции (what)
    /// в тело сообщения произвольный объект
    /// </summary>
    static class MessageProccessor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MessageProccessor));

        public static async Task<dynamic> Process(dynamic message)
        {
            try
            {
                var head = message.head as IDictionary<String, object>;
                string what = message.head.what;
                log.DebugFormat(string.Format(@"обработка сообщения: {0}", what));
                var handler = HandlerManager.Instance.Get(what);

                if (handler == null)
                {
                    var ans = Helper.BuildMessage("error");
                    ans.body.message = string.Format("не найден обработчик для сообщений типа {0}", what);
                    return ans;
                }

                //хук для сообщений авторизации

                var dmessage = message as IDictionary<string, object>;

                if (dmessage.ContainsKey("head"))
                {
                    var dhead = message.head as IDictionary<string, object>;
                    if (!dhead.ContainsKey("sessionId") && what.StartsWith("auth"))
                    {
                        var foobar = await handler.Handle(null, message);
                        return foobar;
                    }
                }

                Guid sessionId = Guid.Parse((string)message.head.sessionId);
                //var session = Data.StructureGraph.Instance.GetSession(sessionId);// SessionManager.Instance.Get(sessionId);
                var session = Data.CacheRepository.Instance.GetSession(sessionId);

                if (session == null)
                {
                    var ans = Helper.BuildMessage("error");
                    ans.body.message = "сессия не найдена";
                    log.Warn($"сессия {(sessionId.ToString() ?? "<NULL>")} не найдена");
                    return ans;
                }

                log.Debug(string.Format("начата обработка сообщения {0}", what));
                var sw = new Stopwatch();
                sw.Start();
                var answer = await handler.Handle(session, message);
                sw.Stop();
                log.Debug(string.Format("сообщение {0} обработано за {1} мс", what, sw.ElapsedMilliseconds));
                return answer;
            }
            catch (Exception ex)
            {                
                log.Error(string.Format("ошибка при обработке сообщения"),ex);
                var ans = Helper.BuildMessage("error");
                ans.body.message = "ошибка при обработке запроса";
                ans.body.description = ex.Message;
                return ans;
            }
        }

        //public void BindConnection()
        //{
        //    //var context = GlobalHost.ConnectionManager.GetConnectionContext<SignalRConnection>();
        //    //context.Connection.Send(connectionId, message);
        //}

        //private MessageProccessor()
        //{

        //}

        //private static MessageProccessor instance = null;
        //public static MessageProccessor Instance
        //{
        //    get
        //    {
        //        if (instance == null) instance = new MessageProccessor();
        //        return instance;
        //    }
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.SignalingServer.Handlers;
using Newtonsoft.Json.Linq;

namespace Matrix.SignalingServer.Transport
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
                
                log.Debug(string.Format("начата обработка сообщения {0}", what));
                var sw = new Stopwatch();
                sw.Start();
                var answer = await handler.Handle(null, message);
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

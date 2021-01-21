using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Data
{
    class NodeBackgroundProccessor
    {
        private const int PROCCESS_TIMEOUT = 500;

        private const int ABNORMAL_LIMIT = 30;
        private const int DAY_LIMIT = 1;

        private static readonly ILog log = LogManager.GetLogger(typeof(NodeBackgroundProccessor));

        private readonly List<dynamic> tokens = new List<dynamic>();

        public void AddTokens(IEnumerable<dynamic> news)
        {
            lock (tokens)
            {
                tokens.AddRange(news);
            }
        }

        public void ReSet()
        {
            loop = true;
            var thread = new Thread(Idle);
            thread.IsBackground = true;
            thread.Start();
        }

        private bool loop = true;
        private void Idle()
        {
            while (loop)
            {
                try
                {
                    Thread.Sleep(PROCCESS_TIMEOUT);

                    IEnumerable<dynamic> copy;
                    lock (tokens)
                    {
                        if (tokens.Count == 0) continue;
                        copy = tokens.ToList();
                        tokens.Clear();
                    }
                    Proccess(copy);

                }
                catch (ThreadAbortException tae)
                {
                    return;
                }
                catch (Exception ex)
                {
                    log.Error("ошибка при пост-обработке записей, записи обработаны не полностью", ex);
                }
            }
        }

        private readonly List<ITokenHandler> handlers = new List<ITokenHandler>();
        public void AddHandler(ITokenHandler handler)
        {
            handlers.Add(handler);
        }

        private void Proccess(IEnumerable<dynamic> part)
        {
            List<Guid> starts = new List<Guid>();

            foreach (var handler in handlers)
            {
                handler.Handle(part);
            }

            foreach (var userTokens in part.GroupBy(p => p.userId))
            {
                foreach (var token in userTokens)
                {
                    try
                    {
                        var sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        var dtoken = token as IDictionary<string, object>;
                        starts.Add(Guid.Parse(token.start.id.ToString()));
                        switch ((string)token.action)
                        {
                            case "save":
                                if (dtoken.ContainsKey("end"))
                                {                                
                                    StructureGraph.Instance.SavePair(token.start, token.end, token.rel, userTokens.Key);
                                }
                                else
                                {
                                    StructureGraph.Instance.SaveSingle(token.start, userTokens.Key);
                                }
                                break;
                            case "delete":
                                StructureGraph.Instance.DeleteNode(Guid.Parse(token.start.id.ToString()), Guid.Parse(userTokens.Key.ToString()));
                                break;
                        }
                        sw.Stop();
                        log.Info(string.Format("токен обработан за {0} мс", sw.ElapsedMilliseconds));
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("ошибка при сохранении токена"), ex);
                    }
                }
            }

            ////рассылка заинтересованным
            //var waiters = starts.Distinct();
            //var tubes = Data.StructureGraph.Instance.GetRelatedTubeIds(waiters);
            //var sessions = StructureGraph.Instance.GetSessions();
            //foreach (var session in sessions)
            //{
            //    var dsession = session as IDictionary<string, object>;
            //    var filtered = StructureGraph.Instance.Filter(tubes, Guid.Parse((string)session.User.id));
            //    if (filtered.Any())
            //    {
            //        dynamic notifyMsg = Helper.BuildMessage("ListUpdate");
            //        notifyMsg.body.ids = filtered.ToArray();
            //        log.Debug(string.Format("уведомление по {0} объектам для сессии {1}", filtered.Count(), session.id));
            //        var connectionId = dsession[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
            //        SignalRConnection.RaiseEvent(notifyMsg, connectionId);
            //    }
            //}
        }

        static NodeBackgroundProccessor() { }
        private NodeBackgroundProccessor()
        {
            ReSet();
        }
        private static readonly NodeBackgroundProccessor instance = new NodeBackgroundProccessor();
        public static NodeBackgroundProccessor Instance
        {
            get
            {
                return instance;
            }
        }
    }
}

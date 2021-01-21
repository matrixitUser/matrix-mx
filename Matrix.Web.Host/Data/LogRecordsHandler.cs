using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Data
{
    class LogRecordsHandler : IRecordHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LogRecordsHandler));
        IDictionary<Guid, string> localCache = new Dictionary<Guid, string>();

        private string nameByOjectId(Guid ObjectId, Guid userId)
        {
            string name = "...";
            if (localCache.ContainsKey(ObjectId))
            {
                name = localCache[ObjectId];
            }
            else
            {
                try
                {
                    var node = StructureGraph.Instance.GetNodeById(ObjectId, userId);
                    name = NamesCache.Instance.UpdateWithoutRedis(node, userId);
                }
                catch { name = "..."; }
                if(name != null)
                    localCache.Add(ObjectId, name);
            }

            return name;
        }

        public void Handle(IEnumerable<Domain.Entities.DataRecord> records, Guid userId)
        {
            var logs = records.Where(r => r.Type == "LogMessage");
            if (!logs.Any()) return;

            var sessions = CacheRepository.Instance.GetSessions();
            //log.Debug(string.Format("извлекли {0} сессии", sessions.Count()));
            //уведомление об изменениях



            sessions.ToList().ForEach(session =>
            {
                try
                {
                    var bag = session as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) return;
                    if (!bag.ContainsKey("logSubscriber")) return;

                    var ids = (List<dynamic>)session.logSubscriber;
                    if (ids == null || !ids.Any()) return;

                    var usrId = Guid.Parse(session.userId.ToString());

                    var filtered = new List<dynamic>();
                    foreach (var record in logs.GroupBy(r => r.ObjectId))
                    {
                        for (var j = 0; j < ids.Count(); j++)
                        {
                            var tube = ids[j];
                            var neights = new List<dynamic>();
                            if ((tube as IDictionary<string, object>).ContainsKey("neighbours"))
                            {
                                neights = (List<dynamic>)tube.neighbours;
                            }
                            else
                            {
                                log.Debug(string.Format("сессия {0}, tube={1}", session.id, tube));
                            }

                            if (neights.Contains(record.Key.ToString()))
                            {
                                foreach (var rec in record)
                                {
                                    dynamic msg = new ExpandoObject();
                                    msg.id = rec.ObjectId;
                                    msg.date = rec.Date;
                                    msg.tubeId = tube.tubeId;
                                    msg.@object = rec.ObjectId;
                                    msg.message = rec.S1;
                                    //msg.name = NamesCache.Instance.GetName(rec.ObjectId, usrId);
                                    msg.name = nameByOjectId(rec.ObjectId, usrId);
                                    filtered.Add(msg);
                                }
                            }
                            else
                            {
                                //log.Debug(string.Format("запись с objid {0} не сидит в сессии {1}", record.Key, session.id));
                            }
                        }
                    }




                    dynamic logsMessage = Helper.BuildMessage("log");
                    logsMessage.body.messages = filtered.ToArray();

                    //log.Debug(string.Format("отправка логов {0} шт, сессия {1}", filtered.Count, session.id));

                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(logsMessage, connectionId);
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("сессия {0} битая", session.id), ex);
                }
            });
        }
    }
}

using Microsoft.Practices.Unity;
using Neo4j.Driver.V1;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Matrix.LogManager
{
    public class LogAnalizer
    {
        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();

        [Dependency]
        public Bus Bus { get; set; }

        public void Analize(IEnumerable<dynamic> records)
        {
            var logMessages = records.Where(r => r.type == "LogMessage");
            if (!logMessages.Any()) return;

            foreach (var subs in subscribers.Keys)
            {
                try
                {

                    var subLogs = new List<dynamic>();
                    foreach (var gr in logMessages.GroupBy(g => g.objectId))
                    {
                        Guid objId = Guid.Parse(gr.Key);
                        if (subscribers[subs].Contains(objId))
                        {
                            subLogs.AddRange(gr);
                        }
                    }

                    if (subLogs.Any())
                    {
                        var msg = Bus.MakeMessageStub("log");
                        msg.body.sessionId = subs;
                        msg.body.messages = subLogs;
                        Bus.SendNotify(msg);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "ошибка при ");
                }
            }
        }

        private readonly Dictionary<Guid, HashSet<Guid>> subscribers = new Dictionary<Guid, HashSet<Guid>>();

        public IDictionary<Guid, HashSet<Guid>> Subscribers { get { return subscribers; } }

        public void Unsubscribe(Guid sessionId, IEnumerable<Guid> tubeIds)
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                var res = sess.Run("match(t:Tube)-[:contains*]->(p) where t.id in {tubeIds} return distinct p.id as id", 
                    new { tubeIds = tubeIds.Select(t=>t.ToString()) });

                var old = new HashSet<Guid>();
                if (subscribers.ContainsKey(sessionId))
                {
                    old = subscribers[sessionId];
                }

                foreach (var id in res)
                {
                    var gid = Guid.Parse(id["id"].As<string>());
                    if (old.Contains(gid)) old.Remove(gid);
                }

                subscribers[sessionId] = old;
            }                        
        }

        public void Subscribe(Guid sessionId, IEnumerable<Guid> tubeIds)
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                var res = sess.Run("match(t:Tube)-[:contains*0..]->(p) where t.id in {tubeIds} return distinct p.id as id", 
                    new { tubeIds = tubeIds.Select(t=>t.ToString()).ToArray() });


                var newIds = res.Select(r=>Guid.Parse(r["id"].As<string>())).ToArray();

                if (newIds.Any())
                {
                    //обновление списка подписки
                    if (subscribers.ContainsKey(sessionId))
                    {
                        subscribers[sessionId].Clear();
                    }
                    else
                    {
                        subscribers.Add(sessionId, new HashSet<Guid>());
                    }

                    foreach (var id in newIds)
                    {
                        if (!subscribers[sessionId].Contains(id)) subscribers[sessionId].Add(id);
                    }
                }
                else
                {
                    //если список подписки пуст, удаляем сессию
                    subscribers.Remove(sessionId);
                }
                logger.Debug("дабавлена подписка для сессии {0} на события {1} узлов", sessionId, newIds.Count());
            }            
        }
    }
}

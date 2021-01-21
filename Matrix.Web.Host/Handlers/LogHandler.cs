using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Matrix.Web.Host.Handlers
{
    class LogHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LogHandler));

        public const string LOG_KEY = "logSubscriber";

        private Bus bus;

        public LogHandler(Bus bus)
        {
            this.bus = bus;
        }

        public bool CanAccept(string what)
        {
            return what.StartsWith("log");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            if (what == "log-subscribe")
            {
                var array = message.body.ids;
                //session.logSubscriber = array;

                var ids = new List<Guid>();

                var tubeIds = new List<dynamic>();

                foreach (var id in array)
                {
                    Guid gid = Guid.Parse(id.ToString());
                    ids.Add(gid);
                    var neighbours = Data.StructureGraph.Instance.GetTubeNeighbourIds(gid).Distinct().ToList();

                    dynamic foo = new ExpandoObject();
                    foo.tubeId = id;
                    foo.neighbours = neighbours;

                    tubeIds.Add(foo);
                }

                //allIds.AddRange(ids);

                //log.Debug(string.Format("подписка для сессии {0}, [{1}]", session.id, string.Join(",", allIds)));

                session.logSubscriber = tubeIds;

                Data.CacheRepository.Instance.SaveSession(session, Guid.Parse(session.userId.ToString()));

                var answer = Helper.BuildMessage(what);
                return answer;
            }

            if (what == "log-unsubscribe")
            {
                //var bag = session.bag as IDictionary<string, object>;
                //if (!bag.ContainsKey(LOG_KEY))
                //{
                //    bag.Add(LOG_KEY, new List<Guid>());
                //}

                //var answer = Helper.BuildMessage(what);
                //foreach (Guid id in message.body.ids)
                //{
                //    (bag[LOG_KEY] as List<Guid>).Remove(id);
                //}

                //return answer;
            }

            return Helper.BuildMessage(what);
        }        
    }
}

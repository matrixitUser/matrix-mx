using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using NCrontab;
using NLog;

namespace Matrix.Scheduler
{
    public class Task
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private const string POLL_QUEUE = "poll";
        private const string CHECK_QUEUE = "check";

        public string Name { get; private set; }
        public string Cron { get; private set; }
        public string Kind { get; private set; }
        public string Settings { get; private set; }
        public string Components { get; private set; }
        public bool? OnlyHoles { get; private set; }
        public bool? HoursDaily { get; private set; }
        public Guid Id { get; private set; }

        private Timer timer;

        public Task(dynamic model)
        {
            Name = "<без названия>";
            Cron = "";
            Kind = "unknown";
            Settings = "";
            Components = "";
            OnlyHoles = null;
            HoursDaily = null;

            var dmodel = model as IDictionary<string, object>;

            if (dmodel.ContainsKey("name"))
            {
                Name = model.name;
            }
            if (dmodel.ContainsKey("cron"))
            {
                Cron = model.cron; 
            }
            if(dmodel.ContainsKey("kind"))
            {
                Kind = model.kind;
            }
            if (dmodel.ContainsKey(Kind))
            {
                Settings = dmodel[Kind].ToString();
            }
            if (dmodel.ContainsKey("components"))
            {
                Components = model.components;
            }

            #region onlyHoles
            if (dmodel.ContainsKey("onlyHoles"))
            {
                if (model.onlyHoles is bool)
                {
                    OnlyHoles = (bool)model.onlyHoles;
                }
                else if (model.onlyHoles is string)
                {
                    bool onlyHoles;
                    bool.TryParse(model.onlyHoles as string, out onlyHoles);
                    OnlyHoles = onlyHoles;
                }
            }
            #endregion

            #region hoursDaily
            if (dmodel.ContainsKey("hoursDaily"))
            {
                if (model.hoursDaily is bool)
                {
                    HoursDaily = (bool)model.hoursDaily;
                }
                else if (model.hoursDaily is string)
                {
                    bool hoursDaily;
                    bool.TryParse(model.hoursDaily as string, out hoursDaily);
                    HoursDaily = hoursDaily;
                }
            }
            #endregion

            Id = Guid.Parse(model.id.ToString());

            WaitNextOccurrence();
        }

        //public DateTime NextOccurrence
        //{
        //    get
        //    {
        //        return Cron == ""? DateTime.MinValue : CrontabSchedule.Parse(Cron).GetNextOccurrence(DateTime.Now);
        //    }
        //}

        public IEnumerable<Guid> GetTubeIds()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var cli = new Neo4jClient.GraphClient(new Uri(url));
            cli.Connect();

            var tbs = cli.Cypher.Match("(a:Task {id:{id}})").
                OptionalMatch("(a)<--(t1:Tube)").
                OptionalMatch("(a)<--(f:Folder)-[*]->(t2:Tube)").
                With("collect(t1.id)+collect(t2.id) as ids unwind ids as id").
                WithParams(new { id = Id }).Return(id => id.As<Guid>());
            var haa = tbs.Results;
            return haa;

            //var dbTasks = cli.Cypher.Match("(ts:Task)<--(foo)-[r*0..]->(tb:Tube)").With("tb.id as tbeId,ts.id as tskId,length(r) as len").Return((tbeId, tskId, len) => new { tubeId = tbeId.As<Guid>(), taskId = tskId.As<Guid>(), len = len.As<int>() }).Results;
            //var foo = dbTasks.GroupBy(t => t.tubeId).Select(g => new { tubeId = g.Key, taskId = g.OrderBy(t => t.len).First().taskId }).Where(t => t.taskId == Id).Select(t => t.tubeId).ToList();
            //return foo;
        }

        public IEnumerable<Guid> GetMailerIds()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var cli = new Neo4jClient.GraphClient(new Uri(url));
            cli.Connect();

            var mls = cli.Cypher.Match("(a:Task {id:{id}})").
                OptionalMatch("(a)<--(ml:Mailer)").
                With("collect(ml.id) as ids unwind ids as id").
                WithParams(new { id = Id }).Return(id => id.As<Guid>());
            var haa = mls.Results;
            return haa;
        }

        public IEnumerable<Guid> GetMaquetteIds()
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var cli = new Neo4jClient.GraphClient(new Uri(url));
            cli.Connect();

            var mqs = cli.Cypher.Match("(a:Task {id:{id}})").
                OptionalMatch("(a)<--(m:Maquette)").
                With("collect(m.id) as ids unwind ids as id").
                WithParams(new { id = Id }).Return(id => id.As<Guid>());
            var haa = mqs.Results;
            return haa;
        }

        /// <summary>
        /// перезапуск таймера по cron-выражению
        /// </summary>
        /// <returns>Возвращает false, если не задано расписание, или если замечено слишком быстрое срабатывание</returns>
        private bool WaitNextOccurrence()
        {
            var now = DateTime.Now;
            if (Cron == "") return false;

            var nextOccurrence = CrontabSchedule.Parse(Cron).GetNextOccurrence(now);
            var timeToWait = (nextOccurrence - now);
            logger.Trace("[{0}] следующее срабатывание - {1:dd.MM.yy HH:mm:ss.FFF} через {2} сек.", Cron, nextOccurrence, timeToWait.TotalSeconds);
            timer = new Timer(Exec, null, timeToWait, new TimeSpan(-1));
            return (timeToWait.TotalSeconds >= 30);
        }

        private void Exec(object arg)
        {
            if (WaitNextOccurrence())
            {
                switch (Kind)
                {
                    case "poll":
                        {
                            //todo
                            //get all tubes
                            var ids = GetTubeIds();
                            //check each
                            //send if need
                            logger.Debug("сработал таск {0} для {1} труб", Name, ids.Count());

                            var bus = ServiceLocator.Current.GetInstance<Bus>();

                            if (Settings != "all")
                            {
                                var part = 500;
                                for (var i = 0; i < ids.Count(); i += part)
                                {
                                    var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "poll");
                                    var partIds = ids.Skip(i).Take(part).ToArray();
                                    msg.body.objectIds = partIds;
                                    msg.body.arg = new ExpandoObject();
                                    msg.body.arg.start = DateTime.Today.AddDays(-2);
                                    msg.body.arg.all = false;
                                    msg.body.what = "all";                                    
                                    msg.body.arg.components = Components;
                                    msg.body.arg.onlyHoles = OnlyHoles;
                                    msg.body.arg.hoursDaily = HoursDaily;
                                    bus.SendPoll(msg);

                                    logger.Debug("порция объектов в количестве {1} оповещена", i, partIds.Count());
                                    //Thread.Sleep(60 * 1000);
                                }
                            }
                            else
                            {
                                var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "poll");
                                msg.body.objectIds = new Guid[] { };
                                msg.body.arg = new ExpandoObject();
                                msg.body.arg.start = DateTime.Today.AddDays(-2);
                                msg.body.arg.all = true;
                                msg.body.what = "all";
                                msg.body.arg.components = Components;
                                msg.body.arg.onlyHoles = OnlyHoles;
                                msg.body.arg.hoursDaily = HoursDaily;
                                bus.SendPoll(msg);

                                logger.Debug("таск оповещён");
                            }
                        }
                        break;

                    case "mailer":
                        {
                            var ids = GetMailerIds();

                            logger.Debug("сработал таск {0} по рассылке {1} писем", Name, ids.Count());

                            var bus = ServiceLocator.Current.GetInstance<Bus>();

                            if (Settings != "all")
                            {
                                var part = 500;
                                for (var i = 0; i < ids.Count(); i += part)
                                {
                                    var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "mailer");
                                    var partIds = ids.Skip(i).Take(part).ToArray();
                                    msg.body.objectIds = partIds;
                                    msg.body.arg = new ExpandoObject();
                                    msg.body.arg.all = false;
                                    msg.body.what = "all";
                                    bus.SendPoll(msg);

                                    logger.Debug("порция объектов в количестве {1} оповещена", i, partIds.Count());
                                }
                            }
                            else
                            {
                                var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "mailer");
                                msg.body.objectIds = new Guid[] { };
                                msg.body.arg = new ExpandoObject();
                                msg.body.arg.all = true;
                                msg.body.what = "all";
                                bus.SendPoll(msg);

                                logger.Debug("таск оповещён");
                            }
                        }
                        break;
                    case "check":
                        {
                            logger.Debug("сработал таск check");

                            var bus = ServiceLocator.Current.GetInstance<Bus>();

                            var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "check");
                            msg.body.objectIds = new Guid[] { };
                            msg.body.what = "scheduler";
                            bus.SendCheck(msg);
                                                                                
                            msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "check-poll");
                            msg.body.objectIds = new Guid[] { };
                            bus.SendPoll(msg);

                            logger.Debug("check и check-poll отправлены");
                        }
                        break;
                    case "maquette":
                        {
                            var ids = GetMaquetteIds();

                            logger.Debug("сработал таск {0} по рассылке {1} макетов", Name, ids.Count());

                            var bus = ServiceLocator.Current.GetInstance<Bus>();

                            if (Settings != "all")
                            {
                                var part = 500;
                                for (var i = 0; i < ids.Count(); i += part)
                                {
                                    var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "maquette");
                                    var partIds = ids.Skip(i).Take(part).ToArray();
                                    msg.body.objectIds = partIds;
                                    msg.body.arg = new ExpandoObject();
                                    msg.body.arg.all = false;
                                    msg.body.what = "all";
                                    bus.SendPoll(msg);

                                    logger.Debug("порция объектов в количестве {1} оповещена", i, partIds.Count());
                                }
                            }
                            else
                            {
                                var msg = bus.MakeMessageStub(Guid.NewGuid().ToString(), "maquette");
                                msg.body.objectIds = new Guid[] { };
                                msg.body.arg = new ExpandoObject();
                                msg.body.arg.all = true;
                                msg.body.what = "all";
                                bus.SendPoll(msg);

                                logger.Debug("таск оповещён");
                            }
                        }
                        break;

                    default:
                        {
                            logger.Debug("Сработал таск вида {0}, расписание {1} id={2}", Kind, Cron, Id);
                        }
                        break;
                }
            }
            else
            {
                logger.Debug("Пропускаем таск вида {0}, расписание {1} id={2}", Kind, Cron, Id);
            }
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }
    }
}

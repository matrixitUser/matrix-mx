using Microsoft.Owin.Hosting;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Neo4j.Driver.V1;
using NLog;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    class Service
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private IDisposable host;
        private Bus bus;

        public void Start()
        {
            var uc = new UnityContainer();
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(uc));

            var rm = new ReportManager();
            uc.RegisterInstance(rm);

            bus = new Bus();
            var rnd = new Random();
            bus.OnMessageReceived += async (se, ea) =>
             {
                 await Task.Run(() =>
                 {
                     try
                     {

                         string what = ea.Message.head.what;

                         if (what == "report-build")
                         {
                             Guid reportId = Guid.Parse((string)ea.Message.body.report);

                             var targets = new List<Guid>();
                             foreach (var t in ea.Message.body.targets)
                             {
                                 if(t is string)
                                 {
                                     targets.Add(Guid.Parse((string)t));
                                 }
                                 else
                                 {
                                     targets.Add(Guid.Parse((string)t.id));
                                 }
                             }
                             var start = (DateTime)ea.Message.body.start;
                             var end = (DateTime)ea.Message.body.end;

                             var session = ea.Message.session;

                             var result = Build(reportId, targets, start, end, Guid.Parse((string)session.userId), session);

                             var answer = bus.MakeMessageStub(what);
                             answer.body.report = result.render;

                             bus.Answer(answer, ea.Properties);
                         }

                         if (what == "report-export")
                         {
                             string type = ea.Message.body.type;
                             string text = ea.Message.body.text;

                             byte[] bytes = Html2PdfConvertor.Instance.Convert(text);

                             logger.Debug("отчет конвертирован в pdf {0} байт", bytes.Length);

                             //var p = new Pechkin.SimplePechkin(new Pechkin.GlobalConfig() { });
                             //var bytes = p.Convert(text);
                             var answer = bus.MakeMessageStub(what);
                             answer.body.bytes = bytes;
                             bus.Answer(answer, ea.Properties);
                         }
                     }
                     catch (Exception ex)
                     {
                         logger.Error(ex, "не удалось построить отчет");
                     }
                 });
             };

            bus.Start();
            uc.RegisterInstance(bus);

            var url = ConfigurationManager.AppSettings["service-url"];
            host = WebApp.Start(url, app =>
            {
                app.UseNancy(n => n.Bootstrapper = new Bootstrapper(uc));
                //Process.Start(url.Replace("*", "localhost"));
            });

            logger.Info("сервис запущен, url: {0}", url);
        }

        public void Stop()
        {
            bus.Stop();
            host.Dispose();
            logger.Info("сервис остановлен");
        }

        public dynamic Build(Guid reportId, List<Guid> targets, DateTime start, DateTime end, Guid userId, dynamic session)
        {
            dynamic model = new ExpandoObject();

            model.targets = GetRows(targets, userId).ToArray();
            model.start = start;
            model.end = end;

            var report = GetNodeById(reportId, userId);

            var result = Reports.Mapper.Instance.Map(model, report.template, session);

            return result;
        }

        public IEnumerable<dynamic> GetRows(IEnumerable<Guid> objectIds, Guid userId)
        {

            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            var su = IsSuperUser(userId);

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                IEnumerable<dynamic> raw;

                if (su)
                {
                    raw = sess.Run(@"match(t:Tube)
Where t.id in {ids} 
Match(i)-[:contains]->(t)-[*1..2]->(o)
With t, collect(distinct i) + collect(distinct o) as n
return t,n", new { ids = objectIds.Select(i => i.ToString()).ToArray() }).Select(r => new { t = r["t"].As<INode>().ToDynamic(), n = r["n"].As<List<INode>>() }).ToArray();
                    logger.Debug("строки получены для su");
                }
                else
                {
                    raw = sess.Run(@"match(g:Group)-[:contains]->(u:User {id:{userId}})
optional match(rg: Group) -[:contains]->(g)
with g
Match (g)-[:right]->()-[*]->(t:Tube)
Where t.id in {ids}
Match(i)-[:contains]->(t)-[*1..2]->(o)
With t, collect(distinct i) + collect(distinct o) as n
return t,n",
new { userId = userId.ToString(), ids = objectIds.Select(i => i.ToString()).ToArray() }).Select(r => new { t = r["t"].As<INode>().ToDynamic(), n = r["n"].As<List<INode>>() }).ToArray();
                    logger.Debug("строки получены");
                }

                var results = raw.Select(r =>
                {
                    dynamic tube = r.t;
                    var dtube = tube as IDictionary<string, object>;
                    foreach (var g in (r.n as List<INode>).Select(f => f.ToDynamic()).GroupBy(d => d.type))
                    {
                        if (dtube.ContainsKey(g.Key))
                        {
                            //dtube[g.Key].
                            logger.Debug("опасно! {0}", g.Key);
                        }
                        else
                        {
                            dtube.Add(g.Key, g.ToArray());
                        }
                    }
                    return tube;
                });

                logger.Debug("строки обернуты {0} шт", results.Count());
                return results.ToArray();

            }
        }

        private bool IsSuperUser(Guid userId)
        {
            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                var res = sess.Run("match(g:Group)-[:contains]->(u:User)where u.id={userId} optional match(rg: Group)-[:contains]->(g) with g, rg is null as admin return admin",
                    new { userId = userId.ToString() }).FirstOrDefault();
                return res != null && res["admin"].As<bool>();
            }
        }

        public dynamic GetNodeById(Guid id, Guid userId)
        {
            var su = IsSuperUser(userId);

            var url = ConfigurationManager.AppSettings["neo4j-url"];
            var login = ConfigurationManager.AppSettings["neo4j-login"];
            var password = ConfigurationManager.AppSettings["neo4j-password"];

            using (var driver = GraphDatabase.Driver(url, AuthTokens.Basic(login, password)))
            using (var sess = driver.Session())
            {
                if (su)
                {
                    var first = sess.Run("match(t) Where t.id={id} return t", new { id = id.ToString() }).FirstOrDefault(); ;
                    if (first == null) return null;
                    return first["t"].As<INode>().ToDynamic();
                }
                else
                {
                    var first = sess.Run(@"match(g:Group)-[:contains]->(u:User {id:{userId}})
optional match(rg:Group)-[:contains]->(g)
with g
match (t)
Where t.id ={id}
return t", new { userId = userId.ToString(), id = id.ToString() }).FirstOrDefault();

                    if (first == null) return null;
                    return first["t"].As<INode>().ToDynamic();
                }
            }
        }
    }
}

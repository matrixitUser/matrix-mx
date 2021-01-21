using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using Neo4jClient;
using Neo4jClient.Cypher;
using Newtonsoft.Json;

namespace Matrix.Web.Host.Data
{
    public class StructureGraph
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StructureGraph));

        private readonly string url = "";

        private static readonly object locker = new object();

        public IEnumerable<dynamic> GetDevices()
        {
            var query = client.Cypher.Match("(d:Device)").
                Return((d) => d.Node<string>());
            var haa = query.Results.ToDynamic();
            return haa;
        }

        public void SaveNode(dynamic node)
        {
            var query = client.Cypher.Merge("(n {id:{id}})").Set("n={node}").WithParams(new { node = node, id = node.id }).Return(n => n.Node<string>());
            var res = query.Results;
        }
        public void UpdateControlConfig(string strConfig, Guid objectId)
        {
            var query = client.Cypher.Match("(n {id:{id}})").Set("n.strConfig={strConfig}").WithParams(new { strConfig = strConfig, id = objectId }).Return(n => n.Node<string>());
            var res = query.Results;
        }
        public void UpdateWaterTowenParametr(float min, float max, int controlMode, Guid objectId)
        {
            var query = client.Cypher.Match("(n {id:{id}})").Set("n.min={min}").Set("n.max={max}").Set("n.controlMode={controlMode}").WithParams(new { min = min, max = max, controlMode = controlMode, id = objectId }).Return(n => n.Node<string>());
            var res = query.Results;
        }
        public void UpdateLightControlsAstronTimersValues(string coordinates, string utc, string afterBeforeSunSetRise, Guid objectId)
        {
            var query = client.Cypher.Match("(n {id:{id}})").Set("n.coordinates={coordinates}").Set("n.utc={utc}").Set("n.afterBeforeSunSetRise={afterBeforeSunSetRise}")
                .WithParams(new { coordinates = coordinates, utc = utc, afterBeforeSunSetRise = afterBeforeSunSetRise, id = objectId }).Return(n => n.Node<string>());
            var res = query.Results;
        }
        public void Merge(dynamic startId, dynamic endId, dynamic rel, string type)
        {
            var query = client.Cypher.Match("(a {id:{startId}})").Match("(b {id:{endId}})").Merge(string.Format("(a)-[:{0}]->(b)", type)).
                WithParams(new { startId = startId, endId = endId }).Return(n => n.Node<string>());
            var res = query.Results;
        }

        public void Break(dynamic startId, dynamic endId)
        {
            var query = client.Cypher.Match("(a {id:{startId}})-[r]->(b {id:{endId}})").
                WithParams(new { startId = startId, endId = endId }).Delete("r");
            query.ExecuteWithoutResults();
        }

        public IEnumerable<Guid> GetRelatedTubs(Guid objectId)
        {
            var query = client.Cypher.Match("(t:Tube)-[*0..1]->(x {id:{id}})").With("t.id as tid").Return(tid => tid.As<Guid>()).UnionAll().
                Match("(t:Tube)<-[*0..1]-(x {id:{id}})").With("t.id as tid").WithParams(new { id = objectId }).Return(tid => tid.As<Guid>());
            var ids = query.Results;
            return ids.Distinct();
        }

        //public IEnumerable<dynamic> GetDevices(Guid userId)
        //{
        //    var result = new List<dynamic>();

        //    var query = client.Cypher;
        //    if (!IsSuperUser(userId))
        //    {
        //        return result;
        //    }

        //    var query1 = query.Match("(d:Device)").
        //        Return((d) => new { device = d.Node<string>() });

        //    var haa = query1.Results;

        //    foreach (var h in haa)
        //    {
        //        var dev = h.device.ToDynamic();
        //        dynamic res = new ExpandoObject();
        //        var ddev = dev as IDictionary<string, object>;
        //        res.id = dev.id;
        //        res.name = dev.name;

        //        if (ddev.ContainsKey("filename"))
        //        {
        //            res.filename = dev.filename;
        //        }
        //        if (ddev.ContainsKey("uploadDate"))
        //        {
        //            res.uploadDate = dev.uploadDate;
        //        }
        //        if (ddev.ContainsKey("reference"))
        //        {
        //            res.reference = dev.reference;
        //        }

        //        if (ddev.ContainsKey("fieldNames"))
        //        {
        //            res.fieldNames = dev.fieldNames;
        //        }
        //        if (ddev.ContainsKey("fieldCaptions"))
        //        {
        //            res.fieldCaptions = dev.fieldCaptions;
        //        }
        //        if (ddev.ContainsKey("fieldDescriptions"))
        //        {
        //            res.fieldDescriptions = dev.fieldDescriptions;
        //        }
        //        if (ddev.ContainsKey("fields"))
        //        {
        //            res.fields = dev.fields;
        //        }
        //        if (ddev.ContainsKey("tags"))
        //        {
        //            res.tags = dev.tags;
        //        }

        //        result.Add(res);
        //    }

        //    return result;
        //}

        public IEnumerable<dynamic> GetDrivers(Guid userId)
        {
            var result = new List<dynamic>();

            var query = client.Cypher;
            if (!IsSuperUser(userId))
            {
                return result;
                //query = client.Cypher.Match("(u:User {id:{userId}})<-[:contains]-(g:Group)-[:right]->()-[*]->(:Tube)-[:device]->(d:Device)").WithParams(new { userId = userId });
            }

            var query1 = query.Match("(d:Device)<-[:driver]-(dr:Driver)").
                Return((d, dr) => new { device = d.Node<string>(), driver = dr.Node<string>() });

            var haa = query1.Results;

            foreach (var h in haa)
            {
                var dev = h.device.ToDynamic();
                var drv = h.driver.ToDynamic();
                dynamic res = new ExpandoObject();
                var ddev = dev as IDictionary<string, object>;
                var ddrv = drv as IDictionary<string, object>;
                res.id = dev.id;
                res.name = dev.name;
                if (ddrv.ContainsKey("driver"))
                {
                    res.driver = drv.driver;
                }
                else
                {
                    //заглушка. иначе ругается
                    res.driver = "ZW1wdHk=";
                }

                if (ddev.ContainsKey("filename"))
                {
                    res.filename = dev.filename;
                }
                if (ddev.ContainsKey("uploadDate"))
                {
                    res.uploadDate = dev.uploadDate;
                }
                if (ddev.ContainsKey("reference"))
                {
                    res.reference = dev.reference;
                }

                if (ddev.ContainsKey("fieldNames"))
                {
                    res.fieldNames = dev.fieldNames;
                }
                if (ddev.ContainsKey("fieldCaptions"))
                {
                    res.fieldCaptions = dev.fieldCaptions;
                }
                if (ddev.ContainsKey("fieldDescriptions"))
                {
                    res.fieldDescriptions = dev.fieldDescriptions;
                }
                if (ddev.ContainsKey("fields"))
                {
                    res.fields = dev.fields;
                }
                if (ddev.ContainsKey("tags"))
                {
                    res.tags = dev.tags;
                }
                if(ddev.ContainsKey("isFilter"))
                {
                    res.isFilter = dev.isFilter;
                }

                result.Add(res);
            }

            return result;
        }

        public IEnumerable<dynamic> GetMailers(Guid userId)
        {
            var result = new List<dynamic>();

            if (IsSuperUser(userId))
            {
                var query = client.Cypher.Match("(n:Mailer)").
                    Return((n) => new { mailer = n.Node<string>() });
                var haa = query.Results;

                foreach (var h in haa)
                {
                    var obj = h.mailer.ToDynamic();
                    result.Add(obj);
                }
            }

            return result;
        }

        public dynamic GetMailerById(Guid id, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match("(n:Mailer)").
                Where("n.id={id}").WithParams(new { id = id }).
                OptionalMatch("(n)-[:based]->(rp:Report)").
                OptionalMatch("(n)-[:using]->(t:Tube)").
                OptionalMatch("(n)-[:task]->(tk:Task)").
                With("n, collect(distinct rp) as rps, collect(distinct t) as ts, collect(distinct tk) as tks").
                Return((n, rps, ts, tks) => new { n = n.Node<string>(), rps = rps.As<IEnumerable<Node<string>>>(), ts = ts.As<IEnumerable<Node<string>>>(), tks = tks.As<IEnumerable<Node<string>>>() });
                var first = query.Results.FirstOrDefault();
                if (first == null) return null;

                dynamic mailer = (first.n as Node<string>).ToDynamic();
                var dmailer = mailer as IDictionary<string, object>;
                dmailer.Add("Report", (first.rps as IEnumerable<Node<string>>).ToDynamic().ToArray());
                dmailer.Add("Tube", (first.ts as IEnumerable<Node<string>>).ToDynamic().ToArray());
                dmailer.Add("Task", (first.tks as IEnumerable<Node<string>>).ToDynamic().ToArray());
                return mailer;
            }

            return null;
        }

        public dynamic GetMailerByTubeIds(Guid rpId, List<Guid>tubeIds, Guid userId)
        {
            var query = client.Cypher.
               Match("(rp)-[]-(m:Mailer)-[]-(t:Tube)").
               Where("rp.id={id}").WithParams(new { id = rpId }).
               AndWhere("t.id in {ids}").WithParams(new { ids = tubeIds }).
               OptionalMatch("(m:Mailer)-[:task]->(tk:Task)").
               With("m, rp, collect(distinct t) as ts, collect(distinct tk) as tks").
               Return((m, rp, ts, tks) => new { m = m.Node<string>(), rp = rp.Node<string>(), ts = ts.As<IEnumerable<Node<string>>>(), tks = tks.As<IEnumerable<Node<string>>>() });

            var first = query.Results.FirstOrDefault();
            if (first == null) return null;

            dynamic mailer = (first.m as Node<string>).ToDynamic();
            var dmailer = mailer as IDictionary<string, object>;
            dmailer.Add("Report", (first.rp as Node<string>).ToDynamic());
            dmailer.Add("Tube", (first.ts as IEnumerable<Node<string>>).ToDynamic().ToArray());
            dmailer.Add("Task", (first.tks as IEnumerable<Node<string>>).ToDynamic().ToArray());
            return mailer;
        }
        public dynamic GetFolderById(Guid id, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                try
                {
                    var query = client.Cypher.
                    Match("(n:Folder)").
                    Where("n.id={id}").WithParams(new { id = id }).
                    OptionalMatch("(n)-[:task]->(tk:Task)").
                    OptionalMatch("(n)-[:contains]->(t:Tube)").
                    With("n, collect(distinct tk) as tks, collect(distinct t) as ts").
                    Return((n, tks, ts) => new { n = n.Node<string>(), tks = tks.As<IEnumerable<Node<string>>>(), ts = ts.As<IEnumerable<Node<string>>>() });
                    var first = query.Results.FirstOrDefault();
                    if (first == null) return null;

                    dynamic node = (first.n as Node<string>).ToDynamic();
                    var dnode = node as IDictionary<string, object>;
                    dnode.Add("Task", (first.tks as IEnumerable<Node<string>>).ToDynamic().ToArray());
                    dnode.Add("Tube", (first.ts as IEnumerable<Node<string>>).ToDynamic().ToArray());
                    return node;
                }
                catch { return null; }
            }

            return null;
        }

        public IEnumerable<dynamic> GetMaquettes(Guid userId)
        {
            var result = new List<dynamic>();

            if (IsSuperUser(userId))
            {
                var query = client.Cypher.Match("(n:Maquette)").
                    Return((n) => new { maquette = n.Node<string>() });
                var haa = query.Results;

                foreach (var h in haa)
                {
                    var obj = h.maquette.ToDynamic();
                    result.Add(obj);
                }
            }

            return result;
        }

        public dynamic GetMaquetteById(Guid id, Guid userId)//TODO use userId
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match("(n:Maquette)").
                Where("n.id={id}").WithParams(new { id = id }).
                OptionalMatch("(n)-[:maquette]->(t:Tube)").
                OptionalMatch("(n)-[:task]->(tk:Task)").
                With("n,collect(distinct t) as ts,collect(distinct tk) as tks").
                Return((n, ts, tks) => new { n = n.Node<string>(), ts = ts.As<IEnumerable<Node<string>>>(), tks = tks.As<IEnumerable<Node<string>>>() });
                var first = query.Results.FirstOrDefault();
                if (first == null) return null;

                dynamic maquette = (first.n as Node<string>).ToDynamic();
                var dmaquette = maquette as IDictionary<string, object>;
                dmaquette.Add("Tube", (first.ts as IEnumerable<Node<string>>).ToDynamic().ToArray());
                dmaquette.Add("Task", (first.tks as IEnumerable<Node<string>>).ToDynamic().ToArray());
                return maquette;
            }
            return null;
        }

        public IEnumerable<dynamic> GetTasks(Guid userId)
        {
            var result = new List<dynamic>();

            if (IsSuperUser(userId))
            {
                var query = client.Cypher.Match("(n:Task)").
                    Return((n) => new { obj = n.Node<string>() });
                var haa = query.Results;

                foreach (var h in haa)
                {
                    var obj = h.obj.ToDynamic();
                    result.Add(obj);
                }
            }

            return result;
        }

        public dynamic GetTaskById(Guid id, Guid userId)//TODO use userId
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match("(n:Task)").
                Where("n.id={id}").WithParams(new { id = id }).
                OptionalMatch("(n)<-[:task]-(f:Folder)").
                OptionalMatch("(n)<-[:task]-(ml:Mailer)").
                OptionalMatch("(n)<-[:task]-(m:Maquette)").
                With("n,collect(distinct f) as fs,collect(distinct ml) as mls,collect(distinct m) as ms").
                Return((n, fs, mls, ms) => new { n = n.Node<string>(), fs = fs.As<IEnumerable<Node<string>>>(), mls = mls.As<IEnumerable<Node<string>>>(), ms = ms.As<IEnumerable<Node<string>>>() });
                var first = query.Results.FirstOrDefault();
                if (first == null) return null;

                dynamic obj = (first.n as Node<string>).ToDynamic();
                var dobj = obj as IDictionary<string, object>;
                dobj.Add("Folder", (first.fs as IEnumerable<Node<string>>).ToDynamic().ToArray());
                dobj.Add("Mailer", (first.mls as IEnumerable<Node<string>>).ToDynamic().ToArray());
                dobj.Add("Maquette", (first.ms as IEnumerable<Node<string>>).ToDynamic().ToArray());
                return obj;
            }
            return null;
        }

        public void SavePair(dynamic start, dynamic end, dynamic rel, Guid userId)
        {
            var dstart = start as IDictionary<string, object>;
            if (!dstart.ContainsKey("type") || !dstart.ContainsKey("id")) return;

            var dend = end as IDictionary<string, object>;
            if (!dend.ContainsKey("type") || !dend.ContainsKey("id")) return;

            var drel = rel as IDictionary<string, object>;
            if (!drel.ContainsKey("type")) return;

            var query = client.Cypher.Match(string.Format("(s:{0} {{id:{{start}}.id}})", (string)start.type)).Set("s+={start}").
                Merge(string.Format("(e:{0} {{id:{{end}}.id}})", (string)end.type)).OnCreate().Set("e={end}").OnMatch().Set("e+={end}").
                Merge(string.Format("(s)-[r:{0}]->(e)", (string)rel.type)).OnCreate().Set("r={rel}").OnMatch().Set("r+={rel}").
                WithParams(new { start = start, end = end, rel = rel }).Return((s, e, r) => e.Node<string>());
            var foo = query.Results;

            Carantine.Instance.Push(Guid.Parse(start.id.ToString()));
        }

        public void SaveSingle(dynamic start, Guid userId)
        {
            var dstart = start as IDictionary<string, object>;
            if (!dstart.ContainsKey("type") || !dstart.ContainsKey("id")) return;

            var query = client.Cypher.Merge(string.Format("(e:{0} {{id:{{start}}.id}})", (string)start.type)).OnCreate().Set("e={start}").OnMatch().Set("e+={start}").
                WithParams(new { start = start }).Return((e) => e.Node<string>());
            var foo = query.Results;
            Carantine.Instance.Push(Guid.Parse(start.id.ToString()));
        }

        public IDictionary<string, string> GetTagMap(Guid tubeId, Guid userId)
        {
            var map = new Dictionary<string, string>();

            var q1 = client.Cypher.Match("(t:Tube)-[:parameter]->(p)").WithParams(new { id = tubeId }).
                Where("t.id={id}").With("p.name as key, p.tag as val").Return((key, val) => new { key = key.As<string>(), val = val.As<string>() });
            var x = q1.Results;

            foreach (var foo in x)
            {
                if (!map.ContainsKey(foo.key))
                {
                    map.Add(foo.key, foo.val);
                }
            }

            var q2 = client.Cypher.Match("(t:Tube)-[:device]->(d:Device)-[:parameter]->(p)").WithParams(new { id = tubeId }).
                Where("t.id={id}").With("p.name as key, p.tag as val").Return((key, val) => new { key = key.As<string>(), val = val.As<string>() });
            var x2 = q2.Results;
            foreach (var foo in x2)
            {
                if (!map.ContainsKey(foo.key))
                {
                    map.Add(foo.key, foo.val);
                }
            }

            return map;
        }

        public dynamic GetUser(string login, string password)
        {
            try
            {
                var user = client.Cypher.Match("(u:User)").Where("u.login={login} and u.password={password}").WithParams(new { login = login, password = password }).Return(u => u.Node<string>()).Results.FirstOrDefault();
                if (user != null) return user.ToDynamic();
            }
            catch { }
            return null;
        }

        public dynamic GetUser(Guid id)
        {
            var user = client.Cypher.Match("(u:User)").Where("u.id={id}").WithParams(new { id = id }).Return(u => u.As<Node<string>>()).Results.FirstOrDefault().ToDynamic();
            return user;
        }

        public dynamic GetHierarchy(string nodeType, string relationType, Guid userId)
        {
            IEnumerable<dynamic> roots;
            if (IsSuperUser(userId))
            {
                var rootsQuery = client.Cypher.
                Match(string.Format("(any:{0})", nodeType)).
                OptionalMatch(string.Format("(any)<-[:{0}]-(none)", relationType)).
                With("any,count(none) as parents").
                Where("parents=0").
                Return(any => any.Node<string>());
                roots = rootsQuery.Results.ToList().ToDynamic();
            }
            else
            {
                var rootsQuery = client.Cypher.
                    Auth(userId).
                    Match(string.Format("(g)-[:right]->(any:{0})", nodeType)).
                    OptionalMatch(string.Format("(any)<-[:{0}]-(none)", relationType)).
                    With("any,count(none) as parents").
                    Where("parents=0").
                    Return(any => any.Node<string>());
                roots = rootsQuery.Results.ToList().ToDynamic();
            }

            Func<dynamic, dynamic[]> getChildren = null;
            getChildren = (root) =>
            {
                var childrenQuery = client.Cypher.
                    Match(string.Format("(root:{0})-[:{1}]->(child:{0})", nodeType, relationType)).
                    Where("root.id={id}").
                    WithParams(new { id = root.id }).
                    Return(child => child.Node<string>());
                var children = childrenQuery.Results.ToList().ToDynamic();
                var result = new List<dynamic>();
                foreach (var child in children)
                {
                    dynamic wrapped = new ExpandoObject();
                    wrapped.data = child;
                    wrapped.group = true;
                    wrapped.children = getChildren(child);
                    result.Add(wrapped);
                }
                return result.ToArray();
            };


            var superChildren = new List<ExpandoObject>();

            foreach (var root in roots)
            {
                dynamic wrapped = new ExpandoObject();
                wrapped.data = root;
                wrapped.group = true;
                wrapped.children = getChildren(root);

                superChildren.Add(wrapped);
            }

            dynamic super = new ExpandoObject();
            super.data = new ExpandoObject();
            super.data.name = "Корень";
            super.group = true;
            super.expanded = true;
            super.children = superChildren.ToArray();
            return super;
        }

        private readonly GraphClient client;

        public long RowsCount(string filter, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match("(i)-[:contains*]->(t:Tube)-[:contains*]->(o)").
                Where("(lower(i.name)=~{filter} or lower(o.name)=~{filter} or lower(o.phone)=~{filter})").WithParams(new { filter = filter }).
                Return(t => t.CountDistinct());
                return query.Results.First();
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[:right]->(i)-[:contains*]->(t:Tube)-[:contains*]->(o)").
                    Where("(lower(i.name)=~{filter} or lower(o.name)=~{filter} or lower(o.phone)=~{filter})").WithParams(new { filter = filter }).
                    Return(t => t.CountDistinct());
                return query.Results.First();
            }
        }

        public IEnumerable<Guid> GetRowIds(string filter, Guid[] groups, Guid userId)
        {
            try
            {
                IEnumerable<Guid> ids = null;

                var isAdmin = IsSuperUser(userId);

                if (!string.IsNullOrEmpty(filter))
                {
                    var tokens = filter.Split(' ');
                    if (tokens.Length > 5) tokens = tokens.Take(5).ToArray();

                    var filterFields = new string[]
                    {
                        "name","phone","imei","id","type"
                    };

                    tokens = tokens.Select(t => string.Join(" OR ", filterFields.Select(f => string.Format("{0}:*{1}*", f, t)))).ToArray();


                    for (int i = 0; i < tokens.Length; i++)
                    {
                        var q = client.Cypher.Start(new { n = Node.ByIndexQuery("node_auto_index", tokens[i]) });

                        if (ids != null)
                        {
                            q = q.OptionalMatch("(n)-[*]->(t1:Tube)").Where("t1.id in {ids}").
                                OptionalMatch("(n)<-[*]-(t2:Tube)").Where("t2.id in {ids}").
                                WithParams(new { ids = ids });
                        }
                        else
                        {
                            q = q.OptionalMatch("(n)-[*]->(t1:Tube)").
                                OptionalMatch("(n)<-[*]-(t2:Tube)");
                        }

                        q = q.With("collect(t1.id)+collect(t2.id) as tt unwind tt as t").With("distinct t as id");

                        ids = q.Return(id => id.As<Guid>()).Results;
                    }
                }
                else
                {
                    var q = client.Cypher.Match("(t:Tube)").With("t.id as id").Return(id => id.As<Guid>());
                    ids = q.Results;
                }

                if (!isAdmin)
                {
                    var q = client.Cypher.Match("(u:User {id:{userId}})<-[:contains]-(g:Group)-[:right]->()-[*]->(t:Tube)").
                        WithParams(new { userId = userId }).With("t.id as id").Return(id => id.As<Guid>());
                    var allids = q.Results;
                    ids = ids.Intersect(allids);
                }

                if (groups.Any())
                {
                    var q = client.Cypher.Match("(f:Folder)-[*]->(t:Tube)").Where("f.id in {groups}").WithParams(new { groups = groups }).With("t.id as id").Return(id => id.As<Guid>());
                    var allids = q.Results;
                    ids = ids.Intersect(allids);
                }

                ////var regex = new Regex(@"[A-Za-zА-Яа-я0-9#№-]*");
                ////var tokens = filter.Split(' ').Select(r => regex.Match(r).Value).Select(r => string.Format("*{0}*", r)).ToArray();

                //var flt = string.IsNullOrEmpty(filter) ? "" : string.Join(" OR ", tokens.Select(t => string.Join(" OR ", filterFields.Select(f => string.Format("{0}:{1}", f, t)))));



                //var q = client.Cypher;


                ////фильтр еси есь

                ////поиск всех тюбов по первому критерию, потом по второму а далее скипы, смысла


                //if (!string.IsNullOrEmpty(filter))
                //{
                //    q = q.StartWithNodeIndexLookup("n", "node_auto_index", flt).
                //        OptionalMatch("(n)-[:contains|:device*0..]->(t1:Tube)").
                //        OptionalMatch("(n)<-[:contains|:device*0..]-(t2:Tube)").
                //        With("collect(distinct t1)+collect(distinct t2) as tube unwind tube as t").
                //        With("distinct t");
                //}
                //else
                //{
                //    q = q.Match("(t:Tube)");
                //}

                //if (groups.Any())
                //{
                //    q = q.Match("(f:Folder)-[*]->(t)").Where("f.id in {groups}").WithParams(new { groups = groups });
                //}

                //if (!isAdmin)
                //{
                //    q = q.Match("(u:User {id:{userId}})<-[:contains]-(g:Group)-[:right]->()-[*]->(t)").
                //        WithParams(new { userId = userId });
                //}

                //var query = q.With("distinct t.id as id").
                //    Return(id => id.As<Guid>());

                //var ids = query.Results;
                return ids;
            }
            catch (Exception ex)
            {

            }
            return new Guid[] { };
        }

        public IEnumerable<dynamic> GetRows(IEnumerable<Guid> objectIds, Guid userId)
        {
            IEnumerable<dynamic> raw;
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                    Match("(t:Tube)").
                    Where("t.id in {ids}").WithParams(new { ids = objectIds }).
                    Match("(i)-[*]->(t)-[*]->(o)").
                    Set("i.type=labels(i)[0]").
                    Set("o.type=labels(o)[0]").
                    With("t,collect(distinct i)+collect(distinct o) as n").
                    Return((t, n) => new { t = t.Node<string>(), n = n.As<IEnumerable<Node<string>>>() });

                raw = query.Results;
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[:right]->()-[*]->(t:Tube)").
                    Where("t.id in {ids}").WithParams(new { ids = objectIds }).
                    Match("(i)-[*]->(t)-[*]->(o)").
                    Set("i.type=labels(i)[0]").
                    Set("o.type=labels(o)[0]").
                    With("t,collect(distinct i)+collect(distinct o) as n").
                    Return((t, n) => new { t = t.Node<string>(), n = n.As<IEnumerable<Node<string>>>() });

                raw = query.Results;
            }

            var results = raw.Select(r =>
            {
                dynamic tube = (r.t as Node<string>).ToDynamic();

                foreach (var g in (r.n as IEnumerable<Node<string>>).ToDynamic().GroupBy(d => d.type))
                {
                    //if (g.Key == "Device")
                    //{
                    //    foreach (var dev in g)
                    //    {
                    //        var devd = dev as IDictionary<string, object>;
                    //        if (devd.ContainsKey("driver")) devd.Remove("driver");
                    //    }
                    //}
                    var dtube = tube as IDictionary<string, object>;
                    dtube.Add(g.Key, g.ToArray());
                }
                return tube;
            });


            return results.ToArray();

        }


        public struct RowFias
        {
            public Guid Id { get; set; }
            public Guid AreaId { get; set; }
            public Guid? FiasId { get; set; }
            public string Address { get; set; }
            public string AreaName { get; set; }
            public string Name { get; set; }
            public string Resource { get; set; }
        }

        public IDictionary<Guid, RowFias> GetRowsFias(List<Guid> fiasIds, Guid userId)
        {
            string whereCondition = "exists(a.id)" + (fiasIds.Count > 0 ? " and exists(a.fiasid) and a.fiasid in {fiasIds}" : "");

            IEnumerable <dynamic> raw = new List<dynamic>();
            Dictionary<Guid, RowFias> result = new Dictionary<Guid, RowFias>();
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                    Match("(a:Area)-[:contains]->(t:Tube)").
                    Where(whereCondition).
                    WithParams(new { fiasIds = fiasIds }).
                    With("a.id as areaId, t.id as id, a.fiasid as fiasId, a.name as areaName, t.name as name, a.address as address, t.resource as resource").
                    Return((areaId, id, fiasId, areaName, name, address, resource) => new {
                        areaId = areaId.As<Guid>(),
                        id = id.As<Guid>(),
                        fiasId = fiasId.As<Guid?>(),
                        areaName = areaName.As<string>(),
                        name = name.As<string>(),
                        address = address.As<string>(),
                        resource = resource.As<string>()
                    });

                raw = query.Results;
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[:right]->()-[*]->(t:Tube)<-[:contains]-(a:Area)").
                    Where(whereCondition).
                    WithParams(new { fiasIds = fiasIds }).
                    With("a.id as areaId, t.id as id, a.fiasid as fiasId, a.name as areaName, t.name as name, a.address as address, t.resource as resource").
                    Return((areaId, id, fiasId, areaName, name, address, resource) => new {
                        areaId = areaId.As<Guid>(),
                        id = id.As<Guid>(),
                        fiasId = fiasId.As<Guid?>(),
                        areaName = areaName.As<string>(),
                        name = name.As<string>(),
                        address = address.As<string>(),
                        resource = resource.As<string>()
                    });

                raw = query.Results;
            }

            foreach(var r in (raw as IEnumerable<dynamic>))
            {
                result[r.id] = new RowFias
                {
                    Id = r.id,
                    FiasId = r.fiasId,
                    AreaId = r.areaId,
                    Address = r.address,
                    AreaName = r.areaName,
                    Name = r.name,
                    Resource = r.resource
                };
            }

            return result;
        }


        private StructureGraph()
        {
            url = ConfigurationManager.AppSettings["neo4j-url"];
            client = new GraphClient(new Uri(url));
            client.Connect();
        }
        private Random rnd = new Random();

        public Guid GetRootUser()
        {
            var q = client.Cypher.Match("(g:Group)-[:contains]->(u:User)").
               OptionalMatch("(rg:Group)-[:contains]->(g)").
               With("rg,g,u").
               Where("rg is null").
               With("u.id as id limit 1").
               Return((id) => id.As<Guid>());
            var uid = q.Results.FirstOrDefault();
            return uid;
        }

        public bool IsSuperUser(Guid userId)
        {
            bool isRoot;
            try
            {
                var q = client.Cypher.Match("(g:Group)-[:contains]->(u:User)").Where("u.id={userId}").WithParams(new { userId = userId }).
                   OptionalMatch("(rg:Group)-[:contains]->(g)").
                   With("g, rg is null as admin").Return((admin) => admin.As<bool>());
                isRoot = q.Results.FirstOrDefault();
            }
            catch(Exception ex)
            {
                isRoot = false;
            }
            return isRoot;
        }

        public dynamic GetBranch(Guid rootId, Guid userId)
        {
            if (IsSuperUser(userId))
            {

                var query = client.Cypher.
                    Match("(r {id:{rootId}})<-[*]-(c)").
                    WithParams(new { rootId = rootId }).
                    Return(n => n.Node<string>());

                var haa = query.Results.ToDynamic();

                var relationsQuery = client.Cypher.
                    Match("(n1 {id:{rootId}})<-[r]-(n2)").
                    With("r as rel, n1.id as end,n2.id as start").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() }).
                    UnionAll().
                    Match("(n1 {id:{rootId}})-[r]->(n2)").
                    With("r as rel, n2.id as end,n1.id as start").
                    WithParams(new { rootId = rootId }).
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() }).
                    UnionAll().
                    Match("(n1:Tube)-[r:device]->(n2:Device)").
                    With("r as rel, n2.id as end,n1.id as start").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() });

                var foo = relationsQuery.Results.Select(r =>
                {
                    var drel = r.rel.ToDynamic();
                    drel.start = r.start;
                    drel.end = r.end;
                    return drel;
                });

                dynamic server = new ExpandoObject();
                server.nodes = haa;
                server.relations = foo;
                return server;
            }
            else
            {
                dynamic server = new ExpandoObject();
                return server;
            }
        }

        /// <summary>
        /// загрузка грозди для сервера опроса (при изменениях структуры)
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public dynamic GetServerBranch(Guid rootId, Guid userId)
        {
            if (IsSuperUser(userId))
            {

                var query = client.Cypher.
                    Match("(a {id:{rootId}})<-[*]-(b)").
                    With("distinct b").
                    WithParams(new { rootId = rootId }).
                    Return(b => b.Node<string>());
                var haa = query.Results.ToDynamic();

                var relationsQuery = client.Cypher.
                    Match("(s {id:{rootId}})<-[*0..]-(a)<-[r]-(b)").
                    Set("r.type=type(r)").
                    With("r as rel, a.id as end,b.id as start").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() }).
                    UnionAll().
                    Match("(s {id:{rootId}})<-[*0..]-(b)-[r:device]->(a)").
                    Set("r.type=type(r)").
                    With("r as rel, a.id as end,b.id as start").
                    WithParams(new { rootId = rootId }).
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() });

                var foo = relationsQuery.Results.Select(r =>
                {
                    var drel = r.rel.ToDynamic();
                    drel.start = r.start;
                    drel.end = r.end;
                    return drel;
                });

                dynamic server = new ExpandoObject();
                server.nodes = haa;
                server.relations = foo;
                return server;
            }
            else
            {
                dynamic server = new ExpandoObject();
                return server;
            }
        }

        public dynamic GetServer(string serverName, Guid userId)
        {
            if (IsSuperUser(userId))
            {

                var query = client.Cypher.
                    Match("(s:SurveyServer)<-[:server]-(port)").
                    Where("s.name={serverName} and (not exists(s.isDeleted) or s.isDeleted)").
                    OptionalMatch("(port)<-[:contains*0..]-(i)").
                    OptionalMatch("(port)-[:contains*0..]->(o)").
                    With("collect(distinct i)+collect(distinct o) as node unwind node as n").
                    With("distinct n").
                    Set("n.type=labels(n)[0]").
                    WithParams(new { serverName = serverName }).
                    Return(n => n.Node<string>());

                var haa = query.Results.ToDynamic();

                var relationsQuery = client.Cypher.
                    Match("(s:SurveyServer)<-[:server]-(port)").
                    Where("s.name={serverName}").
                    Match("(port)<-[*0..]-(n1)<-[r]-(n2)").
                    Set("r.type=type(r)").
                    With("r as rel, n1.id as end,n2.id as start").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() }).
                    UnionAll().
                    Match("(s:SurveyServer)<-[:server]-(port)").
                    Where("s.name={serverName}").
                    Match("(port)-[*0..]->(n1)-[r]->(n2)").
                    Set("r.type=type(r)").
                    With("r as rel, n2.id as end,n1.id as start").
                    WithParams(new { serverName = serverName }).
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() }).
                    UnionAll().
                    Match("(s:SurveyServer)<-[:server]-(port)").
                    Where("s.name={serverName}").
                    Match("(port)<-[*0..]-(n1:Tube)-[r:device]->(n2:Device)").
                    Set("r.type=type(r)").
                    With("r as rel, n2.id as end,n1.id as start").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() });

                var foo = relationsQuery.Results.Select(r =>
                {
                    var drel = r.rel.ToDynamic();
                    drel.start = r.start;
                    drel.end = r.end;
                    return drel;
                });

                dynamic server = new ExpandoObject();
                server.nodes = haa;
                server.relations = foo;
                return server;
            }
            else
            {
                dynamic server = new ExpandoObject();
                return server;
            }
        }

        private static readonly StructureGraph instance = new StructureGraph();
        public static StructureGraph Instance
        {
            get
            {
                return instance;
            }
        }

        public IEnumerable<dynamic> GetNodesByType(string type, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match(string.Format("(t:{0})", type)).
                Return(t => t.Node<string>());

                return query.Results.ToDynamic();
            }
            {
                var query = client.Cypher.
                Match("(u:User {id:{userId}})<--(g:Group)").
                WithParams(new { userId = userId }).
                Match(string.Format("(g)-[*0..]->(t:{0})", type)).
                Return(t => t.Node<string>());
                return query.Results.ToDynamic();
            }
        }
        public dynamic GetNodeById(Guid id)
        {
            var query = client.Cypher.
                Match("(t)").
                Where("t.id={id}").WithParams(new { id = id }).
                Return(t => t.Node<string>());

            var first = query.Results.FirstOrDefault();
            if (first == null) return null;
            return first.ToDynamic();
        }
        public dynamic GetNodeById(Guid id, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                Match("(t)").
                Where("t.id={id}").WithParams(new { id = id }).
                Return(t => t.Node<string>());

                var first = query.Results.FirstOrDefault();
                if (first == null) return null;
                return first.ToDynamic();
            }
            {
                var query = client.Cypher.
                Auth(userId).
                Match("(t)").
                Where("t.id={id}").WithParams(new { id = id }).
                Return(t => t.Node<string>());
                var first = query.Results.FirstOrDefault();
                if (first == null) return null;
                return first.ToDynamic();
            }
        }

        public bool CanSee(Guid id, Guid userId)
        {
            if (IsSuperUser(userId)) return true;

            var query = client.Cypher.Match("(u:User)<-[:contains]-(g:Group)-[]->(n)").
                Where("u.id={userId} and n.id={id}").
                WithParams(new { userId = userId, id = id }).
                Return(n => n.Count());
            var count = query.Results.First();
            return count > 0;
        }

        public void CloseSession(Guid sessionId)
        {
            lock (locker)
            {
                var query = client.Cypher.Match("(s:Session {id:{id}})-[r:session]->(u:User)").
                    WithParams(new { id = sessionId }).
                    Delete("r,s");
                query.ExecuteWithoutResults();
            }
        }

        public bool HasRelation(Guid source, Guid[] dests, string relationLabels)
        {
            var query = client.Cypher.Match(string.Format("(src)<-[r{0}*0..]-(dst)", relationLabels)).
                Where("src.id={src} and dst.id in {dsts}").
                WithParams(new { src = source, dsts = dests }).
                Return(r => r.Count());
            return query.Results.FirstOrDefault() > 0;
        }

        public IEnumerable<dynamic> GetParameters(Guid tubeId, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.
                    Cypher.Match("(n {id:{id}})-[:parameter]->(p)").
                    WithParams(new { id = tubeId }).
                    Return(p => p.Node<string>());
                return query.Results.ToDynamic().ToArray();
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[*]->(n {id:{id}})-[:parameter]->(p)").
                    WithParams(new { id = tubeId }).
                    ReturnDistinct(p => p.Node<string>());
                return query.Results.ToDynamic().ToArray();
            }
        }

        public IEnumerable<dynamic> GetAllParameters(Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.
                    Cypher.Match("(t:Tube)-[:parameter]->(p:Parameter)").
                    With("t.id as id,p").
                    Return((id, p) => new { node = p.Node<string>(), tubeId = id.As<Guid>() });
                return query.Results.Select(p =>
                {
                    dynamic par = p.node.ToDynamic();
                    par.tubeId = p.tubeId;
                    return par;
                });
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[:right]->()-[:contains*]->(t:Tube)-[:parameter]->(p:Parameter)").
                    With("t.id as id,p").
                    Return((id, p) => new { node = p.Node<string>(), tubeId = id.As<Guid>() });
                return query.Results.Select(p =>
                {
                    dynamic par = p.node.ToDynamic();
                    par.tubeId = p.tubeId;
                    return par;
                });
            }
        }

        public void SaveParameter(Guid tubeId, dynamic newParameter, Guid userId)
        {
            lock (locker)
            {
                if (IsSuperUser(userId))
                {
                    var query = client.Cypher.
                        Match("(n {id:{id}})").
                        Merge("(n)-[:parameter]->(p:Parameter {name:{pid}})").
                        OnCreate().Set("p={body}").
                        OnMatch().Set("p+={body}").
                        WithParams(new { id = tubeId, body = newParameter, pid = newParameter.name }).
                        Return(p => p.Node<string>());
                    var foo = query.Results.ToDynamic();
                }
                else
                {
                    var query = client.Cypher.Auth(userId).
                        Match("(g)-[:right]->()-[:contains*]->(n {id:{id}})").
                        Merge("(n)-[:parameter]->(p:Parameter {name:{pid}})").
                        OnCreate().Set("p={body}").
                        OnMatch().Set("p+={body}").
                        WithParams(new { id = tubeId, body = newParameter, pid = newParameter.name }).
                        Return(p => p.Node<string>());
                    var foo = query.Results.ToDynamic();
                }
            }
        }

        public IEnumerable<Guid> GetRelatedTubeIds(IEnumerable<Guid> ids)
        {
            var query = client.Cypher.Match("(n)").
                Where("n.id in {ids}").WithParams(new { ids = ids }).
                OptionalMatch("(t1:Tube)-[*0..]->(n)").
                OptionalMatch("(n)-[*0..]->(t2:Tube)").
                With("collect(distinct t1.id)+collect(distinct t2.id) as tube unwind tube as t").
                Return(t => t.As<Guid>());
            return query.Results;
        }

        public IEnumerable<Guid> Filter(IEnumerable<Guid> ids, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                return ids;
            }
            else
            {
                var query = client.Cypher.Match("(u:User)<-[:contains]-(g:Group)-[*]->(n)").
                Where("u.id={userId} and n.id in {ids}").
                WithParams(new { ids = ids, userId = userId }).
                With("distinct n.id as id").
                Return(id => id.As<Guid>());
                return query.Results;
            }
            //  return new Guid[] { };
        }

        public IEnumerable<dynamic> GetNodeCache(Guid nodeId, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var q = client.Cypher.Match("(n {id:{id}})-[:cache]-(c)").WithParams(new { id = nodeId }).Return(c => c.Node<string>());
                return q.Results.ToDynamic();
            }
            else
            {
                //need test!
                var q = client.Cypher.Auth(userId).Match("(g)-[:right]->()-[*0..]->(n {id:{id}})-[:cache]-(c)").WithParams(new { id = nodeId }).Return(c => c.Node<string>());
                return q.Results.ToDynamic();
            }
        }

        public void DeleteNode(Guid nodeId, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var q = client.Cypher.Match("(n {id:{id}})").WithParams(new { id = nodeId });
                var d1 = q.OptionalMatch("(i)-[r1]->(n)").OptionalMatch("(n)-[r2]->(o)").Delete("r1,r2");
                d1.ExecuteWithoutResults();
                var d2 = q.Delete("n");
                d2.ExecuteWithoutResults();
            }
        }

        public IEnumerable<dynamic> GetPollNodes(string serverName)
        {
            var query = client.Cypher.
                    Match("(s:SurveyServer)<-[:server]-(res)<-[:resource]-(start)").
                    OptionalMatch("(start)<-[:contains*0..]-(end)").
                    Where("s.name={serverName}").
                    With("distinct end").
                    OptionalMatch("(end)-[:device]->(dev)").
                    Set("end.device=dev.id").
                    Set("end.type=labels(end)[0]").
                    WithParams(new { serverName = serverName }).
                    Return(end => end.Node<string>());

            var haa = query.Results.ToDynamic();
            return haa;
        }

        public IEnumerable<Guid> GetRelatedIds(IEnumerable<Guid> ids, string redirect)
        {
            var query = client.Cypher.Match(string.Format("(start)-[*]->(end:{0})", redirect)).Where("start.id in {ids}").
                WithParams(new { ids = ids }).
                With("end.id as id").Return(id => id.As<Guid>());
            return query.Results;
        }

        public IEnumerable<Guid> GetRelatedIds(IEnumerable<Guid> ids)
        {
            var query = client.Cypher.Match("(start:Tube)-[*]->(end)").Where("start.id in {ids}").
                WithParams(new { ids = ids }).
                With("end.id as id").ReturnDistinct(id => id.As<Guid>());
            return query.Results;
        }

        public IEnumerable<Guid> GetTubeNeighbourIds(Guid tubeId)
        {
            var query = client.Cypher.Match("(start:Tube {id:{tubeId}})-[*0..]->(end)").
                WithParams(new { tubeId = tubeId }).
                With("end.id as id").ReturnDistinct(id => id.As<Guid>());
            return query.Results;
        }

        public void DeleteState(Guid tubeId)
        {
            var q = client.Cypher.Match("(t {id:{id}})-[r:cache]->(s:State)").WithParams(new { id = tubeId });
            var d1 = q.OptionalMatch("(i)-[r1]->(s)").OptionalMatch("(s)-[r2]->(o)").Delete("r1,r2");
            d1.ExecuteWithoutResults();
            var d2 = q.Delete("s");
            d2.ExecuteWithoutResults();
        }

        public void SaveDriver(dynamic driver, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                dynamic drv = new ExpandoObject();
                drv.driver = driver.driver;
                var ddriver = driver as IDictionary<string, object>;
                ddriver.Remove("driver");
                var q = client.Cypher.Match("(d:Device {id:{id}})<-[:driver]-(dr:Driver)").Set("d+={driver}").Set("dr+={drv}").
                    WithParams(new { id = driver.id, driver = driver, drv = drv }).Return(d => d.Node<string>());
                var x = q.Results;
            }
        }
        public void CreateDriver(string name, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                Guid guid1 = Guid.NewGuid();
                Guid guid2 = Guid.NewGuid();
                var q = client.Cypher.Create("(dr:Driver{id:{id1},driver:''})-[c:driver]->(dev:Device{id:{id2},name:{name}})").
                    WithParams(new { id1 = guid1, id2 = guid2, name = name}).Return(dev => dev.Node<string>());
                var x = q.Results;
            }
        }
        public void SaveMaquette(dynamic maq)
        {
            var dmaq = maq as IDictionary<string, object>;
            if (dmaq.ContainsKey("Tube"))
            {
                dmaq.Remove("Tube");
            }
            if (dmaq.ContainsKey("Task"))
            {
                dmaq.Remove("Task");
            }
            var q = client.Cypher.Match("(m:Maquette {id:{id}})").Set("m+={maq}").
                WithParams(new { id = maq.id, maq = maq }).Return(m => m.Node<string>());
            var x = q.Results;
        }
        public List<Guid> GetIdsByTubeId(Guid objectId)
        {
            string q = client.Cypher.Match("(t:Tube {id: {objectId}})").WithParams(new { objectId = objectId }).With("t.tp as tp").Return(tp => tp.As<string>()).Results.ToArray()[0];
            var q1 = client.Cypher.Match("(t:Tube {id: {objectId}})--(:Area)--(:Folder)--(:Area)--(t1:Tube)").WithParams(new { objectId = objectId }).Return(t1 => t1.Node<string>()).Results.ToDynamic();//.With("t1.id as id, t1.name as name, t.name as name").Return(id => id.As<Guid>());

            List<Guid> guids = new List<Guid>();
            foreach (var tube in q1)
            {
                var dtube = tube as IDictionary<string,dynamic>;
                if (dtube.ContainsKey("tp") && tube.tp == q)
                {
                    guids.Add(Guid.Parse(tube.id));
                }
            }
            return guids;
        }
        public dynamic GetPollServer(string serverName)
        {
            var q = client.Cypher.Match("(s:SurveyServer {name:{name}})").WithParams(new { name = serverName }).Return(s => s.Node<string>());
            return q.Results.ToDynamic().FirstOrDefault();
        }

        public void AddOrUpdRelation(Guid start, Guid end, string type, dynamic body, Guid userId)
        {
            var query = client.Cypher;
            if (IsSuperUser(userId))
            {
                query = client.Cypher.Match("(a {id:{start}})").Match("(b {id:{end}})");
            }
            else
            {
                query = client.Cypher.Match("(g:Group)-[:contains]->(u:User {id:{userId}})").Match("(g)-[*0..]->(a {id:{start}})").Match("(g)-[*0..]->(b {id:{end}})");
            }

            query = query.Merge(string.Format("(a)-[r:{0}]->(b)", type)).Set("r={body}").
                WithParams(new { start = start, end = end, body = body, userId = userId });
            var res = query.Return(r => r.As<string>()).Results;
        }

        public void DelRelation(Guid start, Guid end, string type, Guid userId)
        {
            var query = client.Cypher;
            if (IsSuperUser(userId))
            {
                query = client.Cypher.Match("(a {id:{start}})").Match("(b {id:{end}})");
            }
            else
            {
                query = client.Cypher.Match("(g:Group)-[:contains]->(u:User {id:{userId}})").Match("(g)-[*]->(a {id:{start}})").Match("(g)-[*]->(b {id:{end}})");
            }

            query = query.Match(string.Format("(a)-[r:{0}]->(b)", type)).
                WithParams(new { start = start, end = end, userId = userId });
            query.Delete("r").ExecuteWithoutResults();
        }

        public void AddNode(Guid id, string type, dynamic body, Guid userId)
        {
            var dbody = body as IDictionary<string, object>;
            foreach (var key in dbody.Keys.ToArray())
            {
                if (key.StartsWith("_")) dbody.Remove(key);
            }
            var query = client.Cypher;
            query = query.Match("(u:User {id:{userId}})").Merge(string.Format(@"(a:{0} {{id:{{id}}}})", type)).OnCreate().Set("a={body}").OnMatch().Set("a+={body}").Merge("(u)-[:create]->(a)").
                WithParams(new { id = id, body = body, userId = userId });
            var res = query.Return(a => a.Node<string>()).Results;
        }

        public void UpdNode(Guid id, string type, dynamic body, Guid userId)
        {
            var dbody = body as IDictionary<string, object>;
            foreach (var key in dbody.Keys.ToArray())
            {
                if (key.StartsWith("_")) dbody.Remove(key);
            }
            var query = client.Cypher;
            if (IsSuperUser(userId))
            {
                query = client.Cypher.Match("(a {id:{id}})");
            }
            else
            {
                query = client.Cypher.Match("(u:User {id:{userId}})<-[:contains]-(g:Group)-[*]->(a {id:{id}})");
            }

            query = query.Set("a+={body}").
                WithParams(new { id = id, body = body, userId = userId });
            var res = query.Return(a => a.Node<string>()).Results;
        }

        public void DelNode(Guid id, string type, Guid userId)
        {
            var query = client.Cypher;
            if (IsSuperUser(userId))
            {
                query = client.Cypher.Match("(n {id:{id}})");
            }
            else
            {
                query = client.Cypher.Match("(g:Group)-[:contains]->(u:User {id:{userId}})").Match("(g)-[*]->(n {id:{id}})");
            }

            query = query.Match(string.Format("(x:User)-[r]->(n:{0})", type)).
                WithParams(new { id = id, userId = userId });
            query.Delete("r,n").ExecuteWithoutResults();
        }

        public IEnumerable<dynamic> GetConnections(string filter, IEnumerable<string> types, Guid userId)
        {
            var q = client.Cypher;
            if (IsSuperUser(userId))
            {
                q = client.Cypher.Match("(c)").Where(string.Format(@"c.type in {{types}} and (c.name=~'.*{0}.*' or c.imei=~'.*{0}.*' or c.phone=~'.*{0}.*' or c.host=~'.*{0}.*' or tostring(c.port)=~'.*{0}.*' or c.id=~'.*{0}.*')", filter)).
                    With("c limit 10");
            }
            else
            {
                q = client.Cypher.Match("(u:User {id:{userId}})<--(g:Group)-[*]->(c)").Where(string.Format(@"c.type in {{types}} and (c.name=~'.*{0}.*' or c.imei=~'.*{0}.*' or c.phone=~'.*{0}.*' or c.host=~'.*{0}.*' or tostring(c.port)=~'.*{0}.*' or c.id=~'.*{0}.*')", filter)).
                    With("c limit 10");
            }
            q = q.WithParams(new { types = types.ToArray(), userId = userId });
            var res = q.Return(c => c.Node<string>()).Results;
            return res.ToDynamic();
        }

        public dynamic GetArea(Guid tubeId, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var q = client.Cypher.
                    Match("(a:Area)-[:contains]->(t:Tube {id:{tubeId}})").
                    WithParams(new { tubeId = tubeId }).
                    Return(a => a.Node<string>());
                return q.Results.ToDynamic().FirstOrDefault();
            }
            else
            {
                var q = client.Cypher.
                    Auth(userId).
                    Match("(a:Area)-[:contains]->(t:Tube {id:{tubeId}})").
                    WithParams(new { tubeId = tubeId }).
                    Return(a => a.Node<string>());
                return q.Results.ToDynamic().FirstOrDefault();
            }
        }

        public dynamic GetTube(Guid tubeId, Guid userId)
        {
            var q = client.Cypher.Match("(t:Tube {id:{tubeId}})").WithParams(new { tubeId = tubeId }).Return(t => t.Node<string>());
            return q.Results.ToDynamic().FirstOrDefault();
        }
        public void DelAllRelationObjects(Guid id, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.Match("(a {id:{id}})-[s]-()").WithParams(new { id = id });
                query.Delete("s").ExecuteWithoutResults();
            }
        }
        public bool DelObject(Guid id, Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.Match("(a {id:{id}})").WithParams(new { id = id });
                query.Delete("a").ExecuteWithoutResults();
                query = client.Cypher.Match("(a {id:{id}})").WithParams(new { id = id }).Return(a => a.Node<string>()).Results.ToDynamic().FirstOrDefault();
                if (query == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        public dynamic GetTubeDevice(Guid tubeId, Guid userId)
        {
            var q = client.Cypher.Match("(t:Tube {id:{tubeId}})-[:device]->(d:Device)").
                WithParams(new { tubeId = tubeId }).Return(d => d.Node<string>());
            return q.Results.ToDynamic().FirstOrDefault();
        }

        public dynamic GetHouseTubes(Guid tubeId, Guid userId)
        {
            var q = client.Cypher.Match("(tube:Tube {id:{tubeId}})-[:reference]->(t:Tube)").
                WithParams(new { tubeId = tubeId }).Return(t => t.Node<string>());
            return q.Results.ToDynamic();
        }

        public dynamic GetTubeConnections(Guid tubeId, Guid userId)
        {
            var types = new string[] { "CsdConnection", "MatrixConnection", "LanConnection" };
            var q = client.Cypher.Match("(t:Tube {id:{tubeId}})-[:contains]->(c)").
                WithParams(new { tubeId = tubeId }).Return(c => c.Node<string>());
            return q.Results.ToDynamic();
        }

        public IEnumerable<dynamic> GetTubeRelations(Guid tubeId, Guid userId)
        {
            var q = client.Cypher.Match("(t:Tube {id:{tubeId}})-[*0..]->(start)-[r]->(end)").
                WithParams(new { tubeId = tubeId }).
                With("r as rel, start.id as start,end.id as end").
                    Return((rel, start, end) => new { rel = rel.Node<string>(), start = start.As<Guid>(), end = end.As<Guid>() });
            var x = q.Results;
            var y = x.Select(r =>
            {
                dynamic rel = r.rel.ToDynamic();
                rel.start = r.start;
                rel.end = r.end;
                return rel;
            });
            return y;
        }

        public IEnumerable<dynamic> GetPollPorts(Guid userId)
        {
            var q = client.Cypher;
            if (IsSuperUser(userId))
            {
                q = q.Match("(p)-[:server]->(s:SurveyServer)");
            }
            else
            {
                q = q.Match("(u:User {id:{id}})<--(g:Group)-[*]->(p)-[:server]->(s:SurveyServer)").WithParams(new { id = userId });
            }
            return q.Return(p => p.Node<string>()).Results.ToDynamic();
        }

        public IEnumerable<dynamic> GetGroups(Guid userId)
        {
            var q = client.Cypher;
            if (IsSuperUser(userId))
            {
                q = q.Match("(g:Group)").OptionalMatch("(g)-[:contains]->(c)").Return((g, c) => new { g = g.Node<string>(), c = c.CollectAs<string>() }).
                UnionAll().Match("(g:User)").OptionalMatch("(g)-[:contains]->(c)");
            }
            else
            {
                q = q.Match("(u:User {id:{id}})<--(rg:Group)-[*0..]->(g:Group)").OptionalMatch("(g)-[:contains]->(c)").Return((g, c) => new { g = g.Node<string>(), c = c.CollectAs<string>() }).
                UnionAll().Match("(u:User {id:{id}})<--(rg:Group)").OptionalMatch("(rg)-[*0..]->(g:User)").OptionalMatch("(g)-[:contains]->(c)").WithParams(new { id = userId });
            }

            return q.Return((g, c) => new { g = g.Node<string>(), c = c.CollectAs<string>() }).Results.Select(r =>
            {
                var group = r.g.ToDynamic();
                group._childrenIds = r.c.ToArray().Select(i => i.ToDynamic().data.id); ;//r.c.ToDynamic().Select(i => i.id);
                return group;
            });
        }

        public IEnumerable<dynamic> GetRightRelations(Guid userId, Guid targetId)
        {
            var q = client.Cypher.Match("(g:Group)-[:right]->(t {id:{id}})").WithParams(new { id = targetId }).Return((g, t) => new { g = g.Node<string>(), t = t.Node<string>() });
            return q.Results.Select(r =>
            {
                dynamic relation = new ExpandoObject();
                relation.start = r.g.ToDynamic().id;
                relation.end = r.t.ToDynamic().id;
                relation.type = "right";
                return relation;
            });
        }

        public IEnumerable<dynamic> GetNeightbours(Guid startId, Guid userId)
        {
            var q = client.Cypher.Match("(s {id:{startId}})-[r:contains]->(e)").WithParams(new { startId = startId }).Return((r, e) => new { r = r.Node<string>(), e = e.Node<string>() });
            return q.Results.Select(r =>
            {
                var rec = r.e.ToDynamic();
                rec._relation = r.r.ToDynamic();
                return rec;
            });
        }

        public dynamic GetFolder(Guid id, Guid userId)
        {
            var req = client.Cypher.Match("(f:Folder)").
                Where("f.id={id}").
                WithParams(new { id = id }).
                OptionalMatch("(r:Folder)-[:contains]->(f)").
                Return((r, f) => new
                {
                    folder = f.Node<string>(),
                    parent = r.Node<string>()
                }).
                Results.Select(r =>
                {
                    dynamic pack = new ExpandoObject();
                    pack.folder = r.folder.ToDynamic();
                    if (r.parent != null)
                    {
                        pack.parent = r.parent.ToDynamic();
                    }
                    else
                    {
                        pack.parent = null;
                    }
                    return pack;
                }).FirstOrDefault();

            return req;
        }

        public Guid GetIdAreaForTube(Guid tubeId, Guid userId)
        {
            var area = GetArea(tubeId, userId);
            if (area != null)
                return Guid.Parse((string)area.id);

            return Guid.Empty;
        }

        public dynamic GetFoldersTubes(IEnumerable<Guid> tubeIds, Guid userId)
        {
            IEnumerable<dynamic> raw;
            if (IsSuperUser(userId))
            {
                var query = client.Cypher.
                    Match("(f:Folder)-[*]->(t:Tube)").
                    Where("t.id in {ids}").WithParams(new { ids = tubeIds }).
                    With("f,collect(distinct t) as n").
                    Return((f, n) => new { f = f.Node<string>(), n = n.As<IEnumerable<Node<string>>>() });

                raw = query.Results;
            }
            else
            {
                var query = client.Cypher.
                    Auth(userId).
                    Match("(g)-[:right]->(f:Folder)-[*]->(t:Tube)").
                    Where("t.id in {ids}").WithParams(new { ids = tubeIds }).
                    With("f,collect(distinct t) as n").
                    Return((f, n) => new { f = f.Node<string>(), n = n.As<IEnumerable<Node<string>>>() });

                raw = query.Results;
            }

            var results = raw.Select(r =>
            {
                dynamic folder = (r.f as Node<string>).ToDynamic();

                foreach (var t in (r.n as IEnumerable<Node<string>>).ToDynamic().GroupBy(d => d.type))
                {
                    //if (g.Key == "Device")
                    //{
                    //    foreach (var dev in g)
                    //    {
                    //        var devd = dev as IDictionary<string, object>;
                    //        if (devd.ContainsKey("driver")) devd.Remove("driver");
                    //    }
                    //}
                    var dfolder = folder as IDictionary<string, object>;
                    dfolder.Add(t.Key, t.ToArray());
                }
                return folder;
            });

            return results.ToArray();
        }

        public IEnumerable<dynamic> GetTags(Guid tubeId, dynamic userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.
                    Cypher.Match("(t:Tube {id:{tubeId}})-[:tag]->(p)").
                    WithParams(new { tubeId = tubeId }).
                    Return(p => p.Node<string>());
                return query.Results.ToDynamic().ToArray();
            }
            else
            {
                var query = client.Cypher.
                    Match("(u:User {id:{userId}})<--(g:Group)").
                    Match("(g)-[*]->(t:Tube {id:{tubeId}})").
                    Match("(t)-[:tag]->(p)").
                    WithParams(new { tubeId = tubeId, userId = userId }).
                    Return(p => p.Node<string>());
                return query.Results.ToDynamic().ToArray();
            }
        }

        public IEnumerable<Guid> GetTubeIdsByFolder(Guid userId, Guid folderId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.
                    Cypher.Match("(f:Folder {id:{folderId}})-[*]->(t:Tube)").
                    With("t.id as id").
                    WithParams(new { folderId = folderId }).
                    Return(id => id.As<Guid>());
                return query.Results.ToArray();
            }
            else
            {
                var query = client.Cypher.
                    Match("(u:User {id:{userId}})<--(g:Group)").
                    Match("(g)-[*]->(f:Folder {id:{folderId}})-[*]->(t:Tube)").
                    With("t.id as id").
                    WithParams(new { folderId = folderId, userId = userId }).
                    Return(id => id.As<Guid>());
                return query.Results.ToArray();

            }
        }

        public IEnumerable<Guid> GetTubeIds(Guid userId)
        {
            if (IsSuperUser(userId))
            {
                var query = client.
                    Cypher.Match("(t:Tube)").
                    With("t.id as id").
                    Return(id => id.As<Guid>());
                return query.Results.ToArray();
            }
            else
            {
                var query = client.Cypher.
                    Match("(u:User {id:{userId}})<--(g:Group)").
                    Match("(g)-[*]->(t:Tube)").
                    With("t.id as id").
                    WithParams(new { userId = userId }).
                    Return(id => id.As<Guid>());
                return query.Results.ToArray();
            }
        }
        public Guid GetTubeIdFromIMEIandNetworkAddress(string imei, string networkAddress)
        {
            var query = client.Cypher.
                    Match("(m:TeleofisWrxConnection {imei:{imei}})-[c:contains]-(t:Tube {networkAddress:{networkAddress}})").
                    WithParams(new { imei = imei, networkAddress = networkAddress }).
                    With("t.id as id").
                    Return(id =>id.As<Guid>());
                    //Return(t => new{Tube = t.Node<string>(),});
            var a = query.Results.First();
            return a;
        }
        public Guid GetTubeIdFromIMEIandNAMatrixTerminal(string imei, string networkAddress)
        {
            var query = client.Cypher.
                    Match("(m:MatrixTerminalConnection {imei:{imei}})-[c:contains]-(t:Tube {networkAddress:{networkAddress}})").
                    WithParams(new { imei = imei, networkAddress = networkAddress }).
                    With("t.id as id").
                    Return(id => id.As<Guid>());
            //Return(t => new{Tube = t.Node<string>(),});
            var a = query.Results.First();
            return a;
        }
        public dynamic GetFolders(Guid userId)
        {
            IEnumerable<dynamic> roots;
            if (IsSuperUser(userId))
            {
                var rootsQuery = client.Cypher.
                Match("(f:Folder)").
                OptionalMatch("(r:Folder)-[:contains]->(f)").
                Return((r, f) => new
                {
                    folder = f.Node<string>(),
                    parent = r.Node<string>()
                });
                roots = rootsQuery.Results.Select(r =>
                {
                    var folder = r.folder.ToDynamic();
                    if (r.parent != null)
                    {
                        folder.parent = r.parent.ToDynamic().id;
                    }
                    return folder;
                });
            }
            else
            {

                var rootsQuery = client.Cypher.
                    Auth(userId).
                Match("(g)-[:right]->(f:Folder)").
                OptionalMatch("(r:Folder)-[:contains]->(f)").
                Return((r, f) => new
                {
                    folder = f.Node<string>(),
                    parent = r.Node<string>()
                });
                roots = rootsQuery.Results.Select(r =>
                {
                    var folder = r.folder.ToDynamic();
                    if (r.parent != null)
                    {
                        folder.parent = r.parent.ToDynamic().id;
                    }
                    return folder;
                });

            }
            return roots;

        }
    }

    public static class Extensions
    {
        public static ICypherFluentQuery Auth(this ICypherFluentQuery query, Guid userId)
        {
            return query.Match("(g:Group)-[:contains]->(u:User {id:{userId}})").
                WithParams(new { userId = userId }).
                OptionalMatch("(rg:Group)-[:contains]->(g)").With("g");
        }

        public static ICypherFluentQuery CascadeWhere(this ICypherFluentQuery query, string[] fields, string[] patterns, object[] parameters, int parameterIndex = 0)
        {
            const string PARAMETER = "parameter";

            if (!parameters.Any()) return query;

            var param = parameters.First();

            var parameterName = string.Format("{0}{1}", PARAMETER, parameterIndex);

            var condition = string.Join(" or ", fields.Select(f => string.Format("({0})", string.Join(" or ", patterns.Select(p => string.Format(@"lower({0}.{1})=~{{{2}}}", p, f, parameterName))))));

            query = query.
                Where(condition).
                With("*").
                WithParam(parameterName, param.ToString().ToLower());

            return CascadeWhere(query, fields, patterns, parameters.Skip(1).ToArray(), ++parameterIndex);
        }

        public static dynamic ToDynamic(this string str)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(str);
        }

        public static dynamic ToDynamic(this Node<string> node)
        {
            return JsonConvert.DeserializeObject<ExpandoObject>(node.Data);
        }

        public static IEnumerable<dynamic> ToDynamic(this IEnumerable<Node<string>> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node.ToDynamic();
            }
        }
    }
}

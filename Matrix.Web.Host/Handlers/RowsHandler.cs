using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Handlers
{
    class RowsHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(RowsHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("row");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse(session.userId.ToString());

            if (what == "rows-cache-update")
            {
                RowsCache.Instance.Update(userId);
                var ans = Helper.BuildMessage(what);
                return ans;
            }

            if (what == "rows-get-2")
            {
                var filter = message.body.filter;

                var rows = RowsCache.Instance.Get(filter, userId);
                var ans = Helper.BuildMessage(what);
                ans.body = rows;
                return ans;
            }

            if (what == "rows-get-3")
            {
                var filter = message.body.filter;

                var rows = RowsCache.Instance.Get(filter, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.ids = (rows.rows as IEnumerable<dynamic>).Select(r => r.id).ToArray();
                return ans;
            }

            if (what == "rows-get-4")
            {
                var ids = new List<Guid>();
                foreach (var id in message.body.ids)
                {
                    ids.Add(Guid.Parse(id.ToString()));
                }

                var rows = RowsCache.Instance.Get(ids, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.rows = rows;
                return ans;
            }

            if (what == "rows-get-light")
            {
                var ids = new List<Guid>();
                foreach (var id in message.body.ids)
                {
                    ids.Add(Guid.Parse(id.ToString()));
                }

                var rows = RowsCache.Instance.Get(ids, userId);
                var sw = new Stopwatch();
                
                var datas = new List<dynamic>();
                var data0 = new List<dynamic>();

                var records = RecordsDecorator.Decorate(ids.ToArray(), DateTime.Now.AddMinutes(-20), DateTime.Now.AddHours(1), "Current", new Guid());

                foreach (var id in ids)
                {
                    data0.Clear();
                    foreach (var group in records.Where(r => r.ObjectId == id).GroupBy(r => r.Date))
                    {
                        dynamic rec = new ExpandoObject();
                        rec.date = group.Key;
                        rec.objectId = id;
                        var drec = rec as IDictionary<string, object>;
                        foreach (var record in group)
                        {
                            var unit = record.S2;
                            var val = record.D1;

                            var name = record.S1.Replace(".", "").Replace(" ", "");
                            if (!drec.ContainsKey(name))
                            {
                                drec.Add(name, val);
                            }
                        }
                        data0.Add(rec);
                    }
                    if(data0.Count > 0)
                        datas.Add(data0[data0.Count-1]);
                }
                foreach(var row in rows)
                {
                    foreach(var data in datas)
                    {
                        if(row.id == data.objectId)
                        {
                            row.dataLight = data;
                        }
                    }
                }
                var ans = Helper.BuildMessage(what);
                
                ans.body.rows = rows;
                return ans;
            }
            if (what == "rows-get")
            {
                var sw = new Stopwatch();

                var result = new List<dynamic>();

                var ids = new List<Guid>();
                sw.Start();
                foreach (var raw in message.body.ids)
                {
                    var id = Guid.Parse((string)raw);

                    //log.Debug(string.Format("рассматривается строка {0}", id));
                    //check for cache
                    var cachedRow = CacheRepository.Instance.Get("row", id);
                    //var cachedRow = CacheRepository.Instance.GetLocal("row", id);
                    if (cachedRow == null)
                    {
                        //log.Debug(string.Format("строка {0} НЕ найдена в кеше", id));
                        ids.Add(id);
                    }
                    else
                    {
                        //log.Debug(string.Format("строка {0} найдена в кеше", id));

                        var cache = CacheRepository.Instance.Get("cache", id);
                        //var cache = CacheRepository.Instance.GetLocal("cache", id);
                        cachedRow.cache = cache;

                        result.Add(cachedRow);
                    }
                }
                sw.Stop();
                log.Debug(string.Format("поиск в кеше {0} мс", sw.ElapsedMilliseconds));

                sw.Restart();
                if (ids.Any())
                {
                    IEnumerable<dynamic> res = StructureGraph.Instance.GetRows(ids, Guid.Parse(session.userId));

                    foreach (var r in res)
                    {
                        var id = Guid.Parse(r.id.ToString());
                        //log.Debug(string.Format("строка {0} записана в кеш", id));
                        CacheRepository.Instance.Set("row", id, r);
                        var cache = CacheRepository.Instance.Get("cache", id);
                        //CacheRepository.Instance.SetLocal("row", id, r);
                        //var cache = CacheRepository.Instance.GetLocal("cache", id);
                        r.cache = cache;
                        result.Add(r);
                    }
                }
                log.Debug(string.Format("поиск в базе {0} мс", sw.ElapsedMilliseconds));

                var answer = Helper.BuildMessage(what);
                answer.body.rows = result;
                return answer;
            }

            if (what == "row-get-devices")
            {
                var devices = StructureGraph.Instance.GetDevices();
                var answer = Helper.BuildMessage(what);
                answer.body.devices = devices;
                return answer;
            }

            if (what == "row-get-for-edit")
            {
                var tubeId = message.body.id;

                //StructureGraph.Instance
            }

            if (what == "row-save-row")
            {

                foreach (var change in message.body.changes)
                {
                    if (change.mode == "add")
                    {
                        if (change.type == "node")
                        {
                            StructureGraph.Instance.SaveNode(change.entity);
                        }
                        else
                        {
                            StructureGraph.Instance.Merge(change.entity.start, change.entity.end, change.entity, change.entity.type);
                        }
                    }
                    if (change.mode == "upd")
                    {
                        if (change.type == "node")
                        {
                            StructureGraph.Instance.SaveNode(change.entity);
                        }
                        else
                        {
                            StructureGraph.Instance.Merge(change.entity.start, change.entity.end, change.entity, change.entity.type);
                        }
                    }
                    if (change.mode == "del")
                    {
                        if (change.type == "relation")
                        {
                            StructureGraph.Instance.Break(change.entity.start, change.entity.end);
                        }
                    }
                }

                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID))
                    {
                        continue;
                    }

                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);
                }

                var answer = Helper.BuildMessage(what);
                return answer;
            }

            if (what == "rows-get-ids")
            {
                string filter = message.body.filter.text;
                //var groups = (message.body.filter.groups as IEnumerable<string>).Select(i=>Guid.Parse(i)).ToArray();
                var groups = new List<Guid>();
                foreach (var gid in message.body.filter.groups)
                {
                    groups.Add(Guid.Parse(gid.ToString()));
                }
                dynamic res = StructureGraph.Instance.GetRowIds(filter, groups.ToArray(), userId);
                var answer = Helper.BuildMessage(what);
                answer.body.ids = res;
                return answer;
            }

            if (what == "rows-for-server")
            {
                string serverName = message.body.serverName;

                var ans = Helper.BuildMessage(what);
                ans.body.server = StructureGraph.Instance.GetServer(serverName, userId);
                return ans;
            }

            if (what == "row-get-card")
            {
                Guid rowId = Guid.Parse(message.body.rowId.ToString());

                var abnormals = Data.Cache.Instance.GetLastRecords("Abnormal", new Guid[] { rowId });
                var currents = Data.Cache.Instance.GetLastRecords("Current", new Guid[] { rowId });
                var constants = Data.Cache.Instance.GetLastRecords("Constant", new Guid[] { rowId });
                
                var dayDate = Data.Cache.Instance.GetLastDate("Day", rowId);
                var days = (dayDate != DateTime.MinValue) ? Data.RecordsDecorator.Decorate(new[] { rowId }, dayDate, dayDate, "Day", userId).Where(d => d.Date == dayDate) : null;

                dynamic ans = Helper.BuildMessage(what);

                try
                {
                    var fr = StructureGraph.Instance.GetRows(new Guid[] { rowId }, userId).FirstOrDefault();
                    var dfr = (fr as IDictionary<string, object>);

                    if(dfr.ContainsKey("Area") && (fr.Area is IEnumerable<dynamic>) && (fr.Area as IEnumerable<object>).Any() && (fr.Area[0] is IDictionary<string, object>))
                    {
                        var obj = fr.Area[0];
                        var dobj = fr.Area[0] as IDictionary<string, object>;
                        ans.body.addr = dobj.ContainsKey("name")? obj.name : "";
                        ans.body.number = dobj.ContainsKey("number") ? obj.number : "";
                        ans.body.address = dobj.ContainsKey("address") ? obj.address : "";
                    }

                    if (dfr.ContainsKey("Device") && (fr.Device is IEnumerable<dynamic>) && (fr.Device as IEnumerable<object>).Any() && (fr.Device[0] is IDictionary<string, object>))
                    {
                        var obj = fr.Device[0];
                        var dobj = fr.Device[0] as IDictionary<string, object>;
                        ans.body.dev = dobj.ContainsKey("name") ? obj.name : "";
                    }


                    if (dfr.ContainsKey("CsdConnection") && (fr.CsdConnection is IEnumerable<dynamic>) && (fr.CsdConnection as IEnumerable<object>).Any() && (fr.CsdConnection[0] is IDictionary<string, object>))
                    {
                        var obj = fr.CsdConnection[0];
                        var dobj = fr.CsdConnection[0] as IDictionary<string, object>;
                        ans.body.phone = dobj.ContainsKey("phone") ? obj.phone : "";
                    }

                }
                catch (Exception exx)
                {

                }


                if (message.body.matrixId != "undefined")
                {
                    Guid matrixId = Guid.Parse(message.body.matrixId.ToString());
                    var signalDate = Data.Cache.Instance.GetLastDate("MatrixSignal", matrixId);
                    var signal = Data.Cache.Instance.GetRecords(signalDate, signalDate, "MatrixSignal", new Guid[] { matrixId });
                    ans.body.signal = signal.Select(c =>
                    {
                        dynamic d = new ExpandoObject();
                        d.date = c.Date;
                        d.level = c.D1;
                        return d;
                    });
                }

                ans.body.currents = currents.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.serverDate = c.Dt1;
                    d.name = c.S1;
                    d.unit = c.S2;
                    d.value = c.D1;
                    return d;
                });
                ans.body.constants = constants.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.name = c.S1;
                    d.value = c.S2;
                    return d;
                });
                ans.body.days = days == null ? new List<dynamic> { } : days.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.name = c.S1;
                    d.unit = c.S2;
                    d.value = c.D1;
                    return d;
                });
                ans.body.abnormals = abnormals.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.name = c.S1;
                    return d;
                });
                return ans;
            }

            if (what == "row-get-card-before20190201")
            {
                Guid rowId = Guid.Parse(message.body.rowId.ToString());

                var abnormals = Data.Cache.Instance.GetLastRecords("Abnormal", new Guid[] { rowId });
                var currents = Data.Cache.Instance.GetLastRecords("Current", new Guid[] { rowId });
                var constants = Data.Cache.Instance.GetLastRecords("Constant", new Guid[] { rowId });

                var dayDate = Data.Cache.Instance.GetLastDate("Day", rowId);
                var days = (dayDate != DateTime.MinValue) ? Data.RecordsDecorator.Decorate(new[] { rowId }, dayDate, dayDate, "Day", userId).Where(d => d.Date == dayDate) : null;

                dynamic ans = Helper.BuildMessage(what);

                try
                {
                    var fr = StructureGraph.Instance.GetRows(new Guid[] { rowId }, userId).FirstOrDefault();
                    var dfr = (fr as IDictionary<string, object>);

                    if (dfr.ContainsKey("Area") && (fr.Area is IEnumerable<dynamic>) && (fr.Area as IEnumerable<object>).Any() && (fr.Area[0] is IDictionary<string, object>))
                    {
                        var obj = fr.Area[0];
                        var dobj = fr.Area[0] as IDictionary<string, object>;
                        ans.body.addr = dobj.ContainsKey("name") ? obj.name : "";
                        ans.body.number = dobj.ContainsKey("number") ? obj.number : "";
                        ans.body.address = dobj.ContainsKey("address") ? obj.address : "";
                    }

                    if (dfr.ContainsKey("Device") && (fr.Device is IEnumerable<dynamic>) && (fr.Device as IEnumerable<object>).Any() && (fr.Device[0] is IDictionary<string, object>))
                    {
                        var obj = fr.Device[0];
                        var dobj = fr.Device[0] as IDictionary<string, object>;
                        ans.body.dev = dobj.ContainsKey("name") ? obj.name : "";
                    }


                    if (dfr.ContainsKey("CsdConnection") && (fr.CsdConnection is IEnumerable<dynamic>) && (fr.CsdConnection as IEnumerable<object>).Any() && (fr.CsdConnection[0] is IDictionary<string, object>))
                    {
                        var obj = fr.CsdConnection[0];
                        var dobj = fr.CsdConnection[0] as IDictionary<string, object>;
                        ans.body.phone = dobj.ContainsKey("phone") ? obj.phone : "";
                    }

                }
                catch (Exception exx)
                {

                }


                if (message.body.matrixId != "undefined")
                {
                    Guid matrixId = Guid.Parse(message.body.matrixId.ToString());
                    var signalDate = Data.Cache.Instance.GetLastDate("MatrixSignal", matrixId);
                    var signal = Data.Cache.Instance.GetRecords(signalDate, signalDate, "MatrixSignal", new Guid[] { matrixId });
                    ans.body.signal = signal.Select(c =>
                    {
                        dynamic d = new ExpandoObject();
                        d.date = c.Date;
                        d.level = c.D1;
                        return d;
                    });
                }

                ans.body.currents = currents.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.serverDate = c.Dt1;
                    d.name = c.S1;
                    d.unit = c.S2;
                    d.value = c.D1;
                    return d;
                });
                ans.body.constants = constants.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.name = c.S1;
                    d.value = c.S2;
                    return d;
                });
                ans.body.days = days == null ? new List<dynamic> { } : days.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.name = c.S1;
                    d.unit = c.S2;
                    d.value = c.D1;
                    return d;
                });
                ans.body.abnormals = abnormals.Select(c =>
                {
                    dynamic d = new ExpandoObject();
                    d.date = c.Date;
                    d.name = c.S1;
                    return d;
                });
                return ans;
            }
            return Helper.BuildMessage(what);
        }
    }
}

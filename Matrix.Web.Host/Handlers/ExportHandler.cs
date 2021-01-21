using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Data;

namespace Matrix.Web.Host.Handlers
{
    class ExportHandler : IHandler
    {
        private struct ExportRecord
        {
            public string parameter { get; set; }
            public double value { get; set; }
            public string unit { get; set; }
            public DateTime date { get; set; }
            public DateTime dt { get; set; }
        }

        private struct ExportValue
        {
            public Guid id { get; set; }
            public Guid? fiasId { get; set; }
            public string address { get; set; }
            public string resource { get; set; }
            public string name { get; set; }
            public List<ExportRecord> lastRecords { get; set; }
        }

        private static readonly ILog log = LogManager.GetLogger(typeof(ExportHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("export");
        }

        private string[] thousand = new string[] { "тыс. м³", "Тм3" };

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            Guid userId = Guid.Parse(session.userId.ToString());

            if (what == "export-ids")
            {
                var ids = Data.StructureGraph.Instance.GetRowIds("", new Guid[] { }, userId);
                var ans = Helper.BuildMessage(what);
                ans.body.ids = ids;
                return ans;
            }
            if (what == "export-watertower")
            {
                var answer = Helper.BuildMessage(what);
                Guid objectId = Guid.Parse((string)message.body.objectId);
                var tube = StructureGraph.Instance.GetTube(objectId, userId);
                var dtube = tube as IDictionary<string, object>;

                if (dtube.ContainsKey("criticalMax"))
                {
                    answer.body.criticalMax = tube.criticalMax.ToString().Replace(',', '.');
                }
                if (dtube.ContainsKey("controlMode"))
                {
                    answer.body.controlMode = tube.controlMode;
                }
                if (dtube.ContainsKey("criticalMin"))
                {
                    answer.body.criticalMin = tube.criticalMin.ToString().Replace(',', '.'); ;
                }
                if (dtube.ContainsKey("max"))
                {
                    answer.body.max = tube.max.ToString().Replace(',', '.'); ;
                }
                if (dtube.ContainsKey("min"))
                {
                    answer.body.min = tube.min.ToString().Replace(',', '.'); ;
                }
                if (dtube.ContainsKey("interval"))
                {
                    answer.body.interval = tube.interval.ToString().Replace(',', '.'); ;
                }
                return answer;
            }
            if (what == "export-names")
            {
                var names = new List<dynamic>();

                Func<dynamic, dynamic> toName = (d) =>
                {
                    var dd = d as IDictionary<string, object>;

                    dynamic name = new ExpandoObject();
                    name.id = d.id;

                    name.device = "";
                    if (dd.ContainsKey("Device") && d.Device is IEnumerable<dynamic>)
                    {
                        name.device = d.Device[0].name;
                    }

                    name.name = "";
                    if (dd.ContainsKey("Area") && d.Area is IEnumerable<dynamic>)
                    {
                        var area = d.Area[0];
                        var darea = area as IDictionary<string, object>;
                        if (darea.ContainsKey("name")) name.name += area.name + " ";
                        if (darea.ContainsKey("city")) name.name += area.city + " ";
                        if (darea.ContainsKey("street")) name.name += area.street + " ";
                        if (darea.ContainsKey("house")) name.name += area.house + " ";
                    }
                    if (dd.ContainsKey("name")) name.name += d.name + " ";
                    return name;
                };

                var nonCachedIds = new List<Guid>();

                foreach (var rid in message.body.ids)
                {
                    Guid id = Guid.Parse(rid.ToString());

                    var cachedRow = CacheRepository.Instance.Get("row", id);
                    //var cachedRow = CacheRepository.Instance.GetLocal("row", id);
                    if (cachedRow == null)
                    {
                        nonCachedIds.Add(id);
                        continue;
                    }

                    names.Add(toName(cachedRow));
                }

                var rows = StructureGraph.Instance.GetRows(nonCachedIds, userId);
                foreach (var row in rows)
                {
                    var id = Guid.Parse(row.id.ToString());
                    CacheRepository.Instance.Set("row", id, row);
                    //CacheRepository.Instance.SetLocal("row", id, row);
                    names.Add(toName(row));
                }

                var ans = Helper.BuildMessage(what);
                ans.body.names = names;
                return ans;
            }

            if (what == "export-values")
            {
                var ans = Helper.BuildMessage(what);
                bool isAll = false;
                List<Guid> fiasIds = new List<Guid>();

                if ((message.body as IDictionary<string, object>).ContainsKey("fiasIds"))
                {
                    if((message.body.fiasIds is string) && (message.body.fiasIds == "*"))
                    {
                        isAll = true;
                    }
                    else if(message.body.fiasIds is IEnumerable<object>)
                    {
                        foreach(object fiasId in (message.body.fiasIds as IEnumerable<object>))
                        {
                            Guid id = new Guid(fiasId.ToString());
                            fiasIds.Add(id);
                        }
                    }
                }

                IDictionary<Guid, ExportValue> exportData = new Dictionary<Guid, ExportValue>();
                if (isAll || (fiasIds.Count > 0))
                {
                    var rows = StructureGraph.Instance.GetRowsFias(fiasIds, userId);
                    foreach(var row in rows)
                    {
                        exportData[row.Key] = new ExportValue
                        {
                            id = row.Value.Id,
                            fiasId = row.Value.FiasId,
                            address = row.Value.Address,
                            name = row.Value.Name,
                            resource = row.Value.Resource,
                            lastRecords = new List<ExportRecord>()
                        };
                    }
                    
                    var records = RecordsDecorator.DecorateLast(exportData.Keys.ToArray(), "Current", userId);
                    foreach(var record in records)
                    {
                        exportData[record.ObjectId].lastRecords.Add(new ExportRecord {
                            parameter = record.S1, value = record.D1 ?? 0.0, unit = record.S2, date = record.Date, dt = record.Dt1 ?? DateTime.MinValue
                        });
                    }
                }

                ans.body.allIds = isAll;
                ans.body.fiasIds = fiasIds;
                ans.body.data = exportData.Values.ToList();
                return ans;
            }
            
            if (what == "export-data")
            {
                var sw = new Stopwatch();

                string type = message.body.type;
                DateTime start = message.body.start;
                DateTime end = message.body.end;

                var data = new List<dynamic>();

                var ids = new List<Guid>();
                foreach (var id in message.body.ids)
                {
                    ids.Add(Guid.Parse(id.ToString()));
                }

                var records = RecordsDecorator.Decorate(ids.ToArray(), start, end, type, userId);

                foreach (var id in ids)
                {
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
                            if (thousand.Contains(unit))
                            {
                                val *= 1000.0;
                            }

                            var name = record.S1.Replace(".", "").Replace(" ", "");
                            if (!drec.ContainsKey(name))
                            {
                                drec.Add(name, val);
                            }
                        }
                        data.Add(rec);
                    }

                }
                var ans = Helper.BuildMessage(what);
                ans.body.data = data;
                return ans;
            }

            return Helper.BuildMessage(what);
        }
    }
}

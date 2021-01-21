using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json;
using suggestionscsharp;
using System.Configuration;

namespace Matrix.Web.Host.Handlers
{
    /// <summary>
    /// формат сообщений
    /// .action="add"|"del"|"upd"
    /// .target="relation"|"node"
    /// .content={content}
    /// 
    /// где {content}
    /// для target="node"
    /// .id
    /// .type
    /// .body
    /// 
    /// для target="relation"
    /// .start
    /// .end
    /// .type
    /// .body
    /// </summary>
    class EditHandler : IHandler
    {
        const string TARGET_RELATION = "relation";
        const string TARGET_NODE = "node";
        const string ACTION_ADD = "add";
        const string ACTION_DEL = "del";
        const string ACTION_UPD = "upd";

        public SuggestClient api { get; set; }

        private static EditHandler instance;
        public static EditHandler Instance()
        {
            if (instance == null)
            {
                instance = new EditHandler();
            }
            return instance;
        }

        public bool CanAccept(string what)
        {
            return what.StartsWith("edit");
        }

        private EditHandler()
        {
            string token = ConfigurationManager.AppSettings["suggestionsToken"];
            var url = "https://suggestions.dadata.ru/suggestions/api/4_1/rs";
            api = new SuggestClient(token, url);
        }

        private IEnumerable<DataRecord> MakeAudit(string message, Guid userId, IEnumerable<Guid> tubeIds, Guid objectId)
        {
            var audits = new List<DataRecord>();
            foreach (var tubeId in tubeIds)
            {
                var audit = new DataRecord();
                audit.Id = Guid.NewGuid();
                audit.Type = "Audit";
                audit.Date = DateTime.Now;
                audit.ObjectId = tubeId;
                audit.S1 = message;
                audit.G1 = userId;
                audit.G2 = objectId;
                audits.Add(audit);
            }
            return audits;
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse(session.userId.ToString());


            #region edit
            if (what == "edit")
            {
                var relations = new List<dynamic>();
                var nodes = new List<dynamic>();
                var tubeIds = new List<Guid>();
                foreach (var rule in message.body.rules)
                {
                    string action = rule.action;
                    string target = rule.target;
                    dynamic content = rule.content;

                    if (target == TARGET_RELATION)
                    {
                        relations.Add(rule);
                    }
                    if (target == TARGET_NODE)
                    {
                        nodes.Add(rule);
                    }
                }

                var audit = new List<DataRecord>();

                var ids = new List<Guid>();

                //Удаление 1.Связей и 2.Нодов
                foreach (var rule in relations.Union(nodes))
                {
                    string action = rule.action;

                    if (action == ACTION_DEL)
                    {
                        string target = rule.target;
                        dynamic content = rule.content;

                        if (target == TARGET_RELATION)
                        {

                            Guid start = Guid.Parse(content.start.ToString());
                            Guid end = Guid.Parse(content.end.ToString());
                            ids.Add(start);
                            ids.Add(end);
                            string type = content.type;
                            dynamic body = content.body;
                            body.type = type;

                            var leftTubes = StructureGraph.Instance.GetRelatedTubs(start);
                            var rightTubes = StructureGraph.Instance.GetRelatedTubs(end);

                            var relatedTubes = leftTubes.Union(rightTubes).Distinct();

                            //var relatedTubes = StructureGraph.Instance.GetRelatedTubs(end);

                            //if (action == ACTION_DEL)
                            {
                                StructureGraph.Instance.DelRelation(start, end, type, userId);
                                var st = StructureGraph.Instance.GetNodeById(start, userId);
                                var ed = StructureGraph.Instance.GetNodeById(end, userId);
                                if (st.type == "Folder" && ed.type == "Folder")
                                {
                                    audit.AddRange(MakeAudit(string.Format("группа {0} удалена из группы {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Folder" && ed.type == "Area")
                                {
                                    audit.AddRange(MakeAudit(string.Format("плошадка {0} удалена из группы {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "MatrixConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("контроллер матрикс {0} отвязан от объекта {1}", ed.imei, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "CsdConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("модем или контроллер СТЕЛ {0} отвязан от объекта {1}", ed.phone, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "LanConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("ethernet контроллер {0}:{1} отвязан от объекта {2}", ed.host, ed.port, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("удалено соединение {0} -> {1}", st.id, Guid.Parse(ed.id.ToString())), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                            }
                        }
                        else if (target == TARGET_NODE)
                        {
                            Guid id = Guid.Parse(content.id.ToString());

                            ids.Add(id);
                            string type = content.type;
                            dynamic body = content.body;

                            //NamesCache.Instance.Update(body, userId);

                            if (type == "Tube")
                            {
                                tubeIds.Add(id);
                            }

                            var relatedTubes = StructureGraph.Instance.GetRelatedTubs(id);

                            {
                                StructureGraph.Instance.DelNode(id, type, userId);
                                //var st = StructureGraph.Instance.GetNodeById(start, userId);
                                //var ed = StructureGraph.Instance.GetNodeById(end, userId);
                                if (body.type == "Folder")
                                {
                                    audit.AddRange(MakeAudit(string.Format("группа {0} удалена", body.name), userId, relatedTubes, Guid.Parse(body.id.ToString())));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("нод {0} удален", body.id), userId, relatedTubes, Guid.Parse(body.id.ToString())));
                                }
                            }
                        }

                    }
                }

                //Добавление или Изменение 1.Нодов и 2.Связей
                foreach (var rule in nodes.Union(relations))
                {
                    string action = rule.action;
                    string target = rule.target;
                    dynamic content = rule.content;

                    if ((action == ACTION_ADD) || (action == ACTION_UPD))
                    {

                        if (target == TARGET_RELATION)
                        {
                            Guid start = Guid.Parse(content.start.ToString());
                            Guid end = Guid.Parse(content.end.ToString());
                            ids.Add(start);
                            ids.Add(end);
                            string type = content.type;
                            dynamic body = content.body;
                            body.type = type;

                            var leftTubes = StructureGraph.Instance.GetRelatedTubs(start);
                            var rightTubes = StructureGraph.Instance.GetRelatedTubs(end);

                            var relatedTubes = leftTubes.Union(rightTubes).Distinct();

                            //var relatedTubes = StructureGraph.Instance.GetRelatedTubs(end);

                            if (action == ACTION_ADD)
                            {
                                StructureGraph.Instance.AddOrUpdRelation(start, end, type, body, userId);
                                var st = StructureGraph.Instance.GetNodeById(start, userId);//F4E311F5-35E6-4E35-90D5-BA85F1A315CB
                                var ed = StructureGraph.Instance.GetNodeById(end, userId);//f4e311f5-35e6-4e35-90d5-ba85f1a315cb
                                if (st.type == "Folder" && ed.type == "Folder") 
                                {
                                    audit.AddRange(MakeAudit(string.Format("группа {0} добавлена в группу {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Folder" && ed.type == "Area")
                                {
                                    audit.AddRange(MakeAudit(string.Format("плошадка {0} добавлена в группу {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "MatrixConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("контроллер матрикс {0} привязан к объекту {1}", ed.imei, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "CsdConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("модем или контроллер СТЕЛ {0} привязан к объекту {1}", ed.phone, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "LanConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("ethernet контроллер {0}:{1} привязан к объекту {2}", ed.host, ed.port, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлено соединение {0} -> {1}", st.id, Guid.Parse(ed.id.ToString())), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                            }
                            else if (action == ACTION_UPD)
                            {
                                StructureGraph.Instance.AddOrUpdRelation(start, end, type, body, userId);
                                var st = StructureGraph.Instance.GetNodeById(start, userId);
                                var ed = StructureGraph.Instance.GetNodeById(end, userId);
                                if (st.type == "Folder" && ed.type == "Folder")
                                {
                                    audit.AddRange(MakeAudit(string.Format("группа {0} добавлена в группу {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Folder" && ed.type == "Area")
                                {
                                    audit.AddRange(MakeAudit(string.Format("плошадка {0} добавлена в группу {1}", ed.name, st.name), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "MatrixConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("контроллер матрикс {0} привязан к объекту {1}", ed.imei, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "CsdConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("модем или контроллер СТЕЛ {0} привязан к объекту {1}", ed.phone, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else if (st.type == "Tube" && ed.type == "LanConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("ethernet контроллер {0}:{1} привязан к объекту {2}", ed.host, ed.port, st.id), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлено соединение {0} -> {1}", st.id, Guid.Parse(ed.id.ToString())), userId, relatedTubes, Guid.Parse(ed.id.ToString())));
                                }
                            }
                        }
                        else if (target == TARGET_NODE)
                        {
                            Guid id = Guid.Parse(content.id.ToString());

                            ids.Add(id);
                            string type = content.type;
                            dynamic body = content.body;

                            //NamesCache.Instance.Update(body, userId);

                            if (type == "Tube")
                            {
                                tubeIds.Add(id);
                            }
                            else if (type == "Area")
                            {
                                if ((body as IDictionary<string, object>).ContainsKey("addr") && (body.addr is string) && ((body.addr as string) != null) && ((body.addr as string) != ""))
                                {
                                    body.city = "";
                                    body.street = "";
                                    body.house = "";
                                    body.fiasid = "";
                                    body.address = "";

                                    var response = api.QueryAddress(body.addr as string);
                                    if (response != null && response.suggestions.Count > 0)
                                    {
                                        var result = response.suggestions.FirstOrDefault().data;
                                        body.city = result.city_with_type ?? "";
                                        body.street = result.street_with_type ?? "";
                                        body.house = result.house ?? "";
                                        body.fiasid = result.house_fias_id ?? "";
                                        body.address = $"{body.city}, {body.street}, {body.house}";
                                    }
                                }
                            }

                            var relatedTubes = StructureGraph.Instance.GetRelatedTubs(id);

                            if (action == ACTION_ADD)
                            {
                                StructureGraph.Instance.AddNode(id, type, body, userId);

                                var ser = JsonConvert.SerializeObject(body).Replace(@"/""", "'");

                                if (body.type == "Folder")
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлена группа ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "Area")
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлена площадка учета ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "Tube")
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлена точка учета ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "CsdConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлено модемное соединение ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "MatrixConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлено соединение matrix ({0})", ser), userId, relatedTubes, id));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("добавлен объект {0}", ser), userId, relatedTubes, id));
                                }
                            }
                            else if (action == ACTION_UPD)
                            {
                                StructureGraph.Instance.UpdNode(id, type, body, userId);

                                var ser = JsonConvert.SerializeObject(body).Replace(@"/""", "'");

                                if (body.type == "Folder")
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлена группа ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "Area")
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлена площадка учета ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "Tube")
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлена точка учета ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "CsdConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлено модемное соединение ({0})", ser), userId, relatedTubes, id));
                                }
                                else if (body.type == "MatrixConnection")
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлено соединение matrix ({0})", ser), userId, relatedTubes, id));
                                }
                                else
                                {
                                    audit.AddRange(MakeAudit(string.Format("обновлен объект {0}", ser), userId, relatedTubes, id));
                                }
                            }
                        }
                    }
                }

                foreach (var node in nodes)
                {
                    NamesCache.Instance.Update(node.content.body, userId);
                }

                Cache.Instance.SaveRecords(audit);
                RowsCache.Instance.UpdateRow(tubeIds, userId);

                ids.Distinct().ToList().ForEach(id => CacheRepository.Instance.Del("row", id));

                var sessions = CacheRepository.Instance.GetSessions();
                sessions.AsParallel().ForAll(s =>
                {
                    var ds = s as IDictionary<string, object>;
                    if (ds == null || !ds.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID) || ds[SignalRConnection.SIGNAL_CONNECTION_ID] == null)
                    {
                        return;
                    }

                    Guid uId = Guid.Parse(s.userId.ToString());
                    dynamic msg = Helper.BuildMessage("changes");
                    msg.body.rules = new List<dynamic>();
                    foreach (var rule in message.body.rules)
                    {
                        string target = rule.target;
                        dynamic content = rule.content;
                        Guid id = Guid.Empty;
                        if (target == TARGET_RELATION)
                        {
                            id = Guid.Parse(content.end.ToString());
                        }
                        else if (target == TARGET_NODE)
                        {
                            id = Guid.Parse(content.id.ToString());
                        }
                        if (id != Guid.Empty && StructureGraph.Instance.CanSee(id, uId))
                            msg.body.rules.Add(rule);
                    }
                    if (msg.body.rules.Count > 0)
                    {
                        var connectionId = ds[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                        SignalRConnection.RaiseEvent(msg, connectionId);
                    }
                });
            }

            #endregion

            if (what == "edit-get-connections")
            {
                string filter = message.body.filter;
                var types = new List<string>();
                foreach (string type in message.body.types)
                {
                    types.Add(type);
                }
                var dbody = message.body as IDictionary<string, object>;
                Guid startId = Guid.Empty;
                //if (dbody.ContainsKey("startId"))
                //{
                //    startId = Guid.Parse(message.body.startId.ToString());
                //}
                var answer = Helper.BuildMessage(what);
                //StructureGraph.Instance.GetNeightbours(startId, userId);
                answer.body.connections = StructureGraph.Instance.GetConnections(filter, types, userId);
                return answer;
            }

            if (what == "edit-get-wave")
            {
                Guid startId = Guid.Parse(message.body.startId.ToString());
                var answer = Helper.BuildMessage(what);
                answer.body.wave = StructureGraph.Instance.GetNeightbours(startId, userId);
                return answer;
            }

            if (what == "edit-get-devices")
            {
                var answer = Helper.BuildMessage(what);
                answer.body.devices = StructureGraph.Instance.GetDevices();
                return answer;
            }

            if (what == "edit-get-fias")
            {
                var answer = Helper.BuildMessage(what);

                string queryText = "";
                if ((message.body as IDictionary<string, object>).ContainsKey("searchText"))
                {
                    queryText = message.body.searchText;
                }
                AddressSuggestQuery asquery = new AddressSuggestQuery(queryText);

                if ((message.body as IDictionary<string, object>).ContainsKey("searchFias"))
                {
                    string queryFias = message.body.searchFias;
                    AddressData addrData = new AddressData();
                    addrData.fias_id = queryFias;
                    asquery.locations = new AddressData[] { addrData };
                }

                var response = api.QueryAddress(asquery);
                if (response == null)
                {
                    throw new Exception("Сервис подсказок недоступен");
                }

                var results = new List<dynamic>();
                foreach (var suggest in response.suggestions)
                {
                    dynamic result = new ExpandoObject();
                    result.value = suggest.value;
                    result.fiasid = suggest.data.fias_id;
                    result.fiaslvl = suggest.data.fias_level;
                    result.housefiasid = suggest.data.house_fias_id;
                    results.Add(result);
                }
                answer.body.results = results;

                return answer;
            }


            if (what == "edit-get-row")
            {
                var answer = Helper.BuildMessage(what);

                Guid id;
                if (message.body.isNew)
                {
                    answer.body.area = new ExpandoObject();
                    answer.body.area.id = Guid.NewGuid();
                    answer.body.area.type = "Area";
                    answer.body.tube = new ExpandoObject();
                    answer.body.tube.id = Guid.NewGuid();
                    answer.body.tube.type = "Tube";
                }
                else
                {
                    id = Guid.Parse(message.body.id.ToString());
                    answer.body.area = StructureGraph.Instance.GetArea(id, userId);
                    answer.body.tube = StructureGraph.Instance.GetTube(id, userId);
                    answer.body.areaIsNew = false;
                    if (answer.body.tube != null && answer.body.area == null)
                    {
                        answer.body.area = new ExpandoObject();
                        answer.body.area.id = Guid.NewGuid();
                        answer.body.area.type = "Area";
                        answer.body.areaIsNew = true;
                    }
                    answer.body.device = StructureGraph.Instance.GetTubeDevice(id, userId);
                    answer.body.Tube = StructureGraph.Instance.GetHouseTubes(id, userId);
                    //answer.body.connections = StructureGraph.Instance.GetTubeConnections(id, userId);
                    answer.body.relations = StructureGraph.Instance.GetTubeRelations(id, userId);
                }

                answer.body.devices = StructureGraph.Instance.GetDevices();
                //answer.body.ports = StructureGraph.Instance.GetPollPorts(userId);
                return answer;
            }

            //if (what == "edit-get-house")
            //{
            //    var answer = Helper.BuildMessage(what);

            //    Guid id;
            //    if (message.body.isNew)
            //    {
            //        answer.body.area = new ExpandoObject();
            //        answer.body.area.id = Guid.NewGuid();
            //        answer.body.area.type = "Area";
            //        answer.body.tube = new ExpandoObject();
            //        answer.body.tube.id = Guid.NewGuid();
            //        answer.body.tube.type = "Tube";
            //    }
            //    else
            //    {
            //        id = Guid.Parse(message.body.id.ToString());
            //        answer.body.area = StructureGraph.Instance.GetArea(id, userId);
            //        answer.body.tube = StructureGraph.Instance.GetTube(id, userId);
            //        answer.body.device = StructureGraph.Instance.GetTubeDevice(id, userId);
            //        answer.body.relations = StructureGraph.Instance.GetTubeRelations(id, userId);
            //    }

            //    return answer;
            //}

            if (what == "edit-get-folder")
            {
                var answer = Helper.BuildMessage(what);

                Guid id;
                if (message.body.isNew)
                {
                    answer.body.folderNew = new ExpandoObject();
                    answer.body.folderNew.id = Guid.NewGuid();
                    answer.body.folderNew.type = "Folder";
                    //answer.body.connections = new List<object>();
                }

                if (message.body.id != null)
                {
                    id = Guid.Parse(message.body.id.ToString());
                    dynamic pack = StructureGraph.Instance.GetFolder(id, userId);
                    answer.body.folder = pack.folder;
                    answer.body.parent = pack.parent;
                }

                return answer;
            }

            if (what == "edit-get-folder-id")
            {
                var folderId = Guid.Parse((string)message.body.id);
                //
                var folder = StructureGraph.Instance.GetFolderById(folderId, userId);
                //
                var answer = Helper.BuildMessage(what);
                answer.body.folder = folder;
                return answer;
            }

            if (what == "edit-get-folders-id")
            {
                var folders = new List<dynamic>();
                foreach (string sid in message.body.ids)
                {
                    var folderId = Guid.Parse(sid);
                    var folder = StructureGraph.Instance.GetFolderById(folderId, userId);
                    folders.Add(folder);
                }

                var answer = Helper.BuildMessage(what);
                answer.body.folders = folders;
                return answer;
            }

            if (what == "edit-get-branch")
            {
                var answer = Helper.BuildMessage(what);
                Guid id = Guid.Parse(message.body.id.ToString());
                answer.body.branch = StructureGraph.Instance.GetServerBranch(id, userId);
                return answer;
            }

            if (what == "edit-get-area-id")
            {
                var answer = Helper.BuildMessage(what);
                Guid id = Guid.Parse(message.body.id.ToString());
                answer.body.areaId = StructureGraph.Instance.GetIdAreaForTube(id, userId);
                return answer;
            }

            if (what == "edit-get-area")
            {
                var answer = Helper.BuildMessage(what);
                Guid id = Guid.Parse(message.body.id.ToString());
                answer.body.area = StructureGraph.Instance.GetArea(id, userId);
                return answer;
            }

            if (what == "edit-get-name-area")
            {
                var answer = Helper.BuildMessage(what);
                var ids = message.body.ids;
                var areas = new List<string>();
                foreach (var id in ids)
                {
                    Guid guid = Guid.Parse(id.ToString());
                    var area = StructureGraph.Instance.GetArea(guid, userId);
                    if (area != null) areas.Add(area.name);
                }
                answer.body.areas = areas.ToArray();
                return answer;
            }

            if (what == "edit-get-tasks")
            {
                var answer = Helper.BuildMessage(what);
                var tasks = StructureGraph.Instance.GetNodesByType("Task", Guid.Parse(session.userId.ToString()));
                answer.body.tasks = tasks;
                return answer;
            }

            if (what == "edit-delate-tube")
            {
                var answer = Helper.BuildMessage(what);
                if (message.body.objectIds == null) return answer;
                List<Guid> listObjectIds = new List<Guid>();
                foreach(var objectId in message.body.objectIds)
                {
                    Guid id = Guid.Parse(objectId.ToString());
                    StructureGraph.Instance.DelAllRelationObjects(id, userId);
                    if(StructureGraph.Instance.DelObject(id, userId))
                    {
                        listObjectIds.Add(id);
                    }

                }
                if (listObjectIds.Any())
                {
                    Data.Cache.Instance.DeleteRow(listObjectIds);
                }
            }

            if (what == "edit-disable-tube")
            {
                Guid id = Guid.Parse(message.body.id.ToString());
                var tube = StructureGraph.Instance.GetTube(id, userId);
                tube.isDisabled = true;
                StructureGraph.Instance.UpdNode(id, "Tube", tube, userId);

                NamesCache.Instance.Update(tube, userId);
            }

            return Helper.BuildMessage(what);
        }
    }
}

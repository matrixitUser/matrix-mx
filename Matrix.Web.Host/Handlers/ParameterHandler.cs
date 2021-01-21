using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Matrix.Web.Host.Handlers
{
    class ParameterHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("parameter");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            var userId = Guid.Parse(session.userId.ToString());
            string what = message.head.what;

            if (what == "parameters-save")
            {
                var tubeId = Guid.Parse(message.body.tubeId.ToString());
                foreach (var parameter in message.body.parameters)
                {
                    Guid pid = Guid.Parse(parameter.id.ToString());
                    StructureGraph.Instance.UpdNode(pid, "Parameter", parameter, userId);
                    StructureGraph.Instance.AddOrUpdRelation(tubeId, pid, "parameter", new ExpandoObject(), userId);
                    CacheRepository.Instance.SaveParameter(tubeId, parameter);
                }
                //Data.NodeBackgroundProccessor.Instance.AddTokens(tokens);
                var ans = Helper.BuildMessage(what);
                return ans;
            }

            if (what == "parameters-save-tags")
            {
                var tubeId = Guid.Parse(message.body.tubeId.ToString());
                CacheRepository.Instance.SetTags(tubeId, message.body.tags);
                var ans = Helper.BuildMessage(what);
                return ans;
            }

            if (what == "parameters-delete")
            {
                //var tokens = new List<dynamic>();
                //foreach (var id in message.body.ids)
                //{
                //    dynamic token = new ExpandoObject();
                //    token.action = "delete";
                //    token.start = new ExpandoObject();
                //    token.start.id = Guid.Parse((string)id);
                //    token.start.type = "Parameter";
                //    token.userId = userId;
                //    tokens.Add(token);
                //    //Data.StructureGraph.Instance.DeleteNode(Guid.Parse((string)id), Guid.Parse((string)session.User.id));
                //}
                ////Data.NodeBackgroundProccessor.Instance.AddTokens(tokens);
                var ans = Helper.BuildMessage(what);
                return ans;
            }

            if (what == "parameters-get")
            {
                Guid tubeId = Guid.Parse(message.body.tubeId.ToString());
                dynamic answer = Helper.BuildMessage(what);
                var parameters = StructureGraph.Instance.GetParameters(tubeId, userId);
                answer.body.parameters = parameters;
                return answer;
            }
            
            if (what == "parameters-get-2")
            {
                Guid tubeId = Guid.Parse(message.body.tubeId.ToString());
                dynamic answer = Helper.BuildMessage(what);

                var tags = StructureGraph.Instance.GetTags(tubeId, userId);
                var parameters = StructureGraph.Instance.GetParameters(tubeId, userId);

                answer.body.parameters = parameters;
                answer.body.tags = tags;
                return answer;
            }

            if (what == "parameters-get-2")
            {
                Guid tubeId = Guid.Parse(message.body.tubeId.ToString());
                dynamic answer = Helper.BuildMessage(what);

                var tags = StructureGraph.Instance.GetTags(tubeId, userId);
                var parameters = StructureGraph.Instance.GetParameters(tubeId, userId);

                answer.body.parameters = parameters;
                answer.body.tags = tags;
                return answer;
            }
            if (what == "parameters-get-3")
            {
                dynamic answer = Helper.BuildMessage(what);
                var tubes = new List<dynamic>();

                foreach (string tubeId in message.body.tubeIds)
                {
                    dynamic tube = new ExpandoObject();
                    Guid id = Guid.Parse(tubeId.ToString());
                    var parameters = StructureGraph.Instance.GetParameters(id, userId);
                    tube.tubeId = id;
                    tube.parameters = parameters;
                    tubes.Add(tube);

                }
                answer.body.tubes = tubes;
                return answer;
            }
            if (what == "parameters-recalc")
            {
                Guid tubeId = Guid.Parse(message.body.tubeId.ToString());
                var all = new List<DataRecord>();
                try
                {
                    //Hour
                    {
                        var date = Data.Cache.Instance.GetLastDate("Hour", tubeId);
                        if (date == DateTime.MinValue)
                        {
                            all.AddRange(Data.Cache.Instance.GetRecords(DateTime.Now.AddHours(-24), DateTime.Now.AddHours(8), "Hour", new Guid[] { tubeId }));
                        }
                        else
                        {
                            all.AddRange(Data.Cache.Instance.GetRecords(date.AddHours(-3), date.AddHours(+1), "Hour", new Guid[] { tubeId }));
                        }
                    }

                    //Day
                    {
                        var date = Data.Cache.Instance.GetLastDate("Day", tubeId);
                        if (date == DateTime.MinValue)
                        {
                            all.AddRange(Data.Cache.Instance.GetRecords(DateTime.Now.AddDays(-7), DateTime.Now.AddDays(1), "Day", new Guid[] { tubeId }));
                        }
                        else
                        {
                            all.AddRange(Data.Cache.Instance.GetRecords(date.AddDays(-3), date.AddDays(+1), "Day", new Guid[] { tubeId }));
                        }
                    }

                    //Current
                    {
                        var date = Data.Cache.Instance.GetLastDate("Current", tubeId);
                        all.AddRange(Data.Cache.Instance.GetRecords(date, date, "Current", new Guid[] { tubeId }));
                    }

                    var s1 = all.Select(r => r.S1).Distinct();

                    var oldParameters = StructureGraph.Instance.GetParameters(tubeId, userId);
                    foreach (var oldParameter in oldParameters)
                    {
                        StructureGraph.Instance.DeleteNode(Guid.Parse(oldParameter.id.ToString()), userId);
                    }
                    CacheRepository.Instance.DelParameters(tubeId);

                    foreach (var s in s1)
                    {
                        dynamic parameter = new ExpandoObject();
                        parameter.id = Guid.NewGuid();
                        parameter.name = s;
                        parameter.type = "Parameter";

                        StructureGraph.Instance.AddNode(parameter.id, "Parameter", parameter, userId);
                        StructureGraph.Instance.AddOrUpdRelation(tubeId, parameter.id, "parameter", new ExpandoObject(), userId);
                        CacheRepository.Instance.SaveParameter(tubeId, parameter);
                    }
                }
                catch (Exception ex)
                {
                    return Helper.BuildMessage(what);
                }
            }

            if (what == "parameters-recalc-driver")
            {
                Guid tubeId = Guid.Parse(message.body.tubeId.ToString());
                
                try
                {
                    dynamic dev = StructureGraph.Instance.GetTubeDevice(tubeId, userId);

                    var ddev = dev as IDictionary<string, object>;
                    if(ddev.ContainsKey("tags"))
                    {
                        //получение сведений

                        var serializer = new JavaScriptSerializer();
                        var tags = new List<dynamic>();
                        foreach (var strTag in dev.tags)
                        {
                            var tag = serializer.Deserialize(strTag, typeof(object)) as Dictionary<string, object>;

                            dynamic parameter = new ExpandoObject();
                            parameter.id = Guid.NewGuid();
                            parameter.name = tag["name"];
                            parameter.parameter = tag["parameter"];
                            parameter.calc = tag["calc"];
                            parameter.dataType = tag["dataType"];
                            parameter.type = "Tag";
                            tags.Add(parameter);
                        }

                        //удаление старых тегов

                        //var oldParameters = StructureGraph.Instance.GetParameters(tubeId, userId);
                        //foreach (var oldParameter in oldParameters)
                        //{
                        //    StructureGraph.Instance.DeleteNode(Guid.Parse(oldParameter.id.ToString()), userId);
                        //}
                        //CacheRepository.Instance.DelParameters(tubeId);

                        var oldTags = StructureGraph.Instance.GetTags(tubeId, userId);
                        foreach (var oldTag in oldTags)
                        {
                            StructureGraph.Instance.DeleteNode(Guid.Parse(oldTag.id.ToString()), userId);
                        }

                        ////установка параметров

                        //foreach (var s in tags.Select(r => r.parameter).Distinct())
                        //{
                        //    if (s == "") continue;

                        //    dynamic parameter = new ExpandoObject();
                        //    parameter.id = Guid.NewGuid();
                        //    parameter.name = s;
                        //    parameter.type = "Parameter";

                        //    StructureGraph.Instance.AddNode(parameter.id, "Parameter", parameter, userId);
                        //    StructureGraph.Instance.AddOrUpdRelation(tubeId, parameter.id, "parameter", new ExpandoObject(), userId);
                        //    CacheRepository.Instance.SaveParameter(tubeId, parameter);
                        //}

                        //установка тегов

                        foreach (var t in tags)
                        {
                            StructureGraph.Instance.AddNode(t.id, "Tag", t, userId);
                            StructureGraph.Instance.AddOrUpdRelation(tubeId, t.id, "tag", new ExpandoObject(), userId);
                        }

                        CacheRepository.Instance.SetTags(tubeId, tags);
                    }
                }
                catch (Exception ex)
                {
                    return Helper.BuildMessage(what);
                }
            }

            return Helper.BuildMessage(what);
        }
    }
}

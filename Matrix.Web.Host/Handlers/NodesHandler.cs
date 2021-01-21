using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Matrix.Web.Host.Data;
using CoordinateSharp;
using Matrix.Web.Host.Transport;

namespace Matrix.Web.Host.Handlers
{
    class NodesHandler : IHandler
    {
        public bool CanAccept(string what)
        {
            return what.StartsWith("node");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            var userId = Guid.Parse(session.userId.ToString());

            if (what == "node-states")
            {
                foreach (var state in message.body.states)
                {
                    Guid nodeId = Guid.Parse(state.nodeId.ToString());
                    var cache = Data.CacheRepository.Instance.GetCache(nodeId);
                    if (cache == null)
                        cache = new ExpandoObject();
                    cache.State = state;
                    Data.CacheRepository.Instance.SaveCache(nodeId, cache);
                    Data.RowsCache.Instance.UpdateState(state, nodeId, userId);
                    Carantine.Instance.Push(nodeId, (int)state.code);
                }
            }
            if (what == "node-get-astronomTimer")
            {
                var answer = Helper.BuildMessage(what);
                Guid id = Guid.Parse(message.body.objectId.ToString());
                dynamic tube = StructureGraph.Instance.GetTube(id, userId);

                var dtube = tube as IDictionary<string, object>;

                if (dtube.ContainsKey("coordinates"))
                {
                    answer.body.coordinates = (string)tube.coordinates;
                }
                if (dtube.ContainsKey("afterBeforeSunSetRise"))
                {
                    answer.body.afterBeforeSunSetRise = (string)tube.afterBeforeSunSetRise;

                }
                if (dtube.ContainsKey("utc"))
                {
                    answer.body.utc = (string)tube.utc;
                }
                return answer;
            }
            if (what == "node-update-astronomTimer")
            {
                string coordinates = (string)message.body.coordinates;
                string utc = (string)message.body.utc;
                string afterBeforeSunSetRise = (string)message.body.afterBeforeSunSetRise;
                Guid objectId = Guid.Parse((string)message.body.objectId);
                StructureGraph.Instance.UpdateLightControlsAstronTimersValues(coordinates,utc, afterBeforeSunSetRise, objectId);
                var ans = Helper.BuildMessage(what);
                return ans;
            }
            if (what == "node-controller-data")
            {
                dynamic control = (dynamic)message.body.control;
                var dcontrol = control as IDictionary<string, object>;
                Guid objectId = Guid.Parse((string)message.body.objectId);
                //для освещения
                if (dcontrol.ContainsKey("controllerData"))
                {
                    string controllerData = (string)control.controllerData;
                    Data.RowsCache.Instance.UpdateControllerData(controllerData, objectId, userId);
                    Carantine.Instance.Push(objectId);
                }
                if (dcontrol.ContainsKey("lightV2Config"))
                {
                    string strConfig = (string)control.lightV2Config;
                    StructureGraph.Instance.UpdateControlConfig(strConfig, objectId); //strConfig
                    var ans = Helper.BuildMessage(what);
                    return ans;
                }
                if (dcontrol.ContainsKey("controllerConfig"))
                {
                    string strConfig = (string)control.controllerConfig;
                    StructureGraph.Instance.UpdateControlConfig(strConfig, objectId); //strConfig
                    var ans = Helper.BuildMessage(what);
                    return ans;
                }
            }
            if(what == "node-config")
            {

            }
            if (what == "node-events")
            {
                int events = Convert.ToInt32(message.body.events);
                Guid objectId = Guid.Parse((string)message.body.objectId);
                Data.RowsCache.Instance.UpdateEvents(events, objectId, userId);
                Carantine.Instance.Push(objectId);
            }
            if (what == "node-watertower")
            {
                float max = Convert.ToSingle(message.body.max);
                float min = Convert.ToSingle(message.body.min);
                int controlMode = Convert.ToInt32(message.body.controlMode);
                Guid objectId = Guid.Parse((string)message.body.objectId);
                StructureGraph.Instance.UpdateWaterTowenParametr(min, max, controlMode, objectId);
                List<dynamic> rules = new List<dynamic>();
                var connections = StructureGraph.Instance.GetNeightbours(objectId, userId);
                foreach(var connection in connections)
                {
                    dynamic ruleConnection = new ExpandoObject();
                    ruleConnection.action = "upd";
                    ruleConnection.target = "node";
                    ruleConnection.content = new ExpandoObject();
                    ruleConnection.content.id = connection.id;
                    ruleConnection.content.body = connection;
                    ruleConnection.content.type = (string)connection.type;
                    rules.Add(ruleConnection);
                }
                var area = StructureGraph.Instance.GetArea(objectId, userId);
                dynamic ruleArea = new ExpandoObject();
                ruleArea.action = "upd";
                ruleArea.target = "node";
                ruleArea.content = new ExpandoObject();
                ruleArea.content.id = area.id;
                ruleArea.content.body = area;
                ruleArea.content.type = "Area";
                rules.Add(ruleArea);
                var tube = StructureGraph.Instance.GetTube(objectId, userId);
                dynamic ruleTube = new ExpandoObject();
                ruleTube.action = "upd";
                ruleTube.target = "node";
                ruleTube.content = new ExpandoObject();
                ruleTube.content.id = tube.id;
                ruleTube.content.body = tube;
                ruleTube.content.type = "Tube";
                rules.Add(ruleTube);
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
                    foreach (var rule in rules)
                    {
                        dynamic content = rule.content;
                        Guid id = Guid.Empty;
                        id = Guid.Parse(content.id.ToString());
                        if (id != Guid.Empty && StructureGraph.Instance.CanSee(id, uId))
                            msg.body.rules.Add(rule);
                    }
                    if (msg.body.rules.Count > 0)
                    {
                        var connectionId = ds[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                        SignalRConnection.RaiseEvent(msg, connectionId);
                    }
                });
                return Helper.BuildMessage(what);
            }
            if (what == "node-get-light-control-config")
            {
                var answer = Helper.BuildMessage(what);
                string[] lightControlMetod = new string[4];
                int[] lightBeforeSunRise = new int[4];

                int[,] afterSunSetAndBeforeSunRise = new int[4,2];

                dynamic[,] lightSheduleOn = new dynamic[4, 2];

                dynamic[,] lightSheduleOff = new dynamic[4, 2];

                DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0);
                DateTime dt1970With1sec = new DateTime(1970, 1, 1, 0, 0, 1);

                Guid id = Guid.Parse(message.body.objectId.ToString());
                dynamic tube = StructureGraph.Instance.GetTube(id, userId);

                var dtube = tube as IDictionary<string, object>;
                
                if (dtube.ContainsKey("strConfig"))
                {
                    string strConfig = (string)tube.strConfig;
                    var bytesConfig = strConfig.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    LightConfig conf = StructsHelper.Instance.setBytesFromConfig<LightConfig>(bytesConfig, new LightConfig());
                                        
                    //Celestial cel = Celestial.CalculateCelestialTimes(Convert.ToInt32(conf.u32lat)+0, Convert.ToInt32(conf.u32lon), new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
                    //DateTime sunRiseUTC = cel.SunRise.Value;
                    //DateTime sunSetUTC = cel.SunSet.Value;
                    //DateTime lightAstronomOn = sunRiseUTC.AddHours(Convert.ToInt32(conf.u8timeDiff));
                    //DateTime lightAstronomOff = sunSetUTC.AddHours(Convert.ToInt32(conf.u8timeDiff));
                    
                    lightControlMetod[0] = (conf.ligthtChannels1.u8ControlMode == 0xFF) ? "" : Convert.ToString(conf.ligthtChannels1.u8ControlMode);
                    lightControlMetod[1] = (conf.ligthtChannels2.u8ControlMode == 0xFF) ? "" : Convert.ToString(conf.ligthtChannels2.u8ControlMode);
                    lightControlMetod[2] = (conf.ligthtChannels3.u8ControlMode == 0xFF) ? "" : Convert.ToString(conf.ligthtChannels3.u8ControlMode);
                    lightControlMetod[3] = (conf.ligthtChannels4.u8ControlMode == 0xFF) ? "" : Convert.ToString(conf.ligthtChannels4.u8ControlMode);
                    
                    afterSunSetAndBeforeSunRise[0, 0] = (conf.ligthtChannels1.u8afterSunSet == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels1.u8afterSunSet);
                    afterSunSetAndBeforeSunRise[1, 0] = (conf.ligthtChannels2.u8afterSunSet == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels2.u8afterSunSet);
                    afterSunSetAndBeforeSunRise[2, 0] = (conf.ligthtChannels3.u8afterSunSet == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels3.u8afterSunSet);
                    afterSunSetAndBeforeSunRise[3, 0] = (conf.ligthtChannels4.u8afterSunSet == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels4.u8afterSunSet);

                    afterSunSetAndBeforeSunRise[0, 1] = (conf.ligthtChannels1.u8beforeSunRise == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels1.u8beforeSunRise);
                    afterSunSetAndBeforeSunRise[1, 1] = (conf.ligthtChannels2.u8beforeSunRise == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels2.u8beforeSunRise);
                    afterSunSetAndBeforeSunRise[2, 1] = (conf.ligthtChannels3.u8beforeSunRise == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels3.u8beforeSunRise);
                    afterSunSetAndBeforeSunRise[3, 1] = (conf.ligthtChannels4.u8beforeSunRise == 0xFF) ? 0 : Convert.ToInt32(conf.ligthtChannels4.u8beforeSunRise);
                    
                    lightSheduleOn[0, 0] = conf.ligthtChannels1.on1;
                    lightSheduleOn[1, 0] = conf.ligthtChannels2.on1;
                    lightSheduleOn[2, 0] = conf.ligthtChannels3.on1;
                    lightSheduleOn[3, 0] = conf.ligthtChannels4.on1;
                    lightSheduleOn[0, 1] = conf.ligthtChannels1.on2;
                    lightSheduleOn[1, 1] = conf.ligthtChannels2.on2;
                    lightSheduleOn[2, 1] = conf.ligthtChannels3.on2;
                    lightSheduleOn[3, 1] = conf.ligthtChannels4.on2;
                    
                    lightSheduleOff[0, 0] = conf.ligthtChannels1.off1;
                    lightSheduleOff[1, 0] = conf.ligthtChannels2.off1;
                    lightSheduleOff[2, 0] = conf.ligthtChannels3.off1;
                    lightSheduleOff[3, 0] = conf.ligthtChannels4.off1;
                    lightSheduleOff[0, 1] = conf.ligthtChannels1.off2;
                    lightSheduleOff[1, 1] = conf.ligthtChannels2.off2;
                    lightSheduleOff[2, 1] = conf.ligthtChannels3.off2;
                    lightSheduleOff[3, 1] = conf.ligthtChannels4.off2;

                    answer.body.strConfig = strConfig;
                    answer.body.lightControlMetod = lightControlMetod;
                    answer.body.afterSunSetAndBeforeSunRise = afterSunSetAndBeforeSunRise;
                    
                    answer.body.lightSheduleOn = lightSheduleOn;
                    answer.body.lightSheduleOff = lightSheduleOff;
                }
                return answer;
            }

            if (what == "node-get-matrix-terminal-config")
            {
                var answer = Helper.BuildMessage(what);
                
                Guid id = Guid.Parse(message.body.objectId.ToString());
                dynamic node = StructureGraph.Instance.GetNodeById(id);

                var dnode = node as IDictionary<string, object>;

                if (dnode.ContainsKey("config"))
                {
                    string strConfig = (string)node.config;
                    var bytesConfig = strConfig.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    MatrixTerminalConfig conf = StructsHelper.Instance.setBytesFromConfig<MatrixTerminalConfig>(bytesConfig, new MatrixTerminalConfig());
                    answer.body.config = conf;

                    List<dynamic> listProfile = new List<dynamic>();
                    List<string> listApnName = new List<string>();
                    foreach (var profile in conf.profile)
                    {
                        dynamic profileTmp = new ExpandoObject();
                        string ipPort = StructsHelper.Instance.ParseStringFromBytes(profile.ip_port);

                        profileTmp.ip = (ipPort.Contains(':')) ? ipPort.Split(':')[0] : ipPort;
                        profileTmp.port = (ipPort.Contains(':')) ? ipPort.Split(':')[1] : "";
                        listProfile.Add(profileTmp);
                    }
                    foreach (var apnName in conf.apnName)
                    {
                        listApnName.Add(StructsHelper.Instance.ParseStringFromBytes(apnName.APN));
                    }
                    answer.body.profiles = listProfile;
                    answer.body.APNs = listApnName;
                    answer.body.strConfig = strConfig;
                }
                return answer;
            }

            if (what == "node-value")
            {
                double value = (double)message.body.indication;
                string valueUnitMeasurement = (string)message.body.indicatioUnitMeasurement;
                DateTime date = DateTime.Parse(message.body.date.ToString());
                Guid objectId = Guid.Parse((string)message.body.objectId);
                Data.RowsCache.Instance.UpdateValue(value, valueUnitMeasurement, date, objectId, userId);
                Carantine.Instance.Push(objectId);
            }
            if (what == "node-save")
            {
                //var node = message.body.node;
                //Data.StructureGraph.Instance.SaveNode(node.type.ToString(), node, Guid.Parse((string)session.User.id));
            }

            if (what == "nodes-save")
            {
                var nodes = message.body.nodes;                
                var tokens = new List<dynamic>();
                foreach (var node in nodes)
                {
                    var token = node;
                    token.userId = userId;
                    tokens.Add(token);
                }

                Data.NodeBackgroundProccessor.Instance.AddTokens(tokens);
                return Helper.BuildMessage(what);
            }

            if (what == "node-poll-server")
            {
                string serverName = message.body.serverName;
                var server = StructureGraph.Instance.GetPollServer(serverName);
                var ans = Helper.BuildMessage(what);
                ans.body.server = StructureGraph.Instance.GetServer(serverName, Guid.Parse((string)session.userId));
                return ans;
            }
           
            if (what == "nodes-poll-branch")
            {
                string serverName = message.body.serverName;
                var server = StructureGraph.Instance.GetPollServer(serverName);
                var ans = Helper.BuildMessage(what);
                ans.body.server = server;//StructureGraph.Instance.GetServer(serverName, Guid.Parse((string)session.userId));
                return ans;
            }

            return Helper.BuildMessage(what);
        }
       
        
    }
}

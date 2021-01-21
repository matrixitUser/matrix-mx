using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json.Linq;

namespace Matrix.Web.Host.Handlers
{
    class PollHandler : IHandler
    {
        public const string SUBSCRIBE = "poll-log-subscribers";

        private static readonly ILog log = LogManager.GetLogger(typeof(PollHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("poll");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            var userId = Guid.Parse(session.userId.ToString());

            if (what == "poll-cancel")
            {
                //var sessions = StructureGraph.Instance.GetSessions();
                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);                    
                }
                return Helper.BuildMessage(what);
            }


            if (what.StartsWith("poll-vcom"))
            {
                //var sessions = StructureGraph.Instance.GetSessions();
                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);
                }
                return Helper.BuildMessage(what);
            }


            if (what == "poll")
            {
                //препроцессинг
                //редиректы

                var dmessage = message.body as IDictionary<string, object>;
                if(dmessage.ContainsKey("redirect"))
                { 
                    var objIds = new List<Guid>();
                    foreach(var objId in message.body.objectIds)
                    {
                        objIds.Add(Guid.Parse(objId));
                    }                    
                    message.body.objectIds = StructureGraph.Instance.GetRelatedIds(objIds, message.body.redirect.ToString());
                }

                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);                    
                }
            }

            if (what == "poll-subscribe")
            {
                var bag = session.bag as IDictionary<string, object>;
                if (!bag.ContainsKey(SUBSCRIBE))
                {
                    bag.Add(SUBSCRIBE, new Dictionary<Guid, IEnumerable<Guid>>());
                }
                var old = (Dictionary<Guid, IEnumerable<Guid>>)bag[SUBSCRIBE];
                foreach (dynamic subs in message.body.subscribers)
                {
                    Guid tubeId = subs.id;
                    var sattelites = new List<Guid>();
                    foreach (Guid satteliteId in subs.sattelites)
                    {
                        sattelites.Add(satteliteId);
                    }
                    if (old.ContainsKey(tubeId))
                    {
                        old[tubeId] = sattelites;
                    }
                    else
                    {
                        old.Add(tubeId, sattelites);
                    }
                }
                session.Bag[SUBSCRIBE] = old;
            }

            if (what == "poll-unsubscribe")
            {
                var bag = session.bag as IDictionary<string, object>;
                if (!bag.ContainsKey(SUBSCRIBE))
                {
                    bag.Add(SUBSCRIBE, new Dictionary<Guid, IEnumerable<Guid>>());
                }
                var old = (Dictionary<Guid, IEnumerable<Guid>>)bag[SUBSCRIBE];
                foreach (dynamic subs in message.body.subscribers)
                {
                    Guid tubeId = subs.tubeId;
                    if (old.ContainsKey(tubeId))
                    {
                        old.Remove(tubeId);
                    }
                }
                session.Bag[SUBSCRIBE] = old;
            }
            if (what == "poll-get-objectid-imeina")
            {
                string imei = message.body.imei;
                string networkaddress = $"{message.body.networkaddress}";
                var objectId = StructureGraph.Instance.GetTubeIdFromIMEIandNetworkAddress(imei, networkaddress);
                var answer = Helper.BuildMessage(what);
                answer.body.objectId = objectId;
                return answer;
            }
            if (what == "poll-get-objectid-imeina-matrixterminal")
            {
                string imei = message.body.imei;
                string networkaddress = $"{message.body.networkaddress}";
                var objectId = StructureGraph.Instance.GetTubeIdFromIMEIandNAMatrixTerminal(imei, networkaddress);
                var answer = Helper.BuildMessage(what);
                answer.body.objectId = objectId;
                return answer;
            }
            if (what == "poll-set-light-astronomtimer")
            {
                byte afterSunSet = Byte.Parse(message.body.afterBeforeSunSetRise[0]);
                byte beforeSunRise = Byte.Parse(message.body.afterBeforeSunSetRise[1]);

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                byte u8timeDiff = Byte.Parse(message.body.utc);
                double dLatitude = Double.Parse(((string)message.body.coordinates[0]).Replace(',','.'));
                UInt32 latitude = (UInt32)(dLatitude * 10000000);
                double dLongitude = Double.Parse(((string)message.body.coordinates[1]).Replace(',', '.'));
                UInt32 longitude = (UInt32)(dLongitude * 10000000);
                string cmd = "setAstronTimer";

                List<byte> listSetAstronom = new List<byte>();
                listSetAstronom.Add(u8timeDiff);

                byte[] u32Lat = BitConverter.GetBytes(latitude);
                listSetAstronom.AddRange(u32Lat);

                byte[] u32Lon = BitConverter.GetBytes(longitude);
                listSetAstronom.AddRange(u32Lon);

                listSetAstronom.Add(afterSunSet);

                listSetAstronom.Add(beforeSunRise);

                dynamic msg = new ExpandoObject();
                msg = Helper.BuildMessage("poll");
                msg.head.sessionId = message.head.sessionId;
                msg.body.objectIds = new string[] { (string)message.body.objectId };
                msg.body.what = "all";
                msg.body.arg = new ExpandoObject();
                msg.body.arg.components = "Current";
                msg.body.arg.onlyHoles = false;
                msg.body.arg.cmd = cmd + BitConverter.ToString(listSetAstronom.ToArray());
                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(msg, connectionId);
                }
            }
            if (what == "poll-light-control-config")
            {
                var lightControlMetod = message.body.lightControlMetod;
                var afterSunSetAndBeforeSunRise = message.body.afterSunSetAndBeforeSunRise;

                var lightSheduleOn = message.body.lightSheduleOn;
                var lightSheduleOff = message.body.lightSheduleOff;
                DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0);
                
                Guid id = Guid.Parse(message.body.objectId.ToString());
                dynamic tube = StructureGraph.Instance.GetTube(id, userId);
                string strConfig = (string)tube.strConfig;
                var bytesConfig = strConfig.Split('-').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                LightConfig conf = StructsHelper.Instance.setBytesFromConfig<LightConfig>(bytesConfig, new LightConfig());

                string cmd = "setAstronTimer";
                
                if (lightControlMetod.Count > 0 && lightControlMetod[0] != null) conf.ligthtChannels1.u8ControlMode = Convert.ToByte(lightControlMetod[0]);
                conf.ligthtChannels1.u8afterSunSet = Convert.ToByte(afterSunSetAndBeforeSunRise[0][0]);
                conf.ligthtChannels1.u8beforeSunRise = Convert.ToByte(afterSunSetAndBeforeSunRise[0][1]);

                conf.ligthtChannels1.on1 = lightChannelOnOff(lightSheduleOn[0][0]);
                conf.ligthtChannels1.on2 = lightChannelOnOff(lightSheduleOn[0][1]);  //------------------

                conf.ligthtChannels1.off1 = lightChannelOnOff(lightSheduleOff[0][0]);//---------------------
                conf.ligthtChannels1.off2 = lightChannelOnOff(lightSheduleOff[0][1]);
                
                if (lightControlMetod.Count > 1 && lightControlMetod[1] != null) conf.ligthtChannels2.u8ControlMode = Convert.ToByte(lightControlMetod[1]);

                conf.ligthtChannels2.u8afterSunSet = Convert.ToByte(afterSunSetAndBeforeSunRise[1][0]);
                conf.ligthtChannels2.u8beforeSunRise = Convert.ToByte(afterSunSetAndBeforeSunRise[1][1]);

                conf.ligthtChannels2.on1 = lightChannelOnOff(lightSheduleOn[1][0]);
                conf.ligthtChannels2.on2 = lightChannelOnOff(lightSheduleOn[1][1]);

                conf.ligthtChannels2.off1 = lightChannelOnOff(lightSheduleOff[1][0]);
                conf.ligthtChannels2.off2 = lightChannelOnOff(lightSheduleOff[1][1]);
                
                if (lightControlMetod.Count > 2 && lightControlMetod[2] != null) conf.ligthtChannels3.u8ControlMode = Convert.ToByte(lightControlMetod[2]);
                conf.ligthtChannels3.u8afterSunSet = Convert.ToByte(afterSunSetAndBeforeSunRise[2][0]);
                conf.ligthtChannels3.u8beforeSunRise = Convert.ToByte(afterSunSetAndBeforeSunRise[2][1]);

                conf.ligthtChannels3.on1 = lightChannelOnOff(lightSheduleOn[2][0]);
                conf.ligthtChannels3.on2 = lightChannelOnOff(lightSheduleOn[2][1]);

                conf.ligthtChannels3.off1 = lightChannelOnOff(lightSheduleOff[2][0]);
                conf.ligthtChannels3.off2 = lightChannelOnOff(lightSheduleOff[2][1]);
                
                if (lightControlMetod.Count > 3 && lightControlMetod[3] != null) conf.ligthtChannels4.u8ControlMode = Convert.ToByte(lightControlMetod[3]);
                conf.ligthtChannels4.u8afterSunSet = Convert.ToByte(afterSunSetAndBeforeSunRise[3][0]);
                conf.ligthtChannels4.u8beforeSunRise = Convert.ToByte(afterSunSetAndBeforeSunRise[3][1]);

                conf.ligthtChannels4.on1 = lightChannelOnOff(lightSheduleOn[3][0]);
                conf.ligthtChannels4.on2 = lightChannelOnOff(lightSheduleOn[3][1]);
                conf.ligthtChannels4.off1 = lightChannelOnOff(lightSheduleOff[3][0]);
                conf.ligthtChannels4.off2 = lightChannelOnOff(lightSheduleOff[3][1]);
                
                byte[] outByte = StructsHelper.Instance.getBytes<LightConfig>(conf);
                dynamic msg = new ExpandoObject();
                msg = Helper.BuildMessage("poll");
                msg.head.sessionId = message.head.sessionId;
                msg.body.objectIds = new string[] { (string)message.body.objectId };
                msg.body.what = "all";
                msg.body.arg = new ExpandoObject();
                msg.body.arg.components = "Current";
                msg.body.arg.onlyHoles = false;
                msg.body.arg.cmd = cmd + BitConverter.ToString(outByte);
                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(msg, connectionId);
                }
            }

            var ans = Helper.BuildMessage("poll-accepted");
            return ans;
        }
        
        public UInt32 lightChannelOnOff(dynamic lightShedule)
        {
            return (lightShedule != null) ? Convert.ToUInt32((lightShedule - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds) : 0xFFFFFFFF;
        }
    }
}

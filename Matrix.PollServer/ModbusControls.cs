using log4net;
using Matrix.PollServer.Handlers;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrix.SurveyServer.Driver.Common.Crc;
using Matrix.PollServer.Storage;

namespace Matrix.PollServer
{
    /// <summary>
    /// отвечает за отправку сообщений на сервер, 
    /// следит за сессией
    /// </summary>
    class ModbusControl
    {
        private const int FORCE_RECONNECT_INTERVAL = 10 * 60 * 1000;
        private const int CONNECTION_TIMEOUT = 5 * 60 * 1000;

        private const string PING = "ping";

        private const string SESSION_ID = "sessionId";
        private const string LOGIN = "login";
        private const string PASSWORD = "password";
        private const string URL = "serverUrl";

        private const string API_PATH = "api/transport";
        private const string SIGNALR_CONNECTOR = "messageacceptor";



        public const string msgCorrectTime = "Корректировка времени";
        public const string msgSetTime = "Установка времени";
        public const int TimeDayForSet = 30;
        public const int TimeSecondForSet = 300; // 5 minute


        private static readonly ILog log = LogManager.GetLogger(typeof(ApiConnector));

        private bool isPingOk = false;

        static ModbusControl() { }

        private static readonly ModbusControl instance = new ModbusControl();
        public static ModbusControl Instance
        {
            get
            {
                return instance;
            }
        }
        
        public List<byte> SendAnswer(byte byte0, byte[] bytesCId, byte func, byte ErrorOrEvent, byte[] byteTime)
        {
            List<byte> sendByte = new List<byte>();
            sendByte.Add(byte0);           
            sendByte.AddRange(bytesCId);        //cid 12 byte   1-12
            sendByte.Add(func);                 //                13
            sendByte.Add(ErrorOrEvent);         //                14
            sendByte.AddRange(byteTime);        //time          15-18
            var crc = Crc.Calc(sendByte.ToArray(), new Crc16Modbus());
            sendByte.Add(crc.CrcData[0]);                       //19
            sendByte.Add(crc.CrcData[1]);
            return sendByte;
        }


        public byte[] TeleofisWrx(byte[] bytes, string imei)
        {
            List<byte> sendByte = new List<byte>();
            if (bytes[0] != 0xFB) return sendByte.ToArray();

            string strCId = "";
            byte[] bytesCId = bytes.Skip(1).Take(12).ToArray();
            strCId = BitConverter.ToString(bytesCId).Replace("-", "");
            byte func = bytes[13];
            Guid nullGuid = new Guid();

            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            if (func == 0x4D)
            {
                byte[] bytesObjectId = bytes.Skip(14).Take(16).ToArray(); //
                byte networkAddress = bytes[30];                          //16
                byte ErrorOrEvent = bytes[31];                            //17
                UInt16 code = Helper.ToUInt16(bytes, 32);
                byte[] byteTime = bytes.Skip(34).Take(4).ToArray();

                byte LightMK = bytes[38];
                byte LightReal = bytes[39];
                byte PhotoSensorState = bytes[40];
                
                UInt32 uInt32Time = (UInt32)(byteTime[3] << 24) | (UInt32)(byteTime[2] << 16) | (UInt32)(byteTime[1] << 8) | byteTime[0];
                DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                
                if (!Relogin()) return sendByte.ToArray();

                Guid objectId = GetObjectId(bytesObjectId, imei, strCId, networkAddress);
                if(objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF)) return sendByte.ToArray();

                List<dynamic> records = new List<dynamic>();
                DateTime dtNow = DateTime.Now;
                if (ErrorOrEvent == 0x01)
                {
                    string light = "";
                    if (bytes.Length > 42)
                    {
                        light = $"lightMK:{LightMK & 0b0001}:{(LightMK & 0b0010) >> 1}:{(LightMK & 0b0100) >> 2}:{(LightMK & 0b1000) >> 3};Real:{LightReal & 0b0001}:{(LightReal & 0b0010) >> 1}:{(LightReal & 0b0100) >> 2}:{(LightReal & 0b1000) >> 3};PSS:{PhotoSensorState};CMetod:{bytes[41]}:{bytes[42]}:{bytes[43]}:{bytes[44]};Event:{code};dt:{dtContollers}T|";
                        Log(objectId, $"LightMK:{LightMK}|{LightMK & 0b0001}:{(LightMK & 0b0010) >> 1}:{(LightMK & 0b0100) >> 2}:{(LightMK & 0b1000) >> 3}; LightReal:{LightReal}|{LightReal & 0b0001}:{(LightReal & 0b0010) >> 1}:{(LightReal & 0b0100) >> 2}:{(LightReal & 0b1000) >> 3}; PhotoSensorState:{PhotoSensorState}; ControlMetod:{bytes[41]}:{bytes[42]}:{bytes[43]}:{bytes[44]}; Event:{code}; dt:{dtContollers}T|");
                        if (bytes.Length > 45)
                        {
                            var devid = bytes[46] >> 3;   // номер(тип) устройства
                            var ver = bytes[46] & 0x7;  // версия ПО устройства
                            var flash = bytes[45];      // размер конфигурации флеш-памяти
                        }
                    }
                    else
                    {
                        light = $"lightMK:{LightMK};Real:{LightReal};PSS:{PhotoSensorState};CMetod:{bytes[41]};Event:{code};dt:{dtContollers}T|";
                        Log(objectId, $"LightMK:{LightMK}; LightReal:{LightReal}; ControlMetod:{bytes[41]}; Event:{code}; dt:{dtContollers}T|");
                    }
                    //string 
                    dynamic control = new ExpandoObject();
                    control.controllerData = light;
                    NodeControllerData(control, objectId);

                    records.Add(MakeCurrentRecord(objectId, "Event", code, "", dtContollers, dtNow));
                    records.Add(MakeCurrentRecord(objectId, "GetLightMK", LightMK, "", dtContollers, dtNow));
                    records.Add(MakeCurrentRecord(objectId, "GetLightReal", LightReal, "", dtContollers, dtNow));
                    records.Add(MakeCurrentRecord(objectId, "GetPhotoSensorState", PhotoSensorState, "", dtContollers, dtNow));
                    RecordsAcceptor.Instance.Save(records);

                }
                else if (ErrorOrEvent == 0x00)
                {
                    DateTime now = DateTime.Now;
                    var diffSecond = ((dtContollers > now) ? (dtContollers - now).TotalSeconds : (now - dtContollers).TotalSeconds);
                    var diffDay = ((dtContollers > now) ? (dtContollers - now).TotalDays : (now - dtContollers).TotalDays);
                    if (diffSecond > TimeSecondForSet)
                    {
                        Guid[] ids = { objectId };
                        Request(ids, "correcttime", imei, "Current");
                        if (diffDay > TimeDayForSet)
                        {
                            Log(objectId, $"{msgSetTime}  разница: {diffDay} дней");
                            SetSoftConfig(0xFF, 0x03);
                        }

                    }

                    if (((code & 0x0100) > 0) || ((code & 0x1000) > 0)) //0x0100 открытие -- 0x1000 закрытие
                    {
                        NodeEvents(code, objectId);
                    }
                    Log(objectId, $"code:{code}");
                    records.Add(MakeAbnormalRecord(objectId, "Error", code, dtContollers));
                    RecordsAcceptor.Instance.Save(records);
                }
                
                sendByte.AddRange(SendAnswer(0xFB, bytesCId, func, ErrorOrEvent, byteTime));
            }
            if (func == 0x38)
            {
                byte[] bytesObjectId = bytes.Skip(14).Take(16).ToArray(); //
                byte networkAddress = bytes[30];                          //16
                byte sendOrReceve = bytes[31];                            //17
                UInt16 code = Helper.ToUInt16(bytes, 32);
                byte[] byteTime = bytes.Skip(34).Take(4).ToArray();

                byte[] bytesSendTmp = bytes.Skip(38).ToArray();
                byte[] bytesSendOrReceve = bytesSendTmp.Take(bytesSendTmp.Length-2).ToArray();

                UInt32 uInt32Time = (UInt32)(byteTime[3] << 24) | (UInt32)(byteTime[2] << 16) | (UInt32)(byteTime[1] << 8) | byteTime[0];
                DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                
                if (!Relogin()) return sendByte.ToArray();

                Guid objectId = GetObjectId(bytesObjectId, imei, strCId, networkAddress);
                if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF)) return sendByte.ToArray();

                if (sendOrReceve == 0x01)
                {
                    Log(objectId, $"R::{BitConverter.ToString(bytesSendOrReceve)}");

                }
                else if (sendOrReceve == 0x00)
                {
                    Log(objectId, $"S::{BitConverter.ToString(bytesSendOrReceve)}");
                }
            }
            if (func == 0x39)
            {
                byte[] bytesObjectId = bytes.Skip(14).Take(16).ToArray(); //
                byte networkAddress = bytes[30];                          //16
                byte sendOrReceve = bytes[31];                            //17
                UInt16 code = Helper.ToUInt16(bytes, 32);
                byte[] byteTime = bytes.Skip(34).Take(4).ToArray();

                byte[] bytesSendTmp = bytes.Skip(38).ToArray();
                byte[] bytesSendOrReceve = bytesSendTmp.Take(bytesSendTmp.Length - 2).ToArray();

                UInt32 uInt32Time = (UInt32)(byteTime[3] << 24) | (UInt32)(byteTime[2] << 16) | (UInt32)(byteTime[1] << 8) | byteTime[0];
                DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                
                if (!Relogin()) return sendByte.ToArray();

                Guid objectId = GetObjectId(bytesObjectId, imei, strCId, networkAddress);
                if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF)) return sendByte.ToArray();

                if (sendOrReceve == 0x01)
                {
                    string strForLog = "";
                    if (bytesSendOrReceve[0] == 0x10 && bytesSendOrReceve[1] == 0x04)
                    {
                        byte waterLevel = bytesSendOrReceve[4];
                        strForLog += $"| max={(waterLevel >> 3) & 1}; middle-max={(waterLevel >> 2) & 1}; middle-min={(waterLevel >> 1) & 1}; min={waterLevel & 1};";
                    }
                    else
                    {
                        if (bytesSendOrReceve[1] == 0x01)
                        {
                            UInt16 uppData = BitConverter.ToUInt16(bytesSendOrReceve, 3);
                            strForLog += (((uppData >> 0x01) & 1) == 0) ? "| Motor running" : "| Motor stopped";
                            strForLog += "| Auto mode status: " + ((((uppData >> 0x05) & 1) == 0) ? "Local control" : "Modbus master control");
                            strForLog += "| Fault status: " + ((((uppData >> 0x06) & 1) == 0) ? "No active fault" : "One or more active faults");
                            strForLog += (((uppData >> 0x0E) & 1) == 0) ? "| Softstarter is not in top of ramp" : "| Softstarter is in top of ramp (bypass closed)";
                        }
                        if (bytesSendOrReceve[1] == 0x05)
                        {
                            strForLog += (bytesSendOrReceve[3] == 0x05) ? ((bytesSendOrReceve[4] == 0xFF) ? "Auto mode" : "Local control") : "";
                            strForLog += (bytesSendOrReceve[3] == 0x06) ? ((bytesSendOrReceve[4] == 0xFF) ? "Ошибка исправлена" : "Ошибка не исправлена") : "";
                        }
                    }
                    Log(objectId, $"R::{Encoding.ASCII.GetString(bytesSendOrReceve)} {strForLog}");

                }
                else if (sendOrReceve == 0x00)
                {
                    Log(objectId, $"S::{Encoding.ASCII.GetString(bytesSendOrReceve)}");
                }
            }
            if (func == 0x4E)
            {
                byte[] bytesObjectId = bytes.Skip(14).Take(16).ToArray(); //
                byte networkAddress = bytes[30];                          //16
                byte ErrorOrEvent = bytes[31];                            //17  
                UInt16 code = Helper.ToUInt16(bytes, 32); //200 - старт; 100 - стоп двигателя //
                /*
                 * ----события------
                    #define UPP_START						     0x0200   //Старт двигателя
                    #define UPP_STOP							 0x0100   //Стop двигателя
                    ----ошибки---
                    #define ERR_NOT_ACTIVITY_UPP				0x01   //Cброшен или не установлен часовой таймера
                    #define ERR_UPP_NO_START					0x02   //Не удалось запустить двигатель
                    #define ERR_UPP_NO_STOP						0x03   //Не удалось остановит двигатель

                    #define ERR_WLS_NOT_RESPONCE				0x05   //Нет ответа от WLS(башни)
                    #define ERR_UPP_NOT_RESPONCE				0x06   //Нет ответа от UPP
                    #define ERR_WLS_SENSOR						0x07   //Ошибка датчика уровня (нет минимума)
                    #define ERR_UPP_AUTOMODE_SET				0x08   //Не удалось перевести УПП в автомод
                    #define ERR_PUMP_IDLE_MAX					0x09   //Превышение допустимого времени простоя насоса
                */
                byte[] byteTime = bytes.Skip(34).Take(4).ToArray();
                UInt32 uInt32Time = (UInt32)(byteTime[3] << 24) | (UInt32)(byteTime[2] << 16) | (UInt32)(byteTime[1] << 8) | byteTime[0];
                DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                
                byte uppNA = bytes[38]; //+  24
                byte WlsNA = bytes[39]; //+ 
                byte inStatus = bytes[40]; //
                byte outStatus = bytes[41];
                UInt16 errorControls = Helper.ToUInt16(bytes, 42); //28 29
                UInt16 eventControls = Helper.ToUInt16(bytes, 44); //30 31
                byte transferAllow = bytes[46];                    //32
                UInt16 typeAndVersion = Helper.ToUInt16(bytes, 47);//33 34
                byte stateUppActive = bytes[49];                   //35

                int lastUppRespTime = (((bytes[53] != 0xFF) && (bytes[52] != 0xFF) && (bytes[51] != 0xFF) && bytes[50] != 0xFF)) ?
                                        (int)(bytes[53] << 24) | (int)(bytes[52] << 16) | (int)(bytes[51] << 8) | bytes[50] : 0;
                DateTime dtUppLastResp = dt1970.AddSeconds(lastUppRespTime); //36 37 38 39

                UInt16 uint16UppActive = Helper.ToUInt16(bytes, 54); //40

                int lastWlsRespTime = (((bytes[59] != 0xFF) && (bytes[58] != 0xFF) && (bytes[57] != 0xFF) && bytes[56] != 0xFF)) ?
                                        (int)(bytes[59] << 24) | (int)(bytes[58] << 16) | (int)(bytes[57] << 8) | bytes[56] : 0;
                DateTime dtWlsLastResp = dt1970.AddSeconds(lastWlsRespTime);

                UInt16 uint16WlsActive = Helper.ToUInt16(bytes, 60);

                int lastPressureSensorRespTime = (((bytes[65] != 0xFF) && (bytes[64] != 0xFF) && (bytes[63] != 0xFF) && bytes[62] != 0xFF)) ?
                                                    (int)(bytes[65] << 24) | (int)(bytes[64] << 16) | (int)(bytes[63] << 8) | bytes[62] : 0;
                DateTime dtPressureSensorResp = dt1970.AddSeconds(lastPressureSensorRespTime);

                byte bytes1PressureSensor = bytes[66];
                byte bytes2PressureSensor = bytes[67];
                                
                if (!Relogin()) return sendByte.ToArray();

                Guid objectId = GetObjectId(bytesObjectId, imei, strCId, networkAddress);
                if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF)) return sendByte.ToArray();

                List<dynamic> records = new List<dynamic>();
                DateTime dtNow = DateTime.Now;
                if (ErrorOrEvent == 0x01) // события
                {
                    string strDtUppLastResp = (lastUppRespTime != 0) ? dtUppLastResp.ToString() : "undefined";
                    string strDtWlsLastResp = (lastWlsRespTime != 0) ? dtWlsLastResp.ToString() : "undefined";
                    string strDtPressureSensorLastResp = (lastPressureSensorRespTime != 0) ? dtPressureSensorResp.ToString() : "undefined";

                    string switchUpp = (stateUppActive == 2) ? "left" : "right";
                    Log(objectId, $"UppNA : {uppNA}; WlsNA : {WlsNA}; Состояние реле:( in={inStatus} / out={outStatus}); error={errorControls}; event={eventControls}; Разрешение трансфера:{transferAllow}", level: 3);
                    Log(objectId, $"typeAndVersion={typeAndVersion}; Время опроса УПП={strDtUppLastResp}; Время опроса датчиков={strDtWlsLastResp};", level: 3);
                    Log(objectId, $"Время опроса датчика давления={strDtPressureSensorLastResp}; bytes1PressureSensor=0x{bytes1PressureSensor:X}; bytes2PressureSensor=0x{bytes2PressureSensor:X};", level: 3);
                    Log(objectId, $"мотор={motorState(uint16UppActive)}; Cостояния УПП=0x{uint16UppActive:X}; Wls=0x{uint16WlsActive:X}; Переключатель={switchUpp};", level: 1);

                    records.Add(MakeCurrentRecord(objectId, "motor", Convert.ToDouble(uint16UppActive), $"мотор: {motorState(uint16UppActive)}", dtContollers, dtNow));
                    RecordsAcceptor.Instance.Save(records);
                }
                else if (ErrorOrEvent == 0x00) // ошибки
                {
                    var diffSecond = ((dtContollers > dtNow) ? (dtContollers - dtNow).TotalSeconds : (dtNow - dtContollers).TotalSeconds);
                    var diffDay = ((dtContollers > dtNow) ? (dtContollers - dtNow).TotalDays : (dtNow - dtContollers).TotalDays);
                    if (diffSecond > TimeSecondForSet)
                    {
                        Guid[] ids = { objectId };
                        Request(ids, "correcttime", imei, "Current");
                        if (diffDay > TimeDayForSet)
                        {
                            Log(objectId, $"{msgSetTime}  разница: {diffDay} дней");
                            SetSoftConfig(0xFF, 0x03);
                        }
                    }

                    if (((code & 0x0100) > 0) || ((code & 0x1000) > 0)) //0x0100 открытие -- 0x1000 закрытие
                    {
                        NodeEvents(code, objectId);
                    }
                    Log(objectId, $"code:{code}");
                    records.Add(MakeAbnormalRecord(objectId, "Error", code, dtContollers));
                    RecordsAcceptor.Instance.Save(records);
                }
                sendByte.AddRange(SendAnswer(0xFB, bytesCId, func, ErrorOrEvent, byteTime));
            }
            return sendByte.ToArray();
        }

        public Guid GetObjectId(byte[] bytesObjectId, string imei, string strCId, byte networkAddress)
        {
            Guid nullGuid = new Guid();
            Guid objectId = new Guid(bytesObjectId);
            if (objectId == nullGuid || (bytesObjectId[0] == 0xFF && bytesObjectId[1] == 0xFF))
            {
                dynamic msg = Helper.BuildMessage("poll-get-objectid-imeina");
                msg.body.imei = imei;
                msg.body.networkaddress = strCId;
                var answer = SendMessage(msg);
                if (answer == null || answer.head.what == "error")
                {
                    msg.body.networkaddress = networkAddress;
                    answer = SendMessage(msg);
                }
                objectId = Guid.Parse((string)answer.body.objectId);
            }
            return objectId;
        }
        public string motorState(UInt16 upp)
        {
            if (upp == 0) return "ОШИБКА считывания УПП";
            if ((upp & 0b1110011001) != 0) return "ОШИБКА считывания УПП";
            if (((upp >> 0x01) & 1) == ((upp >> 0x02) & 1)) return "ОШИБКА считывания УПП";
            switch (((upp >> 0x02) & 1))
            {
                case 0:
                    return "STOP";
                case 1:
                    return "START";
                default:
                    return "undefined";
            }
        }
        public string SetSoftConfig(byte onoff, byte u8ControlMode)
        {
            List<byte> byteLight = new List<byte>();
            byteLight.Add(onoff);
            UInt16 a = 1800;
            byte[] u16TimeOut = BitConverter.GetBytes(a);   //0x08 0x07
            byteLight.AddRange(u16TimeOut);

            byteLight.Add(u8ControlMode);

            List<byte> byteShedule = new List<byte>();
            byte[] result = new byte[4];
            UInt32[] uint32Shedule = new UInt32[8];
            for (int i = 0; i < 2; i++)
            {
                uint32Shedule[i] = 0xFFFFFFFF;
                result = BitConverter.GetBytes(uint32Shedule[i]);
                byteLight.AddRange(result);
                byteLight.Add(0xFF);
            }
            
            return "setSoftConfig" + BitConverter.ToString(byteLight.ToArray());
        }
       
        public void NodeControllerData(dynamic control, Guid objectId)
        {
            if (!Relogin()) return;
            dynamic msgControllerData = Helper.BuildMessage("node-controller-data");
            msgControllerData.body.control = control;
            msgControllerData.body.objectId = objectId;
            var answer = SendMessage(msgControllerData);
        }
        public void NodeEvents(ushort events, Guid objectId)
        {
            if (!Relogin()) return;
            dynamic msgEvents = Helper.BuildMessage("node-events");
            msgEvents.body.events = events;
            msgEvents.body.objectId = objectId;
            var answer = SendMessage(msgEvents);
        }
        public void NodeValve(int heatingSupply, int heatingReturn, int hwsSupply, int hwsReturn, int cws, Guid objectId)
        {
            if (!Relogin()) return;
            dynamic msgValve = Helper.BuildMessage("node-valve");
            msgValve.body.heatingSupply = heatingSupply;
            msgValve.body.heatingReturn = heatingReturn;
            msgValve.body.hwsSupply = hwsSupply;
            msgValve.body.hwsReturn = hwsReturn;
            msgValve.body.cws = cws;
            msgValve.body.objectId = objectId;
            var answer = SendMessage(msgValve);
        }
        #region Request Current
        public void Request(Guid[] id, string cmd, string imei, string components)
        {
            try
            {
                dynamic msg = Helper.BuildMessage("poll");
                msg.body.arg = new ExpandoObject();
                msg.body.objectIds = id;
                msg.body.what = "all";
                msg.body.arg.cmd = cmd;
                msg.body.arg.components = components;
                msg.body.arg.onlyHoles = false;
                var answer = SendMessage(msg);
                
                if (answer.head.what == "poll-accepted")
                    log.Debug(string.Format("Процесс опроса запущен...: [{0}]", imei));
            }
            catch (Exception ex) { }
        }

        #endregion
        public bool Relogin()
        {
            if (SessionId != Guid.Empty)
            {
                var authBySession = Helper.BuildMessage("auth-by-session");
                authBySession.body.sessionId = SessionId;
                var sessionAns = SendByAPI(authBySession);
                if (sessionAns == null) return false;

                if (sessionAns.head.what == "auth-success")
                {
                    SessionId = Guid.Parse((string)sessionAns.body.sessionId);
                    return true;
                }
            }

            var authByLogin = Helper.BuildMessage("auth-by-login");
            authByLogin.body.login = Login;
            authByLogin.body.password = Password;
            var loginAns = SendByAPI(authByLogin);

            if (loginAns == null) return false;

            if (loginAns.head.what == "auth-success")
            {
                SessionId = Guid.Parse((string)loginAns.body.sessionId);
                return true;
            }

            return false;
        }
        public void Log(Guid ObjectId, string message, int level = 0)
        {
            dynamic record = new ExpandoObject();
            record.id = Guid.NewGuid();
            record.type = "LogMessage";
            record.date = DateTime.Now;
            record.objectId = ObjectId;
            record.s1 = message;
            record.i1 = level;
            RecordsAcceptor.Instance.Save(new dynamic[] { record });
        }
        public dynamic MakeAbnormalRecord(Guid ObjectId, string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.id = Guid.NewGuid();
            record.objectId = ObjectId;
            record.i1 = duration;
            record.s1 = name;
            record.date = DateTime.Now;
            record.dt1 = date;
            return record;
        }
       
        public dynamic MakeCurrentRecord(Guid ObjectId, string parameter, double value, string unit, DateTime date, DateTime dateNow)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.id = Guid.NewGuid();
            record.objectId = ObjectId;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = dateNow;
            //record.dt1 = DateTime.Now;
            record.dt1 = date;
            return record;
        }

        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings[key] == null)
            {
                configuration.AppSettings.Settings.Add(key, value);
            }
            else
            {
                configuration.AppSettings.Settings[key].Value = value;
            }

            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }
        public Guid SessionId
        {
            get
            {
                var raw = ConfigurationManager.AppSettings.Get("sessionId-User");
                Guid sessionId = Guid.Empty;
                Guid.TryParse(raw, out sessionId);
                return sessionId;
            }
            set
            {
                UpdateSetting("sessionId-User", value.ToString());
            }
        }
        
        public string Login
        {
            get
            {
                var login = ConfigurationManager.AppSettings.Get(LOGIN);
                return login;
            }
        }

        public string Password
        {
            get
            {
                var password = ConfigurationManager.AppSettings.Get(PASSWORD);
                return password;
            }
        }

        public string ServerUrl
        {
            get
            {
                var serverUrl = ConfigurationManager.AppSettings.Get(URL);
                return serverUrl;
            }
        }

        public dynamic SendMessage(dynamic message)
        {
            if (SessionId != Guid.Empty)
            {
                message.head.sessionId = SessionId;
            }
            return SendByAPI(message);
        }
        
        public dynamic SendByAPI(dynamic message)
        {
            try
            {
                var client = new RestClient(ServerUrl);
                RestRequest request = new RestRequest(API_PATH, RestSharp.Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(message);
                var response = client.Execute(request);
                dynamic answer = JsonConvert.DeserializeObject<ExpandoObject>(response.Content);
                return answer;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return null;
        }
        
    }
}

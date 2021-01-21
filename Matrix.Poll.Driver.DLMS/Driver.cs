// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Dynamic;
using System.Threading;
using Matrix.SurveyServer.Driver.Common.Crc;
using Gurux.Common;
using System.Xml;
using Gurux.DLMS.ManufacturerSettings;
using System.Xml.Serialization;
using System.Timers;
using Gurux.DLMS.Objects;
using Gurux.DLMS.UI;
using Gurux.DLMS;
using Gurux.DLMS.Objects.Enums;
using Gurux.DLMS.Enums;
using System.Reflection;
using Gurux.Serial;
using Gurux.Net;
using System.IO.Ports;
using Gurux.DLMS.Secure;

namespace Matrix.Poll.Driver.DLMS
{
    internal class Password
    {
        public static string Key = "Gurux Ltd.";
    }
    /// <summary>
    /// Драйвер для dlms
    /// </summary>
    public partial class Driver
    {
        

        int hourlyStart = 30;

        private class Block
        {
            public DateTime Date { get; set; }
            public byte Number { get; set; }
        }

        UInt32? NetworkAddress = null;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        private enum DeviceError
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION
        };

        private void log(string message, int level = 2)
        {
            logger(message, level);
        }

        public byte[] SendSimple(byte[] data, int timeout = 4000, int waitCollectedMax = 2)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) >= 0 && !isCollected)
            {

                var buf = response();
                if (buf.Any())
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0;
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == waitCollectedMax)
                        {
                            isCollected = true;
                        }
                    }
                }
                Thread.Sleep(sleep);
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        public dynamic Send(byte[] data, int attempts = 3)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;

            for (var attempt = 0; (attempt < attempts) && (answer.success == false); attempt++)
            {
                buffer = SendSimple(data);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 2)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 6 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else
                    {
                        //if (!Crc.Check(buffer, new Crc16Modbus()))
                        //{
                        //    answer.error = "контрольная сумма кадра не сошлась";
                        //    answer.errorcode = DeviceError.CRC_ERROR;
                        //}
                        //else
                        {
                            answer.success = true;
                            answer.error = string.Empty;
                            answer.errorcode = DeviceError.NO_ERROR;
                        }
                    }
                }
            }

            if (answer.success)
            {
                //answer.Body = buffer.Take(buffer.Length - 2).Skip(5).ToArray();
                answer.Body = buffer;
            }

            return answer;
        }


        private dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
        
        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeResult(int code, DeviceError errorcode, string description)
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion
        
        GXDLMSDevice Device;
        GXDLMSDevice parent;
        GXDLMSMeter Meter;
        #region ImportExport

#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        public Action<string, int> logger;
#endif

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        public Func<byte[]> response;

        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        [Import("cancel")]
        private Func<bool> cancel;

        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;

        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;
            var components = "Hour;Day;Constant;Abnormal;Current";
            if (param.ContainsKey("components"))
            {
                components = arg.components;
                log(string.Format("указаны архивы {0}", components));
            }
            else
            {
                log(string.Format("архивы не указаны, будут опрошены все"));
            }
            using (XmlReader reader = XmlReader.Create("D:\\128.gxc"))
            {
                List<Type> types = new List<Type>(Gurux.DLMS.GXDLMSClient.GetObjectTypes());
                types.Add(typeof(GXDLMSAttributeSettings));
                types.Add(typeof(GXDLMSAttribute));
                //Version is added to namespace.
                XmlSerializer x = new XmlSerializer(typeof(GXDLMSDeviceCollection), null, types.ToArray(), null, "Gurux1");
                if (!x.CanDeserialize(reader))
                {
                    x = new XmlSerializer(typeof(GXDLMSDeviceCollection), types.ToArray());
                }
                Device = ((GXDLMSDeviceCollection)x.Deserialize(reader))[0];       
                parent = Device;
                reader.Close();
            }
            List<dynamic> listObisObjectTmp = new List<dynamic>();
            dynamic obisObject1 = new ExpandoObject();
            obisObject1.obis = "1.0.1.9.0.255";
            obisObject1.objectType = "Register";
            listObisObjectTmp.Add(obisObject1);
            dynamic obisObject2 = new ExpandoObject();
            obisObject2.obis = "0.0.96.2.0.255";
            obisObject2.objectType = "Data";
            listObisObjectTmp.Add(obisObject2);
            dynamic obises = new ExpandoObject();
            obises.Current = listObisObjectTmp;
            var dicObis = (IDictionary<string, object>)obises;
            List<dynamic> listObisObject = new List<dynamic>();
            if (components.Contains("Current") && dicObis.ContainsKey("Current"))
            {
                List<dynamic> listObisObjectTmp1 = (List<dynamic>)obises.Current;
                for (int i = 0; i < listObisObjectTmp1.Count(); i++)
                {
                    if (listObisObject.Contains(listObisObjectTmp1[i])) continue;
                    listObisObject.Add(listObisObjectTmp1[i]);
                }
            }

            InitializeConnection();

            foreach (var obisObject in listObisObject)
            {
                ObjectType objectType = (ObjectType)Enum.Parse(typeof(ObjectType), obisObject.objectType);
                int it = 2;
                string obis = obisObject.obis.ToString();
                ReadMineCommunicator(obis, objectType, it);
            }
            Disconnect();
            dynamic result = new ExpandoObject();
            
            return result;
        }
        byte[] ReleaseRequest()
        {
            byte[][] data = client.ReleaseRequest();
            if (data == null)
            {
                return null;
            }
            return data[0];
        }
        public void Disconnect()
        {
            GXReplyData reply = new GXReplyData();
            try
            {
                //Release is call only for secured connections.
                //All meters are not supporting Release and it's causing problems.
                if (client.InterfaceType == InterfaceType.WRAPPER ||
                    (client.InterfaceType == InterfaceType.HDLC && client.Ciphering.Security != Security.None && !parent.PreEstablished))
                {
                    byte[] data = ReleaseRequest();
                    if (data != null)
                    {
                        ReadDataBlock(data, "Release request", reply);
                    }
                }
                reply.Clear();
                if (client.InterfaceType == InterfaceType.HDLC && !parent.PreEstablished)
                {
                    ReadDataBlock(DisconnectRequest(), "Disconnect request", reply);
                }
            }
            catch (Exception)
            {
                //All meters don't support release.
            }
        }
             
        internal GXDLMSSecureClient client = new GXDLMSSecureClient();
        
        public byte[] SNRMRequest()
        {
            return client.SNRMRequest();
        }

        public void ParseUAResponse(GXByteBuffer data)
        {
            client.ParseUAResponse(data);
        }

        public byte[][] AARQRequest()
        {
            return client.AARQRequest();
        }

        public void ParseAAREResponse(GXByteBuffer data)
        {
            client.ParseAAREResponse(data);
        }

        public byte[] Read(GXDLMSObject it, int attributeOrdinal)
        {
            byte[] tmp = client.Read(it, attributeOrdinal)[0];
            return tmp;
        }

        public byte[] DisconnectRequest()
        {
            byte[] data = client.DisconnectRequest(false);
            if (data == null)
            {
                return null;
            }
            return data;
        }
                
        /// <summary>
        /// Read DLMS Data from the device.
        /// </summary>
        /// <remarks>
        /// If access is denied return null.
        /// </remarks>
        /// <param name="data">Data to send.</param>
        /// <returns>Received data.</returns>
        public void ReadDLMSPacket(byte[] data, GXReplyData reply)
        {
            if ((data == null || data.Length == 0) && !reply.IsStreaming())
            {
                return;
            }
            GXReplyData notify = new GXReplyData();
            reply.Error = 0;
            object eop = (byte)0x7E;
            //In network connection terminator is not used.
            if (client.InterfaceType == InterfaceType.WRAPPER && !parent.UseRemoteSerial)
            {
                eop = null;
            }
            ReceiveParameters<byte[]> p = new ReceiveParameters<byte[]>()
            {
                AllData = false,
                Eop = eop,
                Count = 5,
                WaitTime = parent.WaitTime * 1000,
            };
            var answer = Send(data);
            GXByteBuffer rd = new GXByteBuffer(answer.Body);
            try
            {
                //Loop until whole COSEM packet is received.
                while (!client.GetData(rd, reply, notify))
                {
                    p.Reply = null;
                    if (notify.Data.Size != 0)
                    {
                        // Handle notify.
                        if (!notify.IsMoreData)
                        {
                            rd.Trim();
                            notify.Clear();
                            p.Eop = eop;
                        }
                        continue;
                    }
                    //If Eop is not set read one byte at time.
                    if (p.Eop == null)
                    {
                        p.Count = client.GetFrameSize(rd);
                    }
                    rd.Set(p.Reply);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (reply.Error != 0)
            {
                throw new GXDLMSException(reply.Error);
            }
        }



        public void UpdateSettings()
        {
            client.Authentication = this.parent.Authentication;
            client.InterfaceType = parent.InterfaceType;
            if (!string.IsNullOrEmpty(this.parent.Password))
            {
                client.Password = CryptHelper.Decrypt(this.parent.Password, Password.Key);
            }
            else if (this.parent.HexPassword != null)
            {
                client.Password = CryptHelper.Decrypt(this.parent.HexPassword, Password.Key);
            }
            client.UseLogicalNameReferencing = this.parent.UseLogicalNameReferencing;
            client.UtcTimeZone = parent.UtcTimeZone;
           
            //If network media is used check is manufacturer supporting IEC 62056-47
            if (client.InterfaceType == InterfaceType.WRAPPER && parent.UseRemoteSerial)
            {
                client.InterfaceType = InterfaceType.HDLC;
            }

            client.ClientAddress = parent.ClientAddress;
            if (parent.HDLCAddressing != HDLCAddressType.SerialNumber)
            {
                if (client.InterfaceType == InterfaceType.WRAPPER)
                {
                    client.ServerAddress = Convert.ToInt32(parent.PhysicalAddress);
                }
                else
                {
                    client.ServerAddress = GXDLMSClient.GetServerAddress(parent.LogicalAddress, Convert.ToInt32(parent.PhysicalAddress), parent.ServerAddressSize);
                    client.ServerAddressSize = parent.ServerAddressSize;
                }
            }
            client.Ciphering.Security = parent.Security;
            if (parent.SystemTitle != null && parent.BlockCipherKey != null && parent.AuthenticationKey != null)
            {
                client.Ciphering.SystemTitle = GXCommon.HexToBytes(parent.SystemTitle);
                client.Ciphering.BlockCipherKey = GXCommon.HexToBytes(parent.BlockCipherKey);
                client.Ciphering.AuthenticationKey = GXCommon.HexToBytes(parent.AuthenticationKey);
                client.Ciphering.InvocationCounter = parent.InvocationCounter;
            }
            else
            {
                client.Ciphering.SystemTitle = null;
                client.Ciphering.BlockCipherKey = null;
                client.Ciphering.AuthenticationKey = null;
                client.Ciphering.InvocationCounter = 0;
            }

            if (!string.IsNullOrEmpty(parent.Challenge))
            {
                client.CtoSChallenge = GXCommon.HexToBytes(parent.Challenge);
            }
            else
            {
                client.CtoSChallenge = null;
            }
            if (!string.IsNullOrEmpty(parent.DedicatedKey))
            {
                client.Ciphering.DedicatedKey = GXCommon.HexToBytes(parent.DedicatedKey);
            }
            else
            {
                client.Ciphering.DedicatedKey = null;
            }
            client.Limits.WindowSizeRX = parent.WindowSizeRX;
            client.Limits.WindowSizeTX = parent.WindowSizeTX;
            client.Limits.UseFrameSize = parent.UseFrameSize;
            client.Limits.MaxInfoRX = parent.MaxInfoRX;
            client.Limits.MaxInfoTX = parent.MaxInfoTX;
            client.MaxReceivePDUSize = parent.PduSize;
            client.UserId = parent.UserId;
            client.Priority = parent.Priority;
            client.ServiceClass = parent.ServiceClass;
            if (parent.PreEstablished)
            {
                client.ServerSystemTitle = GXCommon.HexToBytes(parent.ServerSystemTitle);
            }
        }

        public void InitializeConnection()
        {            
            try
            {
                log("Connection");
                GXReplyData reply = new GXReplyData();
                byte[] data;
                UpdateSettings();
                //Read frame counter if GeneralProtection is used.
                if (!string.IsNullOrEmpty(parent.FrameCounter) && client.Ciphering != null && client.Ciphering.Security != Security.None)
                {
                    reply.Clear();
                    int add = client.ClientAddress;
                    Authentication auth = client.Authentication;
                    Security security = client.Ciphering.Security;
                    byte[] challenge = client.CtoSChallenge;
                    try
                    {
                        client.ClientAddress = 16;
                        client.Authentication = Authentication.None;
                        client.Ciphering.Security = Security.None;

                        data = SNRMRequest();
                        if (data != null)
                        {
                            try
                            {
                                ReadDataBlock(data, "Send SNRM request.", reply);
                            }
                            catch (TimeoutException)
                            {
                                reply.Clear();
                                ReadDataBlock(DisconnectRequest(), "Send Disconnect request.", reply);
                                reply.Clear();
                                ReadDataBlock(data, "Send SNRM request.", reply);
                            }
                            catch (Exception e)
                            {
                                reply.Clear();
                                ReadDataBlock(DisconnectRequest(), "Send Disconnect request.", reply);
                                throw e;
                            }
                            //GXLogWriter.WriteLog("Parsing UA reply succeeded.");
                            //Has server accepted client.
                            ParseUAResponse(reply.Data);
                        }
                        ReadDataBlock(AARQRequest(), "Send AARQ request.", reply);
                        try
                        {
                            //Parse reply.
                            ParseAAREResponse(reply.Data);
                            //GXLogWriter.WriteLog("Parsing AARE reply succeeded.");
                            reply.Clear();
                            GXDLMSData d = new GXDLMSData(parent.FrameCounter);
                            ReadDLMSPacket(Read(d, 2), reply);
                            client.UpdateValue(d, 2, reply.Value);
                            client.Ciphering.InvocationCounter = parent.InvocationCounter = 1 + Convert.ToUInt32(d.Value);
                            reply.Clear();
                            ReadDataBlock(DisconnectRequest(), "Disconnect request", reply);
                        }
                        catch (Exception Ex)
                        {
                            reply.Clear();
                            ReadDataBlock(DisconnectRequest(), "Disconnect request", reply);
                            throw Ex;
                        }
                    }
                    finally
                    {
                        client.ClientAddress = add;
                        client.Authentication = auth;
                        client.Ciphering.Security = security;
                        client.CtoSChallenge = challenge;
                    }
                }
                data = SNRMRequest();
                if (data != null)
                {
                    try
                    {
                        reply.Clear();
                        ReadDataBlock(data, "Send SNRM request.", reply);
                    }
                    catch (TimeoutException)
                    {
                        reply.Clear();
                        ReadDataBlock(DisconnectRequest(), "Send Disconnect request.", reply);
                        reply.Clear();
                        ReadDataBlock(data, "Send SNRM request.", reply);
                    }
                    catch (Exception e)
                    {
                        reply.Clear();
                        ReadDataBlock(DisconnectRequest(), "Send Disconnect request.", reply);
                        throw e;
                    }
                    //GXLogWriter.WriteLog("Parsing UA reply succeeded.");
                    //Has server accepted client.
                    ParseUAResponse(reply.Data);
                }
                if (!parent.PreEstablished)
                {
                    //Generate AARQ request.
                    //Split requests to multiple packets if needed.
                    //If password is used all data might not fit to one packet.
                    reply.Clear();
                    ReadDataBlock(AARQRequest(), "Send AARQ request.", reply);
                    try
                    {
                        //Parse reply.
                        ParseAAREResponse(reply.Data);
                        //GXLogWriter.WriteLog("Parsing AARE reply succeeded.");
                    }
                    catch (Exception Ex)
                    {
                        reply.Clear();
                        ReadDLMSPacket(DisconnectRequest(), reply);
                        throw Ex;
                    }
                    //If authentication is required.
                    if (client.Authentication > Authentication.Low)
                    {
                        reply.Clear();
                        ReadDataBlock(client.GetApplicationAssociationRequest(), "Authenticating.", reply);
                        client.ParseApplicationAssociationResponse(reply.Data);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        void ReadDataBlock(byte[][] data, string text, GXReplyData reply)
        {
            foreach (byte[] it in data)
            {
                reply.Clear(); try
                {
                    ReadDataBlock(it, text, reply);
                }
                catch (Exception ex)
                {
                    //Update frame ID if meter returns error.
                    if (client.InterfaceType == InterfaceType.HDLC)
                    {
                        int target, source;
                        byte type;
                        GXDLMSClient.GetHdlcAddressInfo(new GXByteBuffer(it), out target, out source, out type);
                        client.Limits.SenderFrame = type;
                    }
                    throw ex;
                }
            }
        }
        
        public delegate void DataReceivedEventHandler(object sender, GXReplyData reply);
        public event DataReceivedEventHandler OnDataReceived;

        private readonly object balanceLock = new object();
        /// <summary>
        /// Read data block from the device.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <param name="text">Progress text.</param>
        /// <returns>Received data.</returns>
        internal void ReadDataBlock(byte[] data, string text, GXReplyData reply)
        {
            lock (balanceLock)
            {
                log(text);
                ReadDLMSPacket(data, reply);

                if (OnDataReceived != null)
                {
                    OnDataReceived(this, reply);
                }
                if (reply.IsMoreData)
                {
                    while (reply.IsMoreData)
                    {
                        data = client.ReceiverReady(reply.MoreData);
                        if ((reply.MoreData & RequestTypes.Frame) != 0)
                        {
                            log("Get next frame.");
                        }
                        else
                        {
                            log("Get Next Data block.");
                        }
                        //GXLogWriter.WriteLog(text, data);
                        ReadDLMSPacket(data, reply);
                        if (OnDataReceived != null)
                        {
                            OnDataReceived(this, reply);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Read object.
        /// </summary>
        public void ReadMineCommunicator(string objectName, ObjectType objectType, int it)
        {
            GXReplyData reply = new GXReplyData();
            {
                reply.Clear();
                {
                    byte[] data = client.Read(objectName, objectType, it)[0];
                    try
                    {
                        ReadDataBlock(data, $"Read object type {objectName} index: {it}", reply);
                        object value = reply.Value;
                        log($"{objectName}: value = {value}");
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }
        
        #endregion
            
    }
}

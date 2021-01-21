using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{

    public partial class Driver
    {

        public dynamic softStartControlDistributionByFunction(string cmd, dynamic flashver, string objectId, Guid idWls)
        {
            string[] arrString = cmd.Split('#');
            switch (arrString[1])
            {
                //case "waterTowerRequest":
                //    return WaterTowerRequest(flashver, objectId, idWls);
                //case "uppRequest":
                //    return UppRequest(flashver, objectId, idWls);
                //case "pumpStartStop":
                //    return PumpStartStop(Int32.Parse(arrString[2]), flashver, objectId, idWls);
                //case "control":
                //    return selectControl(arrString[2], flashver, objectId, idWls);
                case "controllerSet":
                    return controllerSet(arrString[2], flashver, objectId, idWls);
            }
            return 0;
        }

        public dynamic controllerSet(string strControl, dynamic flashver, string objectId, Guid idWls)
        {
            float height = 12.6F;
            byte wls = 0xFF;
            switch (strControl)
            {
                case "levelmax":
                    height = 13.5F;
                    wls = 0x0F;
                    break;
                case "levelmin":
                    height = 11.8F;
                    wls = 0x00;
                    break;
            }

            if (flashver == null)
            {
                flashver = GetFlashVer();
            }
            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            List<byte> byteFor50Command = new List<byte>();
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;

            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray();
            byteFor50Command.AddRange(byteObjectId);
            DateTime now = DateTime.Now;
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            byte[] byteTotalSecond = BitConverter.GetBytes((int)(now - dt1970).TotalSeconds);
            byteFor50Command.AddRange(byteTotalSecond);
           

            log(string.Format("dataWls != null"), level: 1);
            byte[] byteLastQueryWls = BitConverter.GetBytes((int)(now - dt1970).TotalSeconds);
            byte[] bytesHeight = BitConverter.GetBytes(height);
            byteFor50Command.Add(0x02);
            byteFor50Command.Add(wls);
            byteFor50Command.AddRange(bytesHeight);
            log(string.Format($"wls = 0x{wls:X2} |height={height}"), level: 1);
            byteFor50Command.AddRange(byteLastQueryWls);
            
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();


            if ((ver == 1) && (typeDevice == 8))
            {

                dynamic result = Send(MakeBaseRequest(50, byteFor50Command));
                if (!result.success)
                {
                    log(string.Format("50 команда не введена: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x32)
                {
                    log(string.Format("Получен ответ {0} не на 50 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }

                if ((ver == 1) && (typeDevice == 8))
                {
                    UInt32 uInt32Time = (UInt32)(result.Body[3] << 24) | (UInt32)(result.Body[2] << 16) | (UInt32)(result.Body[1] << 8) | result.Body[0];
                    DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);
                    
                    byte UppNA = (byte)result.Body[4];
                    byte WlsNA = (byte)result.Body[5];
                    byte inStatus = (byte)result.Body[6];
                    byte outStatus = (byte)result.Body[7];
                    UInt16 errorControls = Helper.ToUInt16(result.Body, 8);
                    UInt16 eventControls = Helper.ToUInt16(result.Body, 10);

                    byte transferAllow = (byte)result.Body[12];
                    byte typeAndVersion = (byte)result.Body[13];
                    byte configSize = (byte)result.Body[14];

                    byte stateUppActive = (byte)result.Body[15];
                    int lastUppRespTime = (((result.Body[19] != 0xFF) && (result.Body[18] != 0xFF) && (result.Body[17] != 0xFF) && result.Body[16] != 0xFF)) ?
                            (int)(result.Body[19] << 24) | (int)(result.Body[18] << 16) | (int)(result.Body[17] << 8) | result.Body[16] : 0;
                    DateTime dtUppLastResp = dt1970.AddSeconds(lastUppRespTime);

                    UInt16 uint16UppActive = Helper.ToUInt16(result.Body, 20);

                    int lastWlsRespTime = (((result.Body[25] != 0xFF) && (result.Body[24] != 0xFF) && (result.Body[23] != 0xFF) && result.Body[22] != 0xFF)) ?
                        (int)(result.Body[25] << 24) | (int)(result.Body[24] << 16) | (int)(result.Body[23] << 8) | result.Body[22] : 0;
                    DateTime dtWlsLastResp = dt1970.AddSeconds(lastWlsRespTime);

                    UInt16 uint16WlsActive = Helper.ToUInt16(result.Body, 26);

                    int lastPressureSensorRespTime = (((result.Body[31] != 0xFF) && (result.Body[30] != 0xFF) && (result.Body[29] != 0xFF) && result.Body[28] != 0xFF)) ?
                        (int)(result.Body[31] << 24) | (int)(result.Body[30] << 16) | (int)(result.Body[29] << 8) | result.Body[28] : 0;
                    DateTime dtPressureSensorResp = dt1970.AddSeconds(lastPressureSensorRespTime);


                    float pressure = BitConverter.ToSingle(result.Body, 32);// в нашем случае давление == высота, тк коэфф = 1.03 

                    UInt16 motorCurrent = Helper.ToUInt16(result.Body, 36);
                    UInt16 thermalLoad = Helper.ToUInt16(result.Body, 38);
                    UInt16 currentPhase1 = Helper.ToUInt16(result.Body, 40);
                    UInt16 currentPhase2 = Helper.ToUInt16(result.Body, 42);
                    UInt16 currentPhase3 = Helper.ToUInt16(result.Body, 44);
                    UInt16 currentPhaseMax = Helper.ToUInt16(result.Body, 46);
                    UInt16 frequency = Helper.ToUInt16(result.Body, 48);
                    int power = Helper.ToUInt16(result.Body, 50) / 100;
                    UInt16 voltage = Helper.ToUInt16(result.Body, 52);
                    int startCount = Helper.ToUInt16(result.Body, 54) * 100;
                    int runtimeHours = Helper.ToUInt16(result.Body, 56) * 10;
                    UInt16 modbusError = Helper.ToUInt16(result.Body, 58);
                    UInt16 modbusToglle = Helper.ToUInt16(result.Body, 60);

                    int lastStartTime = (((result.Body[65] != 0xFF) && (result.Body[64] != 0xFF) && (result.Body[63] != 0xFF) && result.Body[62] != 0xFF)) ?
                       (int)(result.Body[65] << 24) | (int)(result.Body[64] << 16) | (int)(result.Body[63] << 8) | result.Body[62] : 0;
                    DateTime dtLastStartTime = dt1970.AddSeconds(lastStartTime);

                    int lastStopTime = (((result.Body[69] != 0xFF) && (result.Body[68] != 0xFF) && (result.Body[67] != 0xFF) && result.Body[66] != 0xFF)) ?
                       (int)(result.Body[69] << 24) | (int)(result.Body[68] << 16) | (int)(result.Body[67] << 8) | result.Body[66] : 0;
                    DateTime dtLastStopTime = dt1970.AddSeconds(lastStopTime);


                    string strDtUppLastResp = (lastUppRespTime != 0) ? dtUppLastResp.ToString() : "undefined";
                    string strDtWlsLastResp = (lastWlsRespTime != 0) ? dtWlsLastResp.ToString() : "undefined";
                    string strDtPressureSensorLastResp = (lastPressureSensorRespTime != 0) ? dtPressureSensorResp.ToString() : "undefined";
                    DateTime date = DateTime.Now;
                    string switchUpp = (stateUppActive == 2) ? "left" : "right";
                    log($"UppNA: {UppNA}; WlsNA: {WlsNA}; Состояние реле:( in={inStatus} / out={outStatus}); error={errorControls}; event={eventControls}; Разрешение трансфера:{transferAllow}", level: 3);
                    log($"typeAndVersion={typeAndVersion}; размер config={configSize}; ", level: 3);

                    string strUppState = UppState(uint16UppActive);

                    log($"УПП(0x{uint16UppActive:X}):{strUppState}; Время опроса ={strDtUppLastResp};", level: 1);
                    log($"lastStartTime:{dtLastStartTime}; lastStopTime:{dtLastStopTime}; currentPhaseMax:{currentPhaseMax}A", level: 1);
                    log($"Wls=0x{uint16WlsActive:X}; Высота={pressure:N2}м; Время опроса={strDtWlsLastResp}| Переключатель={switchUpp};", level: 1);
                    log($"motorCurrent:{motorCurrent}%; thermalLoad:{thermalLoad}%; frequency:{frequency}Hz; power:{power}; voltage:{voltage}%; startCount:{startCount}; runtimeHours:{runtimeHours}h", level: 3);
                    string curPhase123 = $"currentPhase1:{currentPhase1}A; currentPhase2:{currentPhase2}A; currentPhase3:{currentPhase3}A";
                    log($"{curPhase123}; modbusError:{modbusError}; modbusToglle:{modbusToglle}", level: 3);

                    if (!((pressure < 0.5) && (uint16WlsActive > 0)))
                    {
                        records.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", dtContollers, date));
                    }
                    else
                    {
                        log("-----height-----", level: 3);
                    }
                    records.Add(MakeCurrentRecord("wls", Convert.ToDouble(uint16WlsActive), strDtWlsLastResp, dtContollers, date));
                    records.Add(MakeCurrentRecord("upp", Convert.ToDouble(uint16UppActive), strUppState, dtContollers, date));
                    records.Add(MakeCurrentRecord("switch", Convert.ToDouble(stateUppActive), switchUpp, dtContollers, date));
                    records.Add(MakeCurrentRecord("motorCurrent", Convert.ToDouble(motorCurrent), "%", dtContollers, date));
                    records.Add(MakeCurrentRecord("startCount", Convert.ToDouble(startCount), "", dtContollers, date));
                    records.Add(MakeCurrentRecord("runtimeHours", Convert.ToDouble(runtimeHours), "h", dtContollers, date));
                    records.Add(MakeCurrentRecord("currentPhaseMax", Convert.ToDouble(currentPhaseMax), curPhase123, dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStartTime", 0, $"{dtLastStartTime}", dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStopTime", 0, $"{dtLastStopTime}", dtContollers, date));
                }
            }

            current.records = records;
            return current;
        }
        public dynamic selectControl(string strControl, dynamic flashver, string objectId, Guid idWls)
        {
            byte byteControl = 0xFF;
            switch (strControl)
            {
                case "controller":
                    byteControl = 0x00;
                    break;
                case "manual":
                    byteControl = 0x01;
                    break;
            }
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            dynamic wta = WTAForRequestUppOrWls50Command(flashver, objectId, idWls);
            if (!wta.success)
            {
                return MakeResult(101, wta.errorcode, wta.error);
            }
            List<byte> for51Command = new List<byte>();
            for51Command.Add(byteControl);
            for51Command.Add(0xFF);
            for51Command.Add(0xFF);
            dynamic result = Send(MakeBaseRequest(51, for51Command));
            return result;
        }

        #region WaterTowerAutomation 
        public dynamic WaterTowerRequest(dynamic flashver, string objectId, Guid idWls)
        {
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            dynamic wta = WTAForRequestUppOrWls50Command(flashver, objectId, idWls);
            if (!wta.success)
            {
                return MakeResult(101, wta.errorcode, wta.error);
            }
            
            dynamic result = Send(MakeBaseRequest(52, MakeBaseRequest(wta.wlsNA, 0x04, new List<byte> { 0x01, 0x00, 0x00, 0x01 })));
            if (!result.success)
            {
                return MakeResult(101, result.errorcode, result.error);
            }
            Thread.Sleep(10);
            return result;
        }
        public dynamic UppRequest(dynamic flashver, string objectId, Guid idWls)
        {
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            dynamic wta = WTAForRequestUppOrWls50Command(flashver, objectId, idWls);
            if (!wta.success)
            {
                return MakeResult(101, wta.errorcode, wta.error);
            }
            //List<byte> for52Command = new List<byte>();
            //for52Command.AddRange(MakeBaseRequest(wta.uppNA, 0x01, new List<byte> { 0x00, 0x00, 0x00, 0x10 }));
            //dynamic result = Send(MakeBaseRequest(52, for52Command));
            dynamic result = Send(MakeBaseRequest(52, MakeBaseRequest(wta.uppNA, 0x01, new List<byte> { 0x00, 0x00, 0x00, 0x10 })));
            if (!result.success)
            {
                return MakeResult(101, result.errorcode, result.error);
            }
            return result;
        }
        public dynamic PumpStartStop(int startOrStop, dynamic flashver,  string objectId, Guid idWls)
        { 
            byte tmpStartOrStop = (byte)startOrStop;  // 0 , 1
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }

            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            dynamic wta = WTAForRequestUppOrWls50Command(flashver, objectId, idWls);
            if (!wta.success)
            {
                return MakeResult(101, wta.errorcode, wta.error);
            }
            List<byte> for52Command = new List<byte>();

            if (tmpStartOrStop == 0) // выключение
            {
                dynamic result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x06, 0xFF, 0x00 }, 0x06, 1, 100);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                UInt16 uppData = BitConverter.ToUInt16(result.Body, 3);
                if (((uppData >> 0x06) & 1) == 1)
                {
                    log("Сброс ошибки не выполнился", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }
                result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x05, 0xFF, 0x00 }, 0x05, 0, 5000);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                uppData = BitConverter.ToUInt16(result.Body, 3);
                if (((uppData >> 0x05) & 1) == 0)
                {
                    log("Установка автомода не выполнилась", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }
                result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x01, 0xFF, 0x00 }, 0x02, 1, 5000);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                uppData = BitConverter.ToUInt16(result.Body, 3);
                if (((uppData >> 0x02) & 1) == 1)
                {
                    log("Не удалось отключить двигатель", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }

                result = send54CommandWithCycle(54, new List<byte> { 0x00, 0x01, 0x00, 0x00 }, 0x01);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                if (result.Body[3] == 0x01)
                {
                    log("Контактор не выключился", level: 1);
                }
                
                return result;
            }
            if (tmpStartOrStop == 1) // включение
            {
                dynamic result = send54CommandWithCycle(54, new List<byte> { 0x00, 0x01, 0x00, 0x01 }, 0x00);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                if (result.Body[3] == 0x00)
                {
                    log("Контактор не включился", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }
                result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x06, 0xFF, 0x00 }, 0x06, 1, 100);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                UInt16 uppData = BitConverter.ToUInt16(result.Body, 3);
                if ((((uppData >> 0x06) & 1) == 1))
                {
                    log("Сброс ошибки не выполнился", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }

                result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x05, 0xFF, 0x00 }, 0x05, 0, 5000);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                uppData = BitConverter.ToUInt16(result.Body, 3);
                if ((((uppData >> 0x05) & 1) == 0))
                {
                    log("Установка автомода не выполнилась", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }
                result = send52CommandWithCycleForUpp(wta.uppNA, 0x05, new List<byte> { 0x01, 0x02, 0xFF, 0x00 }, 0x02, 0, 100);
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                uppData = BitConverter.ToUInt16(result.Body, 3);
                if ((((uppData >> 0x02) & 1) == 0))
                {
                    log("Не удалось включить двигатель", level: 1);
                    return MakeResult(101, result.errorcode, result.error);
                }
                return result;
            }
            return 0;
        }
        public dynamic send54CommandWithCycle(byte function, List<byte> data, byte forAnswer)
        {
            dynamic result = new ExpandoObject();
            int k = 0;
            do
            {
                Thread.Sleep(100);
                result = Send(MakeBaseRequest(function, data));
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                log($"result.Body[3]={result.Body[3]}", 1);
            } while (result.Body[3] == forAnswer &&  k < 3);

            return result;
        }
        public dynamic send52CommandWithCycleForUpp(byte networkAddress, byte function, List<byte> data, byte protocolAddress, byte description, int sleep)
        {
            dynamic result = new ExpandoObject();
            UInt16 uppData;
            int k = 0;
            do
            {
                Thread.Sleep(sleep);
                result = Send(MakeBaseRequest(52, MakeBaseRequest(networkAddress, function, data)));
                if (!result.success)
                {
                    return MakeResult(101, result.errorcode, result.error);
                }
                Thread.Sleep(10);
                result = Send(MakeBaseRequest(52, MakeBaseRequest(networkAddress, 0x01, new List<byte> { 0x00, 0x00, 0x00, 0x10 })));
                uppData = BitConverter.ToUInt16(result.Body, 3);

                log($"uppData[3]={uppData} || {((uppData >> protocolAddress) & 1)}", 1);
            } while ((((uppData >> protocolAddress) & 1) == description) && k < 3);

            return result;
        }

        public string UppState(UInt16 upp)
        {
            string strTmp = "мотор=";
            if (upp == 0) return "ОШИБКА считывания УПП";
            if ((upp & 0b1110011001) != 0) return "ОШИБКА считывания УПП";
            if (((upp >> 0x01) & 1) == ((upp >> 0x02) & 1)) return "ОШИБКА считывания УПП";
            switch (((upp >> 0x02) & 1))
            {
                case 0:
                    strTmp += "STOP";
                    break;
                case 1:
                    strTmp +=  "START";
                    break;
                default:
                    strTmp += "undefined";
                    break;
            }
            strTmp += $"; Auto mode={((upp >> 5) & 1)}; Fault={((upp >> 6) & 1)}; DI=0b{((upp >> 11) & 1)}{((upp >> 12) & 1)}{((upp >> 13) & 1)}; TOR={((upp >> 14) & 1)}; ReadySS={((upp >> 15) & 1)}";
            return strTmp;
        }
        public double motorState(UInt16 upp)
        {
            if (upp == 0) return -1;
            if ((upp & 0b1110011001) != 0) return -1;
            if (((upp >> 0x01) & 1) == ((upp >> 0x02) & 1)) return -1;
            return (double)((upp >> 0x02) & 1);
        }
        public dynamic WTAForRequestUppOrWls50Command(dynamic flashver, string objectId, Guid idWls)//WaterTowerAutomation
        {
            List<byte> byteFor50Command = new List<byte>();
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray();
            byteFor50Command.AddRange(byteObjectId);
            DateTime now = DateTime.Now;
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            byte[] byteTotalSecond = BitConverter.GetBytes((int)(now - dt1970).TotalSeconds);
            byteFor50Command.AddRange(byteTotalSecond);

            var dataWls = dataFromWLS(idWls);
            byte[] byteLastQueryWls = BitConverter.GetBytes((int)(dataWls.date - dt1970).TotalSeconds);
            byte byteHight = Convert.ToByte(dataWls.hight);
            byte byteWls = Convert.ToByte(dataWls.wls);
            byteFor50Command.Add(0x00);
            
            byteFor50Command.Add(byteWls);
            byteFor50Command.Add(byteHight);
            byteFor50Command.AddRange(byteLastQueryWls);
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();


            if ((ver == 1) && (typeDevice == 8))
            {

                dynamic result = Send(MakeBaseRequest(50, byteFor50Command));
                if (!result.success)
                {
                    log(string.Format("50 команда не введена: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x32)
                {
                    log(string.Format("Получен ответ {0} не на 50 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }

                if ((ver == 1) && (typeDevice == 8))
                {
                    UInt32 uInt32Time = (UInt32)(result.Body[3] << 24) | (UInt32)(result.Body[2] << 16) | (UInt32)(result.Body[1] << 8) | result.Body[0];
                    DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);
                    
                    byte uppNA = (byte)result.Body[4];
                    byte wlsNA = (byte)result.Body[5];
                    byte inStatus = (byte)result.Body[6];
                    byte outStatus = (byte)result.Body[7];
                    UInt16 errorControls = Helper.ToUInt16(result.Body, 8);
                    UInt16 eventControls = Helper.ToUInt16(result.Body, 10);

                    byte transferAllow = (byte)result.Body[12];
                    byte typeAndVersion = (byte)result.Body[13];
                    byte configSize = (byte)result.Body[14];

                    byte stateUppActive = (byte)result.Body[15];
                    UInt32 lastUppRespTime = (((result.Body[19] == 0xFF) && (result.Body[18] == 0xFF) && (result.Body[17] == 0xFF) && result.Body[16] == 0xFF)) ? 0 :
                            (UInt32)(result.Body[19] << 24) | (UInt32)(result.Body[18] << 16) | (UInt32)(result.Body[17] << 8) | result.Body[16];
                    DateTime dtUppLastResp = dt1970.AddSeconds(lastUppRespTime);

                    UInt16 uint16UppActive = Helper.ToUInt16(result.Body, 20);

                    UInt32 lastWlsRespTime = (UInt32)(result.Body[25] << 24) | (UInt32)(result.Body[24] << 16) | (UInt32)(result.Body[23] << 8) | result.Body[22];
                    if (lastWlsRespTime == 0xFFFFFFFF) lastWlsRespTime = 0;

                    DateTime dtWlsLastResp = dt1970.AddSeconds(lastWlsRespTime);

                    UInt16 uint16WlsActive = Helper.ToUInt16(result.Body, 26);

                    float pressure = BitConverter.ToSingle(result.Body, 32);// в нашем случае давление == высота, тк коэфф = 1.03 

                    UInt16 motorCurrent = Helper.ToUInt16(result.Body, 36);
                    UInt16 thermalLoad = Helper.ToUInt16(result.Body, 38);
                    UInt16 currentPhase1 = Helper.ToUInt16(result.Body, 40);
                    UInt16 currentPhase2 = Helper.ToUInt16(result.Body, 42);
                    UInt16 currentPhase3 = Helper.ToUInt16(result.Body, 44);
                    UInt16 currentPhaseMax = Helper.ToUInt16(result.Body, 46);
                    UInt16 frequency = Helper.ToUInt16(result.Body, 48);
                    int power = Helper.ToUInt16(result.Body, 50) / 100;
                    UInt16 voltage = Helper.ToUInt16(result.Body, 52);
                    int startCount = Helper.ToUInt16(result.Body, 54) * 100;
                    int runtimeHours = Helper.ToUInt16(result.Body, 56) * 10;
                    UInt16 modbusError = Helper.ToUInt16(result.Body, 58);
                    UInt16 modbusToglle = Helper.ToUInt16(result.Body, 60);

                    UInt32 lastStartTime = (UInt32)(result.Body[65] << 24) | (UInt32)(result.Body[64] << 16) | (UInt32)(result.Body[63] << 8) | result.Body[62];
                    if (lastStartTime == 0xFFFFFFFF) lastStartTime = 0;
                    DateTime dtLastStartTime = dt1970.AddSeconds(lastStartTime);

                    UInt32 lastStopTime = (UInt32)(result.Body[69] << 24) | (UInt32)(result.Body[68] << 16) | (UInt32)(result.Body[67] << 8) | result.Body[66];
                    if (lastStopTime == 0xFFFFFFFF) lastStopTime = 0;
                    DateTime dtLastStopTime = dt1970.AddSeconds(lastStopTime);

                    string strDtUppLastResp = (lastUppRespTime != 0) ? dtUppLastResp.ToString() : "undefined";
                    string strDtWlsLastResp = (lastWlsRespTime != 0) ? dtWlsLastResp.ToString() : "undefined";
                    DateTime date = DateTime.Now;
                    string switchUpp = (stateUppActive == 2) ? "left" : "right";
                    log($"UppNA : {uppNA}; WlsNA : {wlsNA}; Состояние реле:( in={inStatus} / out={outStatus}); error={errorControls}; event={eventControls}; Разрешение трансфера:{transferAllow}", level: 3);
                    log($"typeAndVersion={typeAndVersion}; размер config={configSize};", level: 3);
                    string strUppState = UppState(uint16UppActive);

                    log($"УПП(0x{uint16UppActive:X}):{strUppState}; Время опроса ={strDtUppLastResp};", level: 1);
                    log($"lastStartTime:{dtLastStartTime}; lastStopTime:{dtLastStopTime}; currentPhaseMax:{currentPhaseMax}A", level: 1);
                    log($"Wls=0x{uint16WlsActive:X}; Время опроса={strDtWlsLastResp}| Переключатель={switchUpp};", level: 1);
                    log($"motorCurrent:{motorCurrent}%; thermalLoad:{thermalLoad}%; frequency:{frequency}Hz; power:{power}; voltage:{voltage}%; startCount:{startCount}; runtimeHours:{runtimeHours}h", level: 3);
                    string curPhase123 = $"currentPhase1:{currentPhase1}A; currentPhase2:{currentPhase2}A; currentPhase3:{currentPhase3}A";
                    log($"{curPhase123}; modbusError:{modbusError}; modbusToglle:{modbusToglle}", level: 3);

                    records.Add(MakeCurrentRecord("upp", Convert.ToDouble(uint16UppActive), strUppState, dtContollers, date));
                    records.Add(MakeCurrentRecord("wls", Convert.ToDouble(uint16WlsActive), "", dtContollers, date));
                    records.Add(MakeCurrentRecord("switch", Convert.ToDouble(stateUppActive), switchUpp, dtContollers, date));
                    records.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", dtContollers, date));
                    records.Add(MakeCurrentRecord("motorCurrent", Convert.ToDouble(motorCurrent), "%", dtContollers, date));
                    records.Add(MakeCurrentRecord("startCount", Convert.ToDouble(startCount), "", dtContollers, date));
                    records.Add(MakeCurrentRecord("runtimeHours", Convert.ToDouble(runtimeHours), "h", dtContollers, date));
                    records.Add(MakeCurrentRecord("currentPhaseMax", Convert.ToDouble(currentPhaseMax), curPhase123, dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStartTime", 0, $"{dtLastStartTime}", dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStopTime", 0, $"{dtLastStopTime}", dtContollers, date));

                    current.uppNA = uppNA;
                    current.wlsNA = wlsNA;
                }
            }
            current.records = records;
            return current;
        }
        public dynamic dataFromWLS(Guid idWls)
        {
            DateTime dtNow = DateTime.Now;
            List<dynamic> listRecords = recordLoadWithId(dtNow.AddMinutes(-25), dtNow, "Current", idWls);

            List<dynamic> listRecHeight = listRecords.FindAll(x => x.s1 == "высота");
            List<dynamic> listRecWls = listRecords.FindAll(x => x.s1 == "wls");
            dynamic data = new ExpandoObject();
            if (listRecWls.Count() == 0 || listRecHeight.Count() == 0) return null;
            data.wls = listRecWls.Find(x => x.date == listRecWls.Max(y => y.date)).d1;//.Last().d1;
            data.height = listRecHeight.Find(x => x.date == listRecHeight.Max(y => y.date)).d1;
            data.date = listRecWls.Max(x => x.date);

            return data;
        }
        public dynamic dataFromRowCache(Guid idWls)
        {
            DateTime dtNow = DateTime.Now;
            List<dynamic> rows = loadRowsCache(idWls);
            dynamic row = rows[0];
            return row;
        }
        public dynamic WTAGetConfig()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            List<byte> tmpBytes = new List<byte>() { 0x00, 0x00, 0x00 };

            var result1 = Send(MakeBaseRequest(96, tmpBytes));
            if (!result1.success)
            {
                log(string.Format("Настройки по команде 96 не записаны: {0}", result1.error), level: 1);
            }
            if (result1.Function != 0x60)
            {
                log(string.Format("Получен ответ {0} не на 96 команду ", result1.Function), level: 1);
                current.success = false;
                return current;
            }
            dynamic control = new ExpandoObject();
            control.controllerConfig = BitConverter.ToString(result1.Body);
            setModbusControl(control);

            log("update", level: 1);
            return null;
        }
        public dynamic WTA50Command(dynamic flashver, string objectId, Guid idWls, float max, float min)//WaterTowerAutomation
        {
            List<byte> byteFor50Command = new List<byte>();
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;

            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray();
            byteFor50Command.AddRange(byteObjectId);
            DateTime now = DateTime.Now;
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            
            byte[] byteTotalSecond = BitConverter.GetBytes((int)(now - dt1970).TotalSeconds);
            byteFor50Command.AddRange(byteTotalSecond);

            var dataWls = dataFromWLS(idWls);

            //var dataRowCache = dataFromRowCache(idWls);
            if(dataWls == null)
            {

                log(String.Format($"dataWls == null"), level: 1);
                //    log(string.Format("dataWls== null"), level: 3);
                //    DateTime dtRow = (DateTime)dataRowCache.date;
                //    if((now - dtRow).Minutes < 25)
                //    {
                //        double height = (double)dataRowCache.value;
                //        log($"height={height} min={min} max={max}", level: 1);
                //        min = 12.5f; max = 13.5f;        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                byte byteWls = 0b0011;
                //        byte[] bytesHeight = BitConverter.GetBytes((float)height);
                //       byte[] byteLastQueryWls = BitConverter.GetBytes((int)(dtRow - dt1970).TotalSeconds);
                //        if (height <= min)
                //        {
                //            log(String.Format($"height <= min"), level: 1);
                //            byteWls = 0b0001;
                //        }
                //        else if (height >= max)
                //        {
                //            log(String.Format($"height >= max"), level: 1);
                //            byteWls = 0b1111;
                //        }
                //        else
                //        {
                //            log(String.Format($"height <= min and height >= max not enter"), level: 1);
                //        }

                byte[] bytesHeight = BitConverter.GetBytes(min);
                byte[] byteLastQueryWls = BitConverter.GetBytes((int)(DateTime.Now - dt1970).TotalSeconds);
                if (min == 15 || min == 14)
                {
                    byteWls = 0b1111;
                }
                else if(min == 0 || min == 10)
                {
                    byteWls = 0;
                }
                log(String.Format($"byteWls = 0x{byteWls:X2} |height={min}"), level: 1);
                byteFor50Command.Add(0x02);
                byteFor50Command.Add(byteWls);
                byteFor50Command.AddRange(bytesHeight);
                byteFor50Command.AddRange(byteLastQueryWls);
                //    }
                //    else
                //    {
                //        byteFor50Command.Add(0x00);
                //        byteFor50Command.Add(0xff);
                //        byteFor50Command.AddRange(new List<byte> { 0xff, 0xff, 0xff, 0xff });
                //        byteFor50Command.AddRange(new List<byte> { 0xff, 0xff, 0xff, 0xff });
                //    }
            }
            else
            {
                log(string.Format("dataWls != null"), level: 3);
                byte[] byteLastQueryWls = BitConverter.GetBytes((int)(dataWls.date - dt1970).TotalSeconds);
                float dataWlsHeight = Convert.ToSingle(dataWls.height);
                byte byteWls = Convert.ToByte(dataWls.wls);
                if(byteWls == 0)
                {
                    dataWlsHeight = 10;
                }
                else if(byteWls == 1)
                {
                    dataWlsHeight = 11;
                }
                else if (byteWls == 0b0011)
                {
                    dataWlsHeight = 12;
                }
                else if (byteWls == 0b0111)
                {
                    dataWlsHeight = 13;
                }
                else if (byteWls == 0b1111)
                {
                    dataWlsHeight = 14;
                }
                else
                {
                    dataWlsHeight = 11;
                }


                if(byteWls > 1 && byteWls <= 7)
                {
                    byteWls = 0b0011;
                }

                /*if (byteWls > 7)
                {
                    byteWls = 0b1111;
                }
                */
                if (min == 15 || min == 14)
                {
                    byteWls = 0b1111;
                }
                else if (min == 0 || min == 10)
                {
                    byteWls = 0;
                }
                byte[] tt = BitConverter.GetBytes(dataWlsHeight);
                //DateTime dtRow = (DateTime)dataRowCache.date;
                //if (dtRow.CompareTo(dataWls.date) > 0)
                //{
                //    dataWlsHeight = Single.Parse(((string)dataRowCache.value).Replace('.', ','));
                //    log($"rowCache height={dataWlsHeight} min={min} max={max}", level: 1);
                //    tt = BitConverter.GetBytes(dataWlsHeight);
                //    byteLastQueryWls = BitConverter.GetBytes((int)(dtRow - dt1970).TotalSeconds);
                //}

                //byteWls = 0b0011;
                //min = 12.5f; max = 13.5f;        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //if (dataWlsHeight <= min)
                //{
                //    log(String.Format($"rr <= min"), level: 1);
                //    byteWls = 0b0001;
                //}
                //else if (dataWlsHeight >= max)
                //{
                //    log(String.Format($"rr >= max"), level: 1);
                //    byteWls = 0b1111;
                //}
                //else
                //{
                //    log(String.Format($"rr <= min and rr >= max not enter"), level: 1);
                //}
                //***** Вставка 16.01.20   конец 
                /*****  Вставка 06/01/2020   конец   */
                //byteWls = 0b111;
                byteFor50Command.Add(0x02);
                byteFor50Command.Add(byteWls);
                byteFor50Command.AddRange(tt);
                log(String.Format($"byteWls = 0x{byteWls:X2} |height={dataWlsHeight}"), level: 1);
                byteFor50Command.AddRange(byteLastQueryWls);
            }

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();
            

            if ((ver == 1) && (typeDevice == 8))
            {

                dynamic result = Send(MakeBaseRequest(50, byteFor50Command));
                if (!result.success)
                {
                    log(string.Format("50 команда не введена: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x32)
                {
                    log(string.Format("Получен ответ {0} не на 50 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }

                if ((ver == 1) && (typeDevice == 8))
                {
                    UInt32 uInt32Time = (UInt32)(result.Body[3] << 24) | (UInt32)(result.Body[2] << 16) | (UInt32)(result.Body[1] << 8) | result.Body[0];
                    DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);

                    /*
                    DateTime now = DateTime.Now;
                    var diffSecond = ((dtContollers > now) ? (dtContollers - now).TotalSeconds : (now - dtContollers).TotalSeconds);
                    var diffDay = ((dtContollers > now) ? (dtContollers - now).TotalDays : (now - dtContollers).TotalDays);
                    if (diffSecond > TimeSecondForSet)
                    {
                        CorrectTime(flashver);
                        if (diffDay > TimeDayForSet)
                        {
                            log($"{msgSetTime}  разница: {diffDay} дней", level: 1);
                            return lightControlSetSoftConfig(0xFF, 0x03, flashver, objectId);
                        }
                    }
                    */

                    byte UppNA = (byte)result.Body[4];
                    byte WlsNA = (byte)result.Body[5];
                    byte inStatus = (byte)result.Body[6];
                    byte outStatus = (byte)result.Body[7];
                    UInt16 errorControls = Helper.ToUInt16(result.Body, 8);
                    UInt16 eventControls = Helper.ToUInt16(result.Body, 10);

                    byte transferAllow = (byte)result.Body[12];
                    byte typeAndVersion = (byte)result.Body[13];
                    byte configSize = (byte)result.Body[14];

                    byte stateUppActive = (byte)result.Body[15];
                    UInt32 lastUppRespTime = (UInt32)(result.Body[19] << 24) | (UInt32)(result.Body[18] << 16) | (UInt32)(result.Body[17] << 8) | result.Body[16];
                    if (lastUppRespTime == 0xFFFFFFFF) lastUppRespTime = 0;
                    DateTime dtUppLastResp = dt1970.AddSeconds(lastUppRespTime);

                    UInt16 uint16UppActive = Helper.ToUInt16(result.Body, 20);

                    UInt32 lastWlsRespTime = (UInt32)(result.Body[25] << 24) | (UInt32)(result.Body[24] << 16) | (UInt32)(result.Body[23] << 8) | result.Body[22];
                    if (lastWlsRespTime == 0xFFFFFFFF) lastWlsRespTime = 0;
                    DateTime dtWlsLastResp = dt1970.AddSeconds(lastWlsRespTime);
                    
                    UInt16 uint16WlsActive = Helper.ToUInt16(result.Body, 26);

                    UInt32 lastPressureSensorRespTime = (UInt32)(result.Body[31] << 24) | (UInt32)(result.Body[30] << 16) | (UInt32)(result.Body[29] << 8) | result.Body[28];
                    if (lastPressureSensorRespTime == 0xFFFFFFFF) lastPressureSensorRespTime = 0;
                    DateTime dtPressureSensorResp = dt1970.AddSeconds(lastPressureSensorRespTime);
                    

                    float pressure = BitConverter.ToSingle(result.Body, 32);// в нашем случае давление == высота, тк коэфф = 1.03 
                    
                    UInt16 motorCurrent = Helper.ToUInt16(result.Body, 36);
                    UInt16 thermalLoad = Helper.ToUInt16(result.Body, 38);
                    UInt16 currentPhase1 = Helper.ToUInt16(result.Body, 40);
                    UInt16 currentPhase2 = Helper.ToUInt16(result.Body, 42);
                    UInt16 currentPhase3 = Helper.ToUInt16(result.Body, 44);
                    UInt16 currentPhaseMax = Helper.ToUInt16(result.Body, 46);
                    UInt16 frequency = Helper.ToUInt16(result.Body, 48);
                    int power = Helper.ToUInt16(result.Body, 50) / 100;
                    UInt16 voltage = Helper.ToUInt16(result.Body, 52);
                    int startCount = Helper.ToUInt16(result.Body, 54) * 100;
                    int runtimeHours = Helper.ToUInt16(result.Body, 56) * 10;
                    UInt16 modbusError = Helper.ToUInt16(result.Body, 58);
                    UInt16 modbusToglle = Helper.ToUInt16(result.Body, 60);

                    UInt32 lastStartTime = (UInt32)(result.Body[65] << 24) | (UInt32)(result.Body[64] << 16) | (UInt32)(result.Body[63] << 8) | result.Body[62];
                    if (lastStartTime == 0xFFFFFFFF) lastStartTime = 0;
                    DateTime dtLastStartTime = dt1970.AddSeconds(lastStartTime);

                    UInt32 lastStopTime = (UInt32)(result.Body[69] << 24) | (UInt32)(result.Body[68] << 16) | (UInt32)(result.Body[67] << 8) | result.Body[66];
                    if (lastStopTime == 0xFFFFFFFF) lastStopTime = 0;
                    DateTime dtLastStopTime = dt1970.AddSeconds(lastStopTime);


                    string strDtUppLastResp = (lastUppRespTime != 0) ? dtUppLastResp.ToString() : "undefined";
                    string strDtWlsLastResp = (lastWlsRespTime != 0) ? dtWlsLastResp.ToString() : "undefined";
                    string strDtPressureSensorLastResp = (lastPressureSensorRespTime != 0) ? dtPressureSensorResp.ToString() : "undefined";
                    DateTime date = DateTime.Now;
                    string switchUpp = (stateUppActive == 2) ? "left" : "right";
                    log($"UppNA: {UppNA}; WlsNA: {WlsNA}; Состояние реле:( in={inStatus} / out={outStatus}); error={errorControls}; event={eventControls}; Разрешение трансфера:{transferAllow}", level: 3);
                    log($"typeAndVersion={typeAndVersion}; размер config={configSize}; ", level: 3);
                    
                    string strUppState = UppState(uint16UppActive);

                    log($"УПП(0x{uint16UppActive:X}):{strUppState}; Время опроса ={strDtUppLastResp};", level: 1);
                    log($"lastStartTime:{dtLastStartTime}; lastStopTime:{dtLastStopTime}; currentPhaseMax:{currentPhaseMax}A", level: 1);
                    log($"Wls=0x{uint16WlsActive:X}; Высота={pressure:N2}м; Время опроса={strDtWlsLastResp}| Переключатель={switchUpp};", level: 1);
                    log($"motorCurrent:{motorCurrent}%; thermalLoad:{thermalLoad}%; frequency:{frequency}Hz; power:{power}; voltage:{voltage}%; startCount:{startCount}; runtimeHours:{runtimeHours}h", level: 3);
                    string curPhase123 = $"currentPhase1:{currentPhase1}A; currentPhase2:{currentPhase2}A; currentPhase3:{currentPhase3}A";
                    log($"{curPhase123}; modbusError:{modbusError}; modbusToglle:{modbusToglle}", level: 3);

                    if (!((pressure < 0.5) && (uint16WlsActive > 0)))
                    {
                        records.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", dtContollers, date));
                    }
                    else
                    {
                        log("-----height-----", level: 3);
                    }
                    records.Add(MakeCurrentRecord("wls", Convert.ToDouble(uint16WlsActive), strDtWlsLastResp, dtContollers, date));
                    records.Add(MakeCurrentRecord("upp", Convert.ToDouble(uint16UppActive), strUppState, dtContollers, date));
                    records.Add(MakeCurrentRecord("switch", Convert.ToDouble(stateUppActive), switchUpp, dtContollers, date));
                    records.Add(MakeCurrentRecord("motorCurrent", Convert.ToDouble(motorCurrent), "%", dtContollers, date));
                    records.Add(MakeCurrentRecord("startCount", Convert.ToDouble(startCount), "", dtContollers, date));
                    records.Add(MakeCurrentRecord("runtimeHours", Convert.ToDouble(runtimeHours), "h", dtContollers, date));
                    records.Add(MakeCurrentRecord("currentPhaseMax", Convert.ToDouble(currentPhaseMax), curPhase123, dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStartTime", 0, $"{dtLastStartTime}", dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStopTime", 0, $"{dtLastStopTime}", dtContollers, date));
                    setIndicationForRowCache(motorState(uint16UppActive), "motor", date);
                }
            }

            current.records = records;
            return current;
        }
        public dynamic SwitchCtrlMaster()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            List<byte> bytes= new List<byte>() { 0,0,0,0};
            dynamic result = Send(MakeBaseRequest(58, bytes));
            log($"switch={result.Body[0]} mode={result.Body[1]}", level: 1);
            
            return 0;
        }
        public dynamic WTA50CommandNew(dynamic flashver, string objectId, Guid idWls, float max, float min)//WaterTowerAutomation
        {
            List<byte> byteFor50Command = new List<byte>();
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;

            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray();
            byteFor50Command.AddRange(byteObjectId);
            DateTime now = DateTime.Now;
            DateTime dt1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0);

            byte[] byteTotalSecond = BitConverter.GetBytes((int)(now - dt1970).TotalSeconds);
            byteFor50Command.AddRange(byteTotalSecond);

            var dataWls = dataFromWLS(idWls);

            //var dataRowCache = dataFromRowCache(idWls);
            if (dataWls == null)
            {

                log(String.Format($"dataWls == null"), level: 1);
                byte byteWls = 0b0011;
                byte[] bytesHeight = BitConverter.GetBytes(min);
                byte[] byteLastQueryWls = BitConverter.GetBytes((int)(DateTime.Now - dt1970).TotalSeconds);
                if (min == 15 || min == 14)
                {
                    byteWls = 0b1111;
                }
                else if (min == 0 || min == 10)
                {
                    byteWls = 0;
                }
                log(String.Format($"byteWls = 0x{byteWls:X2} |height={min}"), level: 1);
                byteFor50Command.Add(0x02);
                byteFor50Command.Add(byteWls);
                byteFor50Command.AddRange(bytesHeight);
                byteFor50Command.AddRange(byteLastQueryWls);
            }
            else
            {
                log(string.Format("dataWls != null"), level: 3);
                byte[] byteLastQueryWls = BitConverter.GetBytes((int)(dataWls.date - dt1970).TotalSeconds);
                float dataWlsHeight = Convert.ToSingle(dataWls.height);
                byte byteWls = Convert.ToByte(dataWls.wls);
                if (byteWls == 0)
                {
                    dataWlsHeight = 10;
                }
                else if (byteWls == 1)
                {
                    dataWlsHeight = 11;
                }
                else if (byteWls == 0b0011)
                {
                    dataWlsHeight = 12;
                }
                else if (byteWls == 0b0111)
                {
                    dataWlsHeight = 13;
                }
                else if (byteWls == 0b1111)
                {
                    dataWlsHeight = 14;
                }
                else
                {
                    dataWlsHeight = 11;
                }

                if (byteWls >= 7)
                {
                    byteWls = 0b0111;
                }
                byte[] tt = BitConverter.GetBytes(dataWlsHeight);
                byteFor50Command.Add(0x02);
                byteFor50Command.Add(byteWls);
                byteFor50Command.AddRange(tt);
                log(String.Format($"byteWls = 0x{byteWls:X2} |height={dataWlsHeight}"), level: 1);
                byteFor50Command.AddRange(byteLastQueryWls);
            }

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();


            if ((ver == 1) && (typeDevice == 8))
            {

                dynamic result = Send(MakeBaseRequest(50, byteFor50Command));
                if (!result.success)
                {
                    log(string.Format("50 команда не введена: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x32)
                {
                    log(string.Format("Получен ответ {0} не на 50 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }

                if ((ver == 1) && (typeDevice == 8))
                {
                    UInt32 uInt32Time = (UInt32)(result.Body[3] << 24) | (UInt32)(result.Body[2] << 16) | (UInt32)(result.Body[1] << 8) | result.Body[0];
                    DateTime dtContollers = dt1970.AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);
                    
                    byte mcMode = (byte)result.Body[4];
                    byte reserved = (byte)result.Body[5];
                    
                    byte firstUppNA = (byte)result.Body[6];
                    byte secondUppNA = (byte)result.Body[7];

                    byte isUsedSecondUpp = (byte)result.Body[8];

                    byte WlsNA = (byte)result.Body[9];

                    byte inStatus = (byte)result.Body[10];
                    byte outStatus = (byte)result.Body[11];

                    UInt16 errorControls = Helper.ToUInt16(result.Body, 12);
                    UInt16 eventControls = Helper.ToUInt16(result.Body, 14);

                    byte transferAllow = (byte)result.Body[16];
                    byte typeAndVersion = (byte)result.Body[17];
                    //byte configSize = (byte)result.Body[14];

                    UInt32 lastFirstUppRespTime = (UInt32)(result.Body[22] << 24) | (UInt32)(result.Body[21] << 16) | (UInt32)(result.Body[20] << 8) | result.Body[19];
                    if (lastFirstUppRespTime == 0xFFFFFFFF) lastFirstUppRespTime = 0;
                    DateTime dtFirstUppLastResp = dt1970.AddSeconds(lastFirstUppRespTime);

                    //byte stateFirstUpp = (byte)result.Body[23];

                    UInt16 u16FirstUpp = Helper.ToUInt16(result.Body, 23);


                    UInt32 lastSecondUppRespTime = (UInt32)(result.Body[28] << 24) | (UInt32)(result.Body[27] << 16) | (UInt32)(result.Body[26] << 8) | result.Body[25];
                    if (lastSecondUppRespTime == 0xFFFFFFFF) lastSecondUppRespTime = 0;
                    DateTime dtSecondUppLastResp = dt1970.AddSeconds(lastSecondUppRespTime);

                    UInt16 u16SecondUpp = Helper.ToUInt16(result.Body, 29);
                    

                    UInt32 lastWlsRespTime = (UInt32)(result.Body[34] << 24) | (UInt32)(result.Body[33] << 16) | (UInt32)(result.Body[32] << 8) | result.Body[31];
                    if (lastWlsRespTime == 0xFFFFFFFF) lastWlsRespTime = 0;
                    DateTime dtWlsLastResp = dt1970.AddSeconds(lastWlsRespTime);

                    UInt16 uint16WlsActive = Helper.ToUInt16(result.Body, 35);

                    UInt32 lastPressureSensorRespTime = (UInt32)(result.Body[40] << 24) | (UInt32)(result.Body[39] << 16) | (UInt32)(result.Body[38] << 8) | result.Body[37];
                    if (lastPressureSensorRespTime == 0xFFFFFFFF) lastPressureSensorRespTime = 0;
                    DateTime dtPressureSensorResp = dt1970.AddSeconds(lastPressureSensorRespTime);
                    
                    float pressure = BitConverter.ToSingle(result.Body, 41);// в нашем случае давление == высота, тк коэфф = 1.03 
                    
                    UInt16 motorCurrent = Helper.ToUInt16(result.Body, 45);
                    UInt16 thermalLoad = Helper.ToUInt16(result.Body, 47);
                    UInt16 currentPhase1 = Helper.ToUInt16(result.Body, 49);
                    UInt16 currentPhase2 = Helper.ToUInt16(result.Body, 51);
                    UInt16 currentPhase3 = Helper.ToUInt16(result.Body, 53);
                    UInt16 currentPhaseMax = Helper.ToUInt16(result.Body, 55);
                    UInt16 frequency = Helper.ToUInt16(result.Body, 57);
                    int power = Helper.ToUInt16(result.Body, 59) / 100;
                    UInt16 voltage = Helper.ToUInt16(result.Body, 61);
                    int startCount = Helper.ToUInt16(result.Body, 63) * 100;
                    int runtimeHours = Helper.ToUInt16(result.Body, 65) * 10;
                    UInt16 modbusError = Helper.ToUInt16(result.Body, 67);
                    UInt16 modbusToglle = Helper.ToUInt16(result.Body, 69);

                    UInt32 lastStartTimeFirstUpp = (UInt32)(result.Body[74] << 24) | (UInt32)(result.Body[73] << 16) | (UInt32)(result.Body[72] << 8) | result.Body[71];
                    if (lastStartTimeFirstUpp == 0xFFFFFFFF) lastStartTimeFirstUpp = 0;
                    DateTime dtLastStartTimeFirstUpp = dt1970.AddSeconds(lastStartTimeFirstUpp);

                    UInt32 lastStopTimeFirstUpp = (UInt32)(result.Body[78] << 24) | (UInt32)(result.Body[77] << 16) | (UInt32)(result.Body[76] << 8) | result.Body[75];
                    if (lastStopTimeFirstUpp == 0xFFFFFFFF) lastStopTimeFirstUpp = 0;
                    DateTime dtLastStopTimeFirstUpp = dt1970.AddSeconds(lastStopTimeFirstUpp);


                    UInt32 lastStartTimeSecondUpp = (UInt32)(result.Body[82] << 24) | (UInt32)(result.Body[81] << 16) | (UInt32)(result.Body[80] << 8) | result.Body[79];
                    if (lastStartTimeSecondUpp == 0xFFFFFFFF) lastStartTimeSecondUpp = 0;
                    DateTime dtLastStartTimeSecondUpp = dt1970.AddSeconds(lastStartTimeSecondUpp);

                    UInt32 lastStopTimeSecondUpp = (UInt32)(result.Body[86] << 24) | (UInt32)(result.Body[85] << 16) | (UInt32)(result.Body[84] << 8) | result.Body[83];
                    if (lastStopTimeSecondUpp == 0xFFFFFFFF) lastStopTimeSecondUpp = 0;
                    DateTime dtLastStopTimeSecondUpp = dt1970.AddSeconds(lastStopTimeSecondUpp);
                    
                    string strDtFirstUppLastResp = (lastFirstUppRespTime != 0) ? dtFirstUppLastResp.ToString() : "undefined";
                    string strDtWlsLastResp = (lastWlsRespTime != 0) ? dtWlsLastResp.ToString() : "undefined";
                    string strDtPressureSensorLastResp = (lastPressureSensorRespTime != 0) ? dtPressureSensorResp.ToString() : "undefined";
                    DateTime date = DateTime.Now;
                    //string switchUpp = (stateFirstUpp == 2) ? "left" : "right";
                    log($"firstUppNA: {firstUppNA}; WlsNA: {WlsNA}; Состояние реле:( in={inStatus} / out={outStatus}); error={errorControls}; event={eventControls}; Разрешение трансфера:{transferAllow}", level: 3);
                    log($"typeAndVersion={typeAndVersion}", level: 3);

                    string strUppState = UppState(u16FirstUpp);

                    log($"УПП(0x{u16FirstUpp:X}):{strUppState}; Время опроса первого ={strDtFirstUppLastResp};", level: 1);
                    log($"lastStartTimeFirstUpp:{dtLastStartTimeFirstUpp}; lastStopTimeFirstUpp:{dtLastStopTimeFirstUpp}; currentPhaseMax:{currentPhaseMax}A", level: 1);
                    log($"Wls=0x{uint16WlsActive:X}; Высота={pressure:N2}м; Время опроса={strDtWlsLastResp}", level: 1);
                    log($"motorCurrent:{motorCurrent}%; thermalLoad:{thermalLoad}%; frequency:{frequency}Hz; power:{power}; voltage:{voltage}%; startCount:{startCount}; runtimeHours:{runtimeHours}h", level: 3);
                    string curPhase123 = $"currentPhase1:{currentPhase1}A; currentPhase2:{currentPhase2}A; currentPhase3:{currentPhase3}A";
                    log($"{curPhase123}; modbusError:{modbusError}; modbusToglle:{modbusToglle}", level: 3);
                    
                    if (!((pressure < 0.5) && (uint16WlsActive > 0)))
                    {
                        records.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", dtContollers, date));
                    }
                    else
                    {
                        log("-----height-----", level: 3);
                    }
                    records.Add(MakeCurrentRecord("wls", Convert.ToDouble(uint16WlsActive), strDtWlsLastResp, dtContollers, date));
                    records.Add(MakeCurrentRecord("upp", Convert.ToDouble(u16FirstUpp), strUppState, dtContollers, date));
                    //records.Add(MakeCurrentRecord("switch", Convert.ToDouble(stateFirstUpp), switchUpp, dtContollers, date));
                    records.Add(MakeCurrentRecord("motorCurrent", Convert.ToDouble(motorCurrent), "%", dtContollers, date));
                    records.Add(MakeCurrentRecord("startCount", Convert.ToDouble(startCount), "", dtContollers, date));
                    records.Add(MakeCurrentRecord("runtimeHours", Convert.ToDouble(runtimeHours), "h", dtContollers, date));
                    records.Add(MakeCurrentRecord("currentPhaseMax", Convert.ToDouble(currentPhaseMax), curPhase123, dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStartTime", 0, $"{dtLastStartTimeFirstUpp}", dtContollers, date));
                    records.Add(MakeCurrentRecord("lastStopTime", 0, $"{dtLastStopTimeFirstUpp}", dtContollers, date));
                    setIndicationForRowCache(motorState(u16FirstUpp), "motor", date);
                }
            }

            current.records = records;
            return current;
        }

        public dynamic WaterTowerAutomation51Command(dynamic flashver, string objectId)
        {
            List<byte> byteFor51Command = new List<byte>();
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();
            
            if ((ver == 1) && (typeDevice == 8))
            {

                dynamic result = Send(MakeBaseRequest(51, byteFor51Command));
                if (!result.success)
                {
                    log(string.Format("51 команда не введена: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x33)
                {
                    log(string.Format("Получен ответ {0} не на 51 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                DateTime dtContollers = DateTime.Now;

                if ((ver == 1) && (typeDevice == 8))
                {
                   
                }
            }

            current.records = records;
            return current;
        }
        #endregion

        public dynamic ControlUpp(dynamic flashver, string objectId)
        {
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            var ver = (int)flashver.ver;
            var typeDevice = (int)flashver.devid;
            return 0;
        }

    }
}

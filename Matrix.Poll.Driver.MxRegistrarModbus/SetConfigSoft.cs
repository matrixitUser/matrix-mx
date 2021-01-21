using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{

    public partial class Driver
    {
        public const string msgCorrectTime = "Корректировка времени";
        public const string msgSetTime = "Установка времени";
        public const int TimeDayForSet = 30; 
        public const int TimeSecondForSet = 300; // 5 minute

        #region ConfigSoft 
        public dynamic lightControlSetSoftConfig(byte onoff, byte u8ControlMode, dynamic flashver, string objectId)
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
            
            dynamic current = SoftConfig(byteLight, flashver, objectId);
            return current;
        }
        public dynamic SoftConfig(List<byte> byteSoftConfig, dynamic flashver, string objectId)
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

            byte[] byteObjectId = Guid.Parse(objectId).ToByteArray();
            byteSoftConfig.AddRange(byteObjectId);

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            var records = new List<dynamic>();
            dynamic lightControl = new ExpandoObject();

            int GetLightMK =0,GetLightReal=0,GetPhotoSensorState=0,GetControlMetod=0;
            
            if ( ((ver == 6) && (typeDevice == 14))  || ((ver == 2) && (typeDevice == 6)))
            {
                dynamic result = Send(MakeLightRequest((Int32)0x0100, (ushort)byteSoftConfig.Count, byteSoftConfig));
                if (!result.success)
                {
                    log(string.Format("ConfigSoft не введён: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x49)
                {
                    log(string.Format("Получен ответ {0} не на 73 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                UInt32 uInt32Time;
                DateTime dtContollers = DateTime.Now;

                if ((ver == 6) && (typeDevice == 14)) 
                {
                    uInt32Time = (UInt32)(result.Body[7] << 24) | (UInt32)(result.Body[6] << 16) | (UInt32)(result.Body[5] << 8) | result.Body[4];
                    dtContollers = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);

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


                    GetLightMK = (int)result.Body[0];
                    GetLightReal = (int)result.Body[1];
                    GetPhotoSensorState = (int)result.Body[2];
                    GetControlMetod = (int)result.Body[3];

                    DateTime date = DateTime.Now;
                    log(string.Format("Фотодатчик: {0}; Выход контроллера: {1}; Состояние контактора: {2}", ((GetPhotoSensorState == 1) ? "включен" : "выключен"), ((GetLightMK == 1) ? "включен" : "выключен"), ((GetLightReal == 1) ? "включено" : "выключено")), level: 1);
                    log(string.Format("Метод управления: {0} ({1})", controlMetodName(result.Body[3]), GetControlMetod), level: 1);


                    records.Add(MakeCurrentRecord("GetLightMK", GetLightMK, "", dtContollers, date));
                    records.Add(MakeCurrentRecord("GetLightReal", GetLightReal, "", dtContollers, date));
                    records.Add(MakeCurrentRecord("GetPhotoSensorState", GetPhotoSensorState, "", dtContollers, date));
                    records.Add(MakeCurrentRecord("GetControlMetod", GetControlMetod, "", dtContollers, date));
                    
                    lightControl.controllerData = $"lightMK:{GetLightMK};Real:{GetLightReal};PSS:{GetPhotoSensorState};CMetod:{GetControlMetod};dt:{dtContollers}T|";
                    setModbusControl(lightControl);
                }

                if ((typeDevice == 6) && (ver == 2))
                {
                    uInt32Time = (UInt32)(result.Body[4] << 24) | (UInt32)(result.Body[3] << 16) | (UInt32)(result.Body[2] << 8) | result.Body[1];
                    dtContollers = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(uInt32Time);
                    log(string.Format("Время на контроллере: {0}", dtContollers), level: 1);

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

                    log(string.Format("ВХОДА: Состояния контакторов 1: {0}, 2:{1},3:{2}, 4:{3}; Фотодатчик: {4}; ",
                                        (result.Body[5] == 1) ? "включено" : "выключено",
                                        (result.Body[6] == 1) ? "включено" : "выключено",
                                        (result.Body[7] == 1) ? "включено" : "выключено",
                                        (result.Body[8] == 1) ? "включено" : "выключено",
                                        (result.Body[6] == 1) ? "включено" : "выключено"
                                        ), level: 1);
                    
                    log(string.Format("ВЫХОДА контроллера: Управления освещением 1:{0}, 2:{1}, 3:{2}, 4:{3}; Статус:{4:X} ",
                                        (result.Body[13] == 1) ? "включено" : "выключено",
                                        (result.Body[14] == 1) ? "включено" : "выключено",
                                        (result.Body[15] == 1) ? "включено" : "выключено",
                                        (result.Body[16] == 1) ? "включено" : "выключено",
                                        result.Body[0]  
                                       ),level: 1);
                    
                    log(string.Format("Методы управления по каналам 1:{0}, 2:{1}, 3:{2}, 4:{3}", controlMetodName(result.Body[9]), controlMetodName(result.Body[10]), controlMetodName(result.Body[11]), controlMetodName(result.Body[12])), level: 1);

                    string textLightMK = $"lightMK:{result.Body[13]}:{result.Body[14]}:{result.Body[15]}:{result.Body[16]}";
                    string textLightReal = $"Real:{result.Body[5]}:{result.Body[6]}:{result.Body[7]}:{result.Body[8]}";
                    string textPhotoSensorState = $"PSS:{result.Body[6]}";
                    string textControlMetod = $"CMetod:{result.Body[9]}:{result.Body[10]}:{result.Body[11]}:{result.Body[12]}";
                    
                    lightControl.controllerData = $"{textLightMK};{textLightReal};{textPhotoSensorState};{textControlMetod};dt:{dtContollers}T|";

                    setModbusControl(lightControl);
                }
                
            }
            current.records = records;
            return current;
        }

        private string controlMetodName(byte controlMetod)
        {
            switch (controlMetod)
            {
                case 0:
                    return "По расписанию";
                case 1:
                    return  "По фотодачику";
                case 2:
                    return "Ручное управление";
                case 3:
                    return "Астрономический таймер контроллера";
                case 18:
                    return "Ручное управление(hard)";
                case 16:
                    return "Астрон.таймер + расписание";
                default:
                    return "************";
            }

        }


        #endregion

        #region Set Asron Timer
        public dynamic SetAstronTimer(List<byte> byteAstronConfig, dynamic flashver)
        {
            if (flashver == null)
            {
                flashver = GetFlashVer();
            }

            if (!flashver.success)
            {
                return MakeResult(101, flashver.errorcode, flashver.error);
            }
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var devid = (UInt16)flashver.devid;
            var device = (string)flashver.device;
            var ver = (int)flashver.ver;
            if(((devid == 6 && ver == 2)|| (devid == 14 && ver == 6)) && byteAstronConfig.Count < 12)
            {
                var result = Send(MakeAstronRequest((Int32)0x0100, (ushort)byteAstronConfig.Count, byteAstronConfig));
                if (result.Function != 0x4A)
                {
                    log(string.Format("Получен ответ {0} не на 74 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                string strTmp="";
                if (!result.success)
                {
                    log(string.Format("AstronTimer не введён: {0}", result.error), level: 1);
                }
                if (devid == 14 && ver == 6)
                {
                    strTmp = (result.Body.Length <= 4) ? string.Format("Время восхода: {0}:{1, 1:00} заката: {2}:{3, 1:00}", result.Body[0], result.Body[1], result.Body[2], result.Body[3]) :
                    string.Format("Время восхода: {0}:{1, 1:00} выключения: {4}:{5, 1:00}. Время заката: {2}:{3, 1:00} включения: {6}:{7, 1:00}", result.Body[0], result.Body[1], result.Body[2], result.Body[3], result.Body[6], result.Body[7], result.Body[8], result.Body[9]);
                    log(strTmp, level: 1);
                }
                if (devid == 6 && ver == 2) 
                {
                    float on, off, RiseOffset, SetOfFset;
                    float sunRise = BitConverter.ToSingle(result.Body, 4);    
                    float sunSet = BitConverter.ToSingle(result.Body, 8);
                    log(string.Format("Время заката: {0:00}:{1:00}; Время восхода: {2:00}:{3:00}", (int)sunSet, (int)((sunSet % 1)*60), (int)sunRise, (int)((sunRise % 1) * 60)), level: 1);

                    RiseOffset = result.Body[12];SetOfFset =  result.Body[13];
                    on = BitConverter.ToSingle(result.Body, 14);  off = BitConverter.ToSingle(result.Body, 18);
                    log(string.Format("Канал 1=> Включение:{0:00}:{1:00}, Выключение:{2:00}:{3:00}; Задержка вкл.:{4:00}мин; Опережение выкл.:{5:00}мин ", (int)off, (int)((off % 1) * 60), (int)on, (int)((on % 1) * 60), SetOfFset, RiseOffset), level: 1);

                    RiseOffset = result.Body[22];SetOfFset = result.Body[23];
                    on = BitConverter.ToSingle(result.Body, 24); off = BitConverter.ToSingle(result.Body, 28);
                    log(string.Format("Канал 2=> Включение:{0:00}:{1:00}, Выключение:{2:00}:{3:00}; Задержка вкл.:{4:00}мин; Опережение выкл.:{5:00}мин ", (int)off, (int)((off % 1) * 60), (int)on, (int)((on % 1) * 60), SetOfFset, RiseOffset), level: 1);

                    RiseOffset = result.Body[32];SetOfFset = result.Body[33];
                    on = BitConverter.ToSingle(result.Body, 34); off = BitConverter.ToSingle(result.Body, 38);
                    log(string.Format("Канал 3=> Включение:{0:00}:{1:00}, Выключение:{2:00}:{3:00}; Задержка вкл.:{4:00}мин; Опережение выкл.:{5:00}мин ", (int)off, (int)((off % 1) * 60), (int)on, (int)((on % 1) * 60), SetOfFset, RiseOffset), level: 1);

                    RiseOffset = result.Body[42];SetOfFset = result.Body[43];
                    on = BitConverter.ToSingle(result.Body, 44); off = BitConverter.ToSingle(result.Body, 48);
                    log(string.Format("Канал 4=> Включение:{0:00}:{1:00}, Выключение:{2:00}:{3:00}; Задержка вкл.:{4:00}мин; Опережение выкл.:{5:00}мин ", (int)off, (int)((off % 1) * 60), (int)on, (int)((on % 1) * 60), SetOfFset, RiseOffset), level: 1);
                }

            }
            if (devid == 6 && ver == 2 && byteAstronConfig.Count > 12)
            {
                var result = Send(MakeBaseRequest(97, byteAstronConfig));
                if (!result.success)
                {
                    log(string.Format("Настройки по команде 97 не записаны: {0}", result.error), level: 1);
                }
                List<byte> tmpBytes = new List<byte>() { 0x00, 0x00, 0x00 };

                var result1 = Send(MakeBaseRequest(96, tmpBytes));
                if (!result1.success)
                {
                    log(string.Format("Настройки по команде 96 не записаны: {0}", result1.error), level: 1);
                }
                if (result1.Function != 0x60)
                {
                    log(string.Format("Получен ответ {0} не на 96 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                dynamic control = new ExpandoObject();
                control.lightV2Config = BitConverter.ToString(result1.Body);
                setModbusControl(control);

                log(string.Format("update", result.error), level: 1);
            }
            return current;
        }

        #endregion
        
        public List<byte> lightSetSoftConfig(string[] arrString, UInt16 timeOut, int ver, int typeDevice)
        {
            List<byte> listSetConfig = new List<byte>();
            byte onoff = 0b00;
            if ((typeDevice == 6) && (ver == 2))
            {
                byte channel = Convert.ToByte(arrString[1].Substring(arrString[1].Length - 1));
                if (arrString[1].Contains("off")) onoff = 0b01;
                if (arrString[1].Contains("on")) onoff = 0b10;
                onoff = (byte)(onoff << (2 * (channel - 1)));
            }
            else if ((typeDevice == 14) && (ver == 6))  //версия  1.2 прошивки первого контроллера управления освещением
            {
                if (arrString[1].Contains("off")) onoff = 0;
                else if (arrString[1].Contains("on")) onoff = 1;
                else onoff = 0xFF;
            }
            listSetConfig.Add(onoff);
            //UInt16 a = 1800;
            byte[] u16TimeOut = BitConverter.GetBytes(timeOut);   //0x08 0x07
            listSetConfig.AddRange(u16TimeOut);

            byte u8ControlMode = ControlMetodConvertStateFromName(arrString[2]);
            //0- по расписанию; 1- фотодачик; 2- команда сверху(ручное управление); 3- астрономический таймер контроллера ; 0x12 - ручное управление без контроля связи 0х10 - Астрон.таймер+расписание
            listSetConfig.Add(u8ControlMode);

            listSetConfig.AddRange(ParsingSheduleToUInt32Soft(arrString[3]));
            return listSetConfig;
        }
        byte[] getBytes(tsConfig str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        tsConfig setBytes(byte[] arr)
        {
            tsConfig str = new tsConfig();

            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(arr, 0, ptr, size);

            str = (tsConfig)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);

            return str;
        }
        public byte ControlMetodConvertStateFromName(string name)  //способ управления конвертер name -> state  (control metod == metod of manegement)
        {
            byte state = 0xFF;
            switch (name)
            {
                case "0":
                case "По расписанию":
                    state = 0x00;
                    break;
                case "1":
                case "По фотодатчику":
                    state = 0x01;
                    break;
                case "2":
                case "Ручное управление":
                    state = 0x02;
                    break;
                case "3":
                case "Астрономический таймер контроллера":
                    state = 0x03;
                    break;
                case "12":
                case "18":
                case "Ручное(hard) управление":
                case "Ручное управление(hard)":
                    state = 0x12;
                    break;
                case "Астрономический таймер сервера":
                    state = 0x02;
                    break;
                case "Астрон.таймер+расписание":
                    state = 0x10;
                    break;
                default:
                    state = 0xFF;
                    break;
            }
            return state;
        }
        public List<byte> ParsingSheduleToUInt32Soft(string sheduleWithoutParsing)
        {
            List<byte> byteShedule = new List<byte>();
            byte[] result = new byte[4];
            UInt32[] uint32Shedule = new UInt32[8];
            byte[] u8Action = new byte[8];
            string[,] sheduleAction = new string[8, 2];
            if (sheduleWithoutParsing != null)
            {
                if (sheduleWithoutParsing.Contains("|") && sheduleWithoutParsing != "")
                {
                    sheduleWithoutParsing = sheduleWithoutParsing.Remove(sheduleWithoutParsing.Length - 1, 1);
                    string[] shedule = sheduleWithoutParsing.Split('|');
                    for (int i = 0; i < shedule.Length; i++)
                    {
                        sheduleAction[i, 0] = shedule[i].Split(';')[0];
                        sheduleAction[i, 1] = shedule[i].Split(';')[1];
                    }
                    
                }
            }

            for (int i = 0; i < 2; i++)
            {
                uint32Shedule[i] = (sheduleAction[i, 0] == null || sheduleAction[i, 0] == "") ? 0xFFFFFFFF : Convert.ToUInt32(sheduleAction[i, 0]);
                result = BitConverter.GetBytes(uint32Shedule[i]);
                byteShedule.AddRange(result);//
                u8Action[i] = 0xFF;
                if (sheduleAction[i, 1] == "1") u8Action[i] = 0x01;
                if (sheduleAction[i, 1] == "0") u8Action[i] = 0x00;

                //result = BitConverter.GetBytes(u8Action[i]);
                byteShedule.Add(u8Action[i]);//

            }

            return byteShedule;
        }
    }
    public struct tsUartConfig
    {
        public UInt32 u32BaudRate;
        public byte u8WordLen;
        public byte u8StopBits;
        public byte u8Parity;
        public byte reserved;
    }

    public struct tsLigthtChannel
    {
        public byte u8ControlMode;
        public byte u8beforeSunRise;
        public byte u8afterSunSet;
        public byte reserved;
        public UInt32 on1;
        public UInt32 off1;
        public UInt32 on2;
        public UInt32 off2;
    }

    public struct tsConfig
    {
        public UInt16 u16FlashVer;
        public byte u8NetworkAddress;
        public byte u8Mode;

        public tsUartConfig sUart1;
        public tsUartConfig sUart2;
        public tsUartConfig sUart3;

        public UInt32 u32ReleaseTs;

        public UInt16 u16TimeOut;
        public byte u8IsRtcError;
        public byte u8timeDiff;//+

        public UInt32 u32lat;//+
        public UInt32 u32lon; //+

        public tsLigthtChannel ligthtChannels1;//+
        public tsLigthtChannel ligthtChannels2;//+
        public tsLigthtChannel ligthtChannels3;//+
        public tsLigthtChannel ligthtChannels4;//+

        public byte u8hardware;
        public byte u8reserved;
        public UInt16 u16reserved;
    }
}

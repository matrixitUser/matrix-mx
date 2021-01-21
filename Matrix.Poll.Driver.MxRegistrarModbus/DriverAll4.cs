using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        public struct ArchiveCounter
        {
            public UInt32 Value;
            public string Param;
            public SByte Point;
            public Byte Unit;
            public bool PinState;
            public bool PinMagState;
            public bool IsEnabled;
            public bool IsError;
        }

        public struct ArchiveRecord
        {
            public UInt32 Ts;
            public ArchiveCounter[] Counter;
        }

        public class MessageInput
        {
            public bool success { get; set; }
            public UInt16 pinState0 { get; set; }
            public UInt16 pinState1 { get; set; }
            public double temp { get; set; }
            public double volt { get; set; }//
            public bool[] pin { get; set; }// = new bool[32];
            public byte rtcCheck { get; set; }// = 0xFF;//16[] bkp = new UInt16[10];
            public UInt16 bkpFlags { get; set; }// = 0;
            public List<string> units { get; set; }
        }

        public class CntConfig
        {
            public bool IsEnabled;
            public bool IsError;
            public UInt16 KDiv;
            public string Param;
            public sbyte point;
            public byte unit;
            public byte filterPDiv;
        }

        public class MessageConfig
        {
            public bool success { get; set; }
            public UInt16 flashVer { get; set; }
            public byte NA { get; set; }
            public byte mode { get; set; }
            public UInt32 uart1Baud { get; set; }
            public byte uart1Wl { get; set; }
            public byte uart1Par { get; set; }
            public byte uart1Sb { get; set; }
            public CntConfig[] cntConfig { get; set; }
            public UInt16 filterPeriod;
            public bool isRtcError { get; set; }
        }

        public class MessageConfig2
        {
            public bool success { get; set; }
            public UInt32 releaseTs { get; set; }
            public DateTime releaseDt { get; set; }
            public UInt32[] cntReleaseTs { get; set; }
            public DateTime[] cntReleaseDt { get; set; }
        }
        
        private dynamic All4(string components, dynamic flashver, string cmd, byte[] password, string objectId, Guid idWls, float max, float min)//, Dictionary<int, Parameter> parameterConfiguration, DateTime startDate, bool isRtcEnabled, DateTime rtcResetDate
        {
            byte counters = 16;
            byte digitals = 16;
            var devid = (UInt16)flashver.devid;
            var device = (string)flashver.device;
            var ver = (int)flashver.ver;
            //приборное время

          
            DateTime date;
            if (components.Contains("Constant") && ver == 2 && devid == 6)
            {
                List<byte> tmpBytes = new List<byte>() { 0x00, 0x00, 0x00 };

                var result1 = Send(MakeBaseRequest(96, tmpBytes));
                if (!result1.success)
                {
                    log(string.Format("Config (96 команда) не введён: {0}", result1.error), level: 1);
                }
                if (result1.Function != 0x60)
                {
                    log(string.Format("Получен ответ {0} не на 96 команду ", result1.Function), level: 1);
                }
                else
                {
                    dynamic control = new ExpandoObject();
                    control.lightV2Config = BitConverter.ToString(result1.Body);
                    setModbusControl(control);
                    log(string.Format("update"), level: 1);
                }
            }
            if (components.Contains("Constant") && ver == 1 && devid == 9)
            {
                List<byte> tmpBytes = new List<byte>() { 0x00, 0x00};

                var result1 = Send(MakeBaseRequest(79, tmpBytes));
                if (!result1.success)
                {
                    log(string.Format("Config (79 команда) не введён: {0}", result1.error), level: 1);
                }
                if (result1.Function != 0x4F)
                {
                    log(string.Format("Получен ответ {0} не на 79 команду ", result1.Function), level: 1);
                }
                else
                {
                    if(result1.Body[0] == 0x00)
                    {
                        log(string.Format("Контроллер включен"), level: 1);
                    }
                    else if (result1.Body[0] == 0x01)
                    {
                        log(string.Format("Контроллер выключен"), level: 1);
                    }
                }
            }
            if (components.Contains("Constant") && ver == 1 && devid == 8)
            {
                //dynamic current = WTAGetConfig();// (flashver, objectId, idWls, max, min);
                dynamic current = SwitchCtrlMaster();// (flashver, objectId, idWls, max, min);
            }
            else if ((devid == 8) && (ver == 1))
            {
                //dynamic current = WTA50CommandNew(flashver, objectId, idWls, max, min);
                
                dynamic current = WTA50Command(flashver, objectId, idWls, max, min);

                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }
                records(current.records); //запись в базу данных
            }
            if ( ((devid == 14) && (ver == 6)) || ((devid == 6) && (ver == 2)) )
            {

                dynamic current = lightControlSetSoftConfig(0xFF, 0xFF, flashver, objectId);
            
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }
                records(current.records); //запись в базу данных
                if (((devid == 14) && (ver == 6)) || ((devid == 6) && (ver == 2)))
                {
                    List<byte> byteLight1 = new List<byte>();
                    for(int i = 0; i < 11; i++)
                        byteLight1.Add(0xFF);
                    dynamic currentAstron = SetAstronTimer(byteLight1, flashver);
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }

                }
                records(current.records); //запись в базу данных
           
                /* //не используем
                if (components.Contains("Current"))
                {
                    dynamic current = new ExpandoObject();
                    current = GetCurrent4_7(DateTime.Now);
                    
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                }*/
            }
            if (ver < 6 && (devid != 8))  //TODO
            {
                {
                    var time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                    if (!time.success)
                    {
                        return MakeResult(101, time.errorcode, time.error);
                    }

                    date = time.date;
                    setTimeDifference(DateTime.Now - time.date);
                }
                ////

                if (getEndDate == null)
                {
                    getEndDate = (type) => date;
                }

                //

                MessageInput mi = null;
                MessageConfig mc = null;
                MessageConfig2 mc2 = null;
                UInt32[] chipid = null;
                //
                #region Коррекция времени прибора
                if ((isRtcEnabled) && (cmd.Contains("correcttime")))
                {
                    var time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                    if (!time.success)
                    {
                        return MakeResult(101, time.errorcode, time.error);
                    }

                    date = time.date;

                    DateTime now = DateTime.Now;
                    var timeOffset = ((date > now) ? (date - now).TotalSeconds : (now - date).TotalSeconds);
                    bool isSetTime = timeOffset > 5;
                    // коррекция времени (елси отличается больше, чем на 5 секунд и если время опроса соответствует HH:04-HH:56)
                    if (isSetTime)
                    {
                        var bkp = Send(MakeWriteBkpRequest(DateTime.Now, devid));
                        if (bkp.success)
                        {
                            time = ParseTimeResponse(Send(MakeTimeRequest(devid)));
                            if (!time.success) return time;
                            //log(string.Format("время на счётчике {0} на сервере {1}", time.date, DateTime.Now), level: 1);
                        }
                        var timeOffsetNew = ((time.date > DateTime.Now) ? (time.date - DateTime.Now).TotalSeconds : (DateTime.Now - time.date).TotalSeconds);
                        if (bkp.success && time.success && (timeOffsetNew < 5))
                        {
                            date = time.date;
                            log(string.Format(isSetTime ? "Время установлено" : "Произведена корректировка времени на {0:0.###} секунд", timeOffset), level: 1);
                        }
                        else
                        {
                            log(string.Format("Время НЕ {0}: {1}", isSetTime ? "установлено" : "скорректировано", bkp.success ? time.error : bkp.error), level: 1);
                        }
                    }
                    else
                    {
                        log(string.Format("Корректировка времени не требуется"), level: 1);
                    }

                }

                #endregion
                {
                    var input = ParseRegisterResponse(Send(MakeRegisterRequest(0x45000, 0x80)));
                    if (!input.success) return input;

                    mi = new MessageInput();
                    mi.success = true;
                    mi.pinState0 = Helper.ToUInt16(input.Register, 0);//  BitConverter.ToUInt16(rcv.Skip(3 + 0).Take(2).Reverse().ToArray(), 0);
                    mi.pinState1 = Helper.ToUInt16(input.Register, 2);// BitConverter.ToUInt16(rcv.Skip(3 + 2).Take(2).Reverse().ToArray(), 0);
                    mi.temp = (double)Helper.ToInt16(input.Register, 4) / 100.0;// //BitConverter.ToInt16(rcv.Skip(3 + 4).Take(2).Reverse().ToArray(), 0) / 100;
                    mi.volt = (double)Helper.ToUInt16(input.Register, 6) / 1000.0;// BitConverter.ToUInt16(rcv.Skip(3 + 6).Take(2).Reverse().ToArray(), 0) / 1000;
                    mi.pin = new bool[32];
                    for (int i = 0; i < 16; i++)
                    {
                        mi.pin[i] = (mi.pinState0 & (1 << i)) > 0;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        mi.pin[i + 16] = (mi.pinState1 & (1 << i)) > 0;
                    }
                    //for (int i = 0; i < 10; i++)
                    //{
                    //    bkp[i] = BitConverter.ToUInt16(rcv, 3 + 16 + i * 2);
                    //}
                    mi.rtcCheck = input.Register[16];
                    mi.bkpFlags = Helper.ToUInt16(input.Register, 16 + 6);

                    byte unitsTotal = input.Register[0x1E];
                    byte unitsMaxlen = input.Register[0x1F];
                    List<string> units = new List<string>();
                    for (int i = 0x20; i < input.Register.Length; i += unitsMaxlen)
                    {
                        string unit = Encoding.UTF8.GetString(input.Register, i, unitsMaxlen).TrimEnd(new char[] { '\0' });
                        units.Add(unit);
                    }
                    mi.units = units;
                }
                {
                    var input = ParseRegisterResponse(Send(MakeRegisterRequest(0x30000, 0xA0)));
                    if (!input.success) return input;

                    mc = new MessageConfig();
                    mc.success = true;
                    mc.flashVer = BitConverter.ToUInt16(input.Register, 0x00);
                    mc.NA = input.Register[0x02];
                    mc.mode = input.Register[0x03];
                    mc.uart1Baud = BitConverter.ToUInt32(input.Register, 0x04);
                    mc.uart1Wl = input.Register[0x08];
                    mc.uart1Sb = input.Register[0x09];
                    mc.uart1Par = input.Register[0x0A];

                    List<CntConfig> countersCfg = new List<CntConfig>();
                    for (int i = 0; i < 16; i++)
                    {
                        int offset = i * 8;
                        CntConfig cnt = new CntConfig();
                        cnt.IsEnabled = input.Register[0x0C + offset] > 0;
                        cnt.IsError = input.Register[0x0D + offset] > 0;
                        UInt16 par = BitConverter.ToUInt16(input.Register, 0x0E + offset);
                        cnt.Param = GetParameterName4(par, i + 1);
                        cnt.KDiv = BitConverter.ToUInt16(input.Register, 0x10 + offset);
                        cnt.point = (sbyte)input.Register[0x12 + offset];
                        cnt.unit = input.Register[0x13 + offset];
                        cnt.filterPDiv = input.Register[0x8C + i];
                        //cnt.releaseTs = BitConverter.ToUInt32(body, 0x84 + i * 4);
                        countersCfg.Add(cnt);
                    }
                    mc.cntConfig = countersCfg.ToArray();
                    mc.filterPeriod = BitConverter.ToUInt16(input.Register, 0x9C);
                    mc.isRtcError = input.Register[0x9E] > 0;
                }
                {
                    var input = ParseRegisterResponse(Send(MakeRegisterRequest(0x300A0, 0x50)));
                    if (!input.success) return input;

                    mc2 = new MessageConfig2();
                    mc2.success = true;
                    mc2.releaseTs = BitConverter.ToUInt32(input.Register, 0);
                    mc2.releaseDt = (mc2.releaseTs == 0xFFFFFFFF || mc2.releaseTs == 0x00000000) ? DateTime.MinValue : new DateTime(1970, 1, 1).AddSeconds(mc2.releaseTs);
                    List<UInt32> cntReleaseTs = new List<UInt32>();
                    for (int i = 0; i < 16; i++)
                    {
                        cntReleaseTs.Add(BitConverter.ToUInt32(input.Register, 4 + i * 4));
                    }
                    mc2.cntReleaseTs = cntReleaseTs.ToArray();
                    mc2.cntReleaseDt = cntReleaseTs.Select(ts => (ts == 0xFFFFFFFF || ts == 0x00000000) ? DateTime.MinValue : new DateTime(1970, 1, 1).AddSeconds(ts)).ToArray();
                }

                {
                    var input = ParseRegisterResponse(Send(MakeRegisterRequest(0x46000, 0x0C)));
                    if (!input.success) return input;

                    chipid = new UInt32[3];
                    chipid[0] = BitConverter.ToUInt32(input.Register, 0);
                    chipid[1] = BitConverter.ToUInt32(input.Register, 4);
                    chipid[2] = BitConverter.ToUInt32(input.Register, 8);
                }

                DateTime startDate = mc2.releaseDt;
                //

                if (components.Contains("Current"))
                {
                    dynamic current = new ExpandoObject();
                    
                    current = GetCurrent4(date, mi);
                    
                    if (!current.success)
                    {
                        log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                        return MakeResult(102, current.errorcode, current.error);
                    }
                    records(current.records); //запись в базу данных
                    List<dynamic> currents = current.records;
                    
                    log(string.Format("Текущие на {0} прочитаны: {1}; {2}; {3}",
                        current.date,
                        current.values != "" ? string.Format("показания - {0}", current.values) : string.Format("количество импульсов - {0}", current.counters),
                        current.inputs,
                        current.adc != "" ? string.Format("значения АЦП - {0}", current.adc) : ""), level: 1);
                }

                //////

                if (components.Contains("Constant"))
                {
                   
                    var constant = GetConstant4(date, mc, mc2, chipid, device, ver);
                    if (!constant.success)
                    {
                        log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                        return MakeResult(103, constant.errorcode, constant.error);
                    }

                    records(constant.records);
                    List<dynamic> constants = constant.records;
                    log(string.Format("Константы прочитаны: всего {0}; {1}", constants.Count, constant.text), level: 1);
                }


                //////чтение часовых
                if (components.Contains("Hour"))
                {
                    var endH = getEndDate("Hour");
                    var startH = getStartDate("Hour");

                    if (DateTime.Compare(endH, startDate) < 0)
                    {
                        log(string.Format("Внимание: дата пусконаладки установлена {0:dd.MM.yyyy}, часовые за период {1:dd.MM.yyyy HH:mm}-{2:dd.MM.yyyy HH:mm} опрошены не будут", startDate, startH, endH), level: 1);
                    }
                    else
                    {
                        if (DateTime.Compare(startH, startDate) < 0)
                        {
                            startH = startDate.Date.AddHours(startDate.Hour + 1);
                            log(string.Format("Внимание: дата пусконаладки установлена {0:dd.MM.yyyy}, новый период опроса часовых {1:dd.MM.yyyy HH:mm}-{2:dd.MM.yyyy HH:mm}", startDate, startH, endH), level: 1);
                        }

                        var hours = new List<dynamic>();

                        var hour = GetHours4(startH, endH, date, counters, devid, mc, mc2, mi);
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                        }
                        else
                        {
                            hours = hour.records;
                            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
                        }
                    }
                }
                if (components.Contains("Day"))
                {
                    dynamic day = new ExpandoObject();

                    day = GetDay(date, mi);

                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании текущих: {0}", day.error), level: 1);
                        return MakeResult(102, day.errorcode, day.error);
                    }
                    records(day.records); //запись в базу данных

                    log(string.Format("Суточные на {0} прочитаны: {1}; {2}; {3}",
                        day.date,
                        day.values != "" ? string.Format("показания - {0}", day.values) : string.Format("количество импульсов - {0}", day.counters),
                        day.inputs,
                        day.adc != "" ? string.Format("значения АЦП - {0}", day.adc) : ""), level: 1);
                }
            }

          

            //if (components.Contains("Abnormal"))
            //{
            //    var startAe = DateTime.Compare(getStartDate("Abnormal"), startDate) > 0 ? getStartDate("Abnormal") : startDate;
            //    var endAe = getEndDate("Abnormal");
            //    var abnormal = GetAbnormals(10, startAe);//startAbnormal, endAbnormal);
            //    if (!abnormal.success)
            //    {
            //        log(string.Format("ошибка при считывании НС: {0}", abnormal.error), level: 1);
            //        return MakeResult(106, abnormal.errorcode, abnormal.error);
            //    }
            //}

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }



        dynamic GetCurrent4(DateTime date, MessageInput mi)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;
            DateTime dateNow = DateTime.Now;
            Dictionary<int, Parameter> parameterConfiguration = new Dictionary<int, Parameter>();
            var records = new List<dynamic>();
            var countersText = new List<string>();
            var valuesText = new List<string>();

            // 16каналов по 8 байт = 128
            var bkp = ParseRegisterResponse(Send(MakeRegisterRequest(0x32204, 0x80)));
            if (!bkp.success) return bkp;

            double indication = 0;
            for (var i = 0; i < 16; i++)
            {
                UInt32 parData = Helper.ToUInt32(bkp.Register, i * 8);
                // 2 байта на номер параметра
                UInt16 par = Helper.ToUInt16(bkp.Register, i * 8 + 4);
                // 1 байт на позицию фиксированной запятой (знаковое)
                SByte point = (SByte)bkp.Register[i * 8 + 6];
                // 1 байт на ед. измерения
                byte unit = bkp.Register[i * 8 + 7];
                // 3 байта на счётчик
                UInt32 counter = parData & 0x00FFFFFF;
               
                Parameter p = new Parameter(i + 1);
                //1 бит - признак того, что счётный вход введён в эксплуатацию.
                p.isEnable = (parData & 0x80000000) > 0;
                //1 бит - признак ошибки в счётном входе.
                p.isError = (parData & 0x40000000) > 0;
                // 1 бит - статус магнитного входа. 
                p.PinMagState = (parData & 0x20000000) > 0;
                // 1 бит - статус счётного входа. 
                p.PinState = (parData & 0x10000000) > 0;
                p.point = point;
                p.name = GetParameterName4(par, i + 1);
                p.unit = mi.units[unit];
                if (p.isEnable)
                {
                    valuesText.Add($"{p.name}={p.GetView(counter)}");
                    indication = (indication == 0)? p.GetValue(counter): indication;
                    records.Add(MakeCurrentRecord(p.name, p.GetValue(counter), "", date, dateNow));
                }
                else
                {
                    countersText.Add($"{p.name}={p.GetView(counter)}");
                }
            }
            setIndicationForRowCache(indication, "м3", dateNow);

            ////

            var adcText = "";
            var inputsText = "";

            current.counters = string.Join("; ", countersText);
            current.values = string.Join("; ", valuesText);
            current.inputs = inputsText;
            current.adc = adcText;
            current.date = date;
            current.records = records;

            return current;
        }
        dynamic GetCurrent4_7(DateTime date)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            Dictionary<int, Parameter> parameterConfiguration = new Dictionary<int, Parameter>();

            var records = new List<dynamic>();
            var countersText = new List<string>();
            var valuesText = new List<string>();
            
            var bkp = ParseRegisterResponse(Send(MakeRegisterRequest(0x32204, 0x04)));
            
            if (!bkp.success) return bkp;
            DateTime dateNow = DateTime.Now;
            double GetLightMK = (double)bkp.Register[0];
            double GetLightReal = (double)bkp.Register[1];
            double GetPhotoSensorState = (double)bkp.Register[2];
            double GetControlMetod = (double)bkp.Register[3];
            log(string.Format("GetPhotoSensorState = {0}", GetPhotoSensorState), level: 1);
            records.Add(MakeCurrentRecord("GetLightMK", GetLightMK, "", date, dateNow));
            records.Add(MakeCurrentRecord("GetLightReal", GetLightReal, "", date, dateNow));
            records.Add(MakeCurrentRecord("GetPhotoSensorState", GetPhotoSensorState, "", date, dateNow));
            records.Add(MakeCurrentRecord("GetControlMetod", GetControlMetod, "", date, dateNow));
            current.records = records;
            return current;
        }

        private dynamic GetHourlyInx4(UInt16 inx)
        {
            if (!hours.ContainsKey(inx))
            {
                hours[inx] = Parse65ValuesResponse(Send(Make65Request(ArchiveType.Hourly, (UInt16)inx, isValues: true)));
            }
            return hours[inx]; //Parse65ValuesResponse(Send(Make65Request(ArchiveType.Hourly, (UInt16)inx, isValues: true))); // 
        }

        private dynamic GetHours4(DateTime start, DateTime end, DateTime current, byte counters, UInt16 devid, MessageConfig mc, MessageConfig2 mc2, MessageInput mi)
        {
            dynamic archive = new ExpandoObject();
            archive.success = false;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var hours = new List<dynamic>();

            {
                UInt16 sInx = 0;
                DateTime lastHour;

                var response = GetHourlyInx4(sInx);
                if (!response.success)
                {
                    archive.errorcode = response.errorcode;
                    archive.error = $"часовая запись {sInx} не прочитана - {response.error}";
                    return archive;
                }

                // последняя запись в архиве
                lastHour = response.Date;
                ArchiveRecord lastRecord = response.Record;
                //if (lastData == null)
                //{
                //    archive.error = string.Format("Не удалось прочитать запись {0}", sInx);
                //    archive.errorcode = DeviceError.NO_ERROR;
                //    return archive;
                //}

                if (lastHour == DateTime.MinValue)
                {
                    archive.errorcode = DeviceError.NO_ERROR;
                    archive.error = "Нет записей в архиве";
                    return archive;
                }

                var startHour = start.Date.AddHours(start.Hour);
                var offset = (int)(lastHour - startHour).TotalHours;

                //сбор часов
                for (var i = offset; i >= 0; i--)
                {
                    if (cancel())
                    {
                        archive.errorcode = DeviceError.NO_ERROR;
                        archive.error = "опрос отменен";
                        break;
                    }

                    if (i > 5842) continue;//capacity

                    response = GetHourlyInx4((UInt16)i);
                    if (!response.success)
                    {
                        archive.errorcode = response.errorcode;
                        archive.error = $"часовая запись {i} не прочитана - {response.error}";
                        return archive;
                    }

                    DateTime reqDate = lastHour.AddHours(-i);
                    DateTime rspDate = response.Date;
                    ArchiveRecord record = response.Record;

                    if (rspDate == DateTime.MinValue)
                    {
                        log(string.Format("Записи #{0} от {1:dd.MM.yyyy HH:00:00} нет в архиве", i, reqDate));
                        continue;
                    }

                    //if (record == null)
                    //{
                    //    archive.errorcode = DeviceError.NO_ERROR;
                    //    archive.error = string.Format("Не удалось прочитать запись {0}", i);
                    //    break;
                    //}

                    if (rspDate < new DateTime(2015, 10, 1))//past
                    {
                        log(string.Format("Запись #{0}: слишком ранняя дата", i));
                        continue;
                    }

                    //if (date > sDate)//future
                    //{
                    //    log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date));
                    //    continue;
                    //}

                    if (rspDate > end)//start-end
                    {
                        log(string.Format("Запись #{0}: прочтённая дата {1:dd.MM.yyyy HH:mm} за пределами установленного периода опроса", i, rspDate));
                        break;
                    }

                    if (rspDate < start)//start-end
                    {
                        log(string.Format("Запись #{0}: прочтённая дата {1:dd.MM.yyyy HH:mm} за пределами установленного периода опроса", i, rspDate));
                        continue;
                    }

                    var hour = new List<dynamic>();

                    foreach (var counter in record.Counter)
                    {
                        if (counter.IsEnabled)
                        {
                            string unit = ((mi != null) && (mi.success) && (counter.Unit < mi.units.Count)) ? $"{mi.units[counter.Unit],-10}" : $"[{counter.Unit:000000}]  ";
                            double value = counter.Value * Math.Pow(10, counter.Point);
                            hour.Add(MakeHourRecord($"{counter.Param}", value, $"{unit}", rspDate));
                            hour.Add(MakeHourRecord($"{counter.Param} - ошибка", counter.IsError ? 1 : 0, "", rspDate));
                            hour.Add(MakeHourRecord($"{counter.Param} - состояние входа", counter.PinState ? 1 : 0, "", rspDate));
                            hour.Add(MakeHourRecord($"{counter.Param} - магнитное воздействие", counter.PinMagState ? 1 : 0, "", rspDate));
                        }
                    }

                    if (hour.Any())
                    {
                        records(hour);
                        hours.AddRange(hour);
                        log(string.Format("Запись #{0} за {1:dd.MM.yyyy HH:mm} успешно прочтена{2}", i, rspDate, rspDate == reqDate ? "" : " (дыра в архиве?)"));
                    }
                    else
                    {
                        log(string.Format("Запись #{0} за {1:dd.MM.yyyy HH:mm} - нет активных каналов{2}", i, rspDate, rspDate == reqDate ? "" : " (дыра в архиве?)"));
                    }
                }

            }

            archive.success = true;
            archive.records = hours;
            return archive;
        }

        dynamic GetDay(DateTime date, MessageInput mi)
        {
            dynamic day = new ExpandoObject();
            day.success = true;
            day.error = string.Empty;
            day.errorcode = DeviceError.NO_ERROR;

            Dictionary<int, Parameter> parameterConfiguration = new Dictionary<int, Parameter>();
            DateTime dtNow = DateTime.Now;
            DateTime dtDay = new DateTime(dtNow.Year, dtNow.Month, (dtNow.Hour > 6) ? dtNow.AddDays(1).Day: dtNow.Day, 0, 0, 0);
            var records = new List<dynamic>();
            var countersText = new List<string>();
            var valuesText = new List<string>();

            // 16каналов по 8 байт = 128
            var bkp = ParseRegisterResponse(Send(MakeRegisterRequest(0x32204, 0x80)));
            if (!bkp.success) return bkp;

            for (var i = 0; i < 16; i++)
            {
                UInt32 parData = Helper.ToUInt32(bkp.Register, i * 8);
                // 2 байта на номер параметра
                UInt16 par = Helper.ToUInt16(bkp.Register, i * 8 + 4);
                // 1 байт на позицию фиксированной запятой (знаковое)
                SByte point = (SByte)bkp.Register[i * 8 + 6];
                // 1 байт на ед. измерения
                byte unit = bkp.Register[i * 8 + 7];
                // 3 байта на счётчик
                UInt32 counter = parData & 0x00FFFFFF;

                Parameter p = new Parameter(i + 1);
                //1 бит - признак того, что счётный вход введён в эксплуатацию.
                p.isEnable = (parData & 0x80000000) > 0;
                //1 бит - признак ошибки в счётном входе.
                p.isError = (parData & 0x40000000) > 0;
                // 1 бит - статус магнитного входа. 
                p.PinMagState = (parData & 0x20000000) > 0;
                // 1 бит - статус счётного входа. 
                p.PinState = (parData & 0x10000000) > 0;
                p.point = point;
                p.name = GetParameterName4(par, i + 1);
                p.unit = mi.units[unit];
                if (p.isEnable)
                {
                    valuesText.Add($"{p.name}={p.GetView(counter)}");
                    records.Add(MakeDayRecord(p.name, p.GetValue(counter), "", dtDay));
                }
                else
                {
                    countersText.Add($"{p.name}={p.GetView(counter)}");
                }
            }

            ////

            var adcText = "";
            var inputsText = "";

            day.counters = string.Join("; ", countersText);
            day.values = string.Join("; ", valuesText);
            day.inputs = inputsText;
            day.adc = adcText;
            day.date = dtDay;
            day.records = records;

            return day;
        }

        private string GetParameterName4(UInt16 par, int n)
        {
            if (par > 0)
            {
                if ((par >> 6) == 0x1)
                {
                    //Параметры в стиле СТ1-1(ор.) до CT16-4(кор.)
                    byte st = (byte)((par >> 2) & 0xF);
                    byte colorN = (byte)(par & 0x3);
                    string[] colors = new string[] { "оранж.", "синий", "зелёный", "коричн." };
                    return $"X{st + 1} №{colorN + 1} ({colors[colorN]})";
                }
                return $"Пар.{par:X4}";
            }
            return $"Канал {n:00}";
        }
    }
}

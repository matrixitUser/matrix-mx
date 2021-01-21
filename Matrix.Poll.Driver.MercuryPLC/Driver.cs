using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.MercuryPLC
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif
        private UInt16 ConcentratorAddress;
        private UInt16 NetworkAddress;
        private UInt16 NetworkVolume;

        private const int TIMEZONE_SERVER = +5;
        private const int TIMEZONE_DEFAULT = +3;

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
#if OLD_DRIVER
            if ((level < 3) || ((level == 3) && debugMode))
            {
                logger(message);
            }
#else
            logger(message, level);
#endif
        }

        private byte[] SendSimple(byte[] data)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var timeout = 7500;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
            {
                Thread.Sleep(sleep);

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
                        if (waitCollected == 6)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data, bool isGate228,  int attempts = 3)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            
            byte[] buffer = null;

            List<byte> dataGate228 = new List<byte>();
            if (isGate228)
            {
                dataGate228.Add(0x5B);
                dataGate228.Add(0x16);
                if (data.Length > 255) dataGate228.AddRange(BitConverter.GetBytes(data.Length));
                else
                {
                    dataGate228.Add((byte)(data.Length - 1));
                    dataGate228.Add(0);
                }
                dataGate228.Add(1);
                UInt32 crc24 = Crc24.Compute(dataGate228.ToArray());
                dataGate228.InsertRange(0, BitConverter.GetBytes(crc24).Take(3));
                dataGate228.AddRange(data);
            }
           
            for (var attempt = 0; attempt < attempts && answer.success == false; attempt++)
            {

                if (isGate228)
                {
                    buffer = SendSimple(dataGate228.ToArray());
                    //buffer = SendSimple(data);
                    buffer = buffer.Skip(8).Take(buffer[5]).ToArray();
                }
                else
                {
                    buffer = SendSimple(data);
                }
              
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 10)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 10 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else
                    {
                        UInt32 crc24 = BitConverter.ToUInt32(new byte[] { buffer[0], buffer[1], buffer[2], 0x00 }, 0);
                        byte cs = (byte)(buffer.Skip(8).Take(buffer.Length - 9).Sum(b => b) - 1);
                        if (crc24 != Crc24.Compute(buffer, 3, 5))
                        {
                            answer.error = "контрольная сумма заголовка не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;
                        }
                        else if (cs != buffer[buffer.Length - 1])
                        {
                            answer.error = "контрольная сумма данных не сошлась";
                            answer.errorcode = DeviceError.CRC_ERROR;
                        }
                        else
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
                answer.Body = buffer.Skip(3).ToArray();
                answer.Payload = buffer.Skip(8).Take(buffer.Length - 9).ToArray();
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

        private dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
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

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date, string s3, int i1, int i2 = 0)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.d2 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.s3 = (s3 == "") ? null : s3;
            if(i1 == 1) record.i1 = i1;
            if (i2 == 1) record.i2 = i2;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date, string s3)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.s3 = (s3 == "") ? null : s3;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeMonthRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Month"; //потому как нет данных помесячных
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

        #region ImportExport
        /// <summary>
        /// Регистр выбора стрраницы
        /// </summary>
        private const short RVS = 0x0084;

#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        private Func<byte[]> response;

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

        [Import("setContractHour")]
        private Action<int> setContractHour;

        [Import("recordLoad")]
        private Func<DateTime, DateTime, string, List<dynamic>> recordLoad;

        [Import("loadRecordsPowerful")]
        private Func<DateTime, DateTime, string, string, string, List<dynamic>> LoadRecordsPowerful;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            //setArchiveDepth("Day", 2);
            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !UInt16.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion

            #region concentratorAddress
            if (!param.ContainsKey("concentratorAddress") || !UInt16.TryParse(arg.concentratorAddress.ToString(), out ConcentratorAddress))
            {
                log("Отсутствуют сведения о адресе концентратора, взят по умолчанию 0x2FFF", level: 1);
                ConcentratorAddress = 0x2FFF;
            }
            #endregion

            #region networkVolume
            if (!param.ContainsKey("networkVolume") || !UInt16.TryParse(arg.networkVolume.ToString(), out NetworkVolume))
            {
                log("Отсутствуют сведения об емкости сети", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "емкость сети");
            }
            #endregion

#if OLD_DRIVER
            #region debug
            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
            }
            #endregion
#endif

            #region components
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
            #endregion

            #region start
            if (param.ContainsKey("start") && arg.start is DateTime)
            {
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }
            #endregion

            #region end
            if (param.ContainsKey("end") && arg.end is DateTime)
            {
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }
            #endregion

            #region hourRanges
            List<dynamic> hourRanges;
            if (param.ContainsKey("hourRanges") && arg.hourRanges is IEnumerable<dynamic>)
            {
                hourRanges = arg.hourRanges;
                foreach (var range in hourRanges)
                {
                    log(string.Format("принят часовой диапазон {0:dd.MM.yyyy HH:mm}-{1:dd.MM.yyyy HH:mm}", range.start, range.end));
                }
            }
            else
            {
                hourRanges = new List<dynamic>();
                dynamic defaultrange = new ExpandoObject();
                defaultrange.start = getStartDate("Hour");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Hour");
                hourRanges.Add(defaultrange);
            }
            #endregion

            #region dayRanges
            List<dynamic> dayRanges;
            if (param.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
            {
                dayRanges = arg.dayRanges;
                foreach (var range in dayRanges)
                {
                    log(string.Format("принят суточный диапазон {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", range.start, range.end));
                }
            }
            else
            {
                dayRanges = new List<dynamic>();
                dynamic defaultrange = new ExpandoObject();
                defaultrange.start = getStartDate("Day");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Day");
                dayRanges.Add(defaultrange);
            }
            #endregion

            #region channels
            ushort[] channelsWithWrongNa = null;
            if (param.ContainsKey("channelWithWrongNa")) //1; 2; 1,2; _
            {
                try
                {
                    string ch = arg.channelWithWrongNa.ToString();
                    channelsWithWrongNa = ch.Split(',').Select(c => ushort.Parse(c)).ToArray();
                }
                catch (Exception ex)
                {

                }
            }
            ushort[] channels = null;
            UInt32[] u32Channels = null;
            int typePLC = 1;
            if (param.ContainsKey("typePLC")) //1; 2; 1,2; _ //TODO потом переделать
            {
                try
                {
                    typePLC = int.Parse(arg.typePLC);
                }
                catch (Exception ex)
                {
                }
            }

            if (param.ContainsKey("channel")) //1; 2; 1,2; _
            {
                try
                {
                    string ch = arg.channel.ToString();
                    if (typePLC == 1) 
                    { 
                        channels = ch.Split(',').Select(c => ushort.Parse(c)).ToArray();
                        log("выбран PLC-1");
                    }
                    else if (typePLC == 2)
                    {
                        u32Channels = ch.Split(',').Select(c => UInt32.Parse(c)).ToArray();
                        log("выбран PLC-2");
                    }
                }
                catch (Exception ex)
                {

                }
            }
           
            if (channels == null || !channels.Any())
            {
                channels = new ushort[512] ;
                for (ushort i = 0; i < 512; i++)
                {
                    channels[i] = i;
                }
            }
            if (typePLC == 2 && (u32Channels == null || !u32Channels.Any()))
            {
                log("Сетевые адреса счетчика не указаны для PLC-2, укажите сетевые адреса счетчиков");
            }
            #endregion

            #region gate228
            int gate228On = 0;
            bool gate228 = false;
            if (param.ContainsKey("gate228") && int.TryParse(arg.gate228.ToString(), out gate228On) && gate228On == 1)
            {
                gate228 = true;
                log("Опрос через Меркурий Шлюз 228");
            }
            else log("Опрос через модем");
           //log($"gate228={gate228}");
            #endregion

            #region isTimeCorrectionEnabled
            bool isTimeCorrectionEnabled;
            int timeCorrectionEnable = 0;
            if (param.ContainsKey("timeCorrectionEnable") && (arg.timeCorrectionEnable is string) && int.TryParse(arg.timeCorrectionEnable, out timeCorrectionEnable) && (timeCorrectionEnable == 1))
            {
                isTimeCorrectionEnabled = true;
            }
            else
            {
                isTimeCorrectionEnabled = false;
            }
            #endregion

            #region timeZone
            int timeZone = 0;
            if (param.ContainsKey("timeZone") && (arg.timeZone is string) && int.TryParse(arg.timeZone, out timeZone))
            {
                if ((timeZone < -12) || (timeZone > +14))
                {
                    timeZone = TIMEZONE_DEFAULT;
                }
            }
            else
            {
                timeZone = TIMEZONE_DEFAULT;
            }
            #endregion

            #region setNetworkSize
            UInt16 setNetworkSize = 0;
            if (param.ContainsKey("networkSize") && (arg.networkSize is string) && UInt16.TryParse(arg.networkSize, out setNetworkSize))
            {
                if(setNetworkSize > 2048)
                {
                    setNetworkSize = 0;
                }
            }
            else
            {
                setNetworkSize = 0;
            }
            #endregion


            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges, gate228, isTimeCorrectionEnabled, timeZone, setNetworkSize, channels, u32Channels, channelsWithWrongNa));
                        }
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func)
        {
            //ACTION
            return func();

            //RELEASE
            //log(cancel() ? "успешно отменено" : "считывание окончено");
        }
        #endregion
        private byte[] MakeBaseRequest(byte type, byte[] data = null) //Для plc1
        {
            return MakeBaseRequest(type, 1, data);
        }

        private byte[] MakeBaseRequest( byte type, int typePlc, byte[] data = null)
        {
            List<byte> dataList = new List<byte>() { type };
            if (data != null)
            {
                dataList.AddRange(data);
                if (typePlc == 2) 
                { 
                    dataList.Add(0x02);
                    log("Опрос данных PLC-2");
                }
            }
            
            UInt16 src = 0xFFFF;
            UInt16 dst = ConcentratorAddress;
            byte cs = (byte)(dataList.Sum(d => d) - 1);
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(src));
            buffer.AddRange(BitConverter.GetBytes(dst));
            buffer.Add((byte)dataList.Count);
            UInt32 crc24 = Crc24.Compute(buffer.ToArray());
            buffer.InsertRange(0, BitConverter.GetBytes(crc24).Take(3));
            buffer.AddRange(dataList);
            buffer.Add(cs);

            byte csLast = (byte)(buffer.Sum(d => d) - 1);
            buffer.Add(csLast);
            return buffer.ToArray();
        }


        private byte[] MakeReadCurrentDateRequest()
        {
            return MakeBaseRequest(0x81);
        }


        private dynamic ParseReadCurrentDateResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            if (payload.Length != 8 || payload[0] != 0x81)
            {
                answer.error = "неожиданный ответ на запрос даты/времени";
                answer.success = false;
                return answer;
            }

            answer.Date = new DateTime(year: 2000 + payload[7], month: payload[6] + 1, day: payload[5] + 1, hour: payload[3], minute: payload[2], second: payload[1]);
            return answer;
        }
       
        private byte[] MakeWriteCurrentDateRequest(DateTime date)
        {
            return MakeBaseRequest(0x01, new byte[] {
                (byte)date.Second,
                (byte)date.Minute,
                (byte)date.Hour,
                (byte)(date.DayOfWeek == DayOfWeek.Sunday? 6 : (byte)date.DayOfWeek - 1),
                (byte)(date.Day - 1),
                (byte)(date.Month - 1),
                (byte)(date.Year % 100)
            });
        }


        private byte[] MakeReadCcConfigRequest()
        {
            return MakeBaseRequest(0x80);
        }


        private byte[] MakeWriteCcConfigRequest(UInt16 netSize, byte configByte)
        {
            return MakeBaseRequest(0x00, new byte[] { (byte)netSize, (byte)(netSize >> 8), configByte });
        }


        private dynamic ParseReadCcConfigResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            if (payload.Length != 4 || payload[0] != 0x80)
            {
                answer.error = "неожиданный ответ на запрос конфигурации концентратора";
                answer.success = false;
                return answer;
            }

            answer.CcConfig = new CcConfiguration(payload, 0, payload.Length);
            return answer;
        }


        private byte[] MakeReadLastPackageRequest(ushort currNetworkAddress)
        {
            return MakeBaseRequest(0x82, BitConverter.GetBytes(currNetworkAddress));
        }
        private byte[] MakeReadLastPackageRequest(UInt32 currNetworkAddress) //plc2
        {
            return MakeBaseRequest(0x9C, 2, ConvertToBcd(currNetworkAddress));
        }

        private byte[] ConvertToBcd(UInt32 value)
        {
            List<byte> bytes = new List<byte>();
            string valStr = value.ToString();
            //log($"valStr={valStr}");
            for (int i = 0; i < 4; i++)
            {

                string tmp = new string(valStr.Skip(2 * i).Take(2).ToArray());
                //log($"tmp{i}={tmp}");
                if (bytes.Any())
                {
                    bytes.Insert(0, Convert.ToByte(tmp, 16));
                }
                else
                {
                    bytes.Add(Convert.ToByte(tmp, 16));
                }
            }

            return bytes.ToArray();
           
        }

        private byte[] MakeReadLastMonthsRequest(ushort currNetworkAddress)
        {
            return MakeBaseRequest(0x85, BitConverter.GetBytes(currNetworkAddress));
        }

        private byte[] MakeReadLastMonthsRequest(UInt32 currNetworkAddress)
        {
            return MakeBaseRequest(0x85, BitConverter.GetBytes(currNetworkAddress));
        }


        private dynamic ParseReadLastPackageResponse1(dynamic answer)
        {
            
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            
            if (payload.Length == 1 && payload[0] == 0x82)
            {
                answer.hasData = false;
            }
            else if (payload.Length == 14 && payload[0] == 0x82)
            {
                answer.hasData = true;
                answer.NetworkAddress = BitConverter.ToUInt16(payload, 1);
                byte[] rr = payload.Skip(4).Take(5).ToArray();
                answer.lastPacket = new Packet(payload, 3, payload.Length - 3);
                if (!answer.success)
                {
                    answer.error = "ошибка в данных на запрос последнего пакета";
                }
            }
            else
            {
                answer.error = "неожиданный ответ на запрос последнего пакета";
                answer.success = false;
            }

            return answer;
        }


        private dynamic ParseReadLastPackageResponse2(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];

            if (payload.Length == 1)
            {
                answer.success = false;
            }
            else if (payload[0] == 0x9C)
            {
               
            }
            else
            {
                answer.error = "неожиданный ответ на запрос последнего пакета";
                answer.success = false;
            }

            return answer;
        }

        private dynamic ParseReadLastMonthsResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            if (payload.Length == 1 && payload[0] == 0x85)
            {
                answer.hasData = false;
            }
            else if ((payload.Length % 11) == 3 && payload[0] == 0x85)
            {
                answer.hasData = true;
                answer.NetworkAddress = BitConverter.ToUInt16(payload, 1);
                List<Packet> packets = new List<Packet>();
                for (int i = 3; i < payload.Length; i += 11)
                {
                    packets.Add(new Packet(payload, i, 11));
                }
                answer.PacketList = packets.ToArray();
            }
            else
            {
                answer.error = "неожиданный ответ на запрос последнего месяца";
                answer.success = false;
            }

            return answer;
        }

        private byte[] MakeReadMonthRequest(ushort NetworkAddress)
        {
            return MakeBaseRequest(0x85, BitConverter.GetBytes(NetworkAddress));
        }


        private byte[] MakeReadBDayRequest()
        {
            return MakeBaseRequest(0x89);
        }


        private dynamic ParseReadBDayResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            if (payload.Length == 2 && payload[0] == 0x89)
            {
                answer.BDay = (SByte)payload[1];
            }
            else
            {
                answer.error = "неожиданный ответ на запрос последнего месяца";
                answer.success = false;
            }

            return answer;
        }


        private byte[] MakeReadVerInfoRequest()
        {
            return MakeBaseRequest(0x83);
        }


        private dynamic ParseReadVerInfoResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] payload = answer.Payload as byte[];
            if (payload.Length > 1 && payload[0] == 0x83)
            {
                answer.Version = Encoding.ASCII.GetString(payload.Skip(1).ToArray());
            }
            else
            {
                answer.error = "неожиданный ответ на запрос последнего месяца";
                answer.success = false;
            }

            return answer;
        }



        private void GetConstants()
        {
            SendSimple(new byte[] { 0xDE, 0x65, 0x71, 0x01, 0x00, 0x00, 0x80, 0x7F, 0x20, 0x3F });
        }

        private void GetConstants2()
        {
           SendSimple(new byte[] { 0x4F, 0x70, 0x37, 0x66, 0x71, 0x04, 0x00, 0x00, 0x01, 0x1A, 0x33, 0x04, 0x51 });
        }



        private dynamic GetConstants(DateTime date, bool isGate228)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();

            dynamic bday = ParseReadBDayResponse(Send(MakeReadBDayRequest(), isGate228));
            if (!bday.success) return bday;

            recs.Add(MakeConstRecord("Расчетный день", $"{bday.BDay}", date));

            answer.records = recs;
            return answer;
        }

        private dynamic GetCurrents(DateTime date, ushort[] channels, ushort[] channelsWithWrongNa, bool gate228)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();
            UInt16 tmp;
            string isFixedBug;

            DateTime dt10MonthBeforeNow = DateTime.Now.AddMonths(-10);
            
            List<Packet> packetListAll = new List<Packet>();
            List<string> tarifList = new List<string>();
            string parameters;
            DateTime tmpdt;
            foreach (ushort currNetworkAddress in channels)
            {
                if (channelsWithWrongNa != null)
                {
                    if (channelsWithWrongNa.Contains(currNetworkAddress))
                    {
                        log($"Чтение данных сетевого адреса: {currNetworkAddress} отклонено", 1);
                        continue;
                    }
                }
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                log($"Чтение данных в последнем пакете для сетевого адреса: {currNetworkAddress}",1);
                dynamic answerSend = Send(MakeReadLastPackageRequest(currNetworkAddress), gate228);

                dynamic lastPacketResponse = ParseReadLastPackageResponse1(answerSend);
                if (!lastPacketResponse.success)
                {
                    log(lastPacketResponse.error, 1);
                    return lastPacketResponse;
                }
                if (!lastPacketResponse.hasData) continue;
                Packet packet = lastPacketResponse.lastPacket as Packet;
                int j = 0;
               
                dynamic recordMin = null, recordMinTmp0, recordMinTmp1;
                if (packet.HasParameter() && packet.Parameter.Type == "Day")
                {
                    parameters = string.Format("na{0}_{1}", currNetworkAddress, packet.Parameter.Parameter);
                    log(string.Format("parameters={0} value={1}  за:{2}", parameters, packet.Parameter.Value, packet.Date));

                    byte[] answerSendBody = answerSend.Body as byte[];
                    string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));
                    recs.Add(MakeCurrentRecord(parameters, packet.Parameter.Value, pvUnit, packet.Date, ""));
                }
                if (packet.HasParameter() && packet.Parameter.Type == "Current")
                {
                    ParameterValue pv = packet.Parameter;
                    parameters = string.Format("na{0}_{1}", currNetworkAddress, pv.Parameter);
                    log(string.Format("parameters={0} value={1} за:{2}", parameters, pv.Value, packet.Date));
                    var recordList = LoadRecordsPowerful(dt10MonthBeforeNow, DateTime.Now, "Day", parameters, "findAnotherTubes");

                    do
                    {
                        tmpdt = packet.Date.AddMonths(-j++);
                        recordMinTmp0 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                    } while (recordMinTmp0 == null && j < 7);

                    tmpdt = packet.Date.AddMonths(-j);
                    recordMinTmp1 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                    if (recordMinTmp0 != null && recordMinTmp1 != null)
                    {
                        recordMin = (recordMinTmp0.d1 < recordMinTmp1.d1) ? recordMinTmp1 : recordMinTmp0;
                    }
                    else if (recordMinTmp0 != null && recordMinTmp1 == null)
                    {
                        recordMin = recordMinTmp0;
                    }
                    else if (recordMinTmp0 == null && recordMinTmp1 != null)
                    {
                        recordMin = recordMinTmp1;
                    }

                    int kk = (int)((recordMin == null) ? 0 : (recordMin.d1 - recordMin.d1 % 10000) / 10000);
                    if ((recordMin != null) && (recordMin.d1 % 10000 > pv.Value))
                    {
                        kk++;
                    }
                    pv.Value += 10000 * kk;

                    isFixedBug = "";
                    tmp = Decoder(packet.Data.Skip(2).ToArray());
                    if (tmp == 0x8000)
                    {
                        isFixedBug = "0x8000, ошибка обнаружена и не может быть исправлена";
                    }
                    else if (!isTableCoder(tmp))
                    {
                        isFixedBug = "возможно ложное декодирование";
                    }
                        
                    log($"Обнаружены данные в последнем пакете для сетевого адреса: {currNetworkAddress}");



                    //Запись в S2 то что пришло
                    byte[] answerSendBody =  answerSend.Body as byte[];
                    string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));

                    //*recs.Add(MakeCurrentRecord(parameters, pv.Value, pv.Unit, packet.Date));  
                    recs.Add(MakeCurrentRecord(parameters, pv.Value, pvUnit, packet.Date, isFixedBug));  
                }

            }
            answer.records = recs;
            return answer;
        }


        private dynamic GetCurrents(DateTime date, UInt32[] channels , bool gate228)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();
            UInt16 tmp;
            string strFixedBug;

            DateTime dt10MonthBeforeNow = DateTime.Now.AddMonths(-10);

            List<Packet> packetListAll = new List<Packet>();
            string parameters;
            foreach (UInt32 currNetworkAddress in channels)
            {
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                log($"Чтение данных в последнем пакете для сетевого адреса: {currNetworkAddress}", 1);
                dynamic answerSend = Send(MakeReadLastPackageRequest(currNetworkAddress), gate228);

                if (!answerSend.success) continue;
                byte[] payload = answerSend.Payload as byte[];

                double tariff1 = (double)Helper.ToInt32WithHalfReverse(payload, 13) / 1000;
                double tariff2 = (double)Helper.ToInt32WithHalfReverse(payload, 29) / 1000;
                double tariff3 = (double)Helper.ToInt32WithHalfReverse(payload, 45) / 1000;
                double tariff4 = (double)Helper.ToInt32WithHalfReverse(payload, 61) / 1000;
                double tariffSum = (double)Helper.ToInt32WithHalfReverse(payload, 77) / 1000;

                byte[] dateWh = payload.Skip(6).Take(6).ToArray();
                int[] dateWh1 = dateWh.Select(i => (int)i).ToArray();
                date = new DateTime(year: 2000 + dateWh1[5], month: dateWh1[4] + 1, day: dateWh1[3] + 1, hour: dateWh1[2], minute: dateWh1[1], second: dateWh1[0]);
                log(string.Format("Дата/время : {0:dd.MM.yy HH:mm:ss}", date));
                
                
                //var dateWh = string.Join(".", dateWh1);
                //log($"ДАТА ПОСЛЕДНЕГО ОПРОСА СЧЕТЧИКА={dateWh}", 3);


                parameters = string.Format("na{0}_{1}", currNetworkAddress, string.Format(ParameterValue.EE_TARIFF_FORMAT, 1));
                log(string.Format("parameters={0} value={1} кВт за дату:{2}", parameters, tariff1, date));
                recs.Add(MakeCurrentRecord(parameters, tariff1, "кВт", date, ""));
                parameters = string.Format("na{0}_{1}", currNetworkAddress, string.Format(ParameterValue.EE_TARIFF_FORMAT, 2));
                log(string.Format("parameters={0} value={1} кВт за дату:{2}", parameters, tariff2, date));
                recs.Add(MakeCurrentRecord(parameters, tariff2, "кВт", date, ""));
                parameters = string.Format("na{0}_{1}", currNetworkAddress, string.Format(ParameterValue.EE_TARIFF_FORMAT, 3));
                log(string.Format("parameters={0} value={1} кВт за дату:{2}", parameters, tariff3, date));
                recs.Add(MakeCurrentRecord(parameters, tariff3, "кВт", date, ""));
                parameters = string.Format("na{0}_{1}", currNetworkAddress, string.Format(ParameterValue.EE_TARIFF_FORMAT, 4));
                log(string.Format("parameters={0} value={1} кВт за дату:{2}", parameters, tariff4, date));
                recs.Add(MakeCurrentRecord(parameters, tariff4, "кВт", date, ""));
                parameters = string.Format("na{0}_{1}", currNetworkAddress, ParameterValue.EE_TARIFF_ALL);
                log(string.Format("parameters={0} value={1} кВт за дату:{2}", parameters, tariffSum, date));
                recs.Add(MakeCurrentRecord(parameters, tariffSum, "кВт", date, ""));
            }
            answer.records = recs;
            return answer;
        }


        private dynamic GetMonths(ushort[] channels, ushort[] channelsWithWrongNa, bool gate228)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();
            DateTime dt16MonthBeforeNow = DateTime.Now.AddMonths(-15);
            DateTime dt1MonthAfterNow = DateTime.Now.AddMonths(1);
            string isFixedBug;
            string parameters;
            DateTime tmpdt;
            foreach (ushort currNetworkAddress in channels)
            {
                if(channelsWithWrongNa != null)
                {
                    if (channelsWithWrongNa.Contains(currNetworkAddress))
                    {
                        log($"Чтение данных сетевого адреса: {currNetworkAddress} отклонено", 1);
                        continue;
                    }
                }
               
                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                log($"Чтение данных последних месяцев для сетевого адреса: {currNetworkAddress}", 1);
                dynamic answerSend = Send(MakeReadLastMonthsRequest(currNetworkAddress), gate228);

                dynamic lastMonthsResponse = ParseReadLastMonthsResponse(answerSend);
                if (!lastMonthsResponse.success) return lastMonthsResponse;
                if (!lastMonthsResponse.hasData) continue;
                int ii = 0, countErr = 0;

                List<Packet> packetListAll = new List<Packet>();
                List<string> tarifList = new List<string>();
                bool islastPacketCom4 = false;
                DateTime packetMinDate = DateTime.MaxValue;
                foreach (var packetI in lastMonthsResponse.PacketList)
                {
                    Packet packet = packetI as Packet;
                  
                    if(packet.Parameter != null)
                    {
                        string tarif = tarifList.Find(x => x.Contains(packet.Parameter.Parameter));
                        if (tarif == null)
                        {
                            tarifList.Add(packet.Parameter.Parameter);
                        }
                        packetListAll.Add(packet);
                    }
                }
                
                if (packetListAll != null)
                {
                    log($"Обнаружены данные последних месяцев для сетевого адреса: {currNetworkAddress}");
                    // для 0x40...0x4F типов
                    foreach (var packet in packetListAll)
                    {
                        if (packet.HasParameter() && packet.Parameter.Type == "Day")
                        {
                            parameters = string.Format("na{0}_{1}", currNetworkAddress, packet.Parameter.Parameter);
                            log(string.Format("parameters={0} value={1}  за:{2} i={3}", parameters, packet.Parameter.Value, packet.Date, ii++));

                            byte[] answerSendBody = answerSend.Body as byte[];
                            string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));
                            DateTime packetDate;
                            if (packet.Date.Day == 1)
                            {
                                packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0);
                            }
                            else
                            {
                                packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0).AddMonths(1); //На начало следующего месяца, данные за 'i' месяц записываем на начало i+1 месяц
                            }
                            
                            if (packetDate > DateTime.Now)
                            {
                                islastPacketCom4 = true;
                            }
                            recs.Add(MakeDayRecord(parameters, packet.Parameter.Value, pvUnit, packetDate, "", packet.dataI1, countErr));
                        }
                    }

                    //для остальных типов
                    dynamic recordMin = null, recordMinTmp0, recordMinTmp1;
                    for (int t = 0; t < tarifList.Count; t++)
                    {
                        int j = 0;
                        List<Packet> l = new List<Packet>();
                        parameters = string.Format("na{0}_{1}", currNetworkAddress, tarifList[t]);
                        
                        var packetListOnly1Tarif = packetListAll.FindAll(x => x.Parameter.Parameter.Contains(tarifList[t]) && x.Parameter.Type=="Current").OrderBy(x => x.Date).ToList();
                        if (packetListOnly1Tarif.Count == 0) continue;
                        packetMinDate = packetListOnly1Tarif.Min(x => x.Date);

                        var recordList = LoadRecordsPowerful(dt16MonthBeforeNow, dt1MonthAfterNow, "Day", parameters, "findAnotherTubes");
                        do
                        {
                            tmpdt = packetMinDate.AddMonths(-j++);
                            recordMinTmp0 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                        } while (recordMinTmp0 == null && j < 7);

                        tmpdt = packetMinDate.AddMonths(-j);
                        recordMinTmp1 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                        if (recordMinTmp0 != null && recordMinTmp1 != null)
                        {

                            if (recordMinTmp0.d1 < recordMinTmp1.d1)
                            {
                                recordMin = recordMinTmp1;
                                countErr = 1;
                            }
                            else
                            {
                                recordMin = recordMinTmp0;
                            }
                        }
                        else if (recordMinTmp0 != null && recordMinTmp1 == null)
                        {
                            recordMin = recordMinTmp0;
                        }
                        else if (recordMinTmp0 == null && recordMinTmp1 != null)
                        {
                            recordMin = recordMinTmp1;
                        }

                        int kk = (int)((recordMin == null) ? 0 : (recordMin.d1 - recordMin.d1 % 10000) / 10000);
                        if ((recordMin != null) && (recordMin.d1 % 10000 > packetListOnly1Tarif.Find(x => x.Date == packetMinDate).Parameter.Value))
                        {
                            packetListOnly1Tarif.Find(x => x.Date == packetMinDate).dataI1 = 1;
                            kk++;
                        }

                        foreach (var packet in packetListOnly1Tarif)
                        {
                            packet.Parameter.Value += 10000 * kk;
                        }
                        for (int i = 0; i < packetListOnly1Tarif.Count - 1; i++)
                        {
                            if (Decoder(packetListOnly1Tarif[i].Data.Skip(2).ToArray()) != 0x8000)
                            {
                                if (packetListOnly1Tarif[i].Date < packetListOnly1Tarif[i + 1].Date && packetListOnly1Tarif[i].Parameter.Value > packetListOnly1Tarif[i + 1].Parameter.Value)
                                {
                                    packetListOnly1Tarif[i + 1].dataI1 = 1;
                                    for (int k = i + 1; k < packetListOnly1Tarif.Count; k++)
                                        packetListOnly1Tarif[k].Parameter.Value += 10000;
                                }
                            }
                            else
                            {
                                if (packetListOnly1Tarif[i].Date < packetListOnly1Tarif[i + 1].Date && packetListOnly1Tarif[i].Parameter.Value > packetListOnly1Tarif[i + 1].Parameter.Value + 255)
                                {
                                    packetListOnly1Tarif[i + 1].dataI1 = 1;
                                    for (int k = i + 1; k < packetListOnly1Tarif.Count; k++)
                                        packetListOnly1Tarif[k].Parameter.Value += 10000;
                                }
                            }
                        }

                        foreach (var packet in packetListOnly1Tarif)
                        {
                            if (packet.HasParameter() && packet.Parameter.Type == "Current")
                            {
                                DateTime packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0).AddMonths(1); //На начало следующего месяца, данные за 'i' месяц записываем на начало i+1 месяц

                                var isRec = recordList.Find(x => x.date == packetDate);
                                if(isRec == null || (packetDate > DateTime.Now && !islastPacketCom4))
                                {
                                    isFixedBug = "";
                                    if (Decoder(packet.Data.Skip(2).ToArray()) == 0x8000)
                                    {
                                        isFixedBug = "0x8000, ошибка обнаружена и не может быть исправлена";
                                    }
                                    else if (!isTableCoder(Decoder(packet.Data.Skip(2).ToArray())))
                                    {
                                        isFixedBug = "возможно ложное декодирование";
                                    }
                                    byte[] answerSendBody = answerSend.Body as byte[];
                                    string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));
                                    log(string.Format("parameters={0} value={1}  за:{2} {4} i={3}", parameters, packet.Parameter.Value, packet.Date, ii++, isFixedBug));
                                    recs.Add(MakeDayRecord(parameters, packet.Parameter.Value, pvUnit, packetDate, isFixedBug, packet.dataI1, countErr));
                                }

                            }
                        }
                        
                        
                    }
                }
            }
            answer.records = recs;
            return answer;
        }


        private dynamic GetMonths(UInt32[] channels, bool gate228)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();
            DateTime dt16MonthBeforeNow = DateTime.Now.AddMonths(-15);
            DateTime dt1MonthAfterNow = DateTime.Now.AddMonths(1);
            string isFixedBug;
            string parameters;
            DateTime tmpdt;
            foreach (UInt32 currNetworkAddress in channels)
            {
               

                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                log($"Чтение данных последних месяцев для сетевого адреса: {currNetworkAddress}", 1);
                dynamic answerSend = Send(MakeReadLastMonthsRequest(currNetworkAddress), gate228);

                dynamic lastMonthsResponse = ParseReadLastMonthsResponse(answerSend);
                if (!lastMonthsResponse.success) return lastMonthsResponse;
                if (!lastMonthsResponse.hasData) continue;
                int ii = 0, countErr = 0;

                List<Packet> packetListAll = new List<Packet>();
                List<string> tarifList = new List<string>();
                bool islastPacketCom4 = false;
                DateTime packetMinDate = DateTime.MaxValue;
                foreach (var packetI in lastMonthsResponse.PacketList)
                {
                    Packet packet = packetI as Packet;

                    if (packet.Parameter != null)
                    {
                        string tarif = tarifList.Find(x => x.Contains(packet.Parameter.Parameter));
                        if (tarif == null)
                        {
                            tarifList.Add(packet.Parameter.Parameter);
                        }
                        packetListAll.Add(packet);
                    }
                }

                if (packetListAll != null)
                {
                    log($"Обнаружены данные последних месяцев для сетевого адреса: {currNetworkAddress}");
                    // для 0x40...0x4F типов
                    foreach (var packet in packetListAll)
                    {
                        if (packet.HasParameter() && packet.Parameter.Type == "Day")
                        {
                            parameters = string.Format("na{0}_{1}", currNetworkAddress, packet.Parameter.Parameter);
                            log(string.Format("parameters={0} value={1}  за:{2} i={3}", parameters, packet.Parameter.Value, packet.Date, ii++));

                            byte[] answerSendBody = answerSend.Body as byte[];
                            string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));
                            DateTime packetDate;
                            if (packet.Date.Day == 1)
                            {
                                packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0);
                            }
                            else
                            {
                                packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0).AddMonths(1); //На начало следующего месяца, данные за 'i' месяц записываем на начало i+1 месяц
                            }

                            if (packetDate > DateTime.Now)
                            {
                                islastPacketCom4 = true;
                            }
                            recs.Add(MakeDayRecord(parameters, packet.Parameter.Value, pvUnit, packetDate, "", packet.dataI1, countErr));
                        }
                    }

                    //для остальных типов
                    dynamic recordMin = null, recordMinTmp0, recordMinTmp1;
                    for (int t = 0; t < tarifList.Count; t++)
                    {
                        int j = 0;
                        List<Packet> l = new List<Packet>();
                        parameters = string.Format("na{0}_{1}", currNetworkAddress, tarifList[t]);

                        var packetListOnly1Tarif = packetListAll.FindAll(x => x.Parameter.Parameter.Contains(tarifList[t]) && x.Parameter.Type == "Current").OrderBy(x => x.Date).ToList();
                        if (packetListOnly1Tarif.Count == 0) continue;
                        packetMinDate = packetListOnly1Tarif.Min(x => x.Date);

                        var recordList = LoadRecordsPowerful(dt16MonthBeforeNow, dt1MonthAfterNow, "Day", parameters, "findAnotherTubes");
                        do
                        {
                            tmpdt = packetMinDate.AddMonths(-j++);
                            recordMinTmp0 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                        } while (recordMinTmp0 == null && j < 7);

                        tmpdt = packetMinDate.AddMonths(-j);
                        recordMinTmp1 = recordList.Find(x => x.date == new DateTime(tmpdt.Year, tmpdt.Month, 1, 0, 0, 0));
                        if (recordMinTmp0 != null && recordMinTmp1 != null)
                        {

                            if (recordMinTmp0.d1 < recordMinTmp1.d1)
                            {
                                recordMin = recordMinTmp1;
                                countErr = 1;
                            }
                            else
                            {
                                recordMin = recordMinTmp0;
                            }
                        }
                        else if (recordMinTmp0 != null && recordMinTmp1 == null)
                        {
                            recordMin = recordMinTmp0;
                        }
                        else if (recordMinTmp0 == null && recordMinTmp1 != null)
                        {
                            recordMin = recordMinTmp1;
                        }

                        int kk = (int)((recordMin == null) ? 0 : (recordMin.d1 - recordMin.d1 % 10000) / 10000);
                        if ((recordMin != null) && (recordMin.d1 % 10000 > packetListOnly1Tarif.Find(x => x.Date == packetMinDate).Parameter.Value))
                        {
                            packetListOnly1Tarif.Find(x => x.Date == packetMinDate).dataI1 = 1;
                            kk++;
                        }

                        foreach (var packet in packetListOnly1Tarif)
                        {
                            packet.Parameter.Value += 10000 * kk;
                        }
                        for (int i = 0; i < packetListOnly1Tarif.Count - 1; i++)
                        {
                            if (Decoder(packetListOnly1Tarif[i].Data.Skip(2).ToArray()) != 0x8000)
                            {
                                if (packetListOnly1Tarif[i].Date < packetListOnly1Tarif[i + 1].Date && packetListOnly1Tarif[i].Parameter.Value > packetListOnly1Tarif[i + 1].Parameter.Value)
                                {
                                    packetListOnly1Tarif[i + 1].dataI1 = 1;
                                    for (int k = i + 1; k < packetListOnly1Tarif.Count; k++)
                                        packetListOnly1Tarif[k].Parameter.Value += 10000;
                                }
                            }
                            else
                            {
                                if (packetListOnly1Tarif[i].Date < packetListOnly1Tarif[i + 1].Date && packetListOnly1Tarif[i].Parameter.Value > packetListOnly1Tarif[i + 1].Parameter.Value + 255)
                                {
                                    packetListOnly1Tarif[i + 1].dataI1 = 1;
                                    for (int k = i + 1; k < packetListOnly1Tarif.Count; k++)
                                        packetListOnly1Tarif[k].Parameter.Value += 10000;
                                }
                            }
                        }

                        foreach (var packet in packetListOnly1Tarif)
                        {
                            if (packet.HasParameter() && packet.Parameter.Type == "Current")
                            {
                                DateTime packetDate = new DateTime(packet.Date.Year, packet.Date.Month, 1, 0, 0, 0).AddMonths(1); //На начало следующего месяца, данные за 'i' месяц записываем на начало i+1 месяц

                                var isRec = recordList.Find(x => x.date == packetDate);
                                if (isRec == null || (packetDate > DateTime.Now && !islastPacketCom4))
                                {
                                    isFixedBug = "";
                                    if (Decoder(packet.Data.Skip(2).ToArray()) == 0x8000)
                                    {
                                        isFixedBug = "0x8000, ошибка обнаружена и не может быть исправлена";
                                    }
                                    else if (!isTableCoder(Decoder(packet.Data.Skip(2).ToArray())))
                                    {
                                        isFixedBug = "возможно ложное декодирование";
                                    }
                                    byte[] answerSendBody = answerSend.Body as byte[];
                                    string pvUnit = string.Format("{0}", string.Join(",", answerSendBody.Select(b => b.ToString("X2"))));
                                    log(string.Format("parameters={0} value={1}  за:{2} {4} i={3}", parameters, packet.Parameter.Value, packet.Date, ii++, isFixedBug));
                                    recs.Add(MakeDayRecord(parameters, packet.Parameter.Value, pvUnit, packetDate, isFixedBug, packet.dataI1, countErr));
                                }

                            }
                        }


                    }
                }
            }
            answer.records = recs;
            return answer;
        }

        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges, bool isGate228, bool isTimeCorrectionEnabled, int timeZone, UInt16 setNetworkSize, ushort[] channels, UInt32[] u32Channels, ushort[] channelsWithWrongNa)
        {
            DateTime date;
            if (isGate228)
            {
                GetConstants();
                GetConstants2();
            }
            var curDate = ParseReadCurrentDateResponse(Send(MakeReadCurrentDateRequest(), isGate228));
            if (!curDate.success)
            {
                log($"Ошибка чтения времени на концентраторе: {curDate.error}", 1);
                return MakeResult(100, curDate.errorcode, curDate.error);
            }
            
            if (isTimeCorrectionEnabled)
            {
                DateTime nowTz = DateTime.Now.AddHours(timeZone - TIMEZONE_SERVER);
                TimeSpan timeDiff = nowTz - curDate.Date;
                bool isTimeCorrectable = (timeDiff.TotalMinutes > -4) && (timeDiff.TotalMinutes < 4);
                bool isTimeNeedToCorrent = (timeDiff.TotalSeconds >= 5) || (timeDiff.TotalSeconds <= -5);

                //log(string.Format("коррекция времени: {0}, {1} разность {2}", isTimeCorrectable ? "осуществима" : "только установка времени", isTimeNeedToCorrent ? "необходима" : "нет необходимости", timeDiff.TotalSeconds));

                if (isTimeCorrectable && isTimeNeedToCorrent)
                {
                    nowTz.AddMilliseconds(1500);
                    curDate = ParseReadCurrentDateResponse(Send(MakeWriteCurrentDateRequest(nowTz), isGate228, attempts: 1));
                    if (!curDate.success)
                    {
                        log(string.Format("Ошибка при попытке коррекции времени: {0}", curDate.error));
                    }
                    else
                    {
                        nowTz = DateTime.Now.AddHours(timeZone - TIMEZONE_SERVER);
                        TimeSpan timeDiffAfterCorrection = nowTz - curDate.Date;
                        log(string.Format("Произведена коррекция времени на {0:0.0} секунд", timeDiff.TotalSeconds - timeDiffAfterCorrection.TotalSeconds), 1);
                    }
                }
               
            }

            date = curDate.Date;
            setTimeDifference(DateTime.Now - date);

            log(string.Format("Дата/время на концентраторе: {0:dd.MM.yy HH:mm:ss}", date));


            if(setNetworkSize > 0)
            {
                CcConfiguration config;
                dynamic configResponse = ParseReadCcConfigResponse(Send(MakeReadCcConfigRequest(), isGate228));
                if (!configResponse.success)
                {
                    log($"Ошибка чтения конфигурации концентратора: {configResponse.error}", 1);
                    return MakeResult(100, configResponse.errorcode, configResponse.error);
                }
                config = configResponse.CcConfig;

                if (setNetworkSize != config.NetSize)
                {
                    configResponse = ParseReadCcConfigResponse(Send(MakeWriteCcConfigRequest(setNetworkSize, config.ConfigByte), isGate228));
                    if (!configResponse.success)
                    {
                        log($"Ошибка при записи конфигурации концентратора: {configResponse.error}", 1);
                        return MakeResult(100, configResponse.errorcode, configResponse.error);
                    }
                    config = configResponse.CcConfig;

                    if (setNetworkSize == config.NetSize)
                    {
                        log($"Размер сети {setNetworkSize} успешно установлен", 1);
                    }
                    else
                    {
                        log($"Не удалось установить размер сети в {setNetworkSize}, текущий размер сети: {config.NetSize}", level: 1);
                    }
                }
            }


            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            if (components.Contains("Constant"))
            {
                var constants = new List<dynamic>();

                var constant = GetConstants(date, isGate228);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                    return MakeResult(103, constant.errorcode, constant.error);
                }

                constants = constant.records;
                log(string.Format("Константы прочитаны: всего {0}", constants.Count));
                records(constants);
            }

            if (components.Contains("Current"))
            {
                var currents = new List<dynamic>();
                dynamic current = new ExpandoObject();
                if (u32Channels == null || !u32Channels.Any())
                {
                    current = GetCurrents(date, channels, channelsWithWrongNa, isGate228);
                }
                else
                {
                    current = GetCurrents(date, u32Channels, isGate228);
                }
                
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
                records(currents);
            }
            
            if (components.Contains("Day"))
            {
                var months = new List<dynamic>();
                dynamic month=new ExpandoObject();
                if (u32Channels == null|| !u32Channels.Any())
                {
                     month = GetMonths(channels, channelsWithWrongNa, isGate228);
                }
                else
                {
                     month = GetMonths(u32Channels, isGate228);
                }
                  
                if (!month.success)
                {
                    log(string.Format("Ошибка при считывании данных последних месяцев: {0}", month.error), level: 1);
                    return MakeResult(102, month.errorcode, month.error);
                }
                
                months = month.records;
                log(string.Format(" Данных последних месяцев прочитаны: всего {0}", months.Count), level: 1);
                records(months);
            }

            //if (components.Contains("Hour"))
            //{
            //    List<dynamic> hours = new List<dynamic>();
            //    if (hourRanges != null)
            //    {
            //        foreach (var range in hourRanges)
            //        {
            //            var startH = range.start;
            //            var endH = range.end;

            //            if (startH > currentDate) continue;
            //            if (endH > currentDate) endH = currentDate;

            //            var hour = GetHours(startH, endH, date, properties);
            //            if (!hour.success)
            //            {
            //                log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
            //                return MakeResult(105, hour.errorcode, hour.error);
            //            }
            //            hours = hour.records;

            //            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            //        }
            //    }
            //    else
            //    {
            //        //чтение часовых
            //        var startH = getStartDate("Hour");
            //        var endH = getEndDate("Hour");

            //        var hour = GetHours(startH, endH, date, properties);
            //        if (!hour.success)
            //        {
            //            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
            //            return MakeResult(105, hour.errorcode, hour.error);
            //        }
            //        hours = hour.records;

            //        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            //    }
            //}

            //if (components.Contains("Day"))
            //{
            //    List<dynamic> days = new List<dynamic>();
            //    if (dayRanges != null)
            //    {
            //        foreach (var range in dayRanges)
            //        {
            //            var startD = range.start;
            //            var endD = range.end;

            //            if (startD > currentDate) continue;
            //            if (endD > currentDate) endD = currentDate;

            //            var day = GetDays(startD, endD, date, properties, info.TotalDay);
            //            if (!day.success)
            //            {
            //                log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
            //                return MakeResult(104, day.errorcode, day.error);
            //            }
            //            days = day.records;
            //            log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            //        }
            //    }
            //    else
            //    {
            //        //чтение суточных
            //        var startD = getStartDate("Day");
            //        var endD = getEndDate("Day");
            //        var day = GetDays(startD, endD, date, properties, info.TotalDay);
            //        if (!day.success)
            //        {
            //            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
            //            return MakeResult(104, day.errorcode, day.error);
            //        }

            //        log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
            //    }
            //}



            ///// Нештатные ситуации ///
            //if (components.Contains("Abnormal"))
            //{
            //    var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
            //    var startAbnormal = lastAbnormal.Date;

            //    var endAbnormal = getEndDate("Abnormal");
            //    byte[] codes = new byte[] { };

            //    List<dynamic> abnormals = new List<dynamic>();

            //    log(string.Format("получено {0} записей НС за период", abnormals.Count));//{1:dd.MM.yy}, date));
            //    records(abnormals);                    
            //}
            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }
    }
}

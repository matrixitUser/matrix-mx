using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.WLS
{
    public partial class Driver
    {
        int hourlyStart = 30;

        private const int SEND_ATTEMPTS_COUNT = 3;
        private const int TIMEOUT_TIME_MIN = 5000;//2200;  //1500
        private const int SLEEP_TIME = 10;
        private const int COLLECT_MUL = 25;
        private const int ADC_ATTEMPTS_COUNT = 10;
        private const double ADC_VMIN = 3.28;
        private const double ADC_VMAX = 3.48;

        //версия флеш со времён Ильшата 16 бит
        private const int Register_FlashVerOld = 0x34A11;
        //обновленная версия флеш 16/32 бит
        private const int Register_FlashVerNew = 0x30000;

        private int packagesSent = 0;
        private bool infiniteMode = false;
        private class Block
        {
            public DateTime Date { get; set; }
            public byte Number { get; set; }
        }

        byte NetworkAddress;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;
        public byte[] MakeBaseRequest(byte Function, List<byte> Data)// = null
        {
            var bytes = new List<byte>();
           
            bytes.Add(NetworkAddress);
            bytes.Add(Function);

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.Add(crc.CrcData[0]);
            bytes.Add(crc.CrcData[1]);

            return bytes.ToArray();
        }
        public byte[] MakeBaseRequest(byte networkAddress, byte Function, List<byte> Data)// = null
        {
            var bytes = new List<byte>();
            bytes.Add(networkAddress);
            bytes.Add(Function);

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.Add(crc.CrcData[0]);
            bytes.Add(crc.CrcData[1]);

            return bytes.ToArray();
        }
       
        private dynamic WaterTowerRequest()
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            DateTime date = DateTime.Now;
            //pressure давление
            dynamic result = Send(MakeBaseRequest(0x04, new List<byte> { 0x0B, 0x02, 0x00, 0x02 }));
            if (!result.success)
            {
                log(string.Format("Функция 0x04 не введён: {0}", result.error), level: 1);
                answer.success = false;
                return answer;
            }

            float pressure = (Helper.ToOfterSingle(result.Body, 1))*1.03f;
            log($"Высота = {pressure} м"); // в нашем случае давление == высота, тк коэфф = 1.03 
            //WLS WLS WLS WLS 
            result = Send(MakeBaseRequest(0x04, new List<byte> { 0x01, 0x00, 0x00, 0x01 }));
            if (!result.success)
            {
                log(string.Format("Функция 0x04 не введён: {0}", result.error), level: 1);
                answer.success = false;
                return answer;
            }
            byte waterLevel = result.Body[2];
            string strWls = $"max={(waterLevel >> 3) & 1}; max-middle={(waterLevel >> 2) & 1}; min-middle={(waterLevel >> 1) & 1}; min={waterLevel & 1};";
            log($"{strWls}||byte = {waterLevel}||");
            string strWlsForRowCache = $"{(waterLevel >> 3) & 1}{(waterLevel >> 2) & 1}{(waterLevel >> 1) & 1}{waterLevel & 1}";
            /*
            double indication = 0;
            for (int i = 0; i < result.Length; i++)
            {
                indication += result[i];
            }
            setIndicationForRowCache(indication, "Вт");
            */
            List<dynamic> rec = new List<dynamic>();
            rec.Add(MakeCurrentRecord("wls", Convert.ToDouble(waterLevel), strWls, date));
            rec.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", date));
            setIndicationForRowCache(Convert.ToInt32(pressure), "м; " + strWlsForRowCache, date);
            answer.records = rec;
            
            return answer;
        }


        private dynamic Listening()
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            DateTime date = DateTime.Now;
            byte[] buffer = SendSimple(new byte[] { }, 1000*30);
            if (buffer.Length < 7) return answer;
            string strTmp = string.Join(",", buffer.Select(b => b.ToString("X2")));
            List<dynamic> rec = new List<dynamic>();
            if (strTmp.Contains("10,04,04"))
            {
                try
                {
                    int index1 = strTmp.IndexOf("10,04,04");
                    string strTmp1 = strTmp.Substring(index1 + 9, 11);
                    byte[] bytesTmp = strTmp1.Split(',').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    float pressure = Helper.ToOfterSingle(bytesTmp, 0);
                    log($"ВЫСОТА = {pressure} м!"); // в нашем случае давление == высота, тк коэфф = 1.03 
                    rec.Add(MakeCurrentRecord("высота", Convert.ToDouble(pressure), "м", date));
                    setIndicationForRowCache(Convert.ToDouble(pressure), "м", date);
                }
                catch { }
            }
            if (strTmp.Contains("10,04,02"))
            {
                try
                {
                    int index1 = strTmp.IndexOf("10,04,02");
                    string strTmp1 = strTmp.Substring(index1 + 9, 5);
                    byte[] bytesTmp = strTmp1.Split(',').Select(x => byte.Parse(x, NumberStyles.HexNumber)).ToArray();
                    byte waterLevel = bytesTmp[1];
                    string strWls = $"MAX={(waterLevel >> 3) & 1}; MAX-middle={(waterLevel >> 2) & 1}; MIN-middle={(waterLevel >> 1) & 1}; MIN={waterLevel & 1};";
                    log($"{strWls}||byte = {waterLevel}||");
                    rec.Add(MakeCurrentRecord("wls", Convert.ToDouble(waterLevel), strWls, date));
                }
                catch { }
            }
            if (!rec.Any()) return answer;
            answer.success = true;
            answer.records = rec;
            return answer;
        }

        dynamic GetCurrent()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            dynamic result = Listening();
            if (!result.success)
            {
                return result;
            }
          
            current.records = result.records;
            return current;
        }
        dynamic GetConstant()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            dynamic result = WaterTowerRequest();
            if (!result.success)
            {
                return result;
            }

            current.records = result.records;
            return current;
        }
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

        private byte[] SendSimple(byte[] data, int timeout, int waitCollectedMax = 10)
        {
            var buffer = new List<byte>();

            log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            response();
            request(data);

            var sleep = 500;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) >= 0 && !isCollected)
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
                        if (waitCollected == waitCollectedMax)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer.ToArray();
        }
       
        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] buffer = null;
            
            var attempts_total = SEND_ATTEMPTS_COUNT;

            for (var attempts = 0; (attempts < attempts_total) && (answer.success == false); attempts++)
            {
                var timeout = TIMEOUT_TIME_MIN * (attempts + 1);
                buffer = SendSimple(data, timeout);

                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 5)
                    {
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                        answer.error = "в кадре ответа не может содержаться менее 5 байт";
                    }
                    else if (!Crc.Check(buffer, new Crc16Modbus()))
                    {
                        answer.errorcode = DeviceError.CRC_ERROR;
                        answer.error = "контрольная сумма кадра не сошлась";
                    }
                    else
                    {
                        packagesSent++;
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                    }
                }
            }

            if (answer.success)
            {
                var offset = 0;
                answer.NetworkAddress = buffer[0];
                if (answer.NetworkAddress == 251)
                {
                    offset = 12;
                }
                answer.Function = buffer.Skip(offset + 1).FirstOrDefault();
                answer.Body = buffer.Skip(offset + 2).Take(buffer.Length - (offset + 2 + 2)).ToArray();
               
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

        #region ImportExport

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

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            double KTr = 1.0;
            string password = "";

            var param = (IDictionary<string, object>)arg;

            byte na = 0;
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            else
            {
                NetworkAddress = na;
            }

            #region hourlyStart
            // минута, с которой считать начало следующего часа (если равна 10 => текущие 10:09 -> часовые 10:00; но 10:10 -> 11:00)
            if (param.ContainsKey("hourlyStart"))
            {
                int.TryParse(arg.hourlyStart.ToString(), out hourlyStart);
                hourlyStart %= 60;
            }
            #endregion


#if OLD_DRIVER
            byte debug = 0;
            if (param.ContainsKey("debug") && byte.TryParse(arg.debug.ToString(), out debug))
            {
                if (debug > 0)
                {
                    debugMode = true;
                }
            }
#endif

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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = All(components);
                        }
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

       
        #endregion

        #region Интерфейс

       
        private dynamic All(string components)
        {
            
            if (components.Contains("Current"))
            {
                if (cancel()) return MakeResult(200, DeviceError.NO_ERROR, "чтение отменено");
                
                var current = GetCurrent();
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }
                List<dynamic> rec = new List<dynamic>();
                rec.AddRange(current.records);
                records(rec);
                log(string.Format("Текущие прочитаны: всего {0}", rec.Count), level: 1);
            }

            ////

            if (components.Contains("Constant"))
            {
                var current = GetConstant();
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }
                List<dynamic> rec = new List<dynamic>();
                rec.AddRange(current.records);
                records(rec);
                log(string.Format("Текущие прочитаны: всего {0}", rec.Count), level: 1);
            }
            
            //////чтение часовых
            if (components.Contains("Hour"))
            {
               
            }

            ////чтение суточных
            if (components.Contains("Day"))
            {
               
            }

            return MakeResult(0, DeviceError.NO_ERROR, "");
        }
        
        #endregion

    }
}

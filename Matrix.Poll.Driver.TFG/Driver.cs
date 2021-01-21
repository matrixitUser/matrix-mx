// !!! СНАЧАЛА ВЫБЕРИТЕ, ДЛЯ КАКОЙ СИСТЕМЫ ВЫ ХОТИТЕ СОБРАТЬ ДРАЙВЕР !!!
// закомментируйте следующую строку, если вы хотите собрать драйвер для системы 3.1.1 и выше
//#define OLD_DRIVER

using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.TFG
{
    /// <summary>
    /// Драйвер для TFG(CFG,РГА100/300 - может быть)
    /// </summary>
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

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

        //Канал / сетевой адрес (по умолчанию 1) 
        byte Channel = 1;
        //Логический номер (по умолчанию НЕТ)
        byte? LogicNumber = null;

        private List<Param> CurrentParam = new List<Param>
        {
            new Param("Расход газа", "м³/ч"),
            new Param("Температура газа", "°C"),
            new Param("Давление газа", "МПа"),
            new Param("Напр. на анем.", "В"),
            new Param("Напр. на термом.", "В"),
            new Param("Ток датч. давления", ""),
            new Param("Число Рейнольдса", ""),
            new Param("Номер диапазона", ""),
            new Param("Дата, время", "")
        };

        private List<Param> CurrentLongParam = new List<Param>
        {
            new Param("Расход газа", "м³/ч"),
            new Param("Температура газа", "°C"),
            new Param("Давление газа", "МПа"),
            new Param("Объём сумм.", "м³"),
            new Param("Время работы", "сек"),
            new Param("Время простоя", "сек")
        };

        private List<Param> DiapParam = new List<Param>
        {
            new Param("Qmin", "м³/ч"),
            new Param("Qmax", "м³/ч"),
            new Param("Qотс", "м³/ч"),
            new Param("Qдог(НС ПП)", "м³/ч"),
            new Param("Qдог(q<Qmin)", "м³/ч"),
            new Param("Qдог(p<Pотс)", "м³/ч")
        };

        private List<Param> HourParam = new List<Param>
        {
            new Param("Vн", "м³"),
            new Param("T", "°C"),
            new Param("P", "МПа"),
            new Param("Код НС", ""),
            new Param("Время НС", "мин")
        };

        private List<Param> DayParam = new List<Param>
        {
            new Param("Объём за сутки", "м³"),
            new Param("Ср. температура", "°С"),
            new Param("Ср. давление", "МПа"),
            new Param("Объём восст.", "м³"),
            new Param("Время НС", "мин"),
        };

        private List<Param> CurrentModbusParam = new List<Param>
        {
            new Param("Расход приведенный к стандартным условиям", ""),
            new Param("Температура газа", "°C"),
            new Param("Давление газа", "МПа"),
            new Param("Напр. на анем.", "В"),
            new Param("Напр. на термом.", "В"),
            new Param("Ток датч. давления", ""),
            new Param("Число Рейнольдса", ""),
            new Param("Номер диапазона", ""),
            new Param("Дата, время", "")
        };

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

        /// <summary>
        /// возвращает значение контрактного часа
        /// </summary>
        [Import("getContractHour")]
        private Func<int> getContractHour;

        /// <summary>
        /// задает значение контрактного часа
        /// </summary>
        [Import("setContractHour")]
        private Action<int> setContractHour;

        /// <summary>
        /// разница между временем на приборе и системным
        /// ('-' спешит, '+' отстает)
        /// </summary>
        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            //Параметры вычислителя
            if (!param.ContainsKey("channel") || !byte.TryParse(arg.channel.ToString(), out Channel))
            {
                log(string.Format("Отсутствуют сведения о канале/сетевом номере, выбран по умолчанию {0}", Channel));
            }

            byte ln = 1;
            if (!param.ContainsKey("logicNumber") || !byte.TryParse(arg.logicNumber.ToString(), out ln))
            {
                log(string.Format("Отсутствуют сведения о логическом номере, включен режим Modbus"));
            }
            else
            {
                LogicNumber = ln;
            }

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

            //Параметры опроса
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

            //Начало опроса
            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        result = All(components);
                        break;
                    case "ping":
                        result = Ping();
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, ex.Message);
            }

            return result;
        }
        #endregion

        #region Common

        private byte[] SendSimple(byte[] data, int timeout = 7500, int wait = 6)
        {
            var buffer = new List<byte>();

            //log(string.Format("Попытка {0}", attempts + 1));
            log(string.Format(">({1}){0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), level: 3);

            response();
            request(data);

            var to = timeout;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((to -= sleep) > 0 && !isCollected)
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
                        if (waitCollected == wait)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            log(string.Format("<({1}){0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), level: 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.proto = TFG_Proto.UNKNOWN;

            byte[] buffer = null;

            for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
            {
                buffer = SendSimple(data);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                }
                else
                {
                    if (buffer.Length < 3)
                    {
                        answer.error = "в кадре ответа не может содержаться менее 3 байт";
                    }
                    else
                    {
                        if (Crc.Check(buffer, new Crc16Modbus())) // протокол типа Модбас
                        {
                            if (buffer.Length < 5)
                            {
                                answer.error = "в кадре ответа не может содежаться менее 5 байт";
                            }
                            else if ((buffer[1] < 0x80) && (buffer[1] != data[1]))
                            {
                                answer.error = "пришёл ответ на другой запрос";
                            }
                            else
                            {
                                answer.proto = TFG_Proto.MODBUS;
                                answer.success = true;
                            }
                        }
                        else if ((buffer.Sum(r => r) & 0xff) == 0) //Собственный протокол
                        {
                            if ((buffer[0] != data[0]) || (buffer[1] != data[1]))
                            {
                                answer.error = "пришёл ответ, не соответствующий запросу";
                            }
                            else                            //два варианта запроса
                            {
                                if (buffer[0] == Channel)   //<channel|func|data|crc>
                                {
                                    answer.proto = TFG_Proto.SHORT;
                                    answer.success = true;
                                    answer.error = string.Empty;
                                }
                                else                        //<func|logicnumber|channel|data|crc>
                                {
                                    if (buffer[2] != data[2])
                                    {
                                        answer.error = "пришёл ответ, не соответствующий запросу";
                                    }
                                    else
                                    {
                                        answer.proto = TFG_Proto.LONG;
                                        answer.success = true;
                                        answer.error = string.Empty;
                                    }
                                }
                            }
                        }
                        else
                        {
                            answer.error = "контрольная сумма кадра не сошлась";
                        }
                    }
                }
            }

            if (answer.success)
            {
                switch ((TFG_Proto)answer.proto)
                {
                    case TFG_Proto.SHORT:                     //<channel|func|data|crc>
                        answer.Func = buffer[1];
                        answer.Body = buffer.Skip(2).Take(buffer.Count() - 3).ToArray();
                        break;
                    case TFG_Proto.LONG:                     //<func|logicnumber|channel|data|crc>
                        answer.Func = buffer[0];
                        answer.Body = buffer.Skip(3).Take(buffer.Count() - 4).ToArray();
                        break;
                    case TFG_Proto.MODBUS:
                        answer.Channel = buffer[0];
                        answer.Func = buffer[1];
                        answer.Length = buffer[2];
                        answer.Body = buffer.Skip(3).Take(buffer.Length - 5).ToArray();
                        answer.data = buffer;

                        //modbus error
                        if (answer.Func > 0x80)
                        {
                            answer.exceptionCode = (ModbusExceptionCode)buffer[2];
                            answer.success = false;
                            answer.error = string.Format("устройство вернуло ошибку: {0}", answer.exceptionCode);
                        }
                        break;
                }
            }

            return answer;
        }

        /*
        private dynamic Send(byte[] dataSend)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;

            byte[] data = null;

            for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
            {
                if (attempts == 0)
                {
                    data = SendSimple(dataSend, 10000, 3);
                }
                else if (attempts == 1)
                {
                    data = SendSimple(dataSend, 10000, 6);
                }
                else
                {
                    data = SendSimple(dataSend, 10000, 6);
                }

                if (data.Length == 0)
                {
                    answer.error = "Нет ответа";
                }
                else
                {
                    if (data.Length < 5)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 5 байт";
                    }
                    else if (!Crc.Check(data, new Crc16Modbus()))
                    {
                        answer.error = "контрольная сумма кадра не сошлась";
                    }
                    else if ((data[1] < 0x80) && (data[1] != dataSend[1]))
                    {
                        answer.error = "пришёл ответ на другой запрос";
                    }
                    else
                    {
                        answer.success = true;
                    }
                }
            }

            if (answer.success)
            {
                answer.NetworkAddress = data[0];
                answer.Function = data[1];
                answer.Length = data[2];
                answer.Body = data.Skip(3).Take(data.Length - 5).ToArray();
                answer.data = data;

                //modbus error
                if (answer.Function > 0x80)
                {
                    answer.exceptionCode = (ModbusExceptionCode)data[2];
                    answer.success = false;
                    answer.error = string.Format("устройство вернуло ошибку: {0}", answer.exceptionCode);
                }
            }

            return answer;
        }*/

        enum ModbusExceptionCode : byte
        {
            ILLEGAL_FUNCTION = 0x01,
            ILLEGAL_DATA_ADDRESS = 0x02,
            ILLEGAL_DATA_VALUE = 0x03,
            SLAVE_DEVICE_FAILURE = 0x04,
            ACKNOWLEDGE = 0x05,
            SLAVE_DEVICE_BUSY = 0x06,
            MEMORY_PARITY_ERROR = 0x07,
            GATEWAY_PATH_UNAVAILABLE = 0x0a,
            GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
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

        public dynamic MakeResult(int code, string description = "")
        {
            dynamic result = new ExpandoObject();
            result.code = code;
            result.success = code == 0 ? true : false;
            result.description = description;
            return result;
        }
        #endregion

        #region Base
        private enum TFG_Proto
        {
            UNKNOWN,
            SHORT,
            LONG,
            MODBUS
        };

        private byte[] MakeBaseRequest(TFG_Proto proto, byte func, List<byte> Data = null)
        {
            var bytes = new List<byte>();

            switch (proto)
            {
                case TFG_Proto.SHORT:
                    bytes.Add(Channel);
                    bytes.Add(func);
                    break;

                case TFG_Proto.LONG:
                    bytes.Add(func);
                    bytes.Add((byte)LogicNumber);
                    bytes.Add(Channel);
                    break;

                case TFG_Proto.MODBUS:
                    bytes.Add(Channel);
                    bytes.Add(func);
                    break;

            }

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            switch (proto)
            {
                case TFG_Proto.SHORT:
                case TFG_Proto.LONG:
                    {
                        var crc = ((bytes.Sum(r => r) & 0xFF) ^ 0xFF) + 1;
                        bytes.Add((byte)crc);
                    }
                    break;

                case TFG_Proto.MODBUS:
                    {
                        var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
                        bytes.Add(crc.CrcData[0]);
                        bytes.Add(crc.CrcData[1]);
                    }
                    break;

            }

            return bytes.ToArray();
        }

        byte[] MakeHourRequest(DateTime day)
        {
            //log(string.Format("отправка запроса часовых {0:yyyy.MM.dd} () в формате {1:X2} {2:X2} {3:X2}", day, Helper.ToBCD((byte)(day.Year - 2000)), (byte)day.Month, (byte)day.Day));
            return MakeBaseRequest(TFG_Proto.LONG, 0x2A, new List<byte> { Helper.ToBCD((byte)(day.Year - 2000)), Helper.ToBCD((byte)day.Month), Helper.ToBCD((byte)day.Day) });
        }

        byte[] MakeDayRequest(DateTime day)
        {
            return MakeBaseRequest(TFG_Proto.LONG, 0x29, new List<byte> { Helper.ToBCD((byte)(day.Year - 2000)), Helper.ToBCD((byte)day.Month), Helper.ToBCD((byte)day.Day) });
        }

        dynamic ParseNumberResponse(dynamic answer) //proto=SHORT func=25(зав.номер ПП) или func=1f (зав.номер ВРГ)
        {
            if (!answer.success) return answer;

            if (answer.Body.Length != 2)
            {
                answer.Number = 0;
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            answer.Number = Helper.FromBCD(answer.Body[0]) * 100 + Helper.FromBCD(answer.Body[1]);
            return answer;
        }

        dynamic ParseCurrentResponse(dynamic answer) //proto=SHORT func=01
        {
            if (!answer.success) return answer;

            var records = new List<dynamic>();

            if (answer.Body.Length != (7 * 4 + 7))
            {
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            var year = 2000 + Helper.FromBCD(answer.Body[29]);
            var month = Helper.FromBCD(answer.Body[30]);
            var day = Helper.FromBCD(answer.Body[31]);
            var hour = Helper.FromBCD(answer.Body[32]);
            var minute = Helper.FromBCD(answer.Body[33]);
            var second = Helper.FromBCD(answer.Body[34]);
            var date = new DateTime(year, month, day, hour, minute, second);

            for (var i = 0; i < 7; i++)
            {
                var value = Helper.ToSingle(answer.Body, i * 4);
                var name = CurrentParam[i].Name;
                var unit = CurrentParam[i].Unit;
                records.Add(MakeCurrentRecord(name, value, unit, date));
            }

            records.Add(MakeCurrentRecord(CurrentParam[7].Name, answer.Body[28], CurrentParam[7].Unit, date));

            //log(string.Format("(Мгновенные) Время = {0:dd.MM.yyyy HH:mm:ss}", date));
            //foreach (var r in records)
            //{
            //    log(string.Format("(Мгновенные) {0} = {1} {2}", r.s1, r.d1, r.s2));
            //}

            answer.records = records;
            answer.date = date;
            return answer;
        }

        dynamic ParseDiapResponse(dynamic answer, DateTime date) //proto=SHORT func=19
        {
            if (!answer.success) return answer;

            var records = new List<dynamic>();

            if (answer.Body.Length != (6 * 4))
            {
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            for (var i = 0; i < 6; i++)
            {
                var value = Helper.ToSingle(answer.Body, i * 4);
                var name = DiapParam[i].Name;
                var unit = DiapParam[i].Unit;
                records.Add(MakeConstRecord(name, string.Format("{0:0.##} {1}", value, unit), date));
            }

            //foreach (var r in records)
            //{
            //    log(string.Format("(Константа) {0} = {1}", r.s1, r.s2));
            //}

            answer.records = records;
            return answer;
        }


        /*        dynamic ParseCurrentModbusResponse(dynamic answer)
                {
                    if (!answer.success) return answer;

                    var records = new List<dynamic>();

                    if (answer.Body.Length != (14 * 4))
                    {
                        answer.success = false;
                        answer.error = "несовпадение по длине ответа";
                        return answer;
                    }

                    //var year = 2000 + Helper.FromBCD(answer.Body[29]);
                    //var month = Helper.FromBCD(answer.Body[30]);
                    //var day = Helper.FromBCD(answer.Body[31]);
                    //var hour = Helper.FromBCD(answer.Body[32]);
                    //var minute = Helper.FromBCD(answer.Body[33]);
                    //var second = Helper.FromBCD(answer.Body[34]);
                    //var date = new DateTime(year, month, day, hour, minute, second);

                    for (var i = 0; i < 7; i++)
                    {
                        var value = Helper.ToSingle(answer.Body, i * 4);
                        var name = CurrentParam[i].Name;
                        var unit = CurrentParam[i].Unit;
                        records.Add(MakeCurrentRecord(name, value, unit, date));
                    }

                    records.Add(MakeCurrentRecord(CurrentParam[7].Name, answer.Body[28], CurrentParam[7].Unit, date));

                    //log(string.Format("(Мгновенные) Время = {0:dd.MM.yyyy HH:mm:ss}", date));
                    //foreach (var r in records)
                    //{
                    //    log(string.Format("(Мгновенные) {0} = {1} {2}", r.s1, r.d1, r.s2));
                    //}

                    answer.records = records;
                    answer.date = date;
                    return answer;
                }*/


        dynamic ParseCurrentLongResponse(dynamic answer) //proto=LONG func=28
        {
            if (!answer.success) return answer;

            answer.records = new List<dynamic>();

            if (answer.Body.Length != (4 + 6 + 4 * 6))
            {
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            answer.NRSh = Helper.FromBCD(answer.Body[0]) * 100 + Helper.FromBCD(answer.Body[1]);
            answer.NPP = Helper.FromBCD(answer.Body[2]) * 100 + Helper.FromBCD(answer.Body[3]);

            var year = 2000 + Helper.FromBCD(answer.Body[4]);
            var month = Helper.FromBCD(answer.Body[5]);
            var day = Helper.FromBCD(answer.Body[6]);
            var hour = Helper.FromBCD(answer.Body[7]);
            var minute = Helper.FromBCD(answer.Body[8]);
            var second = Helper.FromBCD(answer.Body[9]);
            var date = new DateTime(year, month, day, hour, minute, second);

            for (var i = 0; i < 4; i++)
            {
                var value = Helper.ToSingle(answer.Body, 10 + i * 4);
                var name = CurrentLongParam[i].Name;
                var unit = CurrentLongParam[i].Unit;
                answer.records.Add(MakeCurrentRecord(name, value, unit, date));
            }

            var timeWork = answer.Body[22] + answer.Body[23] * 60 + BitConverter.ToUInt16(answer.Body, 24) * 3600;
            var timeOff = answer.Body[26] + answer.Body[27] * 60 + BitConverter.ToUInt16(answer.Body, 28) * 3600;

            answer.records.Add(MakeCurrentRecord(CurrentLongParam[4].Name, timeWork, CurrentLongParam[4].Unit, date));
            answer.records.Add(MakeCurrentRecord(CurrentLongParam[5].Name, timeOff, CurrentLongParam[5].Unit, date));

            return answer;
        }

        dynamic ParseHourlyResponse(dynamic answer) //proto=LONG func=2A
        {
            if (!answer.success) return answer;

            answer.contractHour = null;
            answer.records = new List<dynamic>();

            if (answer.Body.Length == (10) && answer.Body[9] == 0x03)
            {
                log(string.Format("Записей за {0:dd.MM.yyyy} нет в архиве", new DateTime(2000 + Helper.FromBCD(answer.Body[0]), Helper.FromBCD(answer.Body[1]), Helper.FromBCD(answer.Body[2]))));
                return answer;
            }
            else if (answer.Body.Length != (10 + 24 * 10))
            {
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            var requestYear = 2000 + Helper.FromBCD(answer.Body[0]);
            var requestMonth = Helper.FromBCD(answer.Body[1]);
            var requestDay = Helper.FromBCD(answer.Body[2]);

            var year = 2000 + Helper.FromBCD(answer.Body[3]);
            var month = Helper.FromBCD(answer.Body[4]);
            var day = Helper.FromBCD(answer.Body[5]);
            var hour = Helper.FromBCD(answer.Body[6]);
            var minute = Helper.FromBCD(answer.Body[7]);
            var second = Helper.FromBCD(answer.Body[8]);
            var contractHour = Helper.FromBCD(answer.Body[9]);

            var dateCurrent = new DateTime(year, month, day, hour, minute, second);
            var dateRequest = new DateTime(requestYear, requestMonth, requestDay, contractHour, 0, 0);
            //запрошенная дата + контрактный час, например 05.05.2016 12:00

            for (var i = 0; i < 24; i++) //контрактные сутки(!)
            {
                var date = dateRequest.AddHours(i - 1);//данные по часу [date; date+1ч) i=0 => 11:00-12:00

                if (date.AddHours(1) > dateCurrent) break; //ещё нет данных, например, 12:00 > 11:35

                var offset = 10 + i * 10;
                var Vn = Helper.ToSingle(answer.Body, offset);
                var T = Helper.FromBCD(answer.Body[offset + 4]) + Helper.FromBCD(answer.Body[offset + 5]) / 100;
                var P = Helper.FromBCD(answer.Body[offset + 6]) + Helper.FromBCD(answer.Body[offset + 7]) / 100 + Helper.FromBCD((byte)(answer.Body[offset + 8] & 0xF0)) / 10000;
                var NScode = answer.Body[offset + 8] & 0x0F;
                var NSTime = Helper.FromBCD(answer.Body[offset + 9]);

                //log(string.Format("(Часовые){0:dd.MM HH:mm}: Vn={1} T={2} P={3} НС={4} Время НС={5}", date, Vn, T, P, NScode, NSTime));

                answer.records.Add(MakeHourRecord(HourParam[0].Name, Vn, HourParam[0].Unit, date));
                answer.records.Add(MakeHourRecord(HourParam[1].Name, T, HourParam[1].Unit, date));
                answer.records.Add(MakeHourRecord(HourParam[2].Name, P, HourParam[2].Unit, date));
                answer.records.Add(MakeHourRecord(HourParam[3].Name, NScode, HourParam[3].Unit, date));
                answer.records.Add(MakeHourRecord(HourParam[4].Name, NSTime, HourParam[4].Unit, date));
            }

            answer.contractHour = contractHour;
            return answer;
        }

        dynamic ParseDailyResponse(dynamic answer) //proto=LONG func=2A
        {
            if (!answer.success) return answer;

            var records = new List<dynamic>();

            if (answer.Body.Length == (10) && answer.Body[9] == 0x03)
            {
                log(string.Format("Записи за {0:dd.MM.yyyy} нет в архиве", new DateTime(2000 + Helper.FromBCD(answer.Body[0]), Helper.FromBCD(answer.Body[1]), Helper.FromBCD(answer.Body[2]))));
                answer.contractHour = null;
                answer.records = records;
                return answer;
            }
            else if (answer.Body.Length != (10 + 4 * 4 + 2))
            {
                answer.success = false;
                answer.error = "несовпадение по длине ответа";
                return answer;
            }

            var requestYear = 2000 + Helper.FromBCD(answer.Body[0]);
            var requestMonth = Helper.FromBCD(answer.Body[1]);
            var requestDay = Helper.FromBCD(answer.Body[2]);

            var year = 2000 + Helper.FromBCD(answer.Body[3]);
            var month = Helper.FromBCD(answer.Body[4]);
            var day = Helper.FromBCD(answer.Body[5]);
            var hour = Helper.FromBCD(answer.Body[6]);
            var minute = Helper.FromBCD(answer.Body[7]);
            var second = Helper.FromBCD(answer.Body[8]);

            var contractHour = Helper.FromBCD(answer.Body[9]);

            var dateCurrent = new DateTime(year, month, day, hour, minute, second);

            var date = new DateTime(requestYear, requestMonth, requestDay);
            date = date.AddDays(-1);

            for (var i = 0; i < 4; i++)
            {
                var value = Helper.ToSingle(answer.Body, 10 + i * 4);
                records.Add(MakeDayRecord(DayParam[i].Name, value, DayParam[i].Unit, date));
            }

            var timeNS = Helper.FromBCD(answer.Body[26]) * 60 + Helper.FromBCD(answer.Body[27]);
            records.Add(MakeDayRecord(DayParam[4].Name, timeNS, DayParam[4].Unit, date));

            answer.records = records;
            answer.dateCurrent = dateCurrent;
            answer.contractHour = contractHour;

            return answer;
        }

        #endregion

        #region Интерфейс

        private dynamic Ping()
        {
            var ppNumber = ParseNumberResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x25)));
            if (!ppNumber.success)
            {
                log("Не удалось прочесть зав.номер ПП: " + ppNumber.error, level: 1);
                return MakeResult(101, "Не удалось прочесть зав.номер ПП: " + ppNumber.error);
            }

            var vrgNumber = ParseNumberResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x1F)));
            if (!vrgNumber.success)
            {
                log("Не удалось прочесть зав.номер ВРГ: " + vrgNumber.error, level: 1);
                return MakeResult(101, "Не удалось прочесть зав.номер ВРГ: " + vrgNumber.error);
            }

            log(string.Format("Зав.номер ПП={0}, зав.номер ВРГ={1}", ppNumber.Number, vrgNumber.Number), level: 1);
            return MakeResult(0);
        }

        private dynamic All(string components)
        {
            byte? contractHour = null;

            if (LogicNumber == null)    //Режим модбас
            {
                var timeMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x13, 0x00, 0x08 }));
                if (!timeMb.success)
                {
                    return MakeResult(102, timeMb.error);
                }

                var currentDate = new DateTime(2000 + timeMb.Body[13], timeMb.Body[11], timeMb.Body[9], timeMb.Body[5], timeMb.Body[3], timeMb.Body[1]);
                setTimeDifference(DateTime.Now - currentDate);

                if (getEndDate == null)
                {
                    getEndDate = (type) => currentDate;
                }

                if (components.Contains("Current"))
                {
                    var currentMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 4, new List<byte>() { 0x50, 0x00, 0x00, 0x1c }));
                    if (!currentMb.success)
                    {
                        log(string.Format("Ошибка при считывании текущих: {0}", currentMb.error), level: 1);
                        return MakeResult(102, currentMb.error);
                    }

                    if (currentMb.Body.Length != (0x1c * 2))
                    {
                        log("Ошибка при считывании текущих: несовпадение длины ответа", level: 1);
                        return MakeResult(102, "несовпадение длины ответа");
                    }

                    var currents = new List<dynamic>();
                    currents.Add(MakeCurrentRecord("Расход приведенный к стандартным условиям", Helper.ToSingle(currentMb.Body, 0), "", currentDate));
                    currents.Add(MakeCurrentRecord("Номер диапазона", Helper.ToSingle(currentMb.Body, 4), "", currentDate));
                    currents.Add(MakeCurrentRecord("Избыточное давление газа", Helper.ToSingle(currentMb.Body, 8), "", currentDate));
                    currents.Add(MakeCurrentRecord("Абсолютное давление газа", Helper.ToSingle(currentMb.Body, 12), "", currentDate));
                    currents.Add(MakeCurrentRecord("Температура газа", Helper.ToSingle(currentMb.Body, 16), "", currentDate));
                    currents.Add(MakeCurrentRecord("Температура анемометра", Helper.ToSingle(currentMb.Body, 20), "", currentDate));
                    currents.Add(MakeCurrentRecord("Дельта", Helper.ToSingle(currentMb.Body, 24), "", currentDate));
                    currents.Add(MakeCurrentRecord("Число Рейнольдса", Helper.ToSingle(currentMb.Body, 28), "", currentDate));
                    currents.Add(MakeCurrentRecord("MgnModBus.Up", Helper.ToSingle(currentMb.Body, 32), "", currentDate));
                    currents.Add(MakeCurrentRecord("Ток термоанемометра", Helper.ToSingle(currentMb.Body, 36), "", currentDate));
                    currents.Add(MakeCurrentRecord("Сопротивление термометра", Helper.ToSingle(currentMb.Body, 40), "", currentDate));
                    currents.Add(MakeCurrentRecord("Напряжение термоанемометра", Helper.ToSingle(currentMb.Body, 44), "", currentDate));
                    currents.Add(MakeCurrentRecord("32-битное слово НС", Helper.ToUInt32(currentMb.Body, 48), "", currentDate));
                    currents.Add(MakeCurrentRecord("32-битное слово предупреждений", Helper.ToUInt32(currentMb.Body, 52), "", currentDate));

                    records(currents);
                    log(string.Format("Текущие на {0} прочитаны: всего {1}", currentDate, currents.Count), level: 1);
                }
                else
                {
                    log(string.Format("Текущее время на приборе: {0}", currentDate), level: 1);
                }

                if (components.Contains("Constant"))
                {
                    //-> 01 03 40 00 00 13 11 c7
                    //< -01 03(26)(00 0a)(00 01) 00 0a 0e 10(00 01)(00 01)(00 01)(ff ff ff bf)(00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00) b3 f0

                    //Расчётные сутки 1
                    //Кол - во каналов для архивации    1
                    //Логический номер ВРГ    1
                    //Логический номер ПП 1
                    //Управляющий регистр 0xffffffbf
                    //Пароль(до 19 символов) 20 ? байт
                    var constantMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x00, 0x00, 0x13 }));
                    if (!constantMb.success)
                    {
                        log(string.Format("Ошибка при считывании констант (1): {0}", constantMb.error), level: 1);
                        return MakeResult(103, constantMb.error);
                    }

                    if (constantMb.Body.Length != (0x13 * 2))
                    {
                        log("Ошибка при считывании констант (1): несовпадение длины ответа", level: 1);
                        return MakeResult(103, "несовпадение длины ответа");
                    }

                    var constants = new List<dynamic>();
                    constants.Add(MakeConstRecord("Расчётные сутки", string.Format("{0}", constantMb.Body[3]), currentDate));
                    constants.Add(MakeConstRecord("Количество каналов для архивации", string.Format("{0}", constantMb.Body[9]), currentDate));
                    constants.Add(MakeConstRecord("Логический номер ВРГ", string.Format("{0}", Helper.ToUInt16(constantMb.Body, 10)), currentDate));
                    constants.Add(MakeConstRecord("Логический номер ПП", string.Format("{0}", Helper.ToUInt16(constantMb.Body, 12)), currentDate));
                    constants.Add(MakeConstRecord("Управляющий регистр", string.Format("0x{0:X8}", Helper.ToUInt32(constantMb.Body, 14)), currentDate));


                    //-> 01 03 40 24 00 0a 90 06
                    //< -01 03 14 (40 c0 00 00)( 44 c8 00 00) (40 00 00 00) (43 17 00 00) (40 c0 00 00) 5d 3b

                    //Минимальный расход, м³/ ч    6
                    //Максимальный расход, м³/ ч   1600
                    //Порог отсечки по расходу, м³/ ч  2
                    //Договорной расход, м³/ ч     151
                    //Договорной минимальный расход, м³/ ч 6
                    constantMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x24, 0x00, 0x0A }));
                    if (!constantMb.success)
                    {
                        log(string.Format("Ошибка при считывании констант (2): {0}", constantMb.error), level: 1);
                        return MakeResult(103, constantMb.error);
                    }

                    if (constantMb.Body.Length != (0x0A * 2))
                    {
                        log("Ошибка при считывании констант (2): несовпадение длины ответа", level: 1);
                        return MakeResult(103, "несовпадение длины ответа");
                    }

                    constants.Add(MakeConstRecord("Минимальный расход, м³/ч", string.Format("{0}", Helper.ToSingle(constantMb.Body, 0)), currentDate));
                    constants.Add(MakeConstRecord("Максимальный расход, м³/ч", string.Format("{0}", Helper.ToSingle(constantMb.Body, 4)), currentDate));
                    constants.Add(MakeConstRecord("Порог отсечки по расходу, м³/ч", string.Format("{0}", Helper.ToSingle(constantMb.Body, 8)), currentDate));
                    constants.Add(MakeConstRecord("Договорной расход, м³/ч", string.Format("{0}", Helper.ToSingle(constantMb.Body, 12)), currentDate));
                    constants.Add(MakeConstRecord("Договорной минимальный расход, м³/ч", string.Format("{0}", Helper.ToSingle(constantMb.Body, 16)), currentDate));


                    //-> 01 03 40 2e 00 28 30 1d
                    //<- 01 03 50 42 04 01 06 41 d4 53 f8 41 70 20 c5 40 00 2d e0 00 00 00 00 3d 94 12 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 41 b8 cc cd 3d 8e f3 4d 3e 52 05 bc 3f 94 e0 76 40 ee 66 66 b1 07 01 04 38 00 00 00 00 3f 80 00 00 3e 99 99 9a 3e cd 7a 79 41 33 5f f8 42 24 fb ff 41 f0 46 d0 40 41 98 00 33 a4 47 0d 3d 16 12 b8 42 d0 7f 17 00 00 00 00 00 00 00 10 00 00 00 00 10 c4

                    //Метан, %	Float32	
                    //Этан, %		Float32
                    //Пропан, %	Float32
                    //н-Бутан, %
                    //2-Метилпропан, %
                    //н-Пентан, %
                    //2-Метилбутан, %
                    //2.2-Диметилпропан, %

                    //н-Гексан, %
                    //н-Гептан, %
                    //Водород, %
                    //Водяной пар, %
                    //Сульфид водорода, %
                    //Гелий, %
                    //Аргон, %
                    //Азот, %
                    //Кислород, %
                    //Диоксид углерода, %
                    //Плотность, кг/м³
                    //Влажность, %
                    constantMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x2E, 0x00, 0x28 }));
                    if (!constantMb.success)
                    {
                        log(string.Format("Ошибка при считывании констант (3): {0}", constantMb.error), level: 1);
                        return MakeResult(103, constantMb.error);
                    }
                    if (constantMb.Body.Length != (0x28 * 2))
                    {
                        log("Ошибка при считывании констант (3): несовпадение длины ответа", level: 1);
                        return MakeResult(103, "несовпадение длины ответа");
                    }

                    constants.Add(MakeConstRecord("Метан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 0)), currentDate));
                    constants.Add(MakeConstRecord("Этан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 4)), currentDate));
                    constants.Add(MakeConstRecord("Пропан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 8)), currentDate));
                    constants.Add(MakeConstRecord("н-Бутан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 12)), currentDate));
                    constants.Add(MakeConstRecord("2-Метилпропан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 16)), currentDate));
                    constants.Add(MakeConstRecord("н-Пентан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 20)), currentDate));
                    constants.Add(MakeConstRecord("2-Метилбутан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 24)), currentDate));
                    constants.Add(MakeConstRecord("2.2-Диметилпропан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 28)), currentDate));
                    constants.Add(MakeConstRecord("н-Гексан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 32)), currentDate));
                    constants.Add(MakeConstRecord("н-Гептан, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 36)), currentDate));
                    constants.Add(MakeConstRecord("Водород, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 40)), currentDate));
                    constants.Add(MakeConstRecord("Водяной пар, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 44)), currentDate));
                    constants.Add(MakeConstRecord("Сульфид водорода, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 48)), currentDate));
                    constants.Add(MakeConstRecord("Гелий, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 52)), currentDate));
                    constants.Add(MakeConstRecord("Аргон, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 56)), currentDate));
                    constants.Add(MakeConstRecord("Азот, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 60)), currentDate));
                    constants.Add(MakeConstRecord("Кислород, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 64)), currentDate));
                    constants.Add(MakeConstRecord("Диоксид углерода, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 68)), currentDate));
                    constants.Add(MakeConstRecord("Плотность, кг/м³", string.Format("{0}", Helper.ToSingle(constantMb.Body, 72)), currentDate));
                    constants.Add(MakeConstRecord("Влажность, %", string.Format("{0}", Helper.ToSingle(constantMb.Body, 76)), currentDate));

                    //4058 12
                    //Диаметр трубы (внутр.), мм
                    //Барометрическое давление, МПа
                    //Договорное давление, МПа
                    //Договорная температура, °С
                    //Максимальная температура, °С
                    //Минимальная температура, °С
                    constantMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x58, 0x00, 0x12 }));
                    if (!constantMb.success)
                    {
                        log(string.Format("Ошибка при считывании констант (4): {0}", constantMb.error), level: 1);
                        return MakeResult(103, constantMb.error);
                    }
                    if (constantMb.Body.Length != (0x12 * 2))
                    {
                        log("Ошибка при считывании констант (4): несовпадение длины ответа", level: 1);
                        return MakeResult(103, "несовпадение длины ответа");
                    }

                    constants.Add(MakeConstRecord("Диаметр трубы (внутр.), мм", string.Format("{0}", Helper.ToSingle(constantMb.Body, 0)), currentDate));
                    constants.Add(MakeConstRecord("Барометрическое давление, МПа", string.Format("{0}", Helper.ToSingle(constantMb.Body, 4)), currentDate));
                    constants.Add(MakeConstRecord("Договорное давление, МПа", string.Format("{0}", Helper.ToSingle(constantMb.Body, 8)), currentDate));
                    constants.Add(MakeConstRecord("Договорная температура, °С", string.Format("{0}", Helper.ToSingle(constantMb.Body, 12)), currentDate));
                    constants.Add(MakeConstRecord("Максимальная температура, °С", string.Format("{0}", Helper.ToSingle(constantMb.Body, 28)), currentDate));
                    constants.Add(MakeConstRecord("Минимальная температура, °С", string.Format("{0}", Helper.ToSingle(constantMb.Body, 32)), currentDate));

                    //-> 01 03 40 6a 00 0c 70 13
                    //< -01 03 18 3c a3 d7 0a 3b 83 12 6f 3c b4 39 58 3b 44 9b a6 3c 23 d7 0a 00 00 00 00 e6 81
                    constantMb = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x6A, 0x00, 0x0C }));
                    if (!constantMb.success)
                    {
                        log(string.Format("Ошибка при считывании констант (5): {0}", constantMb.error), level: 1);
                        return MakeResult(103, constantMb.error);
                    }
                    if (constantMb.Body.Length != (0x0C * 2))
                    {
                        log("Ошибка при считывании констант (5): несовпадение длины ответа", level: 1);
                        return MakeResult(103, "несовпадение длины ответа");
                    }

                    constants.Add(MakeConstRecord("Максимальное значение тока ДД, А", string.Format("{0}", Helper.ToSingle(constantMb.Body, 0)), currentDate));
                    constants.Add(MakeConstRecord("Минимальное значение тока ДД, А", string.Format("{0}", Helper.ToSingle(constantMb.Body, 4)), currentDate));
                    constants.Add(MakeConstRecord("Верхний порог срабатывания НС по току ДД, А", string.Format("{0}", Helper.ToSingle(constantMb.Body, 8)), currentDate));
                    constants.Add(MakeConstRecord("Нижний порог срабатывания НС по току ДД, А", string.Format("{0}", Helper.ToSingle(constantMb.Body, 12)), currentDate));
                    constants.Add(MakeConstRecord("Верхний предел измерения давления ДД, МПа", string.Format("{0}", Helper.ToSingle(constantMb.Body, 16)), currentDate));
                    constants.Add(MakeConstRecord("Нижний предел измерения давления ДД, МПа", string.Format("{0}", Helper.ToSingle(constantMb.Body, 20)), currentDate));

                    //

                    records(constants);
                    log(string.Format("Константы на {0} прочитаны: всего {1}", currentDate, constants.Count), level: 1);
                }

                if (components.Contains("Hour"))
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");
                    var date = startH.Date.AddHours(startH.Hour);

                    //-> 01 03 40 00 00 01 91 ca
                    Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x00, 0x00, 0x01 }));

                    while (date <= endH)
                    {
                        var hours = new List<dynamic>();

                        if (cancel())
                        {
                            log("Ошибка при считывании часовых: опрос отменен", level: 1);
                            return MakeResult(105, "опрос отменен");
                        }

                        if (DateTime.Compare(date.AddHours(1), currentDate) > 0)
                        {
                            log(string.Format("Часовой записи за {0:dd.MM.yyyy HH:00} ещё нет", date));
                            break;
                        }


                        //-> 01 10 40 1b 00 07 0e 00 00 00 00 00 02 00 00 00 0c 00 0a 00 10 4e ec
                        //< -01 10 40 1b 00 07 e4 0c
                        //-> 01 03 10 00 00 2a c0 d5
                        //< -01 03 54 00 0a 00 db 00 90 00 04 00 00 00 02 00 02 00 0c 00 0a 00 10 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 40 ea 72 6a e4 25 bd 00 40 61 a0 08 3e cd 7a 79 11 20 01 68 00 00 00 00 00 00 00 00 00 00 0e 10 00 00 00 00 0e 10 00 00 00 00 00 00 0e 10 69 f7 2d c0


                        var hourWrite = Send(MakeBaseRequest(TFG_Proto.MODBUS, 0x10, new List<byte>() { 0x40, 0x1b, 0x00, 0x07, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)(date.Hour), 0x00, 0x00, 0x00, (byte)(date.Day), 0x00, (byte)(date.Month), 0x00, (byte)(date.Year % 100) }));
                        if (!hourWrite.success)
                        {
                            log(string.Format("Ошибка при считывании(1) часовых: {0}", hourWrite.error), level: 1);
                            return MakeResult(105, hourWrite.error);
                        }

                        var hourRead = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x10, 0x00, 0x00, 0x2a }));
                        if (!hourRead.success)
                        {
                            log(string.Format("Ошибка при считывании(2) часовых: {0}", hourRead.error), level: 1);
                            return MakeResult(105, hourRead.error);
                        }
                        if (hourRead.Length != (0x2a * 2))
                        {
                            log("Ошибка при считывании(2) часовых: несовпадение длины ответа", level: 1);
                            return MakeResult(105, "несовпадение длины ответа");
                        }

                        var hourDate = new DateTime(2000 + hourRead.Body[19], hourRead.Body[17], hourRead.Body[15], hourRead.Body[11], 0, 0);

                        hours.Add(MakeHourRecord("Vн", Helper.ToDouble(hourRead.Body, 20), "м³", hourDate));
                        hours.Add(MakeHourRecord("Vнв", Helper.ToDouble(hourRead.Body, 28), "м³", hourDate));
                        hours.Add(MakeHourRecord("Vн.сум", Helper.ToDouble(hourRead.Body, 36), "м³", hourDate));
                        hours.Add(MakeHourRecord("T", Helper.ToSingle(hourRead.Body, 44), "°C", hourDate));
                        hours.Add(MakeHourRecord("P", Helper.ToSingle(hourRead.Body, 48), "МПа", hourDate));
                        hours.Add(MakeHourRecord("Код НС", Helper.FromBCD(hourRead.Body[52]) * 100 + Helper.FromBCD(hourRead.Body[53]), "", hourDate));
                        hours.Add(MakeHourRecord("Время НС", Helper.ToUInt16(hourRead.Body, 80), "сек", hourDate));

                        /*if (contractHour == null)
                        {
                            contractHour = hour.contractHour;
                        }

                        hours = hour.records;*/

                        log(string.Format("Прочитана часовая запись за {0:dd.MM.yyyy HH:00}", date, hours.Count));
                        records(hours);

                        date = date.AddHours(1);
                    }
                }


                if (components.Contains("Day"))
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");
                    var days = new List<dynamic>();
                    var date = startD.Date;

                    Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x40, 0x01, 0x00, 0x01 }));

                    while (date <= endD)
                    {
                        if (cancel())
                        {
                            log("Ошибка при считывании суточных: опрос отменен", level: 1);
                            return MakeResult(105, "опрос отменен");
                        }

                        if (DateTime.Compare(date, currentDate.Date) > 0)
                        {
                            log(string.Format("Суточных данных за {0:dd.MM.yyyy} ещё нет", date));
                            break;
                        }

                        var dayWrite = Send(MakeBaseRequest(TFG_Proto.MODBUS, 0x10, new List<byte>() { 0x40, 0x1b, 0x00, 0x07, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte)(date.Day), 0x00, (byte)(date.Month), 0x00, (byte)(date.Year % 100) }));
                        if (!dayWrite.success)
                        {
                            log(string.Format("Ошибка при считывании(1) суточных: {0}", dayWrite.error), level: 1);
                            return MakeResult(105, dayWrite.error);
                        }

                        var dayRead = Send(MakeBaseRequest(TFG_Proto.MODBUS, 3, new List<byte>() { 0x20, 0x00, 0x00, 0x2a }));
                        if (!dayRead.success)
                        {
                            log(string.Format("Ошибка при считывании(2) суточных: {0}", dayRead.error), level: 1);
                            return MakeResult(105, dayRead.error);
                        }
                        if (dayRead.Length != (0x2a * 2))
                        {
                            log("Ошибка при считывании(2) суточных: несовпадение длины ответа", level: 1);
                            return MakeResult(105, "несовпадение длины ответа");
                        }

                        var dayDate = new DateTime(2000 + dayRead.Body[19], dayRead.Body[17], dayRead.Body[15], 0, 0, 0);

                        days.Add(MakeDayRecord("Vн", Helper.ToDouble(dayRead.Body, 20), "м³", dayDate));
                        days.Add(MakeDayRecord("Vнв", Helper.ToDouble(dayRead.Body, 28), "м³", dayDate));
                        days.Add(MakeDayRecord("Vн.сум", Helper.ToDouble(dayRead.Body, 36), "м³", dayDate));
                        days.Add(MakeDayRecord("T", Helper.ToSingle(dayRead.Body, 44), "°C", dayDate));
                        days.Add(MakeDayRecord("P", Helper.ToSingle(dayRead.Body, 48), "МПа", dayDate));
                        days.Add(MakeDayRecord("Код НС", Helper.FromBCD(dayRead.Body[52]) * 100 + Helper.FromBCD(dayRead.Body[53]), "", dayDate));
                        days.Add(MakeDayRecord("Время НС", Helper.ToUInt16(dayRead.Body, 80), "сек", dayDate));

                        /*if (contractHour == null)
                        {
                            contractHour = hour.contractHour;
                        }

                        hours = hour.records;*/

                        log(string.Format("Прочитана суточная запись за {0:dd.MM.yyyy}", date, days.Count), level: 1);
                        records(days);


                        date = date.AddDays(1);
                    }
                }

            }
            else
            {
                var current = ParseCurrentResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 1)));
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих: {0}", current.error), level: 1);
                    return MakeResult(102, current.error);
                }

                records(current.records);
                List<dynamic> currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count), level: 1);

                if (getEndDate == null)
                {
                    getEndDate = (type) => current.date;
                }

                ////

                if (components.Contains("Hour"))
                {
                    //чтение часовых
                    var startH = getStartDate("Hour");
                    var endH = getEndDate("Hour");
                    var date = startH.AddDays(-1).Date; //вместо ".AddDays(-1)" можно написать ".AddHours(-contractHour-1)"

                    while (date <= endH.Date)
                    {
                        var hours = new List<dynamic>();

                        if (cancel())
                        {
                            log("Ошибка при считывании часовых: опрос отменен", level: 1);
                            return MakeResult(105, "опрос отменен");
                        }

                        if (DateTime.Compare(date.AddHours(contractHour ?? 0), current.date) > 0)
                        {
                            log(string.Format("Часовых данных за {0:dd.MM.yyyy} ещё нет", date));
                            break;
                        }

                        var hour = ParseHourlyResponse(Send(MakeHourRequest(date)));
                        if (!hour.success)
                        {
                            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
                            return MakeResult(105, hour.error);
                        }

                        if (contractHour == null)
                        {
                            contractHour = hour.contractHour;
                            setContractHour((int)contractHour);
                        }

                        hours = hour.records;

                        log(string.Format("Прочитаны часовые на {0:dd.MM.yyyy}: {1} записей", date, hours.Count), level: 1);
                        records(hours);

                        date = date.AddDays(1);
                    }
                }

                //0000 – все в норме
                //0001 – нет питания
                //0002 – нет связи с ПП
                //0004 – фильтр загрязнен
                //0008 – ненорма САГ
                //0010 – НС датчика t газа
                //0020 – НС ДД
                //0040 – НС АЦП
                //0080 – НС АЦП фильтра
                //0100 – Общий бит наличия НС от ПП
                //0200 – НС Q > Qmax или НС Qотс < Q < Qmin
                //0400 – T > Tmax или T < Tmin

                if (components.Contains("Day"))
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");
                    var days = new List<dynamic>();
                    var date = startD.Date;

                    while (date <= endD.Date)
                    {
                        if (cancel())
                        {
                            log("Ошибка при считывании суточных: опрос отменен", level: 1);
                            return MakeResult(105, "опрос отменен");
                        }

                        if (DateTime.Compare(date.AddHours(contractHour ?? 0), current.date) > 0)
                        {
                            log(string.Format("Суточных данных за {0:dd.MM.yyyy} ещё нет", date));
                            break;
                        }

                        var day = ParseDailyResponse(Send(MakeDayRequest(date)));
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return MakeResult(104, day.error);
                        }

                        if (contractHour == null)
                        {
                            contractHour = day.contractHour;
                            setContractHour((int)contractHour);
                        }

                        days = day.records;

                        log(string.Format("Прочитаны суточные на {0:dd.MM.yyyy}: {1} записей", date, days.Count), level: 1);
                        records(days);

                        date = date.AddDays(1);
                    }
                }

                ////

                if (components.Contains("Constant"))
                {
                    var constants = new List<dynamic>();

                    var par = new List<Param>()
                    {
                        new Param("Зав.номер ПП", ""),
                        new Param("Зав.номер ВРГ", ""),
                        new Param("Диаметр трубы", "мм")
                    };

                    var num = new List<dynamic>()
                    {
                        ParseNumberResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x25))),      //numPP
                        ParseNumberResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x1F))),      //numVRG
                        ParseNumberResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x1D)))       //dia
                    };

                    for (var i = 0; i < 3; i++)
                    {
                        var param = par[i];
                        if (num[i].success == false)
                        {
                            log(string.Format("Не удалось прочитать {0}: {1}", param.Name, num[i].error));
                            continue;
                        }
                        constants.Add(MakeConstRecord(param.Name, string.Format("{0}{1}", num[i].Number, param.Unit), current.date));
                    }

                    var constant = ParseDiapResponse(Send(MakeBaseRequest(TFG_Proto.SHORT, 0x19)), current.date);
                    if (!constant.success)
                    {
                        log(string.Format("Ошибка при считывании констант: {0}", constant.error), level: 1);
                        return MakeResult(103, constant.error);
                    }

                    constants.AddRange(constant.records);

                    if (contractHour != null)
                    {
                        constants.Add(MakeConstRecord("Контрактный час", contractHour, current.date));
                        setContractHour((int)contractHour);
                    }

                    var prsParam = Send(MakeBaseRequest(TFG_Proto.SHORT, 0x0B));
                    if (prsParam.success)
                    {
                        log(string.Format("ВНИМАНИЕ! Параметры давления не удалось считать из вычислителя, обновите драйвер!"));
                    }

                    var gasComposition = Send(MakeBaseRequest(TFG_Proto.LONG, 0x2C));
                    if (gasComposition.success)
                    {
                        log(string.Format("ВНИМАНИЕ! Состав газа не удалось считать из вычислителя, обновите драйвер!"));
                    }

                    //

                    records(constants);
                    log(string.Format("Константы прочитаны: всего {0}", constants.Count), level: 1);
                }

                /////// Нештатные ситуации ///
                ////var lastAbnormal = getLastTime("Abnormal");
                ////DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
                ////DateTime endAbnormal = current.date;

                ////var abnormal = GetAbnormals(startAbnormal, endAbnormal);
                ////if (!abnormal.success)
                ////{
                ////    log(string.Format("ошибка при считывании НС: {0}", abnormal.error));
                ////    return;
                ////}
            }

            return MakeResult(0);
        }
        #endregion
    }

    public class Param
    {
        public string Name { get; private set; }
        public string Unit { get; private set; }

        public Param(string name, string unit)
        {
            Name = name;
            Unit = unit;
        }
    }
}

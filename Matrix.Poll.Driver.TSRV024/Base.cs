using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Matrix.Poll.Driver.TSRV024
{
    public partial class Driver
    {
        #region Send

        private byte[] SendSimple(byte[] data, int timeout = 30000, int wait = 6)
        {
            var buffer = new List<byte>();

            //log(string.Format("Попытка {0}", attempts + 1));
            log(string.Format("-({1})-> {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), 3);

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
            log(string.Format("<-({1})- {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), 3);

            return buffer.ToArray();
        }

        private dynamic Send(byte[] dataSend)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;

            byte[] data = null;

            for (var attempts = 0; (attempts < 2) && (answer.success == false); attempts++)
            {
                //if (attempts == 0)
                //{
                //    data = SendSimple(dataSend, 750, 1);
                //}
                //if (attempts < 2)
                //{
                data = SendSimple(dataSend);//, 15000, 6);
                //}
                //else
                //{
                //    data = SendSimple(dataSend, 10000, 6);
                //}

                if (data.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (data.Length < 5)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 5 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (!Crc.Check(data, new Crc16Modbus()))
                    {
                        answer.error = "контрольная сумма кадра не сошлась";
                        answer.errorcode = DeviceError.CRC_ERROR;
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
                answer.Length = data[3];
                answer.Body = data.Skip(3).Take(data.Length - 5).ToArray();
                answer.data = data;

                //modbus error
                if (answer.Function > 0x80)
                {
                    var exceptionCode = (ModbusExceptionCode)data[2];
                    answer.success = false;
                    answer.error = string.Format("устройство вернуло ошибку: {0}", exceptionCode);
                    answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                }
            }

            return answer;
        }

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

        #endregion


        enum RequestType : byte
        {
            ByIndex = 0,
            ByDate = 1
        }

        /// <summary>
        /// тип архива
        /// см. документация "структура архивов" стр. 1 (таблица)
        /// TODO дополнить всеми типами
        /// </summary>
        enum ArchiveType : short
        {
            /// <summary>
            /// часовой (тс1)
            /// </summary>
            HourlySystem1 = 0,

            /// <summary>
            /// часовой (тс2)
            /// </summary>
            HourlySystem2 = 3,

            /// <summary>
            /// часовой (тс3)
            /// </summary>
            HourlySystem3 = 6,

            /// <summary>
            /// суточный (тс1)
            /// </summary>
            DailySystem1 = 1,

            /// <summary>
            /// суточный (тс2)
            /// </summary>
            DailySystem2 = 4,

            /// <summary>
            /// суточный (тс3)
            /// </summary>
            DailySystem3 = 7,

            MonthSystem1 = 2,
            MonthSystem2 = 5,
            MonthSystem3 = 8,

            HourlySumm = 9,

            DailyTotal = 10,

            /// <summary>
            /// часовой нарастающим итогом
            /// </summary>
            HourlyGrowing = 18,

            /// <summary>
            /// суточный нарастающим итогом
            /// </summary>
            DailyGrowing = 19,

            /// <summary>
            /// месячный нарастающим итогом
            /// </summary>
            MonthlyGrowing = 20
        }

        #region Request

        byte[] MakeRequest(byte function, List<byte> Data = null)
        {
            var data = new List<byte>();
            data.Add(NetworkAddress);
            data.Add(function);

            if (Data != null)
            {
                data.AddRange(Data);
            }

            var crc = Crc.Calc(data.ToArray(), new Crc16Modbus());
            data.Add(crc.CrcData[0]);
            data.Add(crc.CrcData[1]);
            return data.ToArray();
        }

        byte[] MakeRequest17()
        {
            return MakeRequest(17);
        }

        public int StartAddress { get; private set; }
        public int RegisterCount { get; private set; }

        byte[] MakeRequestRegister(int startAddress, int registerCount, byte func = 4)
        {
            var data = new List<byte>();
            data.Add(Helper.GetHighByte(startAddress));
            data.Add(Helper.GetLowByte(startAddress));
            data.Add(Helper.GetHighByte(registerCount));
            data.Add(Helper.GetLowByte(registerCount));
            return MakeRequest(func, data);
        }

        byte[] MakeRequest65(ArchiveType arrayNumber, short recordCount, RequestType requestType, List<byte> addData = null)
        {
            var data = new List<byte>();

            //номер массива
            data.Add(Helper.GetHighByte((short)arrayNumber));
            data.Add(Helper.GetLowByte((short)arrayNumber));

            //количество записей
            data.Add(Helper.GetHighByte(recordCount));
            data.Add(Helper.GetLowByte(recordCount));

            //тип запроса
            data.Add((byte)requestType);

            if (addData != null)
            {
                data.AddRange(addData);
            }

            return MakeRequest(65, data);
        }

        byte[] MakeRequest65ByDate(ArchiveType arrayNumber, DateTime date)
        {
            var data = new List<byte>();

            data.Add((byte)date.Second);
            data.Add((byte)date.Minute);
            data.Add((byte)date.Hour);
            data.Add((byte)date.Day);
            data.Add((byte)date.Month);
            data.Add((byte)(date.Year - 2000));

            return MakeRequest65(arrayNumber, 1, RequestType.ByDate, data);
        }

        byte[] MakeRequestTime()
        {
            return MakeRequestRegister(0x8000, 2);
        }


        #endregion

        #region Response

        dynamic ParseResponse17(dynamic answer)
        {
            if (!answer.success) return answer;

            var x = Encoding.ASCII.GetString(answer.Body);
            var regex = new Regex(@"VZLJOT (..\.?){4}");
            var match = regex.Match(x);

            answer.Version = "???";
            if (match.Success)
            {
                answer.Version = match.Value;
            }

            return answer;
        }


        dynamic ParseResponse4(dynamic answer)
        {
            if (!answer.success) return answer;

            byte[] data = answer.data;
            //

            byte RegisterCount;

            if (data.Length > 3)
            {
                RegisterCount = data[2]; ;
            }
            else
            {
                RegisterCount = 0;
            }

            var RegisterData = new List<byte>(RegisterCount);

            //количество регистров + 5 байт (сет. адрес, функция, длина, 2 црц)
            if (RegisterCount + 5 == data.Length && RegisterCount > 0)
            {
                RegisterData.AddRange(data.Skip(3).Take(RegisterCount));
            }

            //
            answer.RegisterCount = RegisterCount;
            answer.RegisterData = RegisterData;
            return answer;
        }

        private enum TM24Modification
        {
            Normal,
            M,
            Mplus
        }

        private const string TIMEWORK_TS_FORMAT = "Время работы в штатном режиме (ТС {0})";
        private const string TIMEOFF_TS_FORMAT = "Время простоя при пропадании питания (ТС {0})";
        private const string TIMESENSFAIL_TS_FORMAT = "Время простоя из-за отказа датчиков (ТС {0})";
        private const string TIMENS_TS_FORMAT = "Общее время простоя из-за НС (ТС {0})";
        private const string HEAT_TS_TUBE_FORMAT = "Тепло по трубе {1} (ТС {0})";
        private const string MASS_TS_TUBE_FORMAT = "Масса по трубе {1} (ТС {0})";
        private const string VOLUME_TS_TUBE_FORMAT = "Объем по трубе {1} (ТС {0})";
        private const string TEMPERATURE_TS_TUBE_FORMAT = "Температура по трубе {1} (ТС {0})";
        private const string PRESSURE_TS_TUBE_FORMAT = "Давление по трубе {1} (ТС {0})";
        private const string DV_TS_FORMAT = "dV (ТС {0})";
        private const string SCHEME_TS_FORMAT = "Схема потребления (ТС {0})";
        private const string DISABLED_TS_FORMAT = "Запрет работы (ТС {0})";
        private const string NO_COUNT_TS_FORMAT = "Запрет счета (ТС {0})";
        private const string CFGERROR_TS_FORMAT = "Неверная конфигурация (ТС {0})";
        private const string HWSTOP_TS_FORMAT = "Останов ГВС (ТС {0})";
        private const string HEATSTOP_TS_FORMAT = "Останов тепла (ТС {0})";
        private const string SIGNAL_TS_FORMAT = "Состояние сигнала (ТС {0})";
        private const string CONTRACT_TS_FORMAT = "Переход на договор (ТС {0})";
        private const string TSSTOP_TS_FORMAT = "Останов ТС (ТС {0})";

        // часовой, суточный, месячный архивы ТС
        dynamic ParseResponse65(dynamic answer, int channel, string type, TM24Modification tmMod)
        {
            if (!answer.success) return answer;
            int start = 3;
            if ((answer.data as byte[]).Length != (start + 174))
            {
                answer.success = false;
                answer.error = "длина ответа не соответствует ожидаемой";
                return answer;
            }

            var text = "";

            answer.channel = channel;

            var records = new List<dynamic>();

            var length = answer.data[2];

            var seconds = Helper.ToUInt32(answer.data, start + 0);

            //если данные нулевые, игнорим их
            if (seconds == 0) return answer;

            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);

            text += $"{date:dd.MM.yy HH:mm} ";

            if (tmMod == TM24Modification.Normal || tmMod == TM24Modification.M)
            {
                var timeWork = Helper.ToUInt16(answer.data, start + 4);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMEWORK_TS_FORMAT, channel), timeWork, "мин.", date));

                var timeOff = Helper.ToUInt16(answer.data, start + 6);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMEOFF_TS_FORMAT, channel), timeOff, "мин.", date));

                var timeSensFail = Helper.ToUInt16(answer.data, start + 8);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMESENSFAIL_TS_FORMAT, channel), timeSensFail, "мин.", date));

                var timeNs = Helper.ToUInt16(answer.data, start + 10);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMENS_TS_FORMAT, channel), timeNs, "мин.", date));

                UInt16 info = Helper.ToUInt16(answer.data, start + 78);
                byte scheme = (byte)(info & 0x000F);
                bool isDisabled = (info & 0x10) > 0;
                bool isNoCount = (info & 0x20) > 0;
                bool isCfgError = (info & 0x30) > 0;
                byte signal = (byte)((info >> 8) & 0x000F);
                bool tsStop = (info & 0x1000) > 0;
                bool isContract = (info & 0x2000) > 0;
                bool isStopHeat = (info & 0x4000) > 0;
                bool isStopHW = (info & 0x8000) > 0;

                records.Add(MakeDayOrHourRecord(type, string.Format(SCHEME_TS_FORMAT, channel), scheme, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(DISABLED_TS_FORMAT, channel), isDisabled ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(NO_COUNT_TS_FORMAT, channel), isNoCount ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(CFGERROR_TS_FORMAT, channel), isCfgError ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(TSSTOP_TS_FORMAT, channel), tsStop ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(CONTRACT_TS_FORMAT, channel), isContract ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(HEATSTOP_TS_FORMAT, channel), isStopHeat ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(HWSTOP_TS_FORMAT, channel), isStopHW ? 1 : 0, "", date));
                records.Add(MakeDayOrHourRecord(type, string.Format(SIGNAL_TS_FORMAT, channel), signal, "", date));

                for (int i = 0; i < 4; i++)
                {
                    var heat = Helper.ToSingle(answer.data, start + 96 + i * 4);
                    records.Add(MakeDayOrHourRecord(type, string.Format(HEAT_TS_TUBE_FORMAT, channel, i + 1), heat, "ГКал", date));
                    if (records.Any()) text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

                    var mass = Helper.ToSingle(answer.data, start + 112 + i * 4);
                    records.Add(MakeDayOrHourRecord(type, string.Format(MASS_TS_TUBE_FORMAT, channel, i + 1), mass, "т", date));
                    if (records.Any()) text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

                    var volume = Helper.ToSingle(answer.data, start + 128 + i * 4);
                    records.Add(MakeDayOrHourRecord(type, string.Format(VOLUME_TS_TUBE_FORMAT, channel, i + 1), volume, "м3", date));

                    var temperature = Helper.ToInt16(answer.data, start + 152 + i * 2) / 100.0;
                    records.Add(MakeDayOrHourRecord(type, string.Format(TEMPERATURE_TS_TUBE_FORMAT, channel, i + 1), temperature, "°С", date));

                    var pressure = Helper.ToUInt16(answer.data, start + 160 + i * 2) / 1000.0;
                    records.Add(MakeDayOrHourRecord(type, string.Format(PRESSURE_TS_TUBE_FORMAT, channel, i + 1), pressure, "МПа", date));
                }

                var vol1 = Helper.ToSingle(answer.data, start + 128);
                var vol2 = Helper.ToSingle(answer.data, start + 132);
                records.Add(MakeDayOrHourRecord(type, string.Format(DV_TS_FORMAT, channel), vol1 - vol2, "м3", date));
            }
            else
            {
                var timeWork = Helper.ToUInt32(answer.data, start + 4);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMEWORK_TS_FORMAT, channel), timeWork, "мин.", date));

                var timeOff = Helper.ToUInt32(answer.data, start + 8);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMEOFF_TS_FORMAT, channel), timeOff, "мин.", date));

                var timeSensFail = Helper.ToUInt32(answer.data, start + 12);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMESENSFAIL_TS_FORMAT, channel), timeSensFail, "мин.", date));

                var timeNs = Helper.ToUInt32(answer.data, start + 16);
                records.Add(MakeDayOrHourRecord(type, string.Format(TIMENS_TS_FORMAT, channel), timeNs, "мин.", date));

                for (int i = 0; i < 4; i++)
                {
                    var mass = Helper.ToLongAndFloat(answer.data, start + 84 + i * 8);
                    records.Add(MakeDayOrHourRecord(type, string.Format(MASS_TS_TUBE_FORMAT, channel, i + 1), mass, "т", date));
                    if (records.Any()) text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

                    var volume = Helper.ToLongAndFloat(answer.data, start + 116 + i * 8);
                    records.Add(MakeDayOrHourRecord(type, string.Format(VOLUME_TS_TUBE_FORMAT, channel, i + 1), volume, "м3", date));

                    var temperature = Helper.ToInt16(answer.data, start + 152 + i * 2) / 100;
                    records.Add(MakeDayOrHourRecord(type, string.Format(TEMPERATURE_TS_TUBE_FORMAT, channel, i + 1), temperature, "°С", date));

                    var pressure = Helper.ToUInt16(answer.data, start + 160 + i * 2) / 1000;
                    records.Add(MakeDayOrHourRecord(type, string.Format(PRESSURE_TS_TUBE_FORMAT, channel, i + 1), pressure, "МПа", date));
                }

                var vol1 = Helper.ToLongAndFloat(answer.data, start + 116);
                var vol2 = Helper.ToLongAndFloat(answer.data, start + 116 + 8);
                records.Add(MakeDayOrHourRecord(type, string.Format(DV_TS_FORMAT, channel), vol1 - vol2, "м3", date));
            }

            answer.records = records;
            answer.text = text;
            return answer;
        }

        dynamic ParseResponse65Growing(dynamic answer)
        {
            if (!answer.success) return answer;
            int offset = 3;
            if ((answer.data as byte[]).Length != (offset + 80))
            {
                answer.success = false;
                answer.error = "длина ответа не соответствует ожидаемой";
                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                return answer;
            }

            var text = "";
            var records = new List<dynamic>();
            //


            var seconds = Helper.ToUInt32(answer.data, offset + 0);
            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);
            text += $"{date:dd.MM.yy HH:mm} ";

            var heat1 = Helper.ToLongAndFloat(answer.data, offset + 4);
            records.Add(MakeDayRecord("Тепло ТС1", heat1, "ГКал", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

            var heat2 = Helper.ToLongAndFloat(answer.data, offset + 12);
            records.Add(MakeDayRecord("Тепло ТС2", heat2, "ГКал", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

            var heat3 = Helper.ToLongAndFloat(answer.data, offset + 20);
            records.Add(MakeDayRecord("Тепло ТС3", heat3, "ГКал", date));

            var mass1 = Helper.ToLongAndFloat(answer.data, offset + 28);
            records.Add(MakeDayRecord("Масса ТС1", mass1, "т", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

            var mass2 = Helper.ToLongAndFloat(answer.data, offset + 36);
            records.Add(MakeDayRecord("Масса ТС2", mass2, "т", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

            var mass3 = Helper.ToLongAndFloat(answer.data, offset + 44);
            records.Add(MakeDayRecord("Масса ТС3", mass3, "т", date));

            var heathw1 = Helper.ToLongAndFloat(answer.data, offset + 52);
            records.Add(MakeDayRecord("Тепло ГВС ТС1", heathw1, "ГКал", date));

            var heathw2 = Helper.ToLongAndFloat(answer.data, offset + 60);
            records.Add(MakeDayRecord("Тепло ГВС ТС2", heathw2, "ГКал", date));

            var heathw3 = Helper.ToLongAndFloat(answer.data, offset + 68);
            records.Add(MakeDayRecord("Тепло ГВС ТС3", heathw3, "ГКал", date));

            //
            answer.text = text;
            answer.records = records;
            return answer;
        }

        dynamic ParseResponse65Totals(dynamic answer)
        {
            if (!answer.success) return answer;
            int offset = 3;
            if ((answer.data as byte[]).Length != (offset + 20))
            {
                answer.success = false;
                answer.error = "длина ответа не соответствует ожидаемой";
                answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                return answer;
            }

            var text = "";
            var records = new List<dynamic>();
            //

            var seconds = Helper.ToUInt32(answer.data, offset + 0);
            var date = new DateTime(1970, 1, 1).AddSeconds(seconds);
            text += $"{date:dd.MM.yy HH:mm} ";

            var totalHeat = Helper.ToSingle(answer.data, offset + 4);
            records.Add(MakeDayRecord("Итоговое тепло", totalHeat, "ГКал", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);
            
            var totalMass = Helper.ToSingle(answer.data, offset + 8);
            records.Add(MakeDayRecord("Итоговая масса", totalMass, "т", date));
            text += string.Format("{0}={1:0.###}; ", records.Last().s1, records.Last().d1);

            var temperature = Helper.ToInt16(answer.data, offset + 14);
            records.Add(MakeDayRecord("Температура наружнего воздуха", temperature / 100.0, "°С", date));

            //
            answer.text = text;
            answer.records = records;
            return answer;
        }


        dynamic ParseResponseTime(dynamic answer)
        {
            var rsp = ParseResponse4(answer);
            if (!rsp.success) return rsp;

            var s = Helper.ToUInt32(rsp.RegisterData, 0);
            rsp.date = new DateTime(1970, 1, 1).AddSeconds(s);

            return rsp;
        }

        dynamic ParseResponseLongFloat(dynamic answer)
        {
            var rsp = ParseResponse4(answer);
            if (!rsp.success) return rsp;

            var values = new List<double>();
            for (var offset = 0; offset < rsp.RegisterCount; offset += 8)
            {
                values.Add(Helper.ToLongAndFloat(((List<byte>)rsp.RegisterData).ToArray(), offset));
            }

            rsp.values = values;
            return rsp;
        }

        dynamic ParseResponseFloat(dynamic answer)
        {
            var rsp = ParseResponse4(answer);
            if (!rsp.success) return rsp;

            var values = new List<double>();
            for (var offset = 0; offset < rsp.RegisterCount; offset += 4)
            {
                values.Add(Helper.ToSingle(((List<byte>)rsp.RegisterData).ToArray(), offset));
            }

            rsp.values = values;
            return rsp;
        }


        dynamic ParseResponseWord(dynamic answer)
        {
            var rsp = ParseResponse4(answer);
            if (!rsp.success) return rsp;

            var values = new List<double>();
            for (var offset = 0; offset < rsp.RegisterCount; offset += 4)
            {
                values.Add(Helper.ToUInt32(((List<byte>)rsp.RegisterData).ToArray(), offset));
            }

            rsp.values = values;
            return rsp;
        }

        dynamic ParseResponseUInt16(dynamic answer)
        {
            var rsp = ParseResponse4(answer);
            if (!rsp.success) return rsp;

            var values = new List<UInt16>();
            for (var offset = 0; offset < rsp.RegisterCount; offset += 2)
            {
                values.Add(Helper.ToUInt16(((List<byte>)rsp.RegisterData).ToArray(), offset));
            }

            rsp.values = values;
            return rsp;
        }

        #endregion
    }
}

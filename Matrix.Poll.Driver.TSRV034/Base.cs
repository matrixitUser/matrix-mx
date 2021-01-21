using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Matrix.Poll.Driver.TSRV034
{
    public partial class Driver
    {
        #region Send

        private byte[] SendSimple(byte[] data, int timeout = 7500, int wait = 6)
        {
            var buffer = new List<byte>();

            //log(string.Format("Попытка {0}", attempts + 1));
            if (debugMode)
            {
                log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            }

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

            if (debugMode)
            {
                log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            }

            return buffer.ToArray();
        }

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

            /// <summary>
            /// Часовой нарастающим итогом
            /// </summary>
            HourlyGrowing = 18,

            /// <summary>
            /// ИВК-10x часовой
            /// </summary>
            HourlyIvk = 1,

            /// <summary>
            /// ИВК-10x суточный
            /// </summary>
            DailyIvk = 2
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

        public int StartAddress { get; private set; }
        public int RegisterCount { get; private set; }

        byte[] MakeRegisterRequest(int logicAddress, int registerCount)//4 00072 => request 3 addr = 71 or 47h
        {
            var data = new List<byte>();

            var requestFunction = (logicAddress / 100000) == 4 ? (byte)0x03 : (byte)0x04;
            var requestAddress = (logicAddress % 100000) - 1;

            data.Add(Helper.GetHighByte(requestAddress));
            data.Add(Helper.GetLowByte(requestAddress));
            data.Add(Helper.GetHighByte(registerCount));
            data.Add(Helper.GetLowByte(registerCount));

            return MakeRequest(requestFunction, data);
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
        /*
        byte[] MakeRequestTime()
        {
            return MakeRequest4(0x8000, 2);
        }
        */

        #endregion

        #region Response

        dynamic ParseResponse17(dynamic answer)
        {
            if (!answer.success) return answer;

            var x = Encoding.ASCII.GetString(answer.Body);
            var regex = new Regex(@"VZLJOT (..\.?){4}");
            var match = regex.Match(x);

            answer.Version = x;
            if (match.Success)
            {
                answer.Version = match.Value;
            }

            answer.isIvk = (((string)answer.Version).ToUpper() == "VZLJOT 82.01.91.11");
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
                RegisterCount = data[2];
            }
            else
            {
                RegisterCount = 0;
            }

            var RegisterData = new List<byte>(RegisterCount);

            if (RegisterCount + 3 == data.Length && RegisterCount > 0)
            {
                RegisterData.AddRange(data.Skip(3).Take(RegisterCount));
            }

            //
            answer.RegisterCount = RegisterCount;
            answer.RegisterData = RegisterData;
            return answer;
        }


        dynamic ParseResponseRegisterAsByte(dynamic answer)
        {
            if (!answer.success) return answer;

            byte[] body = answer.Body;

            if (answer.Length != body.Length)
            {
                answer.success = false;
                answer.error = "слишком короткий ответ";
                return answer;
            }

            return answer;
        }

        enum VzljotDevice
        {
            TSRV034,
            TSRV043,
            Ivk,
            Unknown
        };

        dynamic ParseResponse65(dynamic answer, dynamic properties, string type, bool isIvk)
        {
            if (!answer.success) return answer;

            VzljotDevice device = VzljotDevice.Unknown;

            var text = "";

            var records = new List<dynamic>();

            var length = answer.data[2];

            //log(string.Format("длина ответа на ф.65 = {0}", length));

            int start = 3;

            var seconds = Helper.ToUInt32(answer.data, start + 0);

            //если данные нулевые, игнорим их
            if (seconds == 0) return answer;

            DateTime date = new DateTime(1970, 1, 1).AddSeconds(seconds);

            
            //text += string.Format("{0:dd.MM.yy HH:mm} ", date);

            if(device == VzljotDevice.TSRV043)
            {

            }
            //ТСРВ-034
            else if (isIvk == false)
            {
                var heat1 = Helper.ToUInt32(answer.data, start + 4) / 4.1868 * 0.001;
                records.Add(MakeDayOrHourRecord(type, "Потребленное тепло W4", heat1, "ГКал", date));

                var heat2 = Helper.ToUInt32(answer.data, start + 8) / 4.1868 * 0.001;
                records.Add(MakeDayOrHourRecord(type, "Потребленное тепло W5", heat2, "ГКал", date));
                //text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                var heat3 = Helper.ToUInt32(answer.data, start + 12) / 4.1868 * 0.001;
                records.Add(MakeDayOrHourRecord(type, "Потребленное тепло W6", heat3, "ГКал", date));


                var cons1 = Helper.ToUInt32(answer.data, start + 16) * 0.001;
                if (properties.IsMassByChannel1)
                {
                    records.Add(MakeDayOrHourRecord(type, "Масса по каналу 1", cons1, "т", date));
                }
                else
                {
                    records.Add(MakeDayOrHourRecord(type, "Объем по каналу 1", cons1, "м3", date));
                }
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                var cons2 = Helper.ToUInt32(answer.data, start + 20) * 0.001;
                if (properties.IsMassByChannel2)
                {
                    records.Add(MakeDayOrHourRecord(type, "Масса по каналу 2", cons2, "т", date));
                }
                else
                {
                    records.Add(MakeDayOrHourRecord(type, "Объем по каналу 2", cons2, "м3", date));
                }
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                var cons3 = Helper.ToUInt32(answer.data, start + 24) * 0.001;
                if (properties.IsMassByChannel3)
                {
                    records.Add(MakeDayOrHourRecord(type, "Масса по каналу 3", cons3, "т", date));
                }
                else
                {
                    records.Add(MakeDayOrHourRecord(type, "Объем по каналу 3", cons3, "м3", date));
                }
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);


                var temperature1 = Helper.ToInt16(answer.data, start + 28) * 0.01;
                records.Add(MakeDayOrHourRecord(type, "Средняя температура 1 за интервал", temperature1, "°С", date));
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                var temperature2 = Helper.ToInt16(answer.data, start + 30) * 0.01;
                records.Add(MakeDayOrHourRecord(type, "Средняя температура 2 за интервал", temperature2, "°С", date));

                var temperature3 = Helper.ToInt16(answer.data, start + 32) * 0.01;
                records.Add(MakeDayOrHourRecord(type, "Средняя температура 3 за интервал", temperature3, "°С", date));


                var timeWork = Helper.ToUInt32(answer.data, start + 36);
                records.Add(MakeDayOrHourRecord(type, "Полное время наработки", timeWork, "сек.", date));
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                var timeOff = Helper.ToUInt32(answer.data, start + 40);
                records.Add(MakeDayOrHourRecord(type, "Полное время простоя", timeOff, "сек.", date));
            }
            else // ИВК-102
            {
                //var inx = Helper.ToUInt16(answer.data, start + 4);
                //records.Add(MakeDayOrHourRecord(type, "Индекс архивной записи", inx, "", date));

                records.Add(MakeDayOrHourRecord(type, "Среднее давление за интервал", Helper.ToUInt16(answer.data, start + 6), "кПа", date));
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                records.Add(MakeDayOrHourRecord(type, "Суммарный рабочий измеренный объём 1", Helper.ToUInt32(answer.data, start + 8), "л", date));
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                records.Add(MakeDayOrHourRecord(type, "Суммарный рабочий измеренный объём 2", Helper.ToUInt32(answer.data, start + 12), "л", date));
                text += string.Format("{0}={1}; ", records.Last().s1, records.Last().d1);

                records.Add(MakeDayOrHourRecord(type, "Состояние измерений за интервал", Helper.ToUInt16(answer.data, start + 16), "", date));

                if (type == "Hour")
                {
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 1 ниже минимума", answer.data[start + 18], "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 1 выше максимума", answer.data[start + 19], "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время расхода 2 ниже минимума", answer.data[start + 20], "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 2 выше максимума", answer.data[start + 21], "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время давления ниже минимума для выхода 1", answer.data[start + 22], "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время давления выше максимума для выхода 2", answer.data[start + 23], "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время давления ниже минимума для выхода 2", answer.data[start + 24], "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время давления выше максимума для выхода 2", answer.data[start + 25], "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Состояние системы за интервал", answer.data[start + 26], "", date));
                }
                else
                {
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 1 ниже минимума", Helper.ToUInt16(answer.data, start + 18), "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 1 выше максимума", Helper.ToUInt16(answer.data, start + 20), "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время расхода 2 ниже минимума", Helper.ToUInt16(answer.data, start + 22), "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время расхода 2 выше максимума", Helper.ToUInt16(answer.data, start + 24), "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время давления ниже минимума для выхода 1", Helper.ToUInt16(answer.data, start + 26), "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время давления выше максимума для выхода 2", Helper.ToUInt16(answer.data, start + 28), "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Время давления ниже минимума для выхода 2", Helper.ToUInt16(answer.data, start + 30), "мин", date));
                    records.Add(MakeDayOrHourRecord(type, "Время давления выше максимума для выхода 2", Helper.ToUInt16(answer.data, start + 32), "мин", date));

                    records.Add(MakeDayOrHourRecord(type, "Состояние системы за интервал", answer.data[start + 34], "", date));
                }
            }

            answer.date = date;
            answer.records = records;
            answer.text = text;
            return answer;
        }

        /*
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
        }*/

        #endregion
    }
}

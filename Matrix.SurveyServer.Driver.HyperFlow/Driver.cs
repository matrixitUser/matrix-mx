using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.HyperFlow
{
    public partial class Driver
    {
        /// <summary>
        /// число попыток опроса в случае неуспеха
        /// </summary>
        private const int TRY_COUNT = 4;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            try
            {
                var parameters = (IDictionary<string, object>)arg;

                byte na = 1;
                if (!parameters.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out na))
                {
                    log(string.Format("не указан сетевой адрес, принят по-умолчанию {0}", na));
                }
                else
                    log(string.Format("используется сетевой адрес {0}", na));

                if (parameters.ContainsKey("start") && arg.start is DateTime)
                {
                    getStartDate = (type) => (DateTime)arg.start;
                    log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
                }
                else
                {
                    getStartDate = (type) => getLastTime(type);
                    log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
                }

                if (parameters.ContainsKey("end") && arg.end is DateTime)
                {
                    getEndDate = (type) => (DateTime)arg.end;
                    log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
                }
                else
                {
                    getEndDate = null;
                    log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
                }

                var components = "Hour;Day;Constant;Abnormal;Current";
                if (parameters.ContainsKey("components"))
                {
                    components = arg.components;
                    log(string.Format("указаны архивы {0}", components));
                }
                else
                    log(string.Format("архивы не указаны, будут опрошены все"));

                switch (what.ToLower())
                {
                    case "all": return All(na, components);
                    default:
                        {
                            log(string.Format("неопознаная команда {0}", what));
                            return MakeResult(201, what);
                        }
                }
            }
            catch (Exception ex)
            {
                log(string.Format("ошибка: {0} {1}", ex.Message, ex.StackTrace));
                return MakeResult(999, ex.Message);
            }
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        private dynamic All(byte na, string components)
        {
            //1. текущие
            var current = GetCurrents(na);
            if (!current.success)
            {
                log(string.Format("текущие не прочитаны, {0}", current.error));
                return MakeResult(102);
            }
            log(string.Format("текущие прочитаны, дата {0:dd.MM.yyyy HH:mm:ss}", current.date));
            records(current.records);

            if (getEndDate == null)
                getEndDate = (type) => current.date;

            DateTime currentDate = current.date;
            setTimeDifference(DateTime.Now - currentDate);

            //2. константы
            #region Константы

            ///необходимо заново прочесть константы
            var needRead = false;

            int contractHour = getContractHour();

            if (contractHour == -1) needRead = true;

            if (needRead || components.Contains("Constant"))
            {
                var constas = GetConstants(na, current.date);
                if (!constas.success)
                {
                    log(string.Format("константы не прочитаны, {0}", constas.error));
                    return MakeResult(103);
                }

                records(constas.records);
                contractHour = (int)constas.contractHour;
                setContractHour(contractHour);
                log(string.Format("константы прочитаны, контрактный час {0}", constas.contractHour));
            }
            else
            {
                log(string.Format("константы получены из БД, контрактный час {0}", contractHour));
            }

            #endregion

            #region Сутки
            if (components.Contains("Day"))
            {
                //сутки
                var lastDay = getLastTime("Day");
                var index = (int)(currentDate.Date - lastDay.Date).TotalDays;
                while (index-- > 0)
                {
                    if (cancel()) return MakeResult(200);

                    var day = GetDays(na, index);
                    if (day.success)
                    {
                        records(day.records);
                        log(string.Format("прочитана суточная запись #{0} дата {1:dd.MM.yyyy}", index, day.date));
                    }
                    else
                    {
                        log(string.Format("суточная запись #{0} не прочитана, {1}", index, day.error));
                    }
                }
            }
            #endregion

            #region Часы

            if (components.Contains("Hour"))
            {
                //часы
                var lastHour = getLastTime("Hour");
                var index = (int)(currentDate - lastHour).TotalHours;
                while (index-- > 0)
                {
                    if (cancel()) return MakeResult(200);

                    var hour = GetHour(na, index);
                    if (hour.success)
                    {
                        records(hour.records);
                        log(string.Format("прочитана часовая запись #{0} дата {1:dd.MM.yyyy HH:mm}", index, hour.date));
                    }
                    else
                    {
                        log(string.Format("часовая запись #{0} не прочитана, {1}", index, hour.error));
                    }
                }
            }
            #endregion

            #region НС

            if (cancel()) return MakeResult(200);

            if (components.Contains("Abnormal"))
            {
                //события
                var lastEvent = getLastTime("Abnormal");
                DateTime newEvent = DateTime.MinValue;
                var index = 0;
                do
                {
                    var events = GetEvents(na, index++);
                    if (!events.success)
                    {
                        log(string.Format("событие #{0} не прочитано, {1}", index, events.error));
                        break;
                    }
                    records(events.records);
                    log(string.Format("событие #{0} прочитано", index));
                    newEvent = events.date;
                } while (lastEvent < newEvent);
            }

            #endregion

            return MakeResult(0, "опрос успешно завершен");
        }

        private byte[] MakeRequest(Direction direction, byte networkAddress, byte command, byte[] data)
        {
            var bytes = new List<byte>();

            bytes.Add((byte)direction);
            bytes.Add(networkAddress);
            bytes.Add(command);
            bytes.Add((byte)data.Length);
            bytes.AddRange(data);

            var crc = CalcHartCrc(bytes.ToArray());
            bytes.Add(crc);

            for (int i = 0; i < 8; i++)
            {
                bytes.Insert(0, 0xff);
            }

            return bytes.ToArray();
        }

        private dynamic ParseResponse(byte[] data)
        {
            dynamic response = new ExpandoObject();
            response.success = true;
            //убираем преамбулу
            var clearData = data.SkipWhile(b => b == 0xff).ToArray();
            response.direction = (Direction)clearData[0];
            response.networkAddress = clearData[1];
            response.command = clearData[2];
            response.length = (byte)(clearData[3] - (byte)2);
            response.status = BitConverter.ToUInt16(clearData, 4);
            response.body = clearData.Skip(6).Take(clearData.Length - (6 + 1)).ToArray();

            return response;
        }

        private dynamic ParseAsFloat(byte[] data)
        {
            dynamic resp = ParseResponse(data);
            resp.value = BitConverter.ToSingle(resp.body, 2);
            return resp;
        }

        private dynamic ParseAsULong(byte[] data)
        {
            dynamic resp = ParseResponse(data);
            resp.value = BitConverter.ToUInt32(resp.body, 2);
            return resp;
        }

        private dynamic ParseAsULong2(byte[] data)
        {
            dynamic resp = ParseResponse(data);

            var hLong = BitConverter.ToUInt32(resp.body, 2);
            var lLong = BitConverter.ToUInt32(resp.body, 6 + 2);
            resp.value = hLong * 1000 + lLong / 100000;
            return resp;
        }
    }
}

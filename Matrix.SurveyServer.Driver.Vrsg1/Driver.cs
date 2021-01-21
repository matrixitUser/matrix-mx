using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Vrsg1
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
            var parameters = (IDictionary<string, object>)arg;

            byte na = 0x00;
            if (!parameters.ContainsKey("networkAddress") || !byte.TryParse(parameters["networkAddress"].ToString(), out na))
            {
                log(string.Format("отсутствутют сведения о сетевом адресе"));
                return MakeResult(202, "сетевой адрес");
            }
            else
                log(string.Format("используется сетевой адрес {0}", na));


            byte channel = 0x01;
            if (!parameters.ContainsKey("channel") || !byte.TryParse(parameters["channel"].ToString(), out channel))
                log(string.Format("отсутствутют сведения о канале, принят по-умолчанию {0}", channel));
            else
                log(string.Format("используется канал {0}", channel));

            byte password = 0x00;
            if (!parameters.ContainsKey("password") || !byte.TryParse(parameters["password"].ToString(), out password))
                log(string.Format("отсутствутют сведения о пароле, принят по-умолчанию {0}", password));
            else
                log(string.Format("используется пароль {0}", password));

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
            {
                log(string.Format("архивы не указаны, будут опрошены все"));
            }

            switch (what.ToLower())
            {
                case "all": return All(na, channel, password, components);
                default:
                    {
                        log(string.Format("неопознаная команда {0}", what));
                        return MakeResult(201, what);
                    }
            }
        }

        private dynamic All(byte na, byte ch, byte pass, string components)
        {
            try
            {
                #region Текущие

                dynamic current = new ExpandoObject();
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel()) return MakeResult(200);

                    current = GetCurrent(na, ch, pass);
                    if (current.success) break;
                    log(string.Format("текущие параметры не получены, ошибка: {0}", current.error));
                }
                if (!current.success) return MakeResult(102);
                log(string.Format("текущие параметры получены, время вычислителя: {0:dd.MM.yy HH:mm:ss}", current.date));

                var contractHour = current.contractHour;
                setContractHour(contractHour);

                if (getEndDate == null) getEndDate = (type) => current.date;
                records(current.records);

                DateTime currentDate = current.date;
                setTimeDifference(DateTime.Now - currentDate);

                #endregion

                #region Часы

                if(components.Contains("Hour") || components.Contains("Day"))
                {
                    var startHour = getStartDate("Hour").AddHours(-contractHour).Date;
                    var endHour = getEndDate("Hour");

                    var archiveHour = startHour.AddHours(contractHour);
                    List<dynamic> hours = new List<dynamic>();

                    log(string.Format("начат опрос часовых {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm}", startHour, endHour));
                    for (var date = startHour; date < endHour; date = date.AddDays(1))
                    {
                        byte mode = 0; // режим чтения сначала
                        do
                        {
                            dynamic hour = null;
                            for (int i = 0; i < TRY_COUNT; i++)
                            {
                                if (cancel()) return MakeRes(true, "отмена");
                                hour = GetHour(na, ch, pass, date, mode);
                                if (hour.success)
                                    break;

                                if (hour.n == 0)
                                {
                                    log(string.Format("опрос часовых архивов за сутки {0:dd.MM.yy} завершен", date));
                                    break;
                                }

                                log(string.Format("часовая запись {0:dd.MM.yy HH:00} не получена, ошибка: {1}", archiveHour, hour.error));
                                mode = 2;   // режим повторного чтения архива
                            }

                            if (!hour.success)
                            {
                                if (hour.n == 0) break;
                                else return MakeRes(false, "часовой архив не получен");
                            }

                            mode = 1; // режим чтения следующего архива
                            records(hour.records);
                            hours.AddRange(hour.records);

                            if (hour.n == 1)
                            {
                                log(string.Format("часовая запись {0:dd.MM.yy HH:00} получена", (hour.dates as List<DateTime>).First()));
                            }
                            else
                            {
                                log(string.Format("часовые записи {0:dd.MM.yy HH:mm} — {1:dd.MM.yy HH:mm} получены", (hour.dates as List<DateTime>).Min(), (hour.dates as List<DateTime>).Max()));
                            }

                            archiveHour = (hour.dates as List<DateTime>).Last().AddHours(1);

                        } while (true);

                        if (date >= current.date.AddHours(-contractHour).Date)
                        {
                            log(string.Format("суточные архивы за {0:dd.MM.yyyy} еще не сформированы", date));
                            break;
                        }
                        records(CalcDay(hours, contractHour, date));
                    }
                }


                #endregion

                #region НС
                if (cancel()) return MakeResult(200);

                if (components.Contains("Abnormal"))
                {
                    var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
                    var startAbnormal = lastAbnormal.AddHours(-contractHour).Date;

                    var endAbnormal = getEndDate("Abnormal");
                    byte[] codes = new byte[] { };
                    log(string.Format("начато чтение архивов НС с {0:dd.MM.yy HH:mm}", startAbnormal));
                    for (var date = startAbnormal; date < endAbnormal; date = date.AddDays(1))
                    {
                        if (date >= current.date)
                        {
                            log(string.Format("данные по НС за {0:dd.MM.yyyy} еще не собраны", date));
                            break;
                        }

                        byte mode = 0; // режим чтения сначала
                        do
                        {
                            dynamic abnormal = null;
                            for (int i = 0; i < TRY_COUNT; i++)
                            {
                                if (cancel()) return MakeRes(true, "отмена");

                                abnormal = GetAbnormal(na, ch, pass, mode, date, codes);
                                if (abnormal.success)
                                {
                                    // режим чтения следующего архива
                                    mode = 1; break;
                                }

                                if (abnormal.n == 0)
                                {
                                    log(string.Format("завершено чтение архивов НС за {0:dd.MM.yy HH:00}", date)); break;
                                }

                                log(string.Format("записи НС за {1:dd.MM.yy} не получены, причина: {0}", abnormal.error, date));
                                mode = 2;   // режим повторного чтения архива
                            }

                            if (!abnormal.success)
                                if (abnormal.n == 0) break;
                                else return MakeRes(false, "нештатные записи не получены");

                            codes = abnormal.codes;

                            if (abnormal.records.Count > 0)
                            {
                                var rec = (abnormal.records as IEnumerable<dynamic>).Where(r => r.date > lastAbnormal).ToArray();
                                if (rec.Length > 0)
                                {
                                    log(string.Format("получено {0} записей НС за {1:dd.MM.yy}", rec.Length, date));
                                    records(rec);
                                }
                            }

                        } while (true);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
            }
            return MakeRes(true, "опрос успешно завершен");
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        enum ModbusExceptionCode : byte
        {
            ILLEGAL_FUNCTION = 0x01,
            ILLEGAL_DATA_ADDRESS = 0x02,
            ILLEGAL_DATA_VALUE = 0x03,
            FAILURE_IN_ASSOCIATED_DEVICEE = 0x04,
            ACKNOWLEDGE = 0x05,
            SLAVE_DEVICE_BUSY = 0x06,
            MEMORY_PARITY_ERROR = 0x07,
            GATEWAY_PATH_UNAVAILABLE = 0x0a,
            GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
        }

        private string GetModbusException(ModbusExceptionCode code)
        {
            switch (code)
            {
                case ModbusExceptionCode.ILLEGAL_FUNCTION: return "ILLEGAL_FUNCTION";
                case ModbusExceptionCode.ILLEGAL_DATA_ADDRESS: return "ILLEGAL_DATA_ADDRESS";
                case ModbusExceptionCode.ILLEGAL_DATA_VALUE: return "ILLEGAL_DATA_VALUE";
                case ModbusExceptionCode.FAILURE_IN_ASSOCIATED_DEVICEE: return "FAILURE_IN_ASSOCIATED_DEVICEE";
                case ModbusExceptionCode.ACKNOWLEDGE: return "ACKNOWLEDGE";
                case ModbusExceptionCode.SLAVE_DEVICE_BUSY: return "SLAVE_DEVICE_BUSY";
                case ModbusExceptionCode.MEMORY_PARITY_ERROR: return "MEMORY_PARITY_ERROR";
                case ModbusExceptionCode.GATEWAY_PATH_UNAVAILABLE: return "GATEWAY_PATH_UNAVAILABLE";
                case ModbusExceptionCode.GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND: return "GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND";
                default: return "ошибка не известна";
            }
        }

        private dynamic MakeRes(bool success, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.success = success;
            res.description = description;
            return res;
        }
    }
}

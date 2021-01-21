using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.VKT9
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        private byte networkAddress;
        private string password;

        private enum ArchiveType
        {
            Hourly = 0,
            Daily,
            Monthly,
            DailyTotal,
            Abnormal,
            OperatorLog,
            MonthlyTotal
        }

        #region Common

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

        private dynamic Send(byte[] data, int attempts = 1)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = Device.Error.NO_ERROR;

            byte[] buffer = null;

            for (var attempt = 0; attempt < attempts && answer.success == false; attempt++)
            {
                buffer = Device.Send(data, response, request, log);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = Device.Error.NO_ANSWER;
                }
                else
                {
                    if (buffer.Length < 4)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 4 байт";
                        answer.errorcode = Device.Error.TOO_SHORT_ANSWER;
                    }
                    else if (buffer[0] != networkAddress)
                    {
                        answer.error = "Несовпадение сетевого адреса";
                        answer.errorcode = Device.Error.ADDRESS_ERROR;
                    }
                    else if (!Crc.Check(buffer, new Crc16Modbus()))
                    {
                        answer.error = "контрольная сумма кадра не сошлась";
                        answer.errorcode = Device.Error.CRC_ERROR;
                    }
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = Device.Error.NO_ERROR;
                    }
                }
            }

            if (answer.success)
            {
                answer.NetworkAddress = buffer[0];
                answer.Function = buffer[1];
                answer.Body = buffer.Skip(2).Take(buffer.Count() - 4).ToArray();

                //modbus error
                if (answer.Function >= 0x80)
                {
                    answer.errorcode = Device.Error.DEVICE_EXCEPTION;
                    answer.success = false;
                    switch (buffer[2])
                    {
                        case 0x00:
                            answer.error = "Общая ошибка (без конкретизации причины)";
                            break;

                        case 0x01:
                            answer.error = "Недопустимый(неподдерживаемый) номер функции";
                            break;

                        case 0x02:
                            answer.error = "Недопустимый(неверный) номер регистра";
                            break;

                        case 0x03:
                            answer.error = "Недопустимое значение в поле данных";
                            break;

                        case 0x04:
                            answer.error = "Внутренняя ошибка прибора";
                            break;

                        case 0x05:
                            answer.error = "Запущена долговременная операция";
                            break;

                        case 0x06:
                            answer.error = "Устройство занято выполнением долговременной операции";
                            break;

                        case 0x07:
                            answer.error = "Доступ к регистру закрыт";
                            answer.errorcode = Device.Error.ACCESS_DENIED;
                            break;

                        default:
                            answer.error = "Неизвестный вид ошибки (возможно, драйвер устарел)";
                            break;
                    }
                }
            }

            return answer;
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

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out networkAddress))
            {
                log("Отсутствуют сведения о сетевом адресе", level: 1);
                return Device.MakeResult(202, Device.Error.NO_ERROR, "сетевой адрес");
            }
            #endregion

            //#region KTr
            //if (!param.ContainsKey("KTr") || !double.TryParse(arg.KTr.ToString(), out KTr))
            //{
            //    log(string.Format("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию {0}", KTr));
            //}
            //#endregion

            #region password
            if (!param.ContainsKey("password"))
            {
                log("Отсутствуют сведения о пароле, принят по-умолчанию");
            }
            else
            {
                password = arg.password;
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

            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components, hourRanges, dayRanges), password);
                        }
                        break;

                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = Device.MakeResult(201, Device.Error.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = Device.MakeResult(201, Device.Error.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func, string password)
        {
            return func();
        }
        #endregion


        private dynamic GetConstants(DateTime date)
        {
            if (cancel())
            {
                log("Ошибка при считывании констант: опрос отменен", level: 1);
                return Device.MakeResult(103, Device.Error.NO_ERROR, "опрос отменен");
            }

            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = Device.Error.NO_ERROR;
            var records = new List<dynamic>();

            // 30494 Режим работы прибора unsigned char [1] 0 - Работа; 1 - Настройка; 2 - Поверка; 3 - Калибровка
            // 40001 Серийный номер unsigned long [2]
            // 40012 Идентификатор объекта char array [8] Настройка
            //Строка символов, состоящая из
            //букв и цифр длиной 16 байт или
            //заканчивающаяся 0
            //40020 Код организации char array [8] Настройка
            //40028 Договор char array [8] Настройка
            //40036 Адрес char array [8] Настройка
            dynamic reg;
            reg = ParseReadRegisterResponse(Send(MakeReadRegisterByLAddressRequest(40001, 2)));
            if (reg.success)
            {
                records.Add(Device.MakeConstRecord("Серийный номер", $"{Device.ToUInt32(reg.data as byte[], 0)}", date));
            }

            constant.records = records;
            //constant.text = text;
            return constant;
        }

        private dynamic GetCurrents(DateTime date)
        {
            if (cancel())
            {
                log("Ошибка при считывании текущих: опрос отменен", level: 1);
                return Device.MakeResult(102, Device.Error.NO_ERROR, "опрос отменен");
            }

            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = Device.Error.NO_ERROR;

            var records = new List<dynamic>();

            //ИТОГИ (дата=итого)
            //Общие параметры
            //Дата |Qобщ, Гкал| Tвкл, чч:мм |Tэп, чч:мм |tвозд, °C |tхв, °C |Pхв, МПа |Аппаратные НС| Общие НС
            //ТС1,2
            //Дата |Схема |Qо, Гкал |M1, т| M2, т| M1R, т| M2R, т| dM, т| V1, м3| V2, м3| V1R, м3| V2R, м3|  t1, °C |t2, °C |dt1, °C |P1, МПа |P2, МПа |Tраб.ТС, чч: мм| Tост.ТС, чч: мм |Tраб.шт, чч: мм| Tmin, чч:мм| Tmax, чч:мм| Tdt, чч:мм |Tф, чч:мм| Tпуст.тр, чч: мм |Канальные НС| НС ТС
            //Дополнительные каналы
            //Дата |V7, м3 |V8, м3| V9, м3| Tраб.7, чч:мм| Tраб.8, чч:мм| Tраб.9, чч:мм |Дополнит. НС
            //
            //ТЕКУЩИЕ ПАРАМЕТРЫ
            //Общие параметры
            //Дата |Qобщ, Гкал |Wобщ, Гкал/ч| Tвкл, чч:мм| Tэп, чч:мм| tвозд, °C |tхв, °C |Pхв, МПа |Аппаратные НС |Общие НС |Флаги дискр.вых
            //Tепловая система 1,2
            //Дата |Схема |Qо, Гкал |M1, т |M2, т |M1R, т |M2R, т| dM, т |V1, м3| V2, м3 |V1R, м3 |V2R, м3 |t1, °C |t2, °C |dt1, °C |P1, МПа |P2, МПа| Tраб.ТС, чч:мм| Tост.ТС, чч:мм| Tраб.шт, чч:мм |Tmin, чч:мм |Tmax, чч:мм |Tdt, чч:мм |Tф, чч:мм |Tпуст.тр, чч:мм
            //Дата |Wо, Гкал/ч |Gm1, т/ч |Gm2, т/ч |Gv1, м3/ч| Gv2, м3/ч |Канальные НС| НС ТС
            //Дополнительные каналы
            //Дата |V7, м3| V8, м3 |V9, м3| Tраб.7, чч:мм| Tраб.8, чч:мм |Tраб.9, чч:мм |Дополнит. НС
            //Дата |Gv7, м3/ч| Gv8, м3/ч| Gv9, м3/ч
            //
            //СУТОЧНЫЙ АРХИВ
            //Дата |Qобщ, Гкал| Tвкл, чч:мм |Tэп, чч:мм |tвозд, °C |tхв, °C |Pхв, МПа |Аппаратные НС| Общие НС

            bool isGDj = false;
            dynamic reg;
            reg = ParseReadRegisterResponse(Send(MakeReadRegisterByLAddressRequest(30042, 17)));
            if (reg.success)
            {
                isGDj = Device.ToUInt16(reg.data, 16 * 2) > 0;
                records.Add(Device.MakeCurrentRecord("Qобщ", Device.ToLongAndFloat(reg.data, 0), isGDj ? "ГДж" : "Гкал", date));
                records.Add(Device.MakeCurrentRecord("Wобщ", Device.ToSingle(reg.data, 4 * 2), isGDj ? "ГДж/ч" : "Гкал/ч", date));
                records.Add(Device.MakeCurrentRecord("Tвкл", Device.ToUInt32(reg.data, 6 * 2), "мин", date));
                records.Add(Device.MakeCurrentRecord("Tэп", Device.ToUInt32(reg.data, 8 * 2), "мин", date));
                records.Add(Device.MakeCurrentRecord("tхв", (double)Device.ToInt16(reg.data, 10 * 2) / 100.0, "°C", date));
                records.Add(Device.MakeCurrentRecord("Pхв", (double)Device.ToUInt16(reg.data, 11 * 2) / 10000.0, "МПа", date));
                records.Add(Device.MakeCurrentRecord("tвозд", (double)Device.ToInt16(reg.data, 12 * 2) / 100.0, "°C", date));
                records.Add(Device.MakeCurrentRecord("Аппаратные НС", Device.ToUInt16(reg.data, 13 * 2), "", date));
                records.Add(Device.MakeCurrentRecord("Общие НС", Device.ToUInt16(reg.data, 14 * 2), "", date));
                records.Add(Device.MakeCurrentRecord("Флаги дискр.вых", Device.ToUInt16(reg.data, 15 * 2), "", date));
                log("Прочитаны общие текущие параметры");
            }
            else
            {
                log($"Не удалось прочитать общие текущие параметры: {reg.error}");
            }

            for (int i = 0; i < 2; i++)
            {
                int tsNum = i + 1;
                string postfix = $" (ТС {tsNum})";
                UInt16 startReg = (UInt16)(30119 + i * 124);
                reg = ParseReadRegisterResponse(Send(MakeReadRegisterByLAddressRequest(startReg, 108)));
                if (reg.success)
                {
                    //Дата |Схема |Qо, Гкал |M1, т |M2, т |M1R, т |M2R, т| dM, т |V1, м3| V2, м3 |V1R, м3 |V2R, м3 |t1, °C |t2, °C |dt1, °C |P1, МПа |P2, МПа| Tраб.ТС, чч:мм| Tост.ТС, чч:мм| Tраб.шт, чч:мм |Tmin, чч:мм |Tmax, чч:мм |Tdt, чч:мм |Tф, чч:мм |Tпуст.тр, чч:мм
                    records.Add(Device.MakeCurrentRecord("Qо" + postfix, Device.ToLongAndFloat(reg.data, 0), isGDj ? "ГДж" : "Гкал", date));
                    records.Add(Device.MakeCurrentRecord("Qгвс" + postfix, Device.ToLongAndFloat(reg.data, 4 * 2), isGDj ? "ГДж" : "Гкал", date));
                    records.Add(Device.MakeCurrentRecord("M1" + postfix, Device.ToLongAndFloat(reg.data, 8 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("M2" + postfix, Device.ToLongAndFloat(reg.data, 12 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("M3" + postfix, Device.ToLongAndFloat(reg.data, 16 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("M1R" + postfix, Device.ToLongAndFloat(reg.data, 20 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("M2R" + postfix, Device.ToLongAndFloat(reg.data, 24 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("M3R" + postfix, Device.ToLongAndFloat(reg.data, 28 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("dM(dV)" + postfix, Device.ToLongAndFloat(reg.data, 32 * 2), "т", date));
                    records.Add(Device.MakeCurrentRecord("V1" + postfix, Device.ToLongAndFloat(reg.data, 36 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("V2" + postfix, Device.ToLongAndFloat(reg.data, 40 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("V3" + postfix, Device.ToLongAndFloat(reg.data, 44 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("V1R" + postfix, Device.ToLongAndFloat(reg.data, 48 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("V2R" + postfix, Device.ToLongAndFloat(reg.data, 52 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("V3R" + postfix, Device.ToLongAndFloat(reg.data, 56 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("Wо" + postfix, Device.ToSingle(reg.data, 60 * 2), isGDj ? "ГДж/ч" : "Гкал/ч", date));
                    records.Add(Device.MakeCurrentRecord("Wгвс" + postfix, Device.ToSingle(reg.data, 62 * 2), isGDj ? "ГДж/ч" : "Гкал/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gm1" + postfix, Device.ToSingle(reg.data, 64 * 2), "т/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gm2" + postfix, Device.ToSingle(reg.data, 66 * 2), "т/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gm3" + postfix, Device.ToSingle(reg.data, 68 * 2), "т/ч", date));
                    records.Add(Device.MakeCurrentRecord("dGm" + postfix, Device.ToSingle(reg.data, 70 * 2), "т/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gv1" + postfix, Device.ToSingle(reg.data, 72 * 2), "м3/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gv2" + postfix, Device.ToSingle(reg.data, 74 * 2), "м3/ч", date));
                    records.Add(Device.MakeCurrentRecord("Gv3" + postfix, Device.ToSingle(reg.data, 76 * 2), "м3/ч", date));
                    records.Add(Device.MakeCurrentRecord("Tраб" + postfix, Device.ToUInt32(reg.data, 78 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tост" + postfix, Device.ToUInt32(reg.data, 80 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tраб.шт" + postfix, Device.ToUInt32(reg.data, 82 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tmin" + postfix, Device.ToUInt32(reg.data, 84 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tmax" + postfix, Device.ToUInt32(reg.data, 86 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tdt" + postfix, Device.ToUInt32(reg.data, 88 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tф" + postfix, Device.ToUInt32(reg.data, 90 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Tпуст.тр" + postfix, Device.ToUInt32(reg.data, 92 * 2), "мин", date));
                    records.Add(Device.MakeCurrentRecord("Канальные НС" + postfix, Device.ToUInt32(reg.data, 94 * 2), "", date));
                    records.Add(Device.MakeCurrentRecord("НС ТС" + postfix, Device.ToUInt16(reg.data, 96 * 2), "", date));
                    records.Add(Device.MakeCurrentRecord("t1" + postfix, Device.ToInt16(reg.data, 97 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("t2" + postfix, Device.ToInt16(reg.data, 98 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("t3" + postfix, Device.ToInt16(reg.data, 99 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("dt1" + postfix, Device.ToInt16(reg.data, 100 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("dt2" + postfix, Device.ToInt16(reg.data, 101 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("dt3" + postfix, Device.ToInt16(reg.data, 102 * 2) / 100.0, "°C", date));
                    records.Add(Device.MakeCurrentRecord("P1" + postfix, Device.ToUInt16(reg.data, 103 * 2) / 10000.0, "МПа", date));
                    records.Add(Device.MakeCurrentRecord("P2" + postfix, Device.ToUInt16(reg.data, 104 * 2) / 10000.0, "МПа", date));
                    records.Add(Device.MakeCurrentRecord("P3" + postfix, Device.ToUInt16(reg.data, 105 * 2) / 10000.0, "МПа", date));
                    records.Add(Device.MakeCurrentRecord("Схема измерения" + postfix, Device.ToByte(reg.data, 106 * 2), "", date));
                    records.Add(Device.MakeCurrentRecord("База данных" + postfix, Device.ToByte(reg.data, 107 * 2), "", date));
                    log($"Прочитаны текущие параметры по теплосистеме {tsNum}");
                }
                else
                {
                    log($"Не удалось прочитать текущие параметры по теплосистеме {tsNum}: {reg.error}");
                }
            }

            //Дата |V7, м3| V8, м3 |V9, м3| Tраб.7, чч:мм| Tраб.8, чч:мм |Tраб.9, чч:мм |Дополнит. НС
            //Дата |Gv7, м3/ч| Gv8, м3/ч| Gv9, м3/ч
            reg = ParseReadRegisterResponse(Send(MakeReadRegisterByLAddressRequest(30075, 28)));
            if (reg.success)
            {
                Device.ExtraChType ct7 = (Device.ExtraChType)Device.ToByte(reg.data, 24 * 2);
                Device.ExtraChType ct8 = (Device.ExtraChType)Device.ToByte(reg.data, 25 * 2);
                Device.ExtraChType ct9 = (Device.ExtraChType)Device.ToByte(reg.data, 26 * 2);
                if (ct7 == Device.ExtraChType.Electricity)
                {
                    records.Add(Device.MakeCurrentRecord("E7", Device.ToLongAndFloat(reg.data, 0), "кВт*ч", date));
                    records.Add(Device.MakeCurrentRecord("G7", Device.ToSingle(reg.data, 12 * 2), "кВт", date));
                }
                else if (ct7 == Device.ExtraChType.WaterGas)
                {
                    records.Add(Device.MakeCurrentRecord("V7", Device.ToLongAndFloat(reg.data, 0), "м3", date));
                    records.Add(Device.MakeCurrentRecord("G7", Device.ToSingle(reg.data, 12 * 2), "м3/ч", date));
                }
                if (ct8 == Device.ExtraChType.Electricity)
                {
                    records.Add(Device.MakeCurrentRecord("E8", Device.ToLongAndFloat(reg.data, 4 * 2), "кВт*ч", date));
                    records.Add(Device.MakeCurrentRecord("G8", Device.ToSingle(reg.data, 14 * 2), "кВт", date));
                }
                else if (ct8 == Device.ExtraChType.WaterGas)
                {
                    records.Add(Device.MakeCurrentRecord("V8", Device.ToLongAndFloat(reg.data, 4 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("G8", Device.ToSingle(reg.data, 14 * 2), "м3/ч", date));
                }
                if (ct9 == Device.ExtraChType.Electricity)
                {
                    records.Add(Device.MakeCurrentRecord("E9", Device.ToLongAndFloat(reg.data, 8 * 2), "кВт*ч", date));
                    records.Add(Device.MakeCurrentRecord("G9", Device.ToSingle(reg.data, 16 * 2), "кВт", date));
                }
                else if (ct9 == Device.ExtraChType.WaterGas)
                {
                    records.Add(Device.MakeCurrentRecord("V9", Device.ToLongAndFloat(reg.data, 8 * 2), "м3", date));
                    records.Add(Device.MakeCurrentRecord("G9", Device.ToSingle(reg.data, 16 * 2), "м3/ч", date));
                }
                records.Add(Device.MakeCurrentRecord("Tраб.7", Device.ToUInt32(reg.data, 18 * 2), "мин", date));
                records.Add(Device.MakeCurrentRecord("Tраб.8", Device.ToUInt32(reg.data, 20 * 2), "мин", date));
                records.Add(Device.MakeCurrentRecord("Tраб.9", Device.ToUInt32(reg.data, 22 * 2), "мин", date));
                records.Add(Device.MakeCurrentRecord("Общие НС", Device.ToUInt16(reg.data, 27 * 2), "", date));
                log("Прочитаны текущие параметры по дополнительным каналам");
            }
            else
            {
                log($"Не удалось прочитать текущие параметры по дополнительным каналам: {reg.error}");
            }

            current.records = records;
            //current.text = text;
            return current;
        }


        private dynamic ReadArchive(DateTime start, DateTime end, DateTime currentDate, ArchiveType archiveType, TsArchiveVersion ver)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;
            var recs = new List<dynamic>();

            //

            bool isTypeTotal = archiveType == ArchiveType.DailyTotal || archiveType == ArchiveType.MonthlyTotal;

            //

            UInt16 page;
            DateTime date;
            dynamic startPage = ParseFindArchivePageResponse(Send(MakeFindArchivePageRequest(archiveType, start)));
            if (!startPage.success) return startPage;

            //общие

            page = startPage.pageNumber;
            date = startPage.date;
            bool isArchiveEnd = false;

            while (true)
            {
                if (cancel())
                {
                    log("Ошибка при считывании часовых: опрос отменен", level: 1);
                    return Device.MakeResult(105, Device.Error.NO_ERROR, "опрос отменен");
                }

                var currecs = new List<dynamic>();
                dynamic archivePage = ParseReadArchivePageResponse(Send(MakeReadArchivePageRequest(archiveType, page, 1, readCommon: true)));
                if (!archivePage.success) return archivePage;
                if (archivePage.data[0] == 0xFF && archivePage.data[1] == 0xFF && archivePage.data[2] == 0xFF) { isArchiveEnd = true; break; }

                dynamic common = isTypeTotal ? MakeRecordsFromArchiveDataCommonTotal(archivePage.archiveType, archivePage.data) : MakeRecordsFromArchiveDataCommon(archivePage.archiveType, archivePage.data);
                if (!common.success) return common;
                date = common.date;
                if (date >= end) break;
                
                log($"Прочитаны общие параметры {archiveType} архива за {date}");

                dynamic archivePage1 = ParseReadArchivePageResponse(Send(MakeReadArchivePageRequest(archiveType, page, 1, readTs1: true)));
                if (!archivePage1.success) return archivePage1;
                dynamic ts1 = isTypeTotal ? MakeRecordsFromArchiveDataTsTotal(1, date, archivePage1.archiveType, archivePage1.data, ver) : MakeRecordsFromArchiveDataTs(1, date, archivePage1.archiveType, archivePage1.data, ver);
                if (!ts1.success) return ts1;
                log($"Прочитаны параметры ТС1 {archiveType} архива за {date}");

                dynamic archivePage2 = ParseReadArchivePageResponse(Send(MakeReadArchivePageRequest(archiveType, page, 1, readTs2: true)));
                if (!archivePage2.success) return archivePage2;
                dynamic ts2 = isTypeTotal ? MakeRecordsFromArchiveDataTsTotal(2, date, archivePage2.archiveType, archivePage2.data, ver) : MakeRecordsFromArchiveDataTs(2, date, archivePage2.archiveType, archivePage2.data, ver);
                if (!ts2.success) return ts2;
                log($"Прочитаны параметры ТС2 {archiveType} архива за {date}");

                currecs.AddRange(common.records);
                currecs.AddRange(ts1.records);
                currecs.AddRange(ts2.records);
                recs.AddRange(currecs);
                //records(currecs);
                page = archivePage.nextPage;
            }

            archive.isArchiveEnd = isArchiveEnd;
            archive.records = recs;
            return archive;
        }


        private dynamic GetDays(DateTime start, DateTime end, DateTime currentDate)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;
            var allRecs = new List<dynamic>();

            for (DateTime s = start; s < end; s = s.AddDays(7))
            {
                DateTime e = s.AddDays(7);
                if (e > end) e = end;

                dynamic daily = ReadArchive(s, e, currentDate, ArchiveType.Daily, TsArchiveVersion.v1_1);
                if (!daily.success) return daily;

                dynamic dailyTotal = ReadArchive(s, e, currentDate, ArchiveType.DailyTotal, TsArchiveVersion.v1_1);
                if (!dailyTotal.success) return dailyTotal;
                
                var recs = new List<dynamic>();
                recs.AddRange(daily.records);
                recs.AddRange(dailyTotal.records);
                records(recs);
                allRecs.AddRange(recs);

                log($"Прочитаны суточные с {s:dd.MM.yyyy} по {e:dd.MM.yyyy}: {recs.Count} записей", level: 1);

            if (daily.isArchiveEnd) break;
            }

            archive.records = allRecs;
            return archive;
        }


        private dynamic All(string components, List<dynamic> hourRanges, List<dynamic> dayRanges)
        {
            dynamic curDate = ParseReadTimeResponse(Send(MakeReadTimeRequest()));
            if (!curDate.success)
            {
                log(string.Format("Ошибка при считывании текущей даты на вычислителе: {0}", curDate.error), level: 1);
                return Device.MakeResult(102, curDate.errorcode, curDate.error);
            }

            DateTime date = curDate.date;
            setTimeDifference(DateTime.Now - date);

            log($"Дата/время на вычислителе: {date:dd.MM.yy HH:mm:ss}");

            if (getEndDate == null)
            {
                getEndDate = (type) => date;
            }

            if (components.Contains("Constant"))
            {
                var constants = new List<dynamic>();

                var constant = GetConstants(date);
                if (!constant.success)
                {
                    log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                    return Device.MakeResult(103, constant.errorcode, constant.error);
                }

                constants = constant.records as List<dynamic>;
                log(string.Format("Константы прочитаны: всего {0}", constants.Count));
                records(constants);
            }

            if (components.Contains("Current"))
            {
                var currents = new List<dynamic>();

                var current = GetCurrents(date);
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
                    return Device.MakeResult(102, current.errorcode, current.error);
                }

                currents = current.records;
                log(string.Format("Текущие на {0} прочитаны: всего {1}", date, currents.Count), level: 1);
                records(currents);
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

            //            //            var hour = GetHours(startH, endH, date, properties);
            //            //            if (!hour.success)
            //            //            {
            //            //                log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
            //            //                return Device.MakeResult(105, hour.errorcode, hour.error);
            //            //            }
            //            //            hours = hour.records;


            //            var date = startH.Date.AddHours(startH.Hour);

            //            while (date <= endH)
            //            {
            //                var hour = new List<dynamic>();

            //                if (cancel())
            //                {
            //                    log("Ошибка при считывании часовых: опрос отменен", level: 1);
            //                    return Device.MakeResult(105, Device.Error.NO_ERROR, "опрос отменен");
            //                }

            //                if (DateTime.Compare(date.AddHours(1), currentDate) > 0)
            //                {
            //                    log(string.Format("Часовой записи за {0:dd.MM.yyyy HH:00} ещё нет", date));
            //                    break;
            //                }

            //                hour.Add(Device.MakeHourRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));

            //                hours.AddRange(hour);
            //                log(string.Format("Прочитана часовая запись за {0:dd.MM.yyyy HH:00}", date, hour.Count));
            //                records(hour);
            //                date = date.AddHours(1);
            //            }

            //            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            //        }
            //    }
            //    else
            //    {
            //        //чтение часовых
            //        var startH = getStartDate("Hour");
            //        var endH = getEndDate("Hour");

            //        //        var hour = GetHours(startH, endH, date, properties);
            //        //        if (!hour.success)
            //        //        {
            //        //            log(string.Format("Ошибка при считывании часовых: {0}", hour.error), level: 1);
            //        //            return Device.MakeResult(105, hour.errorcode, hour.error);
            //        //        }
            //        //        hours = hour.records;

            //        var date = startH.Date.AddHours(startH.Hour);

            //        while (date <= endH)
            //        {
            //            var hour = new List<dynamic>();

            //            if (cancel())
            //            {
            //                log("Ошибка при считывании часовых: опрос отменен", level: 1);
            //                return Device.MakeResult(105, Device.Error.NO_ERROR, "опрос отменен");
            //            }

            //            if (DateTime.Compare(date.AddHours(1), currentDate) > 0)
            //            {
            //                log(string.Format("Часовой записи за {0:dd.MM.yyyy HH:00} ещё нет", date));
            //                break;
            //            }

            //            hour.Add(Device.MakeHourRecord("UNIX-секунд", (date - new DateTime(1970, 1, 1)).TotalSeconds, "сек", date));

            //            hours.AddRange(hour);
            //            log(string.Format("Прочитана часовая запись за {0:dd.MM.yyyy HH:00}", date, hour.Count));
            //            records(hour);
            //            date = date.AddHours(1);
            //        }

            //        log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count), level: 1);
            //    }
            //}

            if (components.Contains("Day"))
            {
                List<dynamic> days = new List<dynamic>();
                if (dayRanges != null)
                {
                    foreach (var range in dayRanges)
                    {
                        var startD = range.start;
                        var endD = range.end;

                        if (startD > date) continue;
                        if (endD > date) endD = date;

                        var day = GetDays(startD, endD, date);
                        if (!day.success)
                        {
                            log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                            return Device.MakeResult(104, day.errorcode, day.error);
                        }
                        days = day.records;        
                    }
                }
                else
                {
                    //чтение суточных
                    var startD = getStartDate("Day");
                    var endD = getEndDate("Day");

                    var day = GetDays(startD, endD, date);
                    if (!day.success)
                    {
                        log(string.Format("Ошибка при считывании суточных: {0}", day.error), level: 1);
                        return Device.MakeResult(104, day.errorcode, day.error);
                    }
                    days = day.records;
                    //log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count), level: 1);
                }
            }



            //    /// Нештатные ситуации ///
            //    if (components.Contains("Abnormal"))
            //    {
            //        var lastAbnormal = getStartDate("Abnormal");// getLastTime("Abnormal");
            //        var startAbnormal = lastAbnormal.Date;

            //        var endAbnormal = getEndDate("Abnormal");
            //        byte[] codes = new byte[] { };

            //        List<dynamic> abnormals = new List<dynamic>();

            //        var fakeStart = currentDate.Date.AddDays(-1).AddHours(15).AddMinutes(38).AddSeconds(0);
            //        //var fakeEnd = now.Date.AddDays(-1).AddHours(23).AddMinutes(0).AddSeconds(14);
            //        //var fakeStartOld = now.Date.AddDays(-3).AddHours(15).AddMinutes(17).AddSeconds(19);
            //        //var fakeEndOld = now.Date.AddDays(-2).AddHours(1).AddMinutes(2).AddSeconds(3);

            //        //var fakeStart = new DateTime(2016, 10, 26, 0, 1, 15);
            //        //var fakeEnd = new DateTime(2016, 10, 26, 10, 20, 1);

            //        //if ((endAbnormal >= fakeStart) && (fakeStart >= startAbnormal))
            //        {
            //            abnormals.Add(Device.MakeAbnormalRecord("Критическая ситуация: начало", 0, fakeStart, 1000));
            //            abnormals.Add(Device.MakeAbnormalRecord("Некритичное событие 1", 0, fakeStart.AddSeconds(1), 1));
            //            abnormals.Add(Device.MakeAbnormalRecord("Некритичное событие 2", 0, fakeStart.AddSeconds(2), 2));
            //        }
            //        /*
            //        //if ((endAbnormal >= fakeEnd) && (fakeEnd >= startAbnormal))
            //        {
            //            abnormals.Add(MakeAbnormalRecord("Критическая ситуация: окончание", 0, fakeEnd));
            //        }

            //        //if ((endAbnormal >= fakeStart) && (fakeStart >= startAbnormal))
            //        {
            //            abnormals.Add(MakeAbnormalRecord("Критическая ситуация: начало", 0, fakeStartOld));
            //        }

            //        //if ((endAbnormal >= fakeEnd) && (fakeEnd >= startAbnormal))
            //        {
            //            abnormals.Add(MakeAbnormalRecord("Критическая ситуация: окончание", 0, fakeEndOld));
            //        }*/

            //        log(string.Format("получено {0} записей НС за период", abnormals.Count));//{1:dd.MM.yy}, date));
            //        records(abnormals);

            //    }
            //}
            return Device.MakeResult(0, Device.Error.NO_ERROR, "опрос успешно завершен");
        }

        private byte[] MakeBaseRequest(byte function, byte[] data = null)
        {
            List<byte> result = new List<byte>();
            result.Add(networkAddress);
            result.Add(function);
            if (data != null)
            {
                result.AddRange(data);
            }
            result.AddRange(Crc.Calc(result.ToArray(), new Crc16Modbus()).CrcData);
            return result.ToArray();
        }

        private byte[] MakeReadRegisterByLAddressRequest(UInt32 lAddr, UInt16 count = 1)
        {
            UInt16 register = (UInt16)((lAddr % 10000) - 1);
            byte func;
            if(lAddr / 10000 == 3)
            {
                func = 0x4;
            }
            else
            {
                func = 0x3;
            }
            return MakeReadRegisterRequest(func, register, count);
        }

        //private byte[] MakeReadHoldingRegisterRequest(UInt16 register, UInt16 count = 1)
        //{
        //    return MakeReadRegisterRequest(0x03, register, count);
        //}

        //private byte[] MakeReadInputRegisterRequest(UInt16 register, UInt16 count = 1)
        //{
        //    return MakeReadRegisterRequest(0x04, register, count);
        //}

        //private byte[] MakeWriteHoldingRegisterRequest(UInt16 register, UInt16 data)
        //{
        //    return MakeReadRegisterRequest(0x06, register, data);
        //}

        private byte[] MakeReadRegisterRequest(byte func, UInt16 register, UInt16 data = 1)
        {
            return MakeBaseRequest(func, new byte[] { Device.GetHighByte(register), Device.GetLowByte(register), Device.GetHighByte(data), Device.GetLowByte(data) });
        }

        private byte[] MakeReportSlaveIdRequest()
        {
            return MakeBaseRequest(0x11);
        }

        private dynamic ParseReportSlaveIdResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body as byte[];
            if (answer.Function != 0x11 || body.Length != 8)
            {
                answer.success = false;
                answer.error = "ответ не распознан";
                return answer;
            }
            answer.mnemocode = Encoding.ASCII.GetString(body.Take(4).ToArray());
            answer.revision = BitConverter.ToUInt16(body, 4);
            answer.version = BitConverter.ToUInt16(body, 6);
            return answer;
        }

        private dynamic ParseReadRegisterResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body as byte[];
            if (body.Length < 2 || (body[0] != (body.Length - 1)))
            {
                answer.success = false;
                answer.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой (ReadRegister)";
                answer.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return answer;
            }
            List<UInt16> register = new List<ushort>();
            byte[] data = body.Skip(1).ToArray();
            byte[] revdata = data.Reverse().ToArray();
            //log($"data=[{string.Join(",", data.Select(b => $"{b:X2}"))}]");
            //log($"revdata=[{string.Join(",", revdata.Select(b => $"{b:X2}"))}]");
            //string temp = "register=[";
            for (int i = revdata.Length; i > 1; i -= 2)
            {
                register.Add(BitConverter.ToUInt16(revdata, i - 2));
            //    temp += $"{BitConverter.ToUInt16(revdata, i - 2)};";
            }
            //temp += "]";
            //log(temp);
            answer.data = data;
            answer.revdata = revdata;
            answer.register = register.ToArray();
            return answer;
        }


        //

        private byte[] MakeReadTimeRequest()
        {
            return MakeReadRegisterByLAddressRequest(40003, 6);
        }

        private dynamic ParseReadTimeResponse(dynamic answer)
        {
            dynamic result = ParseReadRegisterResponse(answer);
            if (!result.success) return result;

            UInt16[] register = result.register as UInt16[];
            if (register.Length < 6)
            {
                result.success = false;
                result.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой (ReadTime)";
                result.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return result;
            }
            result.date = new DateTime(year: 2000 + (register[0] % 100), month: register[1], day: register[2], hour: register[3], minute: register[4], second: register[5]);
            return result;
        }

        private byte[] MakeReadArchivePageRequest(ArchiveType type, UInt16 startPage, byte pagesCount, bool directionBackward = false, bool readCommon = false, bool readTs1 = false, bool readTs2 = false)
        {
            byte directionAndMask = (byte)((directionBackward ? 0x01 : 0x00) | (readCommon ? 0x08 : 0x00) | (readTs1 ? 0x10 : 0x00) | (readTs2 ? 0x20 : 0x00));
            return MakeBaseRequest(0x41, new byte[] { (byte)type, directionAndMask, (byte)startPage, (byte)(startPage >> 8), pagesCount });
        }

        private dynamic ParseReadArchivePageResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body as byte[];
            //log($"body=[{string.Join(",", body.Select(b => $"{b:X2}"))}]");
            if (body.Length < 6)
            {
                answer.success = false;
                answer.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой";
                answer.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return answer;
            }

            answer.archiveType = (ArchiveType)body[0];
            answer.directionBackward = (body[1] & 0x01) > 0;
            answer.readCommon = (body[1] & 0x08) > 0;
            answer.readTc1 = (body[1] & 0x10) > 0;
            answer.readTc2 = (body[1] & 0x20) > 0;
            answer.nextPage = BitConverter.ToUInt16(body, 2);
            answer.pagesCount = body[4];
            answer.data = body.Skip(5).ToArray();

            return answer;
        }


        private byte[] MakeFindArchivePageRequest(ArchiveType type, DateTime date)
        {
            return MakeBaseRequest(0x42, new byte[] { (byte)type, (byte)(date.Year % 100), (byte)date.Month, (byte)date.Day });
        }

        private dynamic ParseFindArchivePageResponse(dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body as byte[];

            if (body.Length < 6)
            {
                answer.success = false;
                answer.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой (FindArchivePage)";
                answer.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return answer;
            }

            answer.archiveType = (ArchiveType)body[0];
            answer.date = new DateTime(year: 2000 + (body[1] % 100), month: body[2], day: body[3]);
            answer.pageNumber = BitConverter.ToUInt16(body, 4);

            return answer;
        }

        private dynamic MakeRecordsFromArchiveDataCommon(ArchiveType archiveType, byte[] data, int offset = 0)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;

            //if (!archive.success) return archive;
            //if (!Device.HasProperty(archive, "data") || !Device.HasProperty(archive, "archiveType") || !(archive.data is byte[]) || !(archive.archiveType is ArchiveType))
            //{
            //    archive.success = false;
            //    archive.error = "Не найдены данные для формирования архивных записей";
            //    return archive;
            //}
            //byte[] data = archive.data as byte[];
            //ArchiveType archiveType = archive.archiveType;
            
            if ((data.Length - offset) < 44)
            {
                archive.success = false;
                archive.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой (MakeRecordsFromArchiveDataCommon)";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            string type = null;
            switch (archiveType)
            {
                case ArchiveType.Monthly:
                    type = "Day";
                    break;

                case ArchiveType.Daily:
                    type = "Day";
                    break;

                case ArchiveType.Hourly:
                    type = "Hour";
                    break;

                default: break;
            }
            if (type == null)
            {
                archive.success = false;
                archive.error = $"Ошибка при создании записей типа {archiveType}";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }
            
            //log($"data=[{string.Join(",", data.Select(b => $"{b:X2}"))}]");

            DateTime date = new DateTime(year: 2000 + (data[offset + 0] % 100), month: data[offset + 1], day: data[offset + 2]);
            if(data[offset + 3] < 24)
            {
                date.AddHours(data[offset + 3]);
            }
            List<dynamic> records = new List<dynamic>();
            records.Add(Device.MakeDayOrHourRecord(type, "Qобщ", BitConverter.ToSingle(data, offset + 4), "Гкал", date));
            records.Add(Device.MakeDayOrHourRecord(type, "tхв", (double)BitConverter.ToInt16(data, offset + 8) / 100.0, "°C", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Pхв", (double)BitConverter.ToUInt16(data, offset + 10) / 1000.0, "кгс/см2", date));
            records.Add(Device.MakeDayOrHourRecord(type, "tвозд", (double)BitConverter.ToUInt16(data, offset + 12) / 100.0, "°C", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tвкл", BitConverter.ToUInt16(data, offset + 14), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tвыкл", BitConverter.ToUInt16(data, offset + 16), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Аппаратные НС", BitConverter.ToUInt16(data, offset + 18), "", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Общие НС", BitConverter.ToUInt16(data, offset + 20), "", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V7(E7)", BitConverter.ToSingle(data, offset + 22), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V8(E8)", BitConverter.ToSingle(data, offset + 26), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V9(E9)", BitConverter.ToSingle(data, offset + 30), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V7", BitConverter.ToUInt16(data, offset + 34), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V8", BitConverter.ToUInt16(data, offset + 36), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V9", BitConverter.ToUInt16(data, offset + 38), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Доп.НС", BitConverter.ToUInt16(data, offset + 40), "", date));
            archive.date = date;
            archive.records = records;
            return archive;
        }

        private dynamic MakeRecordsFromArchiveDataCommonTotal(ArchiveType archiveType, byte[] data, int offset = 0)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;

            //if (!archive.success) return archive;
            //if (!Device.HasProperty(archive, "data") || !Device.HasProperty(archive, "archiveType") || !(archive.data is byte[]) || !(archive.archiveType is ArchiveType))
            //{
            //    archive.success = false;
            //    archive.error = "Не найдены данные для формирования архивных записей";
            //    return archive;
            //}
            //byte[] data = archive.data as byte[];
            //ArchiveType archiveType = archive.archiveType;
            if ((data.Length - offset) < 44)
            {
                archive.success = false;
                archive.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            string type = null;
            switch (archiveType)
            {
                case ArchiveType.MonthlyTotal:
                    type = "Day";
                    break;

                case ArchiveType.DailyTotal:
                    type = "Day";
                    break;

                default: break;
            }
            if (type == null)
            {
                archive.success = false;
                archive.error = $"Ошибка при создании записей типа {archiveType}";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            DateTime date = new DateTime(year: 2000 + (data[offset + 0] % 100), month: data[offset + 1], day: data[offset + 2]);
            if (data[offset + 3] < 24)
            {
                date.AddHours(data[offset + 3]);
            }
            List<dynamic> records = new List<dynamic>();
            records.Add(Device.MakeDayOrHourRecord(type, "Qобщ итог.", BitConverter.ToSingle(data, offset + 4), "", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tвкл итог.", BitConverter.ToUInt16(data, offset + 8), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tвыкл итог.", BitConverter.ToUInt16(data, offset + 12), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V7(E7) итог.", BitConverter.ToSingle(data, offset + 16), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V8(E8) итог.", BitConverter.ToSingle(data, offset + 20), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V9(E9) итог.", BitConverter.ToSingle(data, offset + 24), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V7 итог.", BitConverter.ToUInt16(data, offset + 28), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V8 итог.", BitConverter.ToUInt16(data, offset + 32), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб.V9 итог.", BitConverter.ToUInt16(data, offset + 36), "мин", date));
            archive.date = date;
            archive.records = records;
            return archive;
        }

        private enum TsArchiveVersion
        {
            v1_0,
            v1_1
        }

        private dynamic MakeRecordsFromArchiveDataTs(int ts, DateTime date, ArchiveType archiveType, byte[] data, TsArchiveVersion ver, int offset = 0)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;

            string tsPost = $" (ТС{ts})";
            //if (!archive.success) return archive;
            //if (!Device.HasProperty(archive, "data") || !Device.HasProperty(archive, "archiveType") || !(archive.data is byte[]) || !(archive.archiveType is ArchiveType))
            //{
            //    archive.success = false;
            //    archive.error = "Не найдены данные для формирования архивных записей";
            //    return archive;
            //}
            //byte[] data = archive.data as byte[];
            //ArchiveType archiveType = archive.archiveType;
            if ((data.Length - offset) < 106)
            {
                archive.success = false;
                archive.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            string type = null;
            switch (archiveType)
            {
                case ArchiveType.Monthly:
                    type = "Day";
                    break;

                case ArchiveType.Daily:
                    type = "Day";
                    break;

                case ArchiveType.Hourly:
                    type = "Hour";
                    break;

                default: break;
            }
            if (type == null)
            {
                archive.success = false;
                archive.error = $"Ошибка при создании записей типа {archiveType}";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            //DateTime date = new DateTime(year: 2000 + (data[0] % 100), month: data[1], day: data[2], hour: data[3], minute: 0, second: 0);
            List<dynamic> records = new List<dynamic>();
            records.Add(Device.MakeDayOrHourRecord(type, "M1" + tsPost, BitConverter.ToSingle(data, offset + 8), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M2" + tsPost, BitConverter.ToSingle(data, offset + 12), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M3" + tsPost, BitConverter.ToSingle(data, offset + 16), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M1R" + tsPost, BitConverter.ToSingle(data, offset + 20), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M2R" + tsPost, BitConverter.ToSingle(data, offset + 24), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M3R" + tsPost, BitConverter.ToSingle(data, offset + 28), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "dM" + tsPost, BitConverter.ToSingle(data, offset + 32), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V1" + tsPost, BitConverter.ToSingle(data, offset + 36), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V2" + tsPost, BitConverter.ToSingle(data, offset + 40), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V3" + tsPost, BitConverter.ToSingle(data, offset + 44), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V1R" + tsPost, BitConverter.ToSingle(data, offset + 48), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V2R" + tsPost, BitConverter.ToSingle(data, offset + 52), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V3R" + tsPost, BitConverter.ToSingle(data, offset + 56), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "t1" + tsPost, (double)BitConverter.ToInt16(data, offset + 60) / 100.0, "°C", date));
            records.Add(Device.MakeDayOrHourRecord(type, "t2" + tsPost, (double)BitConverter.ToInt16(data, offset + 62) / 100.0, "°C", date));
            records.Add(Device.MakeDayOrHourRecord(type, "t3" + tsPost, (double)BitConverter.ToInt16(data, offset + 64) / 100.0, "°C", date));
            if (ver == TsArchiveVersion.v1_0)
            {
                records.Add(Device.MakeDayOrHourRecord(type, "Qо" + tsPost, BitConverter.ToSingle(data, offset + 0), "Гкал", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Qгвс" + tsPost, BitConverter.ToSingle(data, offset + 4), "Гкал", date));
                records.Add(Device.MakeDayOrHourRecord(type, "t1св" + tsPost, (double)BitConverter.ToInt16(data, offset + 66) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "t2св" + tsPost, (double)BitConverter.ToInt16(data, offset + 68) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "t3св" + tsPost, (double)BitConverter.ToInt16(data, offset + 70) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt1" + tsPost, (double)BitConverter.ToInt16(data, offset + 72) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt2" + tsPost, (double)BitConverter.ToInt16(data, offset + 74) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt3" + tsPost, (double)BitConverter.ToInt16(data, offset + 76) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P1" + tsPost, (double)BitConverter.ToUInt16(data, offset + 78) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P2" + tsPost, (double)BitConverter.ToUInt16(data, offset + 80) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P3" + tsPost, (double)BitConverter.ToUInt16(data, offset + 82) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tраб" + tsPost, BitConverter.ToUInt16(data, offset + 84), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tост" + tsPost, BitConverter.ToUInt16(data, offset + 86), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Кан.НС" + tsPost, BitConverter.ToUInt32(data, offset + 88), "", date));
                records.Add(Device.MakeDayOrHourRecord(type, "НС" + tsPost, BitConverter.ToUInt32(data, offset + 92), "", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Схема" + tsPost, data[94], "", date));
            }
            else
            {
                records.Add(Device.MakeDayOrHourRecord(type, "Qо" + tsPost, BitConverter.ToSingle(data, offset + 0), data[offset + 101] == 0? "Гкал" : "ГДж", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Qгвс" + tsPost, BitConverter.ToSingle(data, offset + 4), data[offset + 101] == 0 ? "Гкал" : "ГДж", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt1" + tsPost, (double)BitConverter.ToInt16(data, offset + 66) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt2" + tsPost, (double)BitConverter.ToInt16(data, offset + 68) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "dt3" + tsPost, (double)BitConverter.ToInt16(data, offset + 70) / 100.0, "°C", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P1" + tsPost, (double)BitConverter.ToUInt16(data, offset + 72) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P2" + tsPost, (double)BitConverter.ToUInt16(data, offset + 74) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "P3" + tsPost, (double)BitConverter.ToUInt16(data, offset + 76) / 10000.0, "МПа", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tраб" + tsPost, BitConverter.ToUInt16(data, offset + 78), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tост" + tsPost, BitConverter.ToUInt16(data, offset + 80), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tраб.шт" + tsPost, BitConverter.ToUInt16(data, offset + 82), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tmin" + tsPost, BitConverter.ToUInt16(data, offset + 84), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tmax" + tsPost, BitConverter.ToUInt16(data, offset + 86), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tdt" + tsPost, BitConverter.ToUInt16(data, offset + 88), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tф" + tsPost, BitConverter.ToUInt16(data, offset + 90), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tпуст.тр" + tsPost, BitConverter.ToUInt16(data, offset + 92), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Кан.НС" + tsPost, BitConverter.ToUInt32(data, offset + 94), "", date));
                records.Add(Device.MakeDayOrHourRecord(type, "НС" + tsPost, BitConverter.ToUInt32(data, offset + 98), "", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Схема" + tsPost, data[offset + 100], "", date));
                //records.Add(Device.MakeDayOrHourRecord(type, "Единицы измерения тепловой энергии" + tsPost, data[offset + 101], "", date));
            }
            archive.date = date;
            archive.records = records;
            return archive;
        }

        private dynamic MakeRecordsFromArchiveDataTsTotal(int ts, DateTime date, ArchiveType archiveType, byte[] data, TsArchiveVersion ver, int offset = 0)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = Device.Error.NO_ERROR;

            string tsPost = $" итог. (ТС{ts})";
            //if (!archive.success) return archive;
            //if (!Device.HasProperty(archive, "data") || !Device.HasProperty(archive, "archiveType") || !(archive.data is byte[]) || !(archive.archiveType is ArchiveType))
            //{
            //    archive.success = false;
            //    archive.error = "Не найдены данные для формирования архивных записей";
            //    return archive;
            //}
            //byte[] data = archive.data as byte[];
            //ArchiveType archiveType = archive.archiveType;
            if ((data.Length - offset) < 106)
            {
                archive.success = false;
                archive.error = "сообщение не распознано: фактическая длина ответа не равна ожидаемой";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            string type = null;
            switch (archiveType)
            {
                case ArchiveType.MonthlyTotal:
                    type = "Day";
                    break;
                    
                case ArchiveType.DailyTotal:
                    type = "Day";
                    break;

                case ArchiveType.Hourly:
                    type = "Hour";
                    break;

                default: break;
            }
            if (type == null)
            {
                archive.success = false;
                archive.error = $"Ошибка при создании записей типа {archiveType}";
                archive.errorcode = Device.Error.ANSWER_LENGTH_ERROR;
                return archive;
            }

            //DateTime date = new DateTime(year: 2000 + (data[0] % 100), month: data[1], day: data[2], hour: data[3], minute: 0, second: 0);
            List<dynamic> records = new List<dynamic>();

            records.Add(Device.MakeDayOrHourRecord(type, "M1" + tsPost, BitConverter.ToSingle(data, offset + 8), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M2" + tsPost, BitConverter.ToSingle(data, offset + 12), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M3" + tsPost, BitConverter.ToSingle(data, offset + 16), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M1R" + tsPost, BitConverter.ToSingle(data, offset + 20), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M2R" + tsPost, BitConverter.ToSingle(data, offset + 24), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "M3R" + tsPost, BitConverter.ToSingle(data, offset + 28), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "dM" + tsPost, BitConverter.ToSingle(data, offset + 32), "т", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V1" + tsPost, BitConverter.ToSingle(data, offset + 36), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V2" + tsPost, BitConverter.ToSingle(data, offset + 40), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V3" + tsPost, BitConverter.ToSingle(data, offset + 44), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V1R" + tsPost, BitConverter.ToSingle(data, offset + 48), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V2R" + tsPost, BitConverter.ToSingle(data, offset + 52), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "V3R" + tsPost, BitConverter.ToSingle(data, offset + 56), "м3", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tраб" + tsPost, BitConverter.ToUInt32(data, offset + 60), "мин", date));
            records.Add(Device.MakeDayOrHourRecord(type, "Tост" + tsPost, BitConverter.ToUInt32(data, offset + 64), "мин", date));
            if (ver == TsArchiveVersion.v1_0)
            {
                records.Add(Device.MakeDayOrHourRecord(type, "Qо" + tsPost, BitConverter.ToSingle(data, offset + 0), "Гкал", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Qгвс" + tsPost, BitConverter.ToSingle(data, offset + 4), "Гкал", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.1" + tsPost, BitConverter.ToUInt32(data, offset + 68), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.2" + tsPost, BitConverter.ToUInt32(data, offset + 72), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.3" + tsPost, BitConverter.ToUInt32(data, offset + 76), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.4" + tsPost, BitConverter.ToUInt32(data, offset + 80), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.5" + tsPost, BitConverter.ToUInt32(data, offset + 84), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tнс.6" + tsPost, BitConverter.ToUInt32(data, offset + 88), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Схема" + tsPost, data[offset + 92], "", date));
            }
            else
            {
                records.Add(Device.MakeDayOrHourRecord(type, "Qо" + tsPost, BitConverter.ToSingle(data, offset + 0), data[offset + 93] == 0 ? "Гкал" : "ГДж", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Qгвс" + tsPost, BitConverter.ToSingle(data, offset + 4), data[offset + 93] == 0 ? "Гкал" : "ГДж", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tраб.шт" + tsPost, BitConverter.ToUInt32(data, offset + 68), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tmin" + tsPost, BitConverter.ToUInt32(data, offset + 72), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tmax" + tsPost, BitConverter.ToUInt32(data, offset + 76), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tdt" + tsPost, BitConverter.ToUInt32(data, offset + 80), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tф" + tsPost, BitConverter.ToUInt32(data, offset + 84), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Tпуст.тр" + tsPost, BitConverter.ToUInt32(data, offset + 88), "мин", date));
                records.Add(Device.MakeDayOrHourRecord(type, "Схема" + tsPost, data[offset + 92], "", date));
            }
            archive.date = date;
            archive.records = records;
            return archive;
        }
    }
}

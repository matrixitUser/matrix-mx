using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.VKT7
{
    public partial class Driver
    {
        /// <summary>
        /// отправка сообщения устройству
        /// </summary>
        /// <typeparam name="TResponse">тип ожидаемого ответа</typeparam>
        /// <param name="request">запрос</param>
        /// <returns></returns>

        byte NetworkAddress = 0;
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        private byte[] SendSimple(byte[] data, int attempt)
        {
            var buffer = new List<byte>();

            //log(string.Format("{1}> {0}", string.Join(",", data.Select(b => b.ToString("X2"))), attempt));

            response();
            request(data);


            var collectCycles = attempt == 0 ? 0 : (attempt == 1 ? 2 : 6);
            var timeout = attempt == 0 ? 5000 : (attempt == 1 ? 6000 : 7500);
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
                        if (waitCollected == collectCycles)
                        {
                            isCollected = true;
                        }
                    }
                }
            }
            //log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));

            return buffer.ToArray();
        }

        private dynamic Send(byte[] data)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;

            byte[] buffer = null;

            for (var attempt = 0; attempt < 3 && answer.success == false; attempt++)
            {
                buffer = SendSimple(data, attempt);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                }
                else
                {
                    if (buffer.Length <= 4)
                    {
                        answer.error = "Слишком короткий ответ";
                    }
                    else if (!Crc.Check(buffer.ToArray(), new Crc16Modbus()))
                    {
                        answer.error = "Не сошлась контрольная сумма";
                    }
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                    }
                }
            }

            if (answer.success)
            {
                answer.code = 0;
                answer.NetworkAddress = buffer[0];
                answer.Function = buffer[1];
                answer.data = buffer;
                answer.Body = buffer.Skip(2).Take(buffer.Length - 4).ToArray();
            }

            return answer;
        }
        #endregion

        #region Export

        [Export("do")]
        public void Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;
            if (!param.ContainsKey("networkAddress"))
            {
                log("Отсутствуют сведения о сетевом адресе");
                return;
            }

            NetworkAddress = (byte)arg.networkAddress;

            //if (!param.ContainsKey("KTr"))
            //{
            //    arg.KTr = 1;
            //    log("Отсутствуют сведения о коэффициенте трансформации, принят по-умолчанию 1");
            //}

            //if (!param.ContainsKey("password"))
            //{
            //    arg.password = "";
            //    log("Отсутствуют сведения о пароле, принят по-умолчанию");
            //}


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

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            Wrap(() => All());
                        }
                        break;
                    case "ping":
                        {
                            Wrap(() => Ping());
                        }
                        break;
                    //case "day": Day(arg.data); return;
                    //case "hour": Hour(arg.data); return;
                    //case "constant": Constant(); return;
                    //case "current": Current(); return;
                    //case "abnormal": AbnormalEvents(arg.dateStart, arg.dateEnd); return;
                    default: log(string.Format("неопознаная команда {0}", what)); break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
            }
        }
        #endregion

        #region Интерфейс

        private void Wrap(Action act)
        {
            //PREPARE
            var response = Send(MakeReadHelloRequest());
            if (!response.success)
            {
                log("не удалось открыть канал связи: " + response.error);
                return;
            }

            log("канал связи открыт");

            //ACTION
            act();
        }

        private void Ping()
        {
            log("Проверка связи прошла успешно");
        }

        private void All()
        {
            //var server = ParseReadServerVersionResponse(Send(MakeReadDataRequest()));

            int serverVersion = 1;
            //if (server.success)
            //{
            //    log(string.Format("Версия сервера: {0}", server.version));
            //    serverVersion = server.version;
            //}
            //else
            //{
            //    log(string.Format("Версия сервера не получена: {0}", server.error));
            //    serverVersion = 0;
            //}

            var constant = GetConstants();
            if (!constant.success)
            {
                log(string.Format("Ошибка при считывании констант: {0}", constant.error));
                return;
            }

            List<dynamic> constants = constant.records;
            log(string.Format("Константы прочитаны: всего {0}, отчётный день={1}", constants.Count, constant.TotalDay));
            records(constants);

            //

            var properties = GetProperties(serverVersion);
            if (!properties.success)
            {
                log(string.Format("Ошибка при считывании свойств: {0}", properties.error));
                return;
            }
            log(string.Format("Свойства прочитаны: всего - ед. измерений {0}, дробных частей {1}", properties.Units.Count, properties.Fracs.Count));

            var current = GetCurrents(properties, constant.date);
            if (!current.success)
            {
                log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error));
                return;
            }

            //records(cncs.constants);

            List<dynamic> currents = current.records;
            log(string.Format("Текущие на {0} прочитаны: всего {1}", current.date, currents.Count));
            records(currents);

            if (getEndDate == null)
            {
                getEndDate = (type) => current.date;
            }

            //чтение часовых
            var startH = getStartDate("Hour");
            var endH = getEndDate("Hour");

            var hour = GetHours(startH, endH, current.date, properties);
            if (!hour.success)
            {
                log(string.Format("Ошибка при считывании часовых: {0}", hour.error));
                return;
            }
            List<dynamic> hours = hour.records;
            log(string.Format("Прочитаны часовые с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}: {2} записей", startH, endH, hours.Count));

            //чтение суточных
            var startD = getStartDate("Day");
            var endD = getEndDate("Day");

            var day = GetDays(startD, endD, current.date, properties, constant.TotalDay);
            if (!day.success)
            {
                log(string.Format("Ошибка при считывании суточных: {0}", day.error));
                return;
            }
            List<dynamic> days = day.records;
            log(string.Format("Прочитаны суточные с {0:dd.MM.yyyy} по {1:dd.MM.yyyy}: {2} записей", startD, endD, days.Count));

            ///// Нештатные ситуации ///
            //var lastAbnormal = getLastTime("Abnormal");
            //DateTime startAbnormal = lastAbnormal.AddHours(-constant.contractHour).Date;
            //DateTime endAbnormal = current.date;

            //var abnormal = GetAbnormals(startAbnormal, endAbnormal);
            //if (!abnormal.success)
            //{
            //    log(string.Format("ошибка при считывании НС: {0}", abnormal.error));
            //    return;
            //}

            log(string.Format("Успешно прочитано: {0}/{1}/{2}/{3} записей", constants.Count, currents.Count, days.Count, hours.Count));
        }

        #endregion


    }
}

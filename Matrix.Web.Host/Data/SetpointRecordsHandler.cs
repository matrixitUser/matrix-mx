using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// Класс занимается обработкой полученных от сервера опроса данных:
    /// - сравнение уставок и полученных значений параметров
    /// </summary>
    class SetpointRecordsHandler : IRecordHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SetpointRecordsHandler));
        private static readonly string[] setpointTypes = new string[] { "Current", "Hour" , "Abnormal" };    // только текущие; для архивных нужно еще подумать
        //private static readonly string[] waterCounterAlarmTypes = new string[] {  }; 

        private double? ParseDoubleParameter(dynamic par, string key)
        {
            var dpar = par as IDictionary<string, object>;

            double parsed = 0.0;
            if (!dpar.ContainsKey(key) || !Double.TryParse(dpar[key].ToString(), out parsed)) return null;

            return parsed;
        }

        public void Handle(IEnumerable<Domain.Entities.DataRecord> db_records, Guid userId)
        {
            long paramsTime = 0;
            long statesTime = 0;

            var sw = new Stopwatch();
            sw.Start();

            //

            //Кэш событий
            dynamic events = null;
            var eventsUpdated = new List<dynamic>();

            // УСТАВКИ - Минимум, Максимум по ТЕКУЩИМ

            var groups = db_records.Where(r => setpointTypes.Contains(r.Type)).GroupBy(r => r.ObjectId).Select(g => new { objectId = g.Key, records = g.Select(r => r).Distinct() });   // группировка архивных+текущих значений по объектам

            //var userId = StructureGraph.Instance.GetRootUser();
            foreach (var group in groups)                                                                       // для каждого объекта:
            {
                if (!group.records.Any()) continue;                                                             // проверка наличия записей

                var parameters = CacheRepository.Instance.GetParameters(group.objectId);                        // получаем все параметры

                //Кэш статусов
                dynamic states = CacheRepository.Instance.Get("setpoint", group.objectId);                    // инициализация кэша
                //dynamic states = CacheRepository.Instance.GetLocal("setpoint", group.objectId);                    
                if (states == null)
                {
                    states = new ExpandoObject();
                }
                bool statesUpdate = false;                                                                      // флаг записи в кэш
                var dstates = states as IDictionary<string, object>;


                foreach (var record in group.records)                                                           // (текущие) записи для объекта
                {
                    if (record.D1 == null) continue;

                    var sw2 = new Stopwatch();
                    sw2.Start();

                    if(record.Type == "Abnormal" && record.D1 != null && record.D1 == 1)
                    {
                        sw2.Stop();
                        paramsTime += sw2.ElapsedMilliseconds;
                        
                        var sw3 = new Stopwatch();
                        sw3.Start();
                        
                        // вычислить значение
                        var value = record.D1;
                        var unit = record.S2;
                        var date = record.Date.ToUniversalTime();
                        var dt1 = ((DateTime)record.Dt1).ToUniversalTime();

                        dynamic state = new ExpandoObject();
                        state.bad = true;


                        if (events == null)
                        {
                            events = CacheRepository.Instance.Get("setpoint-event");                                // инициализация кэша
                                                                                                                    //events = CacheRepository.Instance.GetLocal("setpoint-event");
                            if (events == null)
                            {
                                events = new ExpandoObject();
                            }
                        }
                        var devents = events as IDictionary<string, object>;

                        var key = string.Format("{0}-{1}-{2}", group.objectId.ToString(), record.S1, date.ToString());
                        dynamic ev = null;

                        if (!devents.ContainsKey(key))
                        {
                            ev = new ExpandoObject();
                            ev.id = null;
                            ev.objectId = group.objectId;
                            ev.param = record.S1;
                            ev.start = date;
                            ev.end = null;
                            ev.message = record.S2;
                        }
                        else
                        {
                            ev = devents[key];
                        }

                        eventsUpdated.Add(ev);
                        devents[key] = ev;
                        state.bad = true;

                        state.value = value;
                        state.unit = unit;
                        state.date = date;
                        state.dt1 = dt1;

                        dstates[record.S1] = state;
                        statesUpdate = true;

                        sw3.Stop();
                        statesTime += sw3.ElapsedMilliseconds;
                    }
                    else
                    {
                        // взять у каждой записи параметр
                        var par = parameters.Where(p => p.name == record.S1).Select(p => p).FirstOrDefault();
                        var dpar = par as IDictionary<string, object>;

                        // узнать, есть ли тег и уставки
                        if (par == null || !dpar.ContainsKey("tag")) continue; // параметр должен быть теггирован

                        //TODO сравнение date с предыдущим, т.к интересуют только последние по времени record 

                        //dynamic current = new ExpandoObject();  // текущие

                        var min = ParseDoubleParameter(par, "min");
                        var max = ParseDoubleParameter(par, "max");
                        //var minNigth = ParseDoubleParameter(par, "minNight");
                        //var maxNight = ParseDoubleParameter(par, "maxNight");

                        sw2.Stop();
                        paramsTime += sw2.ElapsedMilliseconds;

                        if ((min == null) && (max == null)) continue; // есть уставка?

                        var sw3 = new Stopwatch();
                        sw3.Start();


                        double init = 0.0;
                        if (!dpar.ContainsKey("init") || !Double.TryParse(par.init.ToString(), out init))
                        {
                            init = 0.0;
                        }

                        double k = 1.0;
                        if (!dpar.ContainsKey("k") || !Double.TryParse(par.k.ToString(), out k))
                        {
                            k = 1.0;
                        }

                        // вычислить значение
                        var value = init + record.D1 * k;
                        var unit = record.S2;
                        var date = record.Date.ToUniversalTime();
                        var dt1 = record.Dt1 == null ? DateTime.MinValue : ((DateTime)record.Dt1).ToUniversalTime();

                        dynamic state;
                        if (!dstates.ContainsKey(par.tag))
                        {
                            state = new ExpandoObject();
                            state.bad = false;
                        }
                        else
                        {
                            state = dstates[par.tag];
                            //проверка даты на прошедшее время
                            if (date < state.date) continue;
                        }


                        // проверить на выход за уставку
                        // получаем статус: хорошо-плохо
                        var bad = (value <= min) || (value >= max);

                        // смотрим: если статус изменился, то рождается событие
                        if (state.bad != bad)
                        {
                            if (events == null)
                            {
                                events = CacheRepository.Instance.Get("setpoint-event");                                // инициализация кэша
                                                                                                                        //events = CacheRepository.Instance.GetLocal("setpoint-event");
                                if (events == null)
                                {
                                    events = new ExpandoObject();
                                }
                            }
                            var devents = events as IDictionary<string, object>;

                            var key = string.Format("{0}-{1}", group.objectId.ToString(), par.tag);
                            dynamic ev = null;

                            if (!devents.ContainsKey(key))
                            {
                                ev = new ExpandoObject();
                                ev.id = null;
                                ev.objectId = group.objectId;
                                ev.param = par.tag;
                                ev.start = null;
                                ev.end = null;
                                ev.message = "";
                            }
                            else
                            {
                                ev = devents[key];
                            }


                            //ухудшение = новое событие
                            if (bad == true)
                            {
                                ev.start = date;
                                ev.end = null;
                                var message = string.Format("Выход за уставки {0}={1}{2} {3}{4}",
                                    par.tag, value, unit,
                                    min == null ? "" : string.Format("(min={0})", min),
                                    max == null ? "" : string.Format("(max={0})", max)
                                );

                                if (!dpar.ContainsKey("alertMsg") || (par.alertMsg == ""))
                                {
                                    ev.message = message;
                                }
                                else
                                {
                                    ev.message = par.alertMsg;
                                }

                                log.Info(string.Format("{0:dd HH:mm:ss} началось событие tp={1}: {2}", date, key.Substring(32), ev.message));
                            }
                            else
                            {
                                var message = string.Format("{0}={1}{2}{3}{4}",
                                    par.tag, value, unit,
                                    min == null ? "" : string.Format(" мин.={0}", min),
                                    max == null ? "" : string.Format(" макс.={0}", max)
                                );
                                ev.end = date.ToUniversalTime();
                                log.Info(string.Format("{0:dd HH:mm:ss} закончилось событие tp={1}: {2} при {3}", date, key.Substring(32), ev.message ?? "неизвестное событие", message));
                            }

                            eventsUpdated.Add(ev);
                            devents[key] = ev;
                            state.bad = bad;
                        }
                        state.value = value;
                        state.unit = unit;
                        state.date = date;
                        state.dt1 = dt1;

                        dstates[par.tag] = state;
                        statesUpdate = true;

                        sw3.Stop();
                        statesTime += sw3.ElapsedMilliseconds;
                    }
                    
                }

                if (statesUpdate == true)
                {
                    CacheRepository.Instance.Set(states, "setpoint", group.objectId);
                }

            }

            // ... ЕЩЕ СОБЫТИЯ

            // Обработка событий

            if (eventsUpdated.Count > 0)
            {
                // сохранение кэша событий
                CacheRepository.Instance.Set(events, "setpoint-event");

                var sessions = CacheRepository.Instance.GetSessions();

                // оповещение
                sessions.ToList().ForEach(session =>
                {
                    try
                    {
                        var bag = session as IDictionary<string, object>;
                        if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID))
                        {
                            //log.Debug(string.Format("сессия {0} не содержит сигналр подписки", session.id));
                            return;
                        }

                        dynamic eventsMessage = Helper.BuildMessage("setpoint");
                        eventsMessage.body.events = eventsUpdated.ToArray();

                        log.Debug(string.Format("отправка событий {0} шт, сессия {1}", eventsUpdated.Count, session.id));

                        var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                        SignalRConnection.RaiseEvent(eventsMessage, connectionId);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("сессия {0} битая", session.id), ex);
                    }
                });


                // запись в базу SQL
                try
                {
                    var data = new List<dynamic>();

                    foreach (var ev in eventsUpdated)
                    {
                        dynamic record = new ExpandoObject();
                        record.id = Guid.NewGuid().ToString();
                        record.date = ev.start;
                        record.type = DataRecordTypes.EventType.Name;
                        record.objectId = ev.objectId.ToString();
                        record.s1 = ev.param;
                        record.s2 = ev.message;
                        record.dt1 = DateTime.Now;
                        if (ev.end == null)
                        {
                            record.i1 = 1;
                        }
                        else
                        {
                            record.i1 = 0;
                            record.dt2 = ev.end;
                        }
                        data.Add(record);
                    }

                    //

                    var records = new List<DataRecord>();
                    foreach (var raw in data)
                    {
                        records.Add(EntityExtensions.ToRecord(raw));
                    }

                    if (records.Count > 0)
                    {
                        RecordAcceptor.Instance.Save(records);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("не удалось сохранить {0} событий в SQL DB", eventsUpdated.Count), ex);
                }
            }

            //

            sw.Stop();
            log.Info(string.Format("обработчик уставок принял {0} записей за {1} мс: параметры={2} уставки={3}", db_records.Count(), sw.ElapsedMilliseconds, paramsTime, statesTime));


            //if (events.Any())
            //{
            //}
        }
    }
}

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
    /// - сравнение уставок и полученных значений параметров new (для сигнализации Агидель)
    /// </summary>
    class SetpointNewRecordsHandler : IRecordHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SetpointNewRecordsHandler));
       
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

            // УСТАВКИ - Максимум по ТЕКУЩИМ

            var groups = db_records.Where(r => r.Type == "Current").GroupBy(r => r.ObjectId).Select(g => new { objectId = g.Key, records = g.Select(r => r).Distinct() });   // группировка архивных+текущих значений по объектам
            
            foreach (var group in groups)                                                                       // для каждого объекта:
            {
                if (!group.records.Any()) continue;                                                             // проверка наличия записей

                var parameters = CacheRepository.Instance.GetParameters(group.objectId);                        // получаем все параметры
                //Кэш статусов
                dynamic states = CacheRepository.Instance.Get("setpoint", group.objectId);                    // инициализация кэша              
                if (states == null)
                {
                    states = new ExpandoObject();
                }
                var dstates = states as IDictionary<string, object>;
                
                foreach (var record in group.records)                                                           // (текущие) записи для объекта
                {
                    if (record.D1 == null) continue;

                    var sw2 = new Stopwatch();
                    sw2.Start();

                    // взять у каждой записи параметр
                    var par = parameters.Where(p => p.name == record.S1).Select(p => p).FirstOrDefault();

                    var dpar = par as IDictionary<string, object>;

                    // узнать, есть ли тег и уставки
                    if (par == null || !dpar.ContainsKey("tag")) continue; // параметр должен быть теггирован
                    
                    //TODO сравнение date с предыдущим, т.к интересуют только последние по времени record 
                    
                    //var min = ParseDoubleParameter(par, "min");
                    var max = ParseDoubleParameter(par, "max");
                    //var minNigth = ParseDoubleParameter(par, "minNight");
                    var maxNight = ParseDoubleParameter(par, "maxNight");

                    sw2.Stop();
                    paramsTime += sw2.ElapsedMilliseconds;

                    if ((max == null) && (maxNight == null)) continue;                                               // есть уставка?
                    List<dynamic> activeEvents = TubeEvent.Instance.GetActiveEventByObjectId(group.objectId);
                    dynamic activeEventByTag = activeEvents.Find(x => x.tag == par.tag);                             //ищем запись таким же тэгом
                    dynamic rowCache = RowsCache.Instance.Get(group.objectId)[0];
                    int startDay = (Int32.TryParse(par.startDay.ToString(), out int tmpStartDay)) ? tmpStartDay : 2;
                    int endDay = (Int32.TryParse(par.endDay.ToString(), out int tmpEndDay)) ? tmpEndDay : 5;
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
                    double value = Math.Round((double)(init + record.D1 * k),3);
                    var unit = record.S2;
                    var date = record.Date;
                    
                    // проверить на выход за уставку
                    // получаем статус: хорошо-плохо
                    bool isBad;
                    double setPoint;
                    string strDayNight;
                    if((date.Hour <= startDay) && (date.Hour >= endDay))
                    {
                        isBad = value >= maxNight;
                        setPoint = maxNight;
                        strDayNight = "Ночной";
                    }
                    else
                    {
                        isBad = value >= max;
                        setPoint = max;
                        strDayNight = "Дневной";
                    }

                    if(activeEventByTag != null)
                    {
                        if(!activeEventByTag.replay && !isBad)
                        {
                            //delete row
                            TubeEvent.Instance.DeleteRow(activeEventByTag.id);
                        }
                        else if (activeEventByTag.replay && !isBad)
                        {
                            //update row (endDate)
                            TubeEvent.Instance.UpdateDateEnd(date, activeEventByTag.id);
                        }
                        else
                        {
                            //update row (startDate value etc)
                            TubeEvent.Instance.UpdateRow(activeEventByTag.id, date, value, 1, setPoint);
                        }
                    }
                    else if(isBad)
                    { 
                        //create row
                        TubeEvent.Instance.CreateRow(date, record.ObjectId, $"{strDayNight}: {value} >= {setPoint}", strDayNight, rowCache.name, value, par.tag, DateTime.Now, setPoint);
                    }
                    
                    sw3.Stop();
                    statesTime += sw3.ElapsedMilliseconds;
                }
                

            }
            
            sw.Stop();
            log.Info(string.Format("обработчик уставок принял {0} записей за {1} мс: параметры={2} уставки={3}", db_records.Count(), sw.ElapsedMilliseconds, paramsTime, statesTime));
            
        }
    }
}

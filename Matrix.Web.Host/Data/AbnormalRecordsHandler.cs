using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Handlers;
using Matrix.Web.Host.Transport;
using Newtonsoft.Json.Linq;
using System.Timers;
using System.Dynamic;
using System.Configuration;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// перехват записей о нештатных ситуациях
    /// </summary>
    class AbnormalRecordsHandler : IRecordHandler, IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LogRecordsHandler));
        //private readonly AbnormalsCache cache = new AbnormalsCache();

        public AbnormalRecordsHandler()
        {

        }

        public void Handle(IEnumerable<DataRecord> records, Guid userId)
        {
            var abnormals = records.Where(r => r.Type == "Abnormal");
            if (!abnormals.Any()) return;

            // обновление кэша с группировкой tube   
            abnormals.GroupBy(r => r.ObjectId).ToList().ForEach(g => AbnormalsCache.Instance.Update(g.Key, g.ToList())); // g.Where(p => DateTime.Compare(p.Date, yesterday) >= 0)));
        }

        public void Dispose()
        {
            AbnormalsCache.Instance.Dispose();
        }
    }

    class AbnormalsCache : IDisposable
    {
        private const int ABNORMAL_INTERVAL_SEC = 30;

        private static AbnormalsCache instance = null;
        public static AbnormalsCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AbnormalsCache();
                }
                return instance;
            }
        }

        private object lockObject = new object();
        private static readonly ILog log = LogManager.GetLogger(typeof(LogRecordsHandler));
        private readonly Dictionary<Guid, IEnumerable<DataRecord>> cache = new Dictionary<Guid, IEnumerable<DataRecord>>();
        private readonly Dictionary<Guid, IEnumerable<DataRecord>> newRecords = new Dictionary<Guid, IEnumerable<DataRecord>>();
        private readonly Timer syncTimer = new Timer();
        private Guid userId;

        private AbnormalsCache()
        {
            int intervalSec;
            if (!int.TryParse(ConfigurationManager.AppSettings["abnormal-update-interval"]?.ToLower().Trim(), out intervalSec) || intervalSec < 0)
            {
                intervalSec = ABNORMAL_INTERVAL_SEC;
            }
            if (intervalSec == 0) return;

            userId = StructureGraph.Instance.GetRootUser();
            syncTimer.Interval = intervalSec * 1000;
            syncTimer.Elapsed += (se, ea) =>
            {
                Save();
            };
            syncTimer.Start();
        }

        // первичная загрузка кэша
        // должен загрузить все НС по всем объектам за указанный период
        //тут же необходимо обеспечить обновление всех объектов, чтобы обнулить НС там, где их уже нет
        public void Load()
        {
            //var now = DateTime.Now.Add(fakeNowDiff);
            //var tubeIds = StructureGraph.Instance.GetTubeIds(userId).ToArray();
            //var lastAbnormals = Cache.Instance.GetRecords(now.AddDays(-1), now.AddDays(1), "Abnormal", tubeIds);

            //lock (lockObject)
            //{
            //    cache.Clear();
            //}

            //tubeIds.ToList().ForEach(tid => Update(tid, lastAbnormals.Where(r => r.ObjectId == tid)));
            ////lastAbnormals.GroupBy(r => r.ObjectId).ToList().ForEach(g => Update(g.Key, g.ToList()));
            ////logger.Debug("загружен кеш параметров по {0} объектам", res.Count());

            var tubeIds = StructureGraph.Instance.GetTubeIds(userId).ToArray();
            lock (lockObject)
            {
                cache.Clear();
                foreach (var tubeId in tubeIds)
                {
                    cache.Add(tubeId, new List<DataRecord>());
                }
            }
        }

        public void Save()
        {
            //проверка старых записей на изменение
            if (cache.Any())
            {
                var now = DateTime.Now;
                Guid[] tubeIds = cache.Keys.ToArray();
                IEnumerable<DataRecord> lastAbnormals = Cache.Instance.GetRecords(now.AddDays(-1), now.AddDays(1), "Abnormal", tubeIds);
                tubeIds.ToList().ForEach(tid => Update(tid, lastAbnormals.Where(r => r.ObjectId == tid)));
            }

            //проверка новых записей
            if (newRecords.Any())
            {
                lock (lockObject)
                {
                    // если есть запись в справочнике, то обновляем строку в любом случае
                    var now = DateTime.Now;
                    var minDate = now.AddDays(-1);
                    var maxDate = now.AddDays(1);
                    foreach (var newRecordsTube in newRecords)
                    {
                        //if (newRecordsTube.Value.Any())
                        //{
                        var tubeId = newRecordsTube.Key;

                        var tmp = new List<DataRecord>();
                        if (cache.ContainsKey(tubeId))
                        {
                            tmp.AddRange(cache[tubeId].ToList());
                        }
                        tmp.AddRange(newRecordsTube.Value);
                        IEnumerable<DataRecord> recordsFiltered = tmp.Distinct().Where(r => (r.I2 != null) && (r.I2 >= 1000) && (DateTime.Compare(minDate, r.Date) <= 0) && (DateTime.Compare(r.Date, maxDate) <= 0));
                        if (recordsFiltered.Any())
                        {
                            cache[tubeId] = recordsFiltered;
                        }
                        else
                        {
                            cache.Remove(tubeId);
                        }

                        dynamic abnormals = new ExpandoObject();
                        abnormals.count = recordsFiltered.Count();

                        RowsCache.Instance.UpdateAbnormals(abnormals, tubeId, userId);
                        Carantine.Instance.Push(tubeId);
                        //}
                    }
                    newRecords.Clear();
                }
            }

        }

        public void Update(Guid tubeId, IEnumerable<DataRecord> records)
        {
            //В кэше:
            //записи НС
            //Пришло: 
            //(текущие) записи НС

            //newRecords.Add(tubeId, records);
            lock (lockObject)
            {
                var tmp = new List<DataRecord>();
                if (newRecords.ContainsKey(tubeId))
                {
                    tmp.AddRange(newRecords[tubeId].ToList());
                }
                tmp.AddRange(records);

                // Новые записи. Можно загрузить пустой массив для обновления строки
                newRecords[tubeId] = tmp;
            }
        }

        public void Dispose()
        {
            syncTimer.Stop();
            syncTimer.Dispose();
        }
    }
}

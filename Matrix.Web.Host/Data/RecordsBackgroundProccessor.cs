using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// обработчик заявок на сохранение архивов
    /// собирает заявки в очередь из разных потоков (вызовы от клиентов апи и т.п.)
    /// в фоновом потоке обрабатывает заявки порциями, в временным интервалом
    /// </summary>
    class RecordsBackgroundProccessor
    {
        /// <summary>
        /// интервал между проверками очереди на наличие заявок, и запуска обработчика
        /// </summary>
        private const int WAIT_TIMEOUT = 20;   //интервал между проверками очереди на наличие заявок
        private const int PROCCESS_TIMEOUT = 500;//интервал запуска обработчика

        private static readonly ILog log = LogManager.GetLogger(typeof(RecordsBackgroundProccessor));

        private readonly List<DataRecord> records = new List<DataRecord>();

        private EventWaitHandle wh = new AutoResetEvent(false);

        public void AddPart(IEnumerable<DataRecord> part)
        {
            if (part == null || !part.Any())
            {
                return;
            }

            lock (records)
            {
                log.Debug(string.Format("получена порция для пост-обработки, {0} шт", part.Count()));
                records.AddRange(part);
            }
            //wh.Set();
        }

        public void ReSet()
        {
            loop = true;
            var thread = new Thread(Idle);
            thread.IsBackground = true;
            thread.Start();
        }

        private bool loop = true;
        private void Idle()
        {
            while (loop)
            {
                try
                {
                    Thread.Sleep(WAIT_TIMEOUT);
                    //Thread.Sleep(PROCCESS_TIMEOUT);
                    IEnumerable<DataRecord> copy;
                    lock (records)
                    {
                        copy = records.ToList();
                        records.Clear();
                    }

                    if (copy.Any())
                    {
                        ProccessRecords(copy);
                    }

                    lock (records) if (records.Any()) continue;
                    Thread.Sleep(PROCCESS_TIMEOUT);
                    //wh.WaitOne();
                }
                catch (Exception ex)
                {
                    log.Error("ошибка при пост-обработке записей, записи обработаны не полностью", ex);
                }
            }
        }

        private void ProccessRecords(IEnumerable<DataRecord> recs)
        {
            log.Debug(string.Format("начата пост-обработка записей, {0} шт", recs.Count()));
            Cache.Instance.SaveRecords(recs);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.Handle(recs, Guid.NewGuid());
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("ошибка в хандлере рекордов"), ex);
                }
            }
        }

        private readonly List<IRecordHandler> handlers = new List<IRecordHandler>();

        public void AddHandler(IRecordHandler handler)
        {
            handlers.Add(handler);
        }


        public RecordsBackgroundProccessor()
        {
            ReSet();
            AddHandler(new LogRecordsHandler());
            AddHandler(new AbnormalRecordsHandler());
            AddHandler(new FulnessRecordsHandler());
#if ORENBURG
#else
            AddHandler(new SetpointRecordsHandler());
            AddHandler(new SetpointNewRecordsHandler());
#endif
            //AddHandler(new ParametersHandler());
            //AddHandler(new LastRecordCacheHandler());
        }

        //private RecordsBackgroundProccessor() { }

        //static RecordsBackgroundProccessor() { }

        //private static RecordsBackgroundProccessor instance = new RecordsBackgroundProccessor();
        //public static RecordsBackgroundProccessor Instance
        //{
        //    get
        //    {
        //        return instance;
        //    }
        //}
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using StackExchange.Redis;
using NLog;

namespace Matrix.PollServer.Storage
{
    /// <summary>
    /// канал обработки записей
    /// </summary>
    class RecordsProccessChannel : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private Thread worker;
        private bool loop = true;
        private const int TIME_SLEEP = 100;

        private ConcurrentBag<dynamic> queue = new ConcurrentBag<dynamic>();

        public RecordsProccessChannel()
        {
            worker = new Thread(Idle);
            worker.Start();
        }

        public void Add(IEnumerable<dynamic> records)
        {
            foreach (var record in records)
                queue.Add(record);
        }

        private void Idle()
        {
            while (loop)
            {
                try
                {
                    List<dynamic> copy = new List<dynamic>();

                    while (!queue.IsEmpty)
                    {
                        dynamic record;
                        if (queue.TryTake(out record))
                            copy.Add(record);
                    }

                    if (copy.Count == 0)
                    {
                        Thread.Sleep(TIME_SLEEP);
                        continue;
                    }

                    Saver.Save(copy);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "ошибка в канале сохранения данных");
                }
            }
        }

        public string GetInfo()
        {
            return $"Кол-во элементов в очереди: {queue.Count}";
        }

        public void Dispose()
        {
            loop = false;
            worker.Abort();
            worker.Join();
        }
    }
}

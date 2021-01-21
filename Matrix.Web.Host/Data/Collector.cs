using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// собирает заявки в кучу, и обрабатывает их пачками
    /// </summary>
    class Collector<T>
    {
        private readonly List<T> entities = new List<T>();
        private static readonly ILog log = LogManager.GetLogger(typeof(Collector<T>));

        public int Timeout { get; set; }

        public void AddRange(IEnumerable<T> range)
        {
            lock (entities)
            {
                entities.AddRange(range);
            }
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
                    Thread.Sleep(Timeout);
                    if (entities.Any())
                    {
                        IEnumerable<T> copy;
                        lock (entities)
                        {
                            copy = entities.ToList();
                            entities.Clear();
                        }
                        Proccess(copy);
                    }
                }
                catch (ThreadAbortException tae)
                {
                    return;
                }
                catch (Exception ex)
                {
                    log.Error("ошибка при пост-обработке записей, записи обработаны не полностью", ex);
                }
            }
        }

        private void Proccess(IEnumerable<T> copy)
        {

        }
    }
}

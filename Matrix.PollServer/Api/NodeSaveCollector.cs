using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Matrix.PollServer.Api
{
    /// <summary>
    /// очередь для сохранения нодов
    /// формат: {start:{...},end:{...},rel:{...},action:"save|delete"}
    /// </summary>
    class StateSaveCollector : IDisposable
    {
        private const int PROCCESS_TIMEOUT = 500;

        private readonly static ILog log = LogManager.GetLogger(typeof(StateSaveCollector));

        private readonly List<dynamic> nodes = new List<dynamic>();

        private Thread worker;
        private EventWaitHandle wh = new AutoResetEvent(false);

        /// <summary>
        /// помещает нод в очередь для сохранения        
        /// </summary>
        /// <param name="node"></param>
        public void Add(dynamic node)
        {
            AddRange(new dynamic[] { node });
        }

        public void AddRange(IEnumerable<dynamic> part)
        {
            lock (nodes)
            {
                nodes.AddRange(part);
            }
            if (wh != null)
                wh.Set();
        }

        public void Start()
        {
            loop = true;
            worker = new Thread(Idle);
            worker.IsBackground = true;
            worker.Start();
        }

        private bool loop = true;
        private void Idle()
        {
            while (loop)
            {                
                IEnumerable<dynamic> copy;
                lock (nodes)
                {
                    copy = nodes.ToList();
                    nodes.Clear();
                }
                Proccess(copy);
                Thread.Sleep(300);
                wh.WaitOne();
            }
        }

        private void Proccess(IEnumerable<dynamic> copy)
        {
            //log.Info(string.Format("сохраняется порция статусов {0} шт", copy.Count()));
            Api.ApiProxy.Instance.SaveStates(copy);            
        }

        private StateSaveCollector()
        {
            Start();
        }

        static StateSaveCollector() { }
        private readonly static StateSaveCollector instance = new StateSaveCollector();
        public static StateSaveCollector Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            //ожидаем завершения потока-обработчика
            loop = false;
            wh.Set();
            worker.Join();
            wh.Close();
            wh = null;

            log.Info("сборщик нодов остановлен");
        }
    }
}

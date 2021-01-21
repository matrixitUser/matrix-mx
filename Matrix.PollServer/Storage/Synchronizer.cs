//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Dynamic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using log4net;
//using Matrix.PollServer.Storage;

//namespace Matrix.PollServer
//{
//    /// <summary>
//    /// Синхронизация локальной базы с основной
//    /// </summary>
//    class Synchronizer : IDisposable
//    {
//        private const int SYNC_INTERVAL = 10 * 60 * 1000;

//        private static readonly ILog log = LogManager.GetLogger(typeof(Synchronizer));

//        //private bool loop = true;
//        //private Thread worker;

//        private System.Timers.Timer syncTimer;

//        public void Restart()
//        {
//            Stop();
//            Start();
//        }

//        public void Stop()
//        {
//            syncTimer.Stop();
//            log.Info("синхронизатор данных остановлен");
//        }

//        public void Start()
//        {
//            //if (worker != null) return;

//            //loop = true;
//            //worker = new Thread(Idle);
//            //worker.IsBackground = true;
//            //worker.Name = "синхронизатор данных";
//            //worker.Start();
//            syncTimer.Interval = SYNC_INTERVAL;
//            syncTimer.Start();
//        }

//        private bool SaveRecords(IEnumerable<dynamic> records)
//        {
//            dynamic message = Helper.BuildMessage("records-save");
//            message.body.records = records;

//            var connector = UnityManager.Instance.Resolve<IConnector>();
//            dynamic file = connector.SendMessage(message);

//            return true;
//        }

//        private bool LoadRecords(Guid objectId, string type, DateTime start, DateTime end)
//        {
//            dynamic message = Helper.BuildMessage("records-get");
//            message.body.targets = new List<Guid>() { objectId };
//            message.body.type = type;
//            message.body.start = start;
//            message.body.end = end;

//            ///нужное дописать
//            var connector = UnityManager.Instance.Resolve<IConnector>();
//            dynamic file = connector.SendMessage(message);
//            if (file != null && file.head.what == "records-get")
//            {
//                foreach (var record in file.body.records)
//                {
//                    record.timing = 1;
//                }
//                RecordsRepository2.Instance.Save(file.body.records);
//            }
//            return true;
//        }

//        static Synchronizer() { }

//        private Synchronizer()
//        {
//            syncTimer = new System.Timers.Timer();
//            syncTimer.Elapsed += (se, ea) => Sync();
//            //Start();
//        }

//        private readonly object syncLock = new object();
      
//        private void Sync()
//        {
//            //lock (syncLock)
//            //{
//                var notSyncedRecords = RecordsRepository2.Instance.GetNotSyncedRecords();

//                if (!notSyncedRecords.Any())
//                {
//                    return;
//                }

//                if (SaveRecords(notSyncedRecords))
//                {
//                    RecordsRepository2.Instance.SetSyncedRecords(notSyncedRecords);
//                }
//            //}
//        }

//        private static readonly Synchronizer instance = new Synchronizer();
//        public static Synchronizer Instance
//        {
//            get { return instance; }
//        }

//        public void Dispose()
//        {
//            Stop();
//        }
//    }
//}

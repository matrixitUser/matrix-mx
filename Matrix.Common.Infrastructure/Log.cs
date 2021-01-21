//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using System.Timers;
//using Matrix.Domain.Entities;
//using System.Collections.Concurrent;
//using log4net;

//namespace Matrix.Common.Infrastructure
//{
//    public class Log : ILogger
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(Log));

//        private readonly ConnectionPoint connectionPoint;

//        private Couple<LogMessage, DataRecord> messageCouple;

//        public event EventHandler<LogReceivedEventArgs> LogReceived;
//        public void RaiseLogReceived(DateTime date, string message, Guid objectId)
//        {
//            if (LogReceived != null)
//            {
//                LogReceived(this, new LogReceivedEventArgs(new DataRecord[] { new DataRecord() { Id = Guid.NewGuid(), Type = DataRecordTypes.LogMessageType, ObjectId = objectId, S1 = message, Date = date } }));
//            }
//        }

//        public void RaiseLogReceived(IEnumerable<DataRecord> messages)
//        {
//            if (LogReceived != null)
//            {
//                LogReceived(this, new LogReceivedEventArgs(messages));
//            }
//        }

//        public event EventHandler<LogReceivedEventArgs> LogWritten;
//        public void RaiseLogWritten(IEnumerable<DataRecord> messages)
//        {
//            if (LogWritten != null)
//            {
//                LogWritten(this, new LogReceivedEventArgs(messages));
//            }
//        }

//        public void RegisterRules()
//        {
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(LogMessage).Name, null));
//        }

//        public Log(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;

//            messageCouple = new Couple<LogMessage, DataRecord>(records => new LogMessage(Guid.NewGuid(), records), 1000, 50);
//            messageCouple.OnCoupleMessageReady += (se, ea) => connectionPoint.SendMessage(ea.Message);
//        }

//        /// <summary>
//        /// позволяет получать логи по данным объектам
//        /// </summary>
//        /// <param name="objectIds"></param>
//        public void NeedLogBy(IEnumerable<Guid> objectIds)
//        {
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(LogMessage).Name, objectIds));
//        }

//        public void NoMoreNeedLogBy(IEnumerable<Guid> objectIds)
//        {
//            connectionPoint.SendMessage(new RemoveRuleRequest(Guid.NewGuid(), typeof(LogMessage).Name, objectIds));
//        }

//        public void NeedLogBy(Guid objectId)
//        {
//            NeedLogBy(new Guid[] { objectId });
//        }

//        public void NoMoreNeedLogBy(Guid objectId)
//        {
//            NoMoreNeedLogBy(new Guid[] { objectId });
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            if (e.Message is LogMessage)
//            {
//                var logMessage = e.Message as LogMessage;
//                RaiseLogReceived(logMessage.Messages);
//            }
//        }

//        public void WriteLog(Guid associatedObjectId, string text)
//        {
//            messageCouple.Add(new DataRecord()
//            {
//                Id = Guid.NewGuid(),
//                Date = DateTime.Now,
//                ObjectId = associatedObjectId,
//                Type = DataRecordTypes.LogMessageType,
//                S1 = text
//            });

//            if (monitoringLogs.ContainsKey(associatedObjectId))
//            {
//                var record = monitoringLogs[associatedObjectId];
//                record.S1 = string.Format("{0}\n{1}", record.S1, text);
//            }
//        }

//        private ConcurrentDictionary<Guid, DataRecord> monitoringLogs = new ConcurrentDictionary<Guid, DataRecord>();

//        private List<DataRecord> storeLogs = new List<DataRecord>();
//        public Guid BeginStore(Guid relatedObjectId, Guid initiator)
//        {
//            var storeLog = new DataRecord()
//            {
//                Id = Guid.NewGuid(),
//                Type = DataRecordTypes.CommunicationLogType,
//                Date = DateTime.Now,
//                ObjectId = relatedObjectId,
//                S1 = "",
//                G1 = initiator
//            };

//            monitoringLogs.AddOrUpdate(storeLog.ObjectId, storeLog, (key, value) => value);
//            return relatedObjectId;
//        }

//        public DataRecord EndStore(Guid key)
//        {
//            DataRecord record = null;
//            monitoringLogs.TryRemove(key, out record);

//            return record;
//        }
//    }

//    public class LogReceivedEventArgs : EventArgs
//    {
//        public IEnumerable<DataRecord> Messages { get; private set; }

//        public LogReceivedEventArgs(IEnumerable<DataRecord> messages)
//        {
//            Messages = messages.ToList();
//        }
//    }
//}

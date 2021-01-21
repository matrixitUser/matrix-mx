//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;

//namespace Matrix.Common.Infrastructure
//{
//    /// <summary>
//    /// подсистема управления контроллерами Матрикс
//    /// </summary>
//    public class MatrixController
//    {
//        private readonly ConnectionPoint connectionPoint;

//        private System.Timers.Timer signalTimer = new System.Timers.Timer();

//        public MatrixController(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;
//            signalTimer.Interval = 60000;
//            signalTimer.Elapsed += OnSignalTimerElapsed;
//            signalTimer.Start();
//        }

//        private void OnSignalTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
//        {
//            //RequestSignalChanges()
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            //if (e.Message is MatrixControllerSignalChange)
//            //{
//            //var message = e.Message as MatrixControllerSignalChange;
//            //RaiseSignalLevelChanged(message.ConnectionId, message.Level);
//            //}
//            if (e.Message is MatrixControllerVirtualComData)
//            {
//                var message = e.Message as MatrixControllerVirtualComData;
//                RaiseVirtualComDataReceived(message.ConnectionId, message.Data);
//            }
//        }

//        /// <summary>
//        /// происходит при изменении уровня сигнала модема
//        /// </summary>
//        public event EventHandler<SignalLevelEventArgs> SignalLevelChanged;
//        private void RaiseSignalLevelChanged(Guid connectionId, double level)
//        {
//            if (SignalLevelChanged != null)
//            {
//                SignalLevelChanged(this, new SignalLevelEventArgs(connectionId, level));
//            }
//        }

//        public event EventHandler<VirtualComDataEventArgs> VirtualComDataReceived;
//        private void RaiseVirtualComDataReceived(Guid connectionId, IEnumerable<byte> data)
//        {
//            if (VirtualComDataReceived != null)
//            {
//                VirtualComDataReceived(this, new VirtualComDataEventArgs(connectionId, data));
//            }
//        }

//        public void RegisterRules()
//        {
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerSignalChange).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerChangeServer).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerSendAt).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerCheckVersion).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerVirtualComData).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(MatrixControllerOpenPort).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataRecordsLastRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(DataRecordsResponse).Name, null));
//        }

//        public void ChangeServer(IEnumerable<Guid> connectionIds, string newServer)
//        {
//            connectionPoint.SendMessage(new MatrixControllerChangeServer(Guid.NewGuid(), connectionIds, newServer));
//        }

//        public void SendAt(IEnumerable<Guid> connectionIds, string at)
//        {
//            connectionPoint.SendMessage(new MatrixControllerSendAt(Guid.NewGuid(), at, connectionIds));
//        }

//        public void CheckVersion(IEnumerable<Guid> connectionIds)
//        {
//            connectionPoint.SendMessage(new MatrixControllerCheckVersion(Guid.NewGuid(), connectionIds));
//        }

//        public void VirtualComMessage(Guid connectionId, IEnumerable<byte> data, bool isInit = false)
//        {
//            connectionPoint.SendMessage(new MatrixControllerVirtualComData(Guid.NewGuid(), connectionId, data, isInit));
//        }

//        public void OpenPort(IEnumerable<Guid> connectionIds)
//        {
//            connectionPoint.SendMessage(new MatrixControllerOpenPort(Guid.NewGuid(), connectionIds));
//        }

//        public IEnumerable<SignalLevel> RequestSignalChanges(IEnumerable<Guid> connectionIds)
//        {
//            var answer = connectionPoint.SendSyncMessage(new DataRecordsLastRequest(Guid.NewGuid(), DataRecordTypes.MatrixSignalType, connectionIds));
//            if (answer != null && answer is DataRecordsResponse)
//            {
//                var recordsMessage = answer as DataRecordsResponse;
//                return recordsMessage.Records.Where(d => d.Type == DataRecordTypes.MatrixSignalType).Select(d => new SignalLevel(d.ObjectId, d.D1.Value));
//            }
//            return new SignalLevel[] { };
//        }
//    }

//    public class SignalLevel
//    {
//        public Guid ConnectionId { get; private set; }
//        public double Level { get; private set; }

//        public SignalLevel(Guid connectionId, double level)
//        {
//            ConnectionId = connectionId;
//            Level = level;
//        }
//    }

//    public class SignalLevelEventArgs : EventArgs
//    {
//        public double Level { get; private set; }
//        public Guid ConnectionId { get; private set; }

//        public SignalLevelEventArgs(Guid connectionId, double level)
//        {
//            Level = level;
//            ConnectionId = connectionId;
//        }
//    }
//}

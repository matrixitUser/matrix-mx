//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{
//    public class Current : ICurrent
//    {
//        private readonly ConnectionPoint connectionPoint;

//        public event EventHandler<CurrentDataEventArgs> CurrentDataReceived;
//        private void RaiseCurrentDataReceived(IEnumerable<CurrentData> data)
//        {
//            if (CurrentDataReceived != null)
//            {
//                CurrentDataReceived(this, new CurrentDataEventArgs(data));
//            }
//        }

//        public void RegisterRules()
//        {
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(CurrentDataSave).Name, null));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(CurrentDataRequest).Name, null));
//        }

//        public Current(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;
//        }

//        public void SubscribeToCurrent(IEnumerable<Guid> objectIds)
//        {
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(CurrentDataMessage).Name, objectIds));
//            //connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(CurrentDataRequest).Name, objectIds));
//        }

//        public void UnsubscribeToCurrent(IEnumerable<Guid> objectIds)
//        {
//            //connectionPoint.SendMessage(new RemoveRuleRequest(Guid.NewGuid(), typeof(CurrentDataRequest).Name, objectIds));
//            //connectionPoint.SendMessage(new RemoveRuleRequest(Guid.NewGuid(), typeof(CurrentDataMessage).Name, objectIds));
//        }

//        public void SubscribeToCurrent(Guid objectId)
//        {
//            SubscribeToCurrent(new Guid[] { objectId });
//        }

//        public void UnsubscribeToCurrent(Guid objectId)
//        {
//            UnsubscribeToCurrent(new Guid[] { objectId });
//        }

//        public void Save(IEnumerable<CurrentData> data)
//        {
//            //connectionPoint.SendMessage(new CurrentDataSave(Guid.NewGuid(), data.ToList()));
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            //if (e.Message is CurrentDataMessage)
//            //{
//            //    var currentDataResponse = e.Message as CurrentDataMessage;
//            //    RaiseCurrentDataReceived(currentDataResponse.Data);
//            //}
//        }

//        public void Get(IEnumerable<Guid> tubeIds)
//        {
//            //connectionPoint.SendMessage(new CurrentDataRequest(Guid.NewGuid(), tubeIds));
//        }
//    }

//    public class CurrentDataEventArgs : EventArgs
//    {
//        public IEnumerable<CurrentData> Data { get; private set; }

//        public CurrentDataEventArgs(IEnumerable<CurrentData> data)
//        {
//            Data = data;
//        }
//    }
//}

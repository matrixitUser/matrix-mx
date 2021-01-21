//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure
//{

//    public class Event
//    {
//        private readonly ConnectionPoint connectionPoint;

//        public Event(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            if (e.Message is EventDataResponse)
//            {
//                var message = e.Message as EventDataResponse;
//                RaiseEventReceived(message.Data);
//            }
//        }

//        public event EventHandler<CurrentEventReceivedEventArgs> CurrentEventReceived;
//        private void RaiseEventReceived(IEnumerable<EventData> data)
//        {
//            if (CurrentEventReceived != null)
//            {
//                CurrentEventReceived(this, new CurrentEventReceivedEventArgs(data));
//            }
//        }
//        public void RegisterRules()
//        {
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(EventDataResponse).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(EventDataRequest).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(EventConfirmResponse).Name, null));
//        }

//        //public void Get(DateTime dateFrom, int count, bool notConfirmedOnly, Action<IEnumerable<EventData>> callback)
//        //{
//        //    connectionPoint.SendMessage(new EventDataRequest(Guid.NewGuid(), dateFrom, count, notConfirmedOnly), response =>
//        //    {
//        //        var answer = response as EventDataResponse;
//        //        if (answer != null)
//        //        {
//        //            callback(answer.Data);
//        //        }
//        //        else
//        //        {
//        //            callback(null);
//        //        }
//        //    });
//        //}

//        public void Get(Guid? lastId, int count, bool notConfirmedOnly, Action<IEnumerable<EventData>> callback)
//        {
//            connectionPoint.SendMessage(new EventDataRequest(Guid.NewGuid(), lastId, count, notConfirmedOnly), response =>
//            {
//                var answer = response as EventDataResponse;
//                if (answer != null)
//                {
//                    callback(answer.Data);
//                }
//                else
//                {
//                    callback(null);
//                }
//            });
//        }

//        public void Confirm(Guid userId, Guid eventId)
//        {
//            connectionPoint.SendMessage(new EventConfirmResponse(Guid.NewGuid(), userId, eventId));
//        }
//    }


//    public class CurrentEventReceivedEventArgs : EventArgs
//    {
//        public IEnumerable<EventData> Data { get; private set; }

//        public CurrentEventReceivedEventArgs(IEnumerable<EventData> data)
//        {
//            Data = data;
//        }
//    }
//}

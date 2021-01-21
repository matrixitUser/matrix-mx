//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Infrastructure.Protocol;
//using Matrix.Common.Infrastructure.Protocol.Messages;

//namespace Matrix.Common.Infrastructure
//{
//    public class VirtualCom
//    {
//        private readonly ConnectionPoint connectionPoint;

//        public VirtualCom(ConnectionPoint connectionPoint)
//        {
//            this.connectionPoint = connectionPoint;
//            this.connectionPoint.MessageRecieved += OnMessageRecieved;
//        }

//        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
//        {
//            if (e.Message is VirtualComData)
//            {
//                var message = e.Message as VirtualComData;
//                RaiseVirtualComDataReceived(message.ConnectionId, message.Data);
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
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(VirtualComData).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(VirtualComPrepare).Name, null));
//            connectionPoint.SendMessage(new AddRuleRequest(Guid.NewGuid(), typeof(VirtualComRelease).Name, null));
//        }

//        public void Release(IEnumerable<Guid> connectionIds)
//        {
//            connectionPoint.SendMessage(new VirtualComRelease(Guid.NewGuid(), connectionIds));
//        }

//        public void Data(Guid connectionId, IEnumerable<byte> data)
//        {
//            connectionPoint.SendMessage(new VirtualComData(Guid.NewGuid(), connectionId, data));
//        }

//        public void Prepare(IEnumerable<Guid> connectionIds)
//        {
//            connectionPoint.SendMessage(new VirtualComPrepare(Guid.NewGuid(), connectionIds));
//        }
//    }

//    public class VirtualComDataEventArgs : EventArgs
//    {
//        public Guid ConnectionId { get; private set; }
//        public IEnumerable<byte> Data { get; private set; }

//        public VirtualComDataEventArgs(Guid connectionId, IEnumerable<byte> data)
//        {
//            ConnectionId = connectionId;
//            Data = data;
//        }
//    }
//}

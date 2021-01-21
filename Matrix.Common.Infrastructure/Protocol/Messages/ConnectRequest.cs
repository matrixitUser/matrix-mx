//using System;
//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос на соединение
//    /// сюда можно заложить проверку версии
//    /// </summary>
//    class ConnectRequest : Message
//    {
//        public ConnectRequest(Guid id, Guid connectionId, string version)
//            : base(id)
//        {
//            ConnectionId = connectionId;
//            Version = version;
//        }

//        public string Version { get; private set; }

//        public Guid ConnectionId { get; private set; }

//        public override string ToString()
//        {
//            return string.Format("запрос на соединение ({0})", Id);
//        }
//    }
//}

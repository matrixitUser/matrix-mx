//using System;
//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// ответ на запрос соединиться
//    /// сюда можно заложить проверку версии
//    /// </summary>
//    public class ConnectResponse : Message
//    {
//        public ConnectResponse(Guid id, Guid connectionId, bool isVersionAcceptable)
//            : base(id)
//        {
//            ConnectionId = connectionId;
//            IsVersionAcceptable = isVersionAcceptable;
//        }

//        public bool IsVersionAcceptable { get; private set; }

//        public Guid ConnectionId { get; private set; }

//        public override string ToString()
//        {
//            return string.Format("ответ на запрос соединения ({0}), версия совместима: {1}", Id, IsVersionAcceptable ? "да" : "нет");
//        }
//    }
//}

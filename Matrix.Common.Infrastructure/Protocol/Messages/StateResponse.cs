//using System;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// Ответ состояния
//    /// </summary>
//    public class StateResponse: Message
//    {
//        public StateResponse(Guid id, string message, bool isOk) : base(id)
//        {
//            Message = message;
//            IsOk = isOk;
//        }
//        public string Message { get; private set; }
//        public bool IsOk { get; private set; }

//        public override string ToString()
//        {
//            return "Ответ на запрос состояния";
//        }
//    }
//}

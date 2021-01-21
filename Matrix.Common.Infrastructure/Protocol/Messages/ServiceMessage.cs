//using System;
//using System.Collections.Generic;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    internal class ServiceMessage : Message
//    {
//        public ServiceMessage(Guid id) : base(id)
//        {
//        }
//    }

//    /// <summary>
//    /// Сообщение для отмены посылки кусочков большого сообщения
//    /// </summary>
//    internal class CancelMessage : ServiceMessage
//    {
//        public CancelMessage(Guid id) : base(id)
//        {
//        }
//    }

//    /// <summary>
//    /// Сообщения для продолжения длинных сообщений
//    /// </summary>
//    internal class ContinueMessage : ServiceMessage
//    {
//        public ContinueMessage(Guid id)
//            : base(id)
//        {
//        }
//    }

//    /// <summary>
//    /// Сообщение, которое используется для отправки кусочков большого сообщения
//    /// </summary>
//    internal class PartMessage : ServiceMessage
//    {
//        public PartMessage(Guid id, int index, int partsCount, IEnumerable<byte> data)
//            : base(id)
//        {
//            Data = data;
//            PartsCount = partsCount;
//            Index = index;
//        }

//        public int Index { get; private set; }
//        public int PartsCount { get; private set; }

//        public IEnumerable<byte> Data { get; set; }
//    }

//    internal class EndMessage : ServiceMessage
//    {
//        public EndReason EndReason { get; private set; }
//        public EndMessage(Guid id, EndReason endReason) : base(id)
//        {
//            EndReason = endReason;
//        }
//    }
//    internal enum EndReason
//    {
//        /// <summary>
//        /// Закончились данные - нормальное завершение
//        /// </summary>
//        DataEnd,
//        /// <summary>
//        /// Отменено принимающей стороной
//        /// </summary>
//        Canceled,
//        /// <summary>
//        /// Ошибка внутри сервера
//        /// </summary>
//        Error,
//    }
//}

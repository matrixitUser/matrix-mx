//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// базовый класс для всех сообщений между модулями
//    /// </summary>
//    public class Message
//    {
//        public virtual int Priority
//        {
//            get
//            {
//                return 1;
//            }
//        }

//        /// <summary>
//        /// код сообщения
//        /// </summary>
//        public Guid Id { get; protected set; }

//        public Message(Guid id)
//        {
//            Id = id;
//        }

//        /// <summary>
//        /// возвращает коды всех объектов внутри сообщения
//        /// используется при фильтрации сообщений
//        /// </summary>
//        /// <returns></returns>
//        public virtual IEnumerable<Guid> GetEntityIds()
//        {
//            return null;
//        }

//        /// <summary>
//        /// возвращает обрезанное сообщение
//        /// или null, если 
//        /// </summary>
//        /// <param name="avalibleEntityIds"></param>
//        /// <returns></returns>
//        public virtual Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            return null;
//        }
//    }
//    public interface IInitiator
//    {
//        /// <summary>
//        /// Инициатор
//        /// </summary>
//        Guid InitiatorId { get; }
//    }
//}

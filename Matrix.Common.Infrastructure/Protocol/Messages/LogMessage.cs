//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class LogMessage : Message
//    {
//        public IEnumerable<DataRecord> Messages { get; private set; }

//        public LogMessage(Guid id, IEnumerable<DataRecord> messages)
//            : base(id)
//        {
//            Messages = messages.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return Messages.Select(m => m.ObjectId);
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedMessages = Messages.Where(m => avalibleEntityIds.Contains(m.ObjectId));
//            return new LogMessage(Id, truncatedMessages);
//        }

//        public override string ToString()
//        {
//            return string.Format("сообщения ({0} шт)", Messages.Count());
//        }
//    }
//}

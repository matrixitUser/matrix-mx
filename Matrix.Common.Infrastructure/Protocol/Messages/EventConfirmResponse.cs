//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
// {
//    public class EventConfirmResponse:Message
//    {
//        public EventConfirmResponse(Guid id,Guid userId,Guid eventId):base(id)
//        {
//            UserId = userId;
//            EventId = eventId;
//        }

//        public Guid UserId { get;private set; }
//        public Guid EventId { get;private set; }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return new Guid[] {UserId};
//        }

//        public override string ToString()
//        {
//            return string.Format("квитирование события {0}", EventId);
//        }
//    }
//}

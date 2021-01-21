//using System.Collections.Generic;
//using Matrix.Domain.Entities;
//using System;
//using System.Linq;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class EventDataResponse : Message
//    {
//        public EventDataResponse(Guid id, IEnumerable<EventData> data)
//            : base(id)
//        {
//            Data = data.ToList();
//        }

//        public IEnumerable<EventData> Data { get;private set; }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return Data.Select(d => d.RelaitedObject).Distinct();
//        }

//        public override string ToString()
//        {
//            return string.Format("события ({0} шт)",Data.Count());
//        }
//    }
//}

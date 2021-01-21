//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class EventDataRequest:Message
//    {
//        //public DateTime DateFrom { get;private set; }
//        public int Count { get;private set; }
//        public bool NotConfirmedOnly { get; private set; }
//        public Guid? LastId { get; private set; }

//        //public EventDataRequest(Guid id,DateTime dateFrom,int count,bool notConfirmedOnly):base(id)
//        //{
//        //    DateFrom = dateFrom;
//        //    Count = count;
//        //    NotConfirmedOnly = notConfirmedOnly;
//        //}

//        public EventDataRequest(Guid id, Guid? lastId, int count, bool notConfirmedOnly)
//            : base(id)
//        {
//            LastId = lastId;
//            Count = count;
//            NotConfirmedOnly = notConfirmedOnly;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос событий с {0} в количестве {1} {2}", LastId, Count,
//                                 NotConfirmedOnly ? "только не квитированные" : "все");
//        }
//    }
//}

//using System;
//using System.Linq;
//using System.Collections.Generic;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// набор дыр
//    /// </summary>
//    public class DatesResponse : Message
//    {
//        public DatesResponse(Guid id, IEnumerable<DateTime> dates)
//            : base(id)
//        {
//            Dates = dates;
//        }

//        public Guid TubeId { get; private set; }
//        public IEnumerable<DateTime> Dates { get; private set; }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            if (avalibleEntityIds.Contains(TubeId))
//            {
//                return this;
//            }

//            return null;
//        }

//        public override string ToString()
//        {
//            return string.Format("набор дат ({0} шт)", Dates.Count());
//        }
//    }
//}

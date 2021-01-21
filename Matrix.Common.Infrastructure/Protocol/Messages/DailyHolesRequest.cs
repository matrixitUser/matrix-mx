//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос дыр в суточном архиве для точки учета
//    /// </summary>
//    public class DailyHolesRequest : Message
//    {
//        public Guid TubeId { get; private set; }
//        public DateTime DateStart { get; private set; }
//        public DateTime DateEnd { get; private set; }

//        public DailyHolesRequest(Guid id, Guid tubeId, DateTime dateStart, DateTime dateEnd)
//            : base(id)
//        {
//            TubeId = tubeId;
//            DateStart = dateStart;
//            DateEnd = dateEnd;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос суточных дыр с {0:dd.MM.yyyy} по {1:dd.MM.yyyy} для точки учета {2}", DateStart, DateEnd, TubeId);
//        }
//    }
//}

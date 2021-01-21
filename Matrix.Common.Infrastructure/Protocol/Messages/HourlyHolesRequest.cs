//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class HourlyHolesRequest : Message
//    {
//        public Guid TubeId { get; private set; }
//        public DateTime DateStart { get; private set; }
//        public DateTime DateEnd { get; private set; }

//        public HourlyHolesRequest(Guid id, Guid tubeId, DateTime dateStart, DateTime dateEnd)
//            : base(id)
//        {
//            TubeId = tubeId;
//            DateStart = dateStart;
//            DateEnd = dateEnd;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос часовых дыр с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm} для точки учета {2}", DateStart, DateEnd, TubeId);
//        }
//    }
//}

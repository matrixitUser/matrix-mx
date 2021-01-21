//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class DataTotalsRequest : Message
//    {
//        public IEnumerable<Guid> TubeIds { get; private set; }
//        public DateTime DateStart { get; private set; }
//        public DateTime DateEnd { get; private set; }

//        public DataTotalsRequest(Guid id, IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd)
//            : base(id)
//        {
//            TubeIds = tubeIds;
//            DateStart = dateStart;
//            DateEnd = dateEnd;
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return TubeIds;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос тотальных с {0:dd.MM.yyyy HH:mm} по {1:dd.MM.yyyy HH:mm}", DateStart, DateEnd);
//        }
//    }
//}

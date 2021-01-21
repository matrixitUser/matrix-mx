//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class Maquette80020Send : Message
//    {
//        public IEnumerable<DateTime> Days { get; private set; }
//        public Guid MaquetteId { get; private set; }

//        public Maquette80020Send(Guid id, Guid maquetteId, IEnumerable<DateTime> days)
//            : base(id)
//        {
//            MaquetteId = maquetteId;
//            Days = days.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return new Guid[] { MaquetteId };
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            if (avalibleEntityIds.Contains(MaquetteId))
//            {
//                return this;
//            }

//            return null;
//        }

//        public override string ToString()
//        {
//            return string.Format("отправить макет");
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class SurveyCurrentRequest : Message, IInitiator
//    {
//        public IEnumerable<Guid> TubeIds { get; private set; }

//        public SurveyCurrentRequest(Guid id, IEnumerable<Guid> tubeIds, Guid initiatorId)
//            : base(id)
//        {
//            InitiatorId = initiatorId;
//            TubeIds = tubeIds.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return TubeIds;
//        }

//        public override string ToString()
//        {
//            return string.Format("опрос текущих ({0} шт)", TubeIds.Count());
//        }

//        public Guid InitiatorId
//        {
//            get;
//            private set;
//        }
//    }
//}

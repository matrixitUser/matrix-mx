//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class VirtualComPrepare : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }

//        public VirtualComPrepare(Guid id, IEnumerable<Guid> connectionIds)
//            : base(id)
//        {
//            if (connectionIds == null)
//            {
//                ConnectionIds = new List<Guid>();
//            }
//            else
//            {
//                ConnectionIds = connectionIds.ToList();
//            }
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ConnectionIds;
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncateConnections = ConnectionIds.Where(c => avalibleEntityIds.Contains(c));
//            return new VirtualComPrepare(Id, truncateConnections);
//        }

//        public override string ToString()
//        {
//            return string.Format("подготовить {0} соединений", ConnectionIds.Count());
//        }
//    }
//}

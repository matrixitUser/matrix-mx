//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class VirtualComRelease : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }

//        public VirtualComRelease(Guid id, IEnumerable<Guid> connectionIds)
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
//            return new VirtualComRelease(Id, truncateConnections);
//        }

//        public override string ToString()
//        {
//            return string.Format("освободить {0} соединений", ConnectionIds.Count());
//        }
//    }
//}

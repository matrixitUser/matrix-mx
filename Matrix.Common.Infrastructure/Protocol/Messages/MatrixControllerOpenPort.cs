//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class MatrixControllerOpenPort : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }
//        public MatrixControllerOpenPort(Guid id, IEnumerable<Guid> connectionIds)
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

//        public override string ToString()
//        {
//            return string.Format("открыть порт у {0} соединений", ConnectionIds.Count());
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedConnections = ConnectionIds.Where(c => avalibleEntityIds.Contains(c));
//            return new MatrixControllerOpenPort(Id, truncatedConnections);
//        }
//    }
//}

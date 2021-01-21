//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class MatrixControllerCheckVersion : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }

//        public MatrixControllerCheckVersion(Guid id, IEnumerable<Guid> connectionIds)
//            : base(id)
//        {
//            ConnectionIds = connectionIds.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ConnectionIds;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос версии прошивки для {0} соединений", ConnectionIds.Count());
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedConnections = ConnectionIds.Where(c => avalibleEntityIds.Contains(c));
//            return new MatrixControllerCheckVersion(Id, truncatedConnections);
//        }
//    }
//}

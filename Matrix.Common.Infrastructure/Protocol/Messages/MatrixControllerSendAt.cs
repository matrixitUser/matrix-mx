//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class MatrixControllerSendAt : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }
//        public string At { get; private set; }

//        public MatrixControllerSendAt(Guid id, string at, IEnumerable<Guid> connectionIds)
//            : base(id)
//        {
//            ConnectionIds = connectionIds.ToList();
//            At = at;
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ConnectionIds;
//        }

//        public override string ToString()
//        {
//            return string.Format("отправка at команды {0} для {1} соединений", At, ConnectionIds.Count());
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedConnections = ConnectionIds.Where(c => avalibleEntityIds.Contains(c));
//            return new MatrixControllerSendAt(Id, At, truncatedConnections);
//        }
//    }
//}

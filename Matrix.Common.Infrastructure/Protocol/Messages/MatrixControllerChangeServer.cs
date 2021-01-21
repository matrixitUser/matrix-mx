//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class MatrixControllerChangeServer : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }
//        public string NewServer { get; private set; }

//        public MatrixControllerChangeServer(Guid id, IEnumerable<Guid> connectionIds, string newServer)
//            : base(id)
//        {
//            ConnectionIds = connectionIds.ToList();
//            NewServer = newServer;
//        }

//        public override string ToString()
//        {
//            return string.Format("смена сервера контроллеров Матрикс ({0} шт) на {1}", ConnectionIds.Count(), NewServer);
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ConnectionIds;
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedConnections = ConnectionIds.Where(c => avalibleEntityIds.Contains(c));
//            return new MatrixControllerChangeServer(Id, truncatedConnections, NewServer);
//        }
//    }
//}

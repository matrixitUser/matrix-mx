//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class MatrixControllerVirtualComData : Message
//    {
//        public Guid ConnectionId { get; private set; }
//        public IEnumerable<byte> Data { get; private set; }
//        public bool IsInit { get; private set; }

//        public MatrixControllerVirtualComData(Guid id, Guid connectionId, IEnumerable<byte> data, bool isInit = false)
//            : base(id)
//        {
//            IsInit = isInit;
//            ConnectionId = connectionId;
//            Data = data.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return new Guid[] { ConnectionId };
//        }

//        public override string ToString()
//        {
//            return string.Format("данные по виртуальному ком порту");
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            if (avalibleEntityIds.Contains(ConnectionId))
//            {
//                return this;
//            }
//            return null;
//        }
//    }
//}

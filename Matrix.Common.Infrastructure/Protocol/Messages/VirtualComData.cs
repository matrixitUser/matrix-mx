//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class VirtualComData : Message
//    {
//        public Guid ConnectionId { get; private set; }
//        public IEnumerable<byte> Data { get; private set; }

//        public VirtualComData(Guid id, Guid connectionId, IEnumerable<byte> data)
//            : base(id)
//        {
//            ConnectionId = connectionId;
//            Data = data.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return new Guid[] { ConnectionId };
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            if (avalibleEntityIds.Contains(ConnectionId))
//            {
//                return this;
//            }
//            return null;
//        }

//        public override string ToString()
//        {
//            return string.Format("данные по виртуальному ком порту");
//        }
//    }
//}

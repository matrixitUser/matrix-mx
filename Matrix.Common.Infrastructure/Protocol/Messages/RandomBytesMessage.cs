//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// произвольные данные для отправки на соединение
//    /// </summary>
//    public class RandomBytesMessage : Message
//    {
//        public IEnumerable<Guid> ConnectionIds { get; private set; }
//        public IEnumerable<byte> Bytes { get; private set; }

//        public RandomBytesMessage(Guid id, IEnumerable<Guid> connectionIds, IEnumerable<byte> bytes)
//            : base(id)
//        {
//            ConnectionIds = connectionIds.ToList();
//            Bytes = bytes.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ConnectionIds;
//        }

//        public override string ToString()
//        {
//            return string.Format("произвольные данные для {0} соединений ([{1}])", ConnectionIds.Count(), string.Join(",", Bytes.Select(b => b.ToString("X2"))));
//        }
//    }
//}

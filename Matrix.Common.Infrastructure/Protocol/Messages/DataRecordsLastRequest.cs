//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос архивных записей
//    /// </summary>
//    public class DataRecordsLastRequest : Message
//    {
//        public string Type { get; private set; }
//        public IEnumerable<Guid> ObjectIds { get; private set; }

//        public DataRecordsLastRequest(Guid id, string type, IEnumerable<Guid> objectIds)
//            : base(id)
//        {
//            Type = type;
//            ObjectIds = objectIds.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ObjectIds;
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedRecords = ObjectIds.Where(o => avalibleEntityIds.Contains(o));
//            return new DataRecordsLastRequest(Id, Type, truncatedRecords);
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос последних записей типа {0}", Type);
//        }
//    }
//}

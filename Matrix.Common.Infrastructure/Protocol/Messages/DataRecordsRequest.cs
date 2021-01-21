//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос архивных записей
//    /// </summary>
//    public class DataRecordsRequest : Message
//    {
//        public string Type { get; private set; }
//        public IEnumerable<Guid> ObjectIds { get; private set; }        
//        public object Arguments { get; private set; }

//        public DataRecordsRequest(string type, IEnumerable<Guid> objectIds, object arguments)
//            : base(Guid.NewGuid())
//        {
//            Type = type;
//            ObjectIds = objectIds.ToList();
//            Arguments = arguments;
//        }        

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return ObjectIds;
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            IEnumerable<Guid> truncatedObjects = ObjectIds.Where(r => avalibleEntityIds.Contains(r));
//            var tr = new DataRecordsRequest(Type, truncatedObjects, Arguments);
//            tr.Id = Id;
//            return tr;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос записей типа {0}", Type);
//        }
//    }
//}

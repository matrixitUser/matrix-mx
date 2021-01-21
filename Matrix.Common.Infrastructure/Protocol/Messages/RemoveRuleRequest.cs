//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class RemoveRuleRequest : Message
//    {
//        public string Name { get; private set; }
//        public IEnumerable<Guid> Ids { get; private set; }

//        public RemoveRuleRequest(Guid id, string name, IEnumerable<Guid> ids)
//            : base(id)
//        {
//            Name = name;
//            Ids = ids;
//        }

//        public override string ToString()
//        {
//            return string.Format("удалить правило: {0}, для: {1} объектов", Name, Ids == null ? "всех" : Ids.Count().ToString());
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// специальное сообщение добавть правило для сообщений определенного типа
//    /// не проверяется фильтрами
//    /// </summary>
//    public class AddRuleRequest : Message
//    {
//        public string Name { get; private set; }
//        public IEnumerable<Guid> Ids { get; private set; }

//        public AddRuleRequest(Guid id, string name, IEnumerable<Guid> ids)
//            : base(id)
//        {
//            Name = name;
//            Ids = ids == null ? null : ids.ToList();
//        }

//        public override string ToString()
//        {
//            return string.Format("добавить правило: {0}, для: {1} объектов", Name, Ids == null ? "всех" : Ids.Count().ToString());
//        }
//    }
//}

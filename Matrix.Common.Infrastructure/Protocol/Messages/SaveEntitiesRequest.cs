//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос на сохранение сущностей
//    /// сохранить можно корни аггрегации
//    /// </summary>
//    public class SaveEntitiesRequest : Message
//    {
//        public SaveEntitiesRequest(IEnumerable<AggregationRoot> entities, Guid userId)
//            : base(Guid.NewGuid())
//        {
//            if (entities == null) throw new ArgumentException("аргумент entities не может быть пустым");
//            Entities = entities;
//            UserId = userId;
//        }
//        public IEnumerable<AggregationRoot> Entities { get; private set; }
//        public Guid UserId { get; private set; }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return Entities.Select(e => e.Id);
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedEntities = Entities.Where(e => avalibleEntityIds.Contains(e.Id));
//            return new SaveEntitiesRequest(truncatedEntities, UserId);
//        }

//        public override string ToString()
//        {
//            return string.Format("сохранить {0} сущностей", Entities.Count());
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// сообщение о обновлении сущностей
//    /// </summary>
//    public class EntitiesUpdatedMessage : Message
//    {
//        public IEnumerable<CacheModelUpdateUnit> Units { get; private set; }

//        public EntitiesUpdatedMessage(IEnumerable<CacheModelUpdateUnit> units)
//            : base(Guid.NewGuid())
//        {
//            Units = units.ToList();
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedUnits = Units.Where(u => avalibleEntityIds.Contains(u.UpdatedCachedModel.Id));
//            return new EntitiesUpdatedMessage(truncatedUnits);
//        }

//        public override string ToString()
//        {
//            return string.Format("сущности обновлены ({0}  шт)", Units.Count());
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// список сущностей
//    /// </summary>
//    public class EntityListResponse : Message
//    {
//        public IEnumerable<AggregationRoot> Entities { get; private set; }

//        public EntityListResponse(Guid id, IEnumerable<AggregationRoot> entities)
//            : base(id)
//        {
//            Entities = entities;
//        }

//        public override string ToString()
//        {
//            return string.Format("ответ список сущностей ({0}) ({1} шт)", Id, Entities.Count());
//        }
//    }
//}

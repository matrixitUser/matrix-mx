//using System.Collections.Generic;
//using Matrix.Domain.Entities;
//using System;
//using System.Linq;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class NonCachedDataResponse : Message
//    {
//        public NonCachedDataResponse(Guid id, IEnumerable<INonCached> data)
//            : base(id)
//        {
//            Data = data.ToList();
//        }
//        public IEnumerable<INonCached> Data { get; private set; }

//        public override string ToString()
//        {
//            return string.Format("некэшируемые данные ({0} шт)", Data.Count());
//        }
//    }
//}

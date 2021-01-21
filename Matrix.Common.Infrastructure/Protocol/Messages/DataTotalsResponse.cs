//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class DataTotalsResponse : Message
//    {
//        public TotalResult TotalResult { get; private set; }

//        public DataTotalsResponse(Guid id, TotalResult totalResult)
//            : base(id)
//        {
//            TotalResult = totalResult;
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return TotalResult.Sum.Union(TotalResult.Avg).Union(TotalResult.Left).Union(TotalResult.Right).Select(d => d.TubeId).Distinct();
//        }

//        public override string ToString()
//        {
//            return string.Format("тотальные значения ({0} шт)", TotalResult.Sum.Union(TotalResult.Avg).Union(TotalResult.Left).Union(TotalResult.Right).Count());
//        }
//    }
//}

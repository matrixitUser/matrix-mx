//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Matrix.Domain.Entities;

//namespace Matrix.Web.Host.Data
//{
//    class HoleHandler : IRecordHandler
//    {
//        public void Handle(IEnumerable<DataRecord> records)
//        {
//            //1. группируем по объектам
//            var groups = records.GroupBy(r => r.ObjectId);
//            foreach (var objectRecs in groups)
//            {
//                //var cache = Cache.Instance.GetNodeCache(objectRecs.Key);
                
//            }
//        }
//    }
//}

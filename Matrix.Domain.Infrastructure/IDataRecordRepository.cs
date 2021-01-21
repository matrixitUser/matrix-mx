//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Domain.Infrastructure
//{
//    public interface IDataRecordRepository
//    {
//        //void Save(IEnumerable<DataRecord> data);
//        //void Delete<TSelector>(IEnumerable<DataRecord> records, Func<DataRecord, TSelector> selector);
//        //IEnumerable<DataRecord> Get(Expression<Func<DataRecord, bool>> predicate);
//        IEnumerable<DataRecord> GetLast(string type, IEnumerable<Guid> objectIds);
//        IEnumerable<DataRecord> Count(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd);
//        IEnumerable<DataRecord> Dates(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd);
//    }
//}

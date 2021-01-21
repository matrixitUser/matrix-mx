using System;
using System.Collections.Generic;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
{
    public interface IDataRecordRepository
    {
        IEnumerable<DataRecord> Count(DateTime start, DateTime end, string type, IEnumerable<Guid> objectIds);
        DataRecord Get(Guid id);
        IEnumerable<DataRecord> Get(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type);
        IEnumerable<DataRecordDate> GetDates(string type, DateTime start, DateTime end, IEnumerable<Guid> objectIds);
        IEnumerable<DataRecordDate> GetDatesAll(string type, DateTime start, DateTime end);
        IEnumerable<DataRecord> GetLast(string type, Guid[] objectIds, int count);
        DateTime GetLastDate(string type, Guid objectId);
        DateTime GetLastDate1(string type, Guid objectId);
        void Save(IEnumerable<DataRecord> records);
    }
}
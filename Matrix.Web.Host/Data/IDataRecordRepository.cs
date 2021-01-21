using System;
using System.Collections.Generic;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
{
    public interface IDataRecordRepository
    {
        IEnumerable<DataRecord> Count(DateTime start, DateTime end, string type, IEnumerable<Guid> objectIds);
        DataRecord Get(Guid id);
        //DataRecord Get(DateTime date, Guid objectId, string type);
        IEnumerable<DataRecord> Get(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type);
        void RecordsDelete(List<Guid> ids, string type);
        IEnumerable<DataRecord> GetWithidsAndS1(List<Guid> ids, DateTime start, DateTime end, string type, string s1); //add 21.01.2019 for PLC
        void DeleteRow(List<Guid> ids);
        IEnumerable<DataRecord> GetDataOnlyWithType(DateTime start, DateTime end, string type);
        IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Get3D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type, string[] tableRange = null);
        IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> Get4D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type);
        HashSet<DateTime> GetDateSet(DateTime start, DateTime end, Guid id, string type);
        string[] TableRange(DateTime start, DateTime end, string type);
        IDictionary<string, DataRecord> GetByParameter(DateTime date, Guid id, string type);
        IDictionary<string, DataRecord> GetByParameter(string[] tableRange, DateTime date, Guid id, string type);
        IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(DateTime start, DateTime end, Guid id, string type);
        IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(string[] tableRange, DateTime start, DateTime end, Guid id, string type);
        IEnumerable<DataRecordDate> GetDates(string type, DateTime start, DateTime end, IEnumerable<Guid> objectIds);
        IEnumerable<DataRecordDate> GetDatesAll(string type, DateTime start, DateTime end);
        IEnumerable<DataRecord> GetLast(string type, Guid[] objectIds, int count);
        IEnumerable<DataRecord> GetLastRecords(string type, Guid[] objectIds, DateTime start = default(DateTime));
        DateTime GetLastDate(string type, Guid objectId);
        DateTime GetLastDate1(string type, Guid objectId);
        void Save(IEnumerable<DataRecord> records);
    }
}
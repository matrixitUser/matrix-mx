using System;
using System.Collections.Generic;
using System.Dynamic;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Domain.Infrastructure.EntityFramework.Repositories;
using System.Configuration;

namespace Matrix.Web.Host.Data
{
    ///<summary>
    ///отвечает за взаимодействие с хранилищем.
    ///хранит локальный кеш сущностей для быстрого доступа
    ///todo хранить бы сразу в виде графа?
    ///</summary>
    internal class Cache
    {
        private readonly IDataRecordRepository repository;

        private static readonly ILog log = LogManager.GetLogger(typeof(Cache));

        public DataRecord GetRecord(Guid recordId)
        {            
            return repository.Get(recordId);
        }

        public void SaveRecords(IEnumerable<DataRecord> dataRecords)
        {            
            repository.Save(dataRecords);
        }

        //public DataRecord GetRecord(DateTime date, string type, Guid id)
        //{
        //    return repository.Get(date, id, type);
        //}

        public IEnumerable<DataRecord> GetRecords(DateTime start, DateTime end, string type, Guid[] ids)
        {         
            return repository.Get(start, end, ids, type);
        }
        public void DeleteRecords(List<Guid> ids, string type)
        {
            repository.RecordsDelete(ids, type);
        }
        //add 21.01.2019 for PLC
        public IEnumerable<DataRecord> GetWithIdAndS1Records(List<Guid> ids, DateTime start, DateTime end, string type, string s1)
        {
            return repository.GetWithidsAndS1(ids, start, end, type, s1);
        }
        public void DeleteRow(List<Guid> ids)
        {
            repository.DeleteRow(ids);
        }
        public IEnumerable<DataRecord> GetDataOnlyWithTypeRecords(DateTime start, DateTime end, string type)
        {
            return repository.GetDataOnlyWithType(start, end, type);
        }
        public IDictionary<Tuple<Guid, DateTime, string>, DataRecord> GetRecords3D(DateTime start, DateTime end, string type, Guid[] ids, string[] tableRange = null)
        {
            return repository.Get3D(start, end, ids, type, tableRange);
        }

        public IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> GetRecords4D(DateTime start, DateTime end, string type, Guid[] ids)
        {
            return repository.Get4D(start, end, ids, type);
        }

        public HashSet<DateTime> GetDateSet(DateTime start, DateTime end, string type, Guid id)
        {
            return repository.GetDateSet(start, end, id, type);
        }

        //public IDictionary<DateTime, DataRecord> GetRecordsByDate(DateTime start, DateTime end, string type, Guid id)
        //{
        //    return repository.GetByDate(start, end, id, type);
        //}

        public IDictionary<string, DataRecord> GetRecordsByParameter(DateTime date, Guid id, string type)
        {
            return repository.GetByParameter(date, id, type);
        }

        public IDictionary<string, DataRecord> GetRecordsByParameter(string[] tableRange, DateTime date, Guid id, string type)
        {
            return repository.GetByParameter(tableRange, date, id, type);
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetRecordsByDateParameter(DateTime start, DateTime end, Guid id, string type)
        {
            return repository.GetByDateParameter(start, end, id, type);
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetRecordsByDateParameter(string[] tableRange, DateTime start, DateTime end, Guid id, string type)
        {
            return repository.GetByDateParameter(tableRange, start, end, id, type);
        }

        public string[] GetTableRange(DateTime start, DateTime end, string type)
        {
            return repository.TableRange(start, end, type);
        }

        public IEnumerable<DataRecord> GetLastRecords(string type, Guid[] ids, int count)
        {            
            return repository.GetLast(type, ids, count);
        }

        public IEnumerable<DataRecord> GetLastRecords(string type, Guid[] ids, DateTime start = default(DateTime))
        {
            return repository.GetLastRecords(type, ids, start);
        }

        public IEnumerable<DataRecordDate> GetDatesAll(string type, DateTime start, DateTime end)
        {            
            return repository.GetDatesAll(type, start, end);
        }

        public IEnumerable<DataRecordDate> GetDates(string type, DateTime start, DateTime end, IEnumerable<Guid> objectIds)
        {            
            return repository.GetDates(type, start, end, objectIds);
        }

        public DateTime GetLastDate(string type, Guid objectId)
        {            
            return repository.GetLastDate(type, objectId);
        }

        public DateTime GetLastDate1(string type, Guid objectId)
        {            
            return repository.GetLastDate1(type, objectId);
        }

        private Cache()
        {
            var storageType = ConfigurationManager.AppSettings["storage-type"];
            if (storageType == "mssql")
            {
                repository = new MsSqlDataRecordRepository();
            }
            else if (storageType == "pg")
            {
                try
                {
                    repository = new PGDataRecordRepository();
                }
                catch(TypeInitializationException ex)
                {
                    Console.WriteLine(ex.InnerException);
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        private static Cache instance = null;
        public static Cache Instance
        {
            get
            {
                if (instance == null) instance = new Cache();
                return instance;
            }
        }
    }

    static class CacheExtensions
    {
        public static IEnumerable<dynamic> ToDynamic(this IEnumerable<DataRecord> records)
        {
            foreach (var record in records)
            {
                yield return record.ToDynamic();
            }
        }

        public static dynamic ToDynamic(this DataRecord record)
        {
            dynamic result = new ExpandoObject();
            var dresult = result as IDictionary<string, object>;
            foreach (var property in typeof(DataRecord).GetProperties())
            {
                dresult.Add(ToCamelCase(property.Name), property.GetValue(record, null));
            }
            return result;
        }

        private static string ToCamelCase(string name)
        {
            return name.Substring(0, 1).ToLower() + name.Substring(1, name.Length - 1);
        }
    }
}

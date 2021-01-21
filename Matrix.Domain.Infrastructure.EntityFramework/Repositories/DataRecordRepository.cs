//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;
//using System.Data.Common;
//using System.Data;
//using System.Linq.Expressions;
//using System.Data.SqlClient;
//using log4net;
//using System.Diagnostics;
//using System.Configuration;

//namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
//{
//    public class DataRecordRepository : BaseRepository, IDataRecordRepository
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(DataRecordRepository));

//        public DataRecordRepository(Context context) : base(context) { }

//        public static readonly object saveLocker = new object();

//        //public void Save(IEnumerable<DataRecord> records)
//        //{
//        //    lock (saveLocker)
//        //    {
//        //        var sw = new Stopwatch();
//        //        sw.Start();
//        //        log.Debug(string.Format("начинаем сохранять записи"));
//        //        //context.BulkInsert(data.ToList());

//        //        //DbConnection conn = context.Database.Connection;

//        //        var cs = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;// context.Database.Connection.ConnectionString;
//        //        var conn = new SqlConnection(cs);

//        //        var properties = typeof(DataRecord).GetProperties();

//        //        var step = MAX_PARAMETERS_COUNT / properties.Count();
//        //        for (int offset = 0; offset < records.Count(); offset += step)
//        //        {
//        //            using (var cmd = conn.CreateCommand())
//        //            {
//        //                int count = 0;
//        //                var insertQuery = string.Format("insert into [DataRecordBuffer]({0})values", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))));

//        //                var rows = new List<string>();
//        //                foreach (var record in records.Skip(offset).Take(step))
//        //                {
//        //                    var parameters = new List<string>();
//        //                    foreach (var property in properties)
//        //                    {
//        //                        //var name = string.Format("@{0}{1}", property.Name, count);
//        //                        //cmd.Parameters.AddWithValue(name, property.GetValue(record, null));
//        //                        //parameters.Add(name);
//        //                        var parameter = cmd.CreateParameter();
//        //                        parameter.ParameterName = string.Format("@{0}{1}", property.Name, count);
//        //                        parameter.Value = property.GetValue(record, null);
//        //                        if (parameter.Value == null) parameter.Value = DBNull.Value;
//        //                        if (parameter.Value != null && parameter.Value.GetType() == typeof(DateTime))
//        //                        {
//        //                            parameter.DbType = DbType.DateTime2;
//        //                        }

//        //                        cmd.Parameters.Add(parameter);
//        //                        parameters.Add(parameter.ParameterName);
//        //                    }
//        //                    count++;
//        //                    rows.Add(string.Format("({0})", string.Join(",", parameters)));
//        //                }
//        //                insertQuery = string.Format("{0}{1}", insertQuery, string.Join(",", rows));

//        //                cmd.CommandText = insertQuery;
//        //                conn.Open();
//        //                try
//        //                {
//        //                    cmd.ExecuteNonQuery();
//        //                }
//        //                catch (Exception ex)
//        //                {
//        //                    log.Error(string.Format("ошибка при сохраниении записей в бд"), ex);
//        //                }
//        //                finally
//        //                {
//        //                    conn.Close();
//        //                }
//        //            }
//        //        }

//        //        sw.Stop();
//        //        log.DebugFormat(string.Format("закончили сохранять записи {0} шт за {1} мс", records.Count(), sw.ElapsedMilliseconds));
//        //    }
//        //}

//        //const int MAX_PARAMETERS_COUNT = 2000;

//        //public void Delete<TSelector>(IEnumerable<DataRecord> records, Func<DataRecord, TSelector> selector)
//        //{
//        //    return;
//        //    if (records == null || !records.Any()) return;

//        //    var sw = new Stopwatch();
//        //    sw.Start();
//        //    log.Debug(string.Format("начинаем удалять {0} записей", records.Count()));
//        //    DbConnection conn = context.Database.Connection;

//        //    var propertyNames = typeof(TSelector).GetProperties().Select(p => p.Name);
//        //    var properties = typeof(DataRecord).GetProperties().Where(p => propertyNames.Contains(p.Name));

//        //    var step = MAX_PARAMETERS_COUNT / properties.Count();
//        //    for (int offset = 0; offset < records.Count(); offset += step)
//        //    {
//        //        using (DbCommand cmd = conn.CreateCommand())
//        //        {
//        //            int count = 0;
//        //            var deleteQuery = new StringBuilder();
//        //            foreach (var record in records.Skip(offset).Take(step))
//        //            {
//        //                var pairs = new List<string>();
//        //                foreach (var property in properties)
//        //                {
//        //                    var parameter = cmd.CreateParameter();
//        //                    parameter.ParameterName = string.Format("@{0}{1}", property.Name, count);
//        //                    parameter.Value = property.GetValue(record, null);
//        //                    if (parameter.Value.GetType() == typeof(DateTime))
//        //                    {
//        //                        parameter.DbType = DbType.DateTime2;
//        //                    }
//        //                    cmd.Parameters.Add(parameter);
//        //                    pairs.Add(string.Format("[{0}]={1}", property.Name, parameter.ParameterName));
//        //                }
//        //                count++;

//        //                deleteQuery.AppendFormat("delete from [{0}] where {1};",
//        //                    typeof(DataRecord).Name, string.Join(" and ", pairs));
//        //            }
//        //            cmd.CommandText = deleteQuery.ToString();
//        //            conn.Open();
//        //            cmd.ExecuteNonQuery();
//        //            conn.Close();
//        //        }
//        //    }
//        //    sw.Stop();
//        //    log.DebugFormat(string.Format("закончили удалять за {0} мс", sw.ElapsedMilliseconds));

//        //}

//        //public IEnumerable<DataRecord> Get(Expression<Func<DataRecord, bool>> predicate)
//        //{
//        //    try
//        //    {
//        //        var sw = new Stopwatch();
//        //        sw.Start();
//        //        log.Debug(string.Format("начинаем получать записи"));
//        //        var result = context.Set<DataRecord>().Where(predicate).ToList();
//        //        sw.Stop();
//        //        log.DebugFormat(string.Format("закончили получать записи за {0} мс", sw.ElapsedMilliseconds));
//        //        return result;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        log.Error(string.Format("не удалось получить записи"), ex);
//        //    }
//        //    return new DataRecord[] { };
//        //}

//        public IEnumerable<DataRecord> GetLast(string type, IEnumerable<Guid> objectIds)
//        {

//            if (objectIds == null || !objectIds.Any()) return new DataRecord[] { };
//            try
//            {
//                var sw = new Stopwatch();
//                sw.Start();
//                log.Debug(string.Format("начинаем получать последние записи типа {0}", type));

//                var query = string.Format(@"select d1.* from datarecordview d1 inner join
//(select max([date]) as [date],objectid from datarecordview where 
//		objectid in ({0})
//		and [type]='{1}'
//	group by objectid) d2 on d1.[date]=d2.[date] and d1.objectid=d2.objectid
//where d1.[type]='{1}'", string.Join(",", objectIds.Select(i => string.Format("'{0}'", i))), type);

//                var result = context.Database.SqlQuery<DataRecord>(query).ToList();

//                sw.Stop();
//                log.DebugFormat(string.Format("закончили получать последние записи типа {0} за {1} мс", type, sw.ElapsedMilliseconds));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                log.Error(string.Format("не удалось получить последние записи"), ex);
//            }
//            return null;

//        }

//        /// <summary>
//        /// максимально число параметров, которое можно передать команды
//        /// </summary>
//        const int MAX_PARAMETERS_COUNT = 500;
//        /// <summary>
//        /// имя строки соединеия
//        /// </summary>
//        const string CS_NAME = "Context";

//        public IEnumerable<DataRecord> Count(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd)
//        {
//            if (objectIds == null || !objectIds.Any()) return new DataRecord[] { };

//            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
//            var con = new SqlConnection(cs);

//            var res = new List<DataRecord>();

//            var step = MAX_PARAMETERS_COUNT - 3;

//            var sw = new Stopwatch();
//            sw.Start();
//            log.Debug(string.Format("начинаем получать количество записей типа {0}", type));

//            try
//            {                
//                con.Open();
//                for (var offset = 0; offset < objectIds.Count(); offset += step)
//                {

//                    var parameters = new List<string>();
//                    var cmd = new SqlCommand();
//                    cmd.Connection = con;

//                    cmd.Parameters.AddWithValue("@type", type);
//                    cmd.Parameters.AddWithValue("@start", dateStart);
//                    cmd.Parameters.AddWithValue("@end", dateEnd);

//                    var pn = 0;
//                    foreach (var obj in objectIds.Skip(offset).Take(step))
//                    {
//                        var parameterName = string.Format("@{0}", pn++);
//                        cmd.Parameters.AddWithValue(parameterName, obj);
//                        parameters.Add(parameterName);
//                    }


//                    var query1 = string.Format(@"select 
//ObjectId,count(*) as I1
//from 
//(select distinct objectid,[date] from datarecordview
//where [type]=@type
//and [date] between @start and @end
//and objectid in ({0})) t
//group by t.ObjectId", string.Join(",", parameters));

//                    cmd.CommandText = query1;
//                    var reader = cmd.ExecuteReader();
//                    while (reader.Read())
//                    {
//                        res.Add(new DataRecord()
//                        {
//                            Id = Guid.NewGuid(),
//                            ObjectId = reader.GetGuid(0),
//                            I1 = reader.GetInt32(1),
//                            Type = string.Format("{0}Count", type),
//                            Date = dateEnd
//                        });
//                    }
//                    reader.Close();
//                }
//                log.DebugFormat(string.Format("закончили получать количество записей типа {0} за {1} мс", type, sw.ElapsedMilliseconds));

//                return res;
//            }
//            catch (Exception ex)
//            {
//                log.Error(string.Format("не удалось получить количество записей типа {0}", type), ex);
//            }
//            finally
//            {
//                con.Close();
//            }
//            return null;
//        }

//        public IEnumerable<DataRecord> Dates(string type, IEnumerable<Guid> objectIds, DateTime dateStart, DateTime dateEnd)
//        {
//            if (objectIds == null || !objectIds.Any()) return new DataRecord[] { };
//            try
//            {
//                var sw = new Stopwatch();
//                sw.Start();
//                log.Debug(string.Format("начинаем получать даты записей типа {0}", type));
//                var query = string.Format(@"select distinct [Date],
//ObjectId, @type+'Date' as [Type],newid() as Id,
//null as D1,null as D2,null as D3,null as I1,null as I2,null as I3,null as Dt1,null as Dt2,null as Dt3,
//null as S1,null as S2,null as S3,null as G1,null as G2,null as G3
//from datarecordview 
//where [date] between @start and @end
//and [type]=@type
//and objectid in ({0})", string.Join(",", objectIds.Select(i => string.Format("'{0}'", i))), type);

//                var parameters = new object[] 
//                { 
//                    new SqlParameter("@type",type),
//                    new SqlParameter("@start",dateStart),
//                    new SqlParameter("@end",dateEnd)
//                };

//                var result = context.Database.SqlQuery<DataRecord>(query, parameters).ToList();
//                log.DebugFormat(string.Format("закончили получать даты записей типа {0} за {1} мс", type, sw.ElapsedMilliseconds));
//                return result;
//            }
//            catch (Exception ex)
//            {
//                log.Error(string.Format("не удалось получить даты записей типа {0}", type), ex);
//            }
//            return null;
//        }

//    }

//    /// <summary>
//    /// генерирует скрипт и сохраняет данные в бд
//    /// </summary>
//    public class Saver
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(Saver));

//        const int MAX_PARAMETERS_COUNT = 2000;

//        /// <summary>
//        /// сохраняет 
//        /// </summary>
//        /// <param name="tableName"></param>
//        /// <param name="fields"></param>
//        /// <param name="records"></param>
//        public void Save(string tableName, string[] fields, IEnumerable<DataRecord> records)
//        {

//            var sw = new Stopwatch();
//            sw.Start();
//            log.Debug(string.Format("начинаем сохранять записи"));

//            var cs = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;
//            var conn = new SqlConnection(cs);

//            var properties = typeof(DataRecord).GetProperties().Where(p => fields.Contains(p.Name));

//            var step = MAX_PARAMETERS_COUNT / properties.Count();
//            for (int offset = 0; offset < records.Count(); offset += step)
//            {
//                using (var cmd = conn.CreateCommand())
//                {
//                    int count = 0;
//                    var insertQuery = string.Format("insert into [{0}]({1})values", tableName, string.Join(",", fields.Select(f => string.Format("[{0}]", f))));

//                    var rows = new List<string>();
//                    foreach (var record in records.Skip(offset).Take(step))
//                    {
//                        var parameters = new List<string>();
//                        foreach (var property in properties)
//                        {
//                            var parameter = cmd.CreateParameter();
//                            parameter.ParameterName = string.Format("@{0}{1}", property.Name, count);
//                            parameter.Value = property.GetValue(record, null);
//                            if (parameter.Value == null) parameter.Value = DBNull.Value;
//                            if (parameter.Value != null && parameter.Value.GetType() == typeof(DateTime))
//                            {
//                                parameter.DbType = DbType.DateTime2;
//                            }

//                            cmd.Parameters.Add(parameter);
//                            parameters.Add(parameter.ParameterName);
//                        }
//                        count++;
//                        rows.Add(string.Format("({0})", string.Join(",", parameters)));
//                    }
//                    insertQuery = string.Format("{0}{1}", insertQuery, string.Join(",", rows));

//                    cmd.CommandText = insertQuery;
//                    conn.Open();
//                    try
//                    {
//                        cmd.ExecuteNonQuery();
//                    }
//                    catch (Exception ex)
//                    {
//                        log.Error(string.Format("ошибка при сохраниении записей в бд"), ex);
//                    }
//                    finally
//                    {
//                        conn.Close();
//                    }
//                }
//            }

//            sw.Stop();
//            log.DebugFormat(string.Format("закончили сохранять записи {0} шт за {1} мс", records.Count(), sw.ElapsedMilliseconds));

//        }
//    }

//    /// <summary>
//    /// распределяет архивные данные по разным секциям
//    /// создает секции при необходимости
//    /// </summary>
//    public class Distributer
//    {
//        private readonly List<Rule> rules = new List<Rule>();

//        private readonly Saver saver = new Saver();

//        public void Distribute(IEnumerable<DataRecord> records)
//        {
//            foreach (var rule in rules)
//            {
//                if (!records.Any()) break;

//                Func<DataRecord, bool> predicate = r => rule.Types.Contains(r.Type);
//                var recordsToWork = records.Where(r => predicate(r)).ToArray();
//                records = records.Where(r => !predicate(r)).ToArray();

//                //saver.Save()
//            }
//        }
//    }

//    public class Rule
//    {
//        public string TableNameMask { get; set; }
//        public string[] UsedFields { get; set; }
//        public string[] IndexFields { get; set; }
//        public string[] Types { get; set; }
//    }
//}

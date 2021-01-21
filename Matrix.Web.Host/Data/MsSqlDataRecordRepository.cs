using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;
using NLog;
using Matrix.Web.Host.Data;
using System.Data.Common;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
{
    /// <summary>
    /// ручное чтение/запись архивов
    /// 
    /// при чтении используется хинт NOLOCK - "грязное" чтение, позволяет избежать блокировок
    /// запись в бд BULK INSERT-ом, при этом сначала пишем во временную таблицу, а потом MERGE-им ее с целевой
    /// </summary>
    public class MsSqlDataRecordRepository : IDataRecordRepository
    {
        //private static readonly ILog log = LogManager.GetLogger(typeof(DataRecordRepository2));
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        int maxyear = 0;
        int MAX_YEAR
        {
            get
            {
                if (maxyear == 0) maxyear = DateTime.Now.Year + 1;
                return maxyear;
            }

        }
        /// <summary>
        /// имя таблицы с необрабатываемыми записями
        /// </summary>
        const string TABLE_DEFAULT = "DataRecordDefault";

        /// <summary>
        /// имя таблицы именами секций
        /// </summary>
        const string TABLE_SECTIONS = "DataRecordSections";

        /// <summary>
        /// имя представления с объединением секций
        /// </summary>
        const string TABLE_ROWS = "RowsCache";

        /// <summary>
        /// имя представления с объединением секций
        /// </summary>
        const string TABLE_VIEW = "DataRecordView";

        /// <summary>
        /// максимально число параметров, которое можно передать команды
        /// </summary>
        const int MAX_PARAMETERS_COUNT = 2000;

        /// <summary>
        /// имя строки соединеия
        /// </summary>
        const string CS_NAME = "Context";

        private static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            }
        }

        private static readonly object saveLocker = new object();

        private readonly MsSqlBulkInserter bulkInserter = new MsSqlBulkInserter();

        private RulesCollection _rules;
        public MsSqlDataRecordRepository()
        {
            _rules = SaveRulesSection.Instance.Rules;
        }

        public void Save(IEnumerable<DataRecord> records)
        {
            if (records == null || !records.Any())
            {
                log.Warn("записи для сохранения отсутствуют");
                return;
            }

            var allProperties = typeof(DataRecord).GetProperties().Select(p => p.Name).ToArray();

            lock (saveLocker)
            {
                using (var con = new SqlConnection(ConnectionString))
                {
                    con.Open();
                    var cnt = records.Count();
                    var sw = new Stopwatch();
                    sw.Start();

                    log.Debug(string.Format("начинаем сохранять записи {0} шт, типы [{1}]", cnt, string.Join(",", records.Select(r => r.Type).Distinct())));

                    try
                    {
                        Func<DataRecord, string, IEnumerable<string>, string> formatter = (r, format, parameters) =>
                        {
                            var values = parameters.Select(p => typeof(DataRecord).GetProperty(p).GetValue(r, null));
                            return string.Format(format, values.ToArray());
                        };

                        //1. 
                        foreach (RuleElement rule in SaveRulesSection.Instance.Rules)
                        {
                            if (!records.Any()) break;

                            var uf = rule.UniqueFields.ToStringArray().ToArray();

                            var ruleTypes = rule.Types.ToStringArray();
                            Func<DataRecord, bool> predicate = r => ruleTypes.Contains(r.Type);
                            //записи удовлетворяющие правилу
                            var allowToRuleRecords = records.Where(r => predicate(r)).ToArray();
                            //выкидываем уже обработанные записи
                            records = records.Where(r => !predicate(r));

                            if (!allowToRuleRecords.Any()) continue;
                            log.Debug(string.Format("сработало правило {0}", rule.Name));
                            //разбивка записей правила по секциям
                            var groupedByTables = allowToRuleRecords.GroupBy(r => formatter(r, rule.Format, rule.FormatFields.ToStringArray())).Select(g => new { TableName = g.Key, Records = g.ToArray() });
                            foreach (var table in groupedByTables)
                            {
                                var year = table.Records.FirstOrDefault().Date.Year;
                                if (year <= 2010 || year > MAX_YEAR)
                                {
                                    log.Warn(string.Format("записи с нереальным годом {0}, пропуск", year));
                                    continue;
                                }

                                try
                                {
                                    var uniqueProperties = rule.UniqueFields.ToStringArray().ToArray();

                                    if (!IsTableExists(table.TableName, con))
                                    {
                                        CreateDataRecordTable(table.TableName, uniqueProperties, con);
                                        AddSection(con, table.TableName);
                                        RebuildView(con);
                                    }
                                    else
                                    {
                                        log.Debug(string.Format("таблица {0} уже существует", table.TableName));
                                    }

                                    //DeleteDublicates(table.TableName, uf, table.Records);
                                    //проверка корректности полей используемых в индексе (например допустимая длина)
                                    var indexCheckRecords = new List<DataRecord>();
                                    foreach (var r in table.Records)
                                    {
                                        if (uf.Contains("S1") && r.S1.Length > 255) r.S1 = r.S1.Substring(0, 250);
                                        if (uf.Contains("S2") && r.S2.Length > 255) r.S2 = r.S2.Substring(0, 250);
                                        if (uf.Contains("S3") && r.S3.Length > 255) r.S3 = r.S3.Substring(0, 250);
                                        indexCheckRecords.Add(r);
                                    }

                                    BulkUpdate(indexCheckRecords, table.TableName, uniqueProperties, allProperties, con);
                                }
                                catch (Exception tex)
                                {
                                    log.Error(string.Format("ошибка при сохранении записей в секцию {0}", table.TableName), tex);
                                }
                            }
                        }

                        if (records.Any())
                        {
                            log.Debug(string.Format("сохранение записей не попавших под правила {0} шт, типы [{1}]", records.Count(), string.Join(",", records.Select(r => r.Type).Distinct())));
                            //Insert(TABLE_DEFAULT, records, new string[] { });

                            BulkUpdate(records, TABLE_DEFAULT, allProperties, allProperties, con);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("ошибка при сохранении записей (количество записей {0} шт типы [{1}])", cnt, string.Join(",", records.Select(r => r.Type).Distinct())), ex);
                    }
                    finally
                    {
                    }
                    sw.Stop();
                    log.Debug(string.Format("общее время обработки {0} записей {1} мс", cnt, sw.ElapsedMilliseconds));
                }
            }
        }

        private void BulkUpdate(IEnumerable<DataRecord> records, string table, string[] uniqueFields, string[] allFields, SqlConnection con)
        {
            var tempTable = string.Format("#tmp{0}{1}", table, DateTime.Now.ToString("ddMMyyHHmmssttt"));
            CreateDataRecordTable(tempTable, new string[] { }, con);

            bulkInserter.Insert(records, tempTable, con);

            var mrg = new StringBuilder();
            mrg.AppendFormat("merge [{0}] r ", table);
            mrg.AppendFormat("using ");
            mrg.AppendFormat("(select ");
            mrg.AppendFormat("{0}", string.Join(",", uniqueFields.Select(p => string.Format("[{0}]", p))));
            mrg.Append(uniqueFields.Any() && allFields.Where(ap => !uniqueFields.Contains(ap)).Any() ? "," : "");
            mrg.AppendFormat("{0}", string.Join(",", allFields.Where(ap => !uniqueFields.Contains(ap)).Select(p => string.Format("max([{0}]) as [{0}]", p))));
            mrg.AppendFormat(" from {0} group by {1}) t ", tempTable, string.Join(",", uniqueFields.Select(p => string.Format("[{0}]", p))));
            mrg.AppendFormat("on {0} ", string.Join(" and ", uniqueFields.Select(p => string.Format("r.[{0}]=t.[{0}]", p))));
            if (allFields.Any(ap => !uniqueFields.Contains(ap))) mrg.AppendFormat(" when matched then update set {0} ", string.Join(",", allFields.Where(ap => !uniqueFields.Contains(ap)).Select(p => string.Format("r.[{0}]=t.[{0}]", p))));
            mrg.AppendFormat(" when not matched then insert({0})values({1});", string.Join(",", allFields.Select(p => string.Format("[{0}]", p))), string.Join(",", allFields.Select(p => string.Format("t.[{0}]", p))));
            mrg.AppendFormat("drop table {0};", tempTable);
            new SqlCommand(mrg.ToString(), con).ExecuteNonQuery();
        }

        //public void SaveOld(IEnumerable<DataRecord> records)
        //{
        //    if (records == null || !records.Any())
        //    {
        //        log.Warn("записи для сохранения отсутствуют");
        //        return;
        //    }

        //    lock (saveLocker)
        //    {
        //        var cnt = records.Count();
        //        var sw = new Stopwatch();
        //        sw.Start();

        //        log.Debug(string.Format("начинаем сохранять записи {0} шт, типы [{1}]", cnt, string.Join(",", records.Select(r => r.Type).Distinct())));

        //        try
        //        {
        //            Func<DataRecord, string, IEnumerable<string>, string> formatter = (r, format, parameters) =>
        //            {
        //                var values = parameters.Select(p => typeof(DataRecord).GetProperty(p).GetValue(r, null));
        //                return string.Format(format, values.ToArray());
        //            };

        //            //1. 
        //            foreach (RuleElement rule in SaveRulesSection.Instance.Rules)
        //            {
        //                if (!records.Any()) break;

        //                var uf = rule.UniqueFields.ToStringArray().ToArray();

        //                var ruleTypes = rule.Types.ToStringArray();
        //                Func<DataRecord, bool> predicate = r => ruleTypes.Contains(r.Type);
        //                //записи удовлетворяющие правилу
        //                var allowToRuleRecords = records.Where(r => predicate(r)).ToArray();
        //                //выкидываем уже обработанные записи
        //                records = records.Where(r => !predicate(r));

        //                if (!allowToRuleRecords.Any()) continue;
        //                log.Debug(string.Format("сработало правило {0}", rule.Name));
        //                //разбивка записей правила по секциям
        //                var groupedByTables = allowToRuleRecords.GroupBy(r => formatter(r, rule.Format, rule.FormatFields.ToStringArray())).Select(g => new { TableName = g.Key, Records = g.ToArray() });
        //                foreach (var table in groupedByTables)
        //                {
        //                    try
        //                    {
        //                        if (!IsTableExists(table.TableName))
        //                        {
        //                            CreateTable(table.TableName, rule.UniqueFields.ToStringArray().ToArray());
        //                            RebuildView();
        //                        }
        //                        else
        //                        {
        //                            log.Debug(string.Format("таблица {0} уже существует", table.TableName));
        //                        }

        //                        DeleteDublicates(table.TableName, uf, table.Records);
        //                        //проверка корректности полей используемых в индексе (например допустимая длина)
        //                        var indexCheckRecords = new List<DataRecord>();
        //                        foreach (var r in table.Records)
        //                        {
        //                            if (uf.Contains("S1") && r.S1.Length > 255) r.S1 = r.S1.Substring(0, 250);
        //                            if (uf.Contains("S2") && r.S2.Length > 255) r.S2 = r.S2.Substring(0, 250);
        //                            if (uf.Contains("S3") && r.S3.Length > 255) r.S3 = r.S3.Substring(0, 250);
        //                            indexCheckRecords.Add(r);
        //                        }

        //                        bulkInserter.Insert(indexCheckRecords, table.TableName, ConnectionString);
        //                    }
        //                    catch (Exception tex)
        //                    {
        //                        log.Error(string.Format("ошибка при сохранении записей в секцию {0}", table.TableName), tex);
        //                    }
        //                }
        //            }

        //            if (records.Any())
        //            {
        //                log.Debug(string.Format("сохранение записей не попавших под правила {0} шт, типы [{1}]", records.Count(), string.Join(",", records.Select(r => r.Type).Distinct())));
        //                Insert(TABLE_DEFAULT, records, new string[] { });
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(string.Format("ошибка при сохранении записей (количество записей {0} шт типы [{1}])", cnt, string.Join(",", records.Select(r => r.Type).Distinct())), ex);
        //        }
        //        finally
        //        {
        //        }
        //        sw.Stop();
        //        log.Debug(string.Format("общее время обработки {0} записей {1} мс", cnt, sw.ElapsedMilliseconds));
        //    }
        //}

        private static void CreateDataRecordTable(string tableName, string[] uniqueFields, SqlConnection con)
        {
            log.Debug(string.Format("создание секции {0}", tableName));
            var sw = new Stopwatch();
            sw.Start();
            var properties = typeof(DataRecord).GetProperties();
            var columns = new List<string>();
            var notNulls = new string[] { "Id", "Date", "Type", "ObjectId" }.Union(uniqueFields).Distinct();
            foreach (var property in properties)
            {
                var column = string.Format("[{0}]", property.Name);

                if (typeof(Guid) == property.PropertyType || typeof(Nullable<Guid>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "[uniqueidentifier]");
                }
                if (typeof(DateTime) == property.PropertyType || typeof(Nullable<DateTime>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "[datetime2](7)");
                }
                if (typeof(string) == property.PropertyType)
                {
                    if (notNulls.Contains(property.Name))
                    {
                        column = string.Format("{0} {1}", column, "[nvarchar](255)");
                    }
                    else
                    {
                        column = string.Format("{0} {1}", column, "[nvarchar](max)");
                    }
                }
                if (typeof(double) == property.PropertyType || typeof(Nullable<double>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "[float]");
                }
                if (typeof(int) == property.PropertyType || typeof(Nullable<int>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "[int]");
                }

                if (notNulls.Contains(property.Name))
                {
                    column = string.Format("{0} {1}", column, "NOT NULL");
                }
                else
                {
                    column = string.Format("{0} {1}", column, "NULL");
                }

                if (property.Name == "Id")
                {
                    column = string.Format("{0} {1}", column, "PRIMARY KEY");
                }

                columns.Add(column);
            }

            var q1 = string.Format(@"CREATE TABLE [dbo].[{0}]({1})", tableName, string.Join(",", columns));
            new SqlCommand(q1, con).ExecuteNonQuery();

            if (uniqueFields.Any())
            {
                var q2 = string.Format("create unique nonclustered index unique_row on [{0}]({1}) with (ignore_dup_key = on)", tableName, string.Join(",", uniqueFields.Select(u => string.Format("{0} asc", u))));
                new SqlCommand(q2, con).ExecuteNonQuery();
            }

            sw.Stop();
            log.Debug(string.Format("создание секции [{0}] за {1} мс", tableName, sw.ElapsedMilliseconds));
        }

        private static bool IsTableExists(string tableName, SqlConnection con)
        {
            var cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "select count(*) as cnt from INFORMATION_SCHEMA.TABLES where TABLE_NAME=@name";
            cmd.Parameters.AddWithValue("@name", tableName);
            return (int)cmd.ExecuteScalar() > 0;
        }

        private void DeleteDublicates(string tableName, IEnumerable<string> fields, IEnumerable<DataRecord> records)
        {
            if (!fields.Any()) return;

            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var sw = new Stopwatch();
                sw.Start();

                int bulkRows = MAX_PARAMETERS_COUNT / fields.Count();

                for (var start = 0; start < records.Count(); start += bulkRows)
                {
                    var cmd = new SqlCommand();
                    cmd.Connection = con;
                    var part = records.Skip(start).Take(bulkRows).ToArray();

                    log.Debug(string.Format("удаление порции из {0} записей", part.Count()));

                    var parameterNumber = 0;

                    var deletes = new List<string>();
                    foreach (var record in part)
                    {
                        var pairs = new List<string>();
                        foreach (var field in fields)
                        {
                            var parameter = new SqlParameter();
                            parameter.ParameterName = string.Format("@p{0}", parameterNumber++);
                            var property = typeof(DataRecord).GetProperty(field);
                            var value = property.GetValue(record, null);
                            if (value == null) value = DBNull.Value;
                            parameter.Value = value;
                            cmd.Parameters.Add(parameter);

                            pairs.Add(string.Format("{0} = {1}", field, parameter.ParameterName));
                        }
                        deletes.Add(string.Format("delete from [{0}] where {1}", tableName, string.Join(" and ", pairs)));
                    }

                    log.Debug(string.Format("параметров {0}", parameterNumber));

                    cmd.CommandText = string.Join(";", deletes);
                    cmd.ExecuteNonQuery();
                }

                sw.Stop();
                log.Debug(string.Format("удаление возможных дубликатов для {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
            }
        }

        private static void CreateSectionsTable()
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                log.Debug(string.Format("создание таблицы DataRecordSections"));

                var cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "create table [DataRecordSections]([Name] nvarchar(255) not null primary key)";
                cmd.ExecuteNonQuery();
            }
        }

        static MsSqlDataRecordRepository()
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                if (!IsTableExists("DataRecordSections", con))
                {
                    CreateSectionsTable();
                }

                if (!IsTableExists("DataRecordDefault", con))
                {
                    CreateDataRecordTable("DataRecordDefault", new string[] { "id" }, con);
                }
                con.Close();
            }
        }

        private void AddSection(SqlConnection con, string tableName)
        {
            var q3 = string.Format("insert into [{0}]([Name])values('{1}')", TABLE_SECTIONS, tableName);
            new SqlCommand(q3, con).ExecuteNonQuery();

            RebuildView(con);
        }

        private void RebuildView(SqlConnection con)
        {
            try
            {
                var com = new SqlCommand();
                com.Connection = con;
                com.CommandText = string.Format("select name from [{0}]", TABLE_SECTIONS);
                var reader = com.ExecuteReader();
                var comDelete = new SqlCommand();
                comDelete.Connection = con;
                var commonSelect = string.Format("select {0} from ", string.Join(",", typeof(DataRecord).GetProperties().Select(p => string.Format("[{0}]", p.Name))));

                var tables = new List<string>();
                var tableForDelete = new List<string>();
                List<string> tableSectionName = new List<string>();
                while (reader.Read())
                {
                    var sectionName = reader.GetString(0);
                    tableSectionName.Add(sectionName);
                }
                reader.Close();
                foreach (var sectionName in tableSectionName)
                {
                    try
                    {
                        comDelete.CommandText = string.Format("select top 10 {0} from [{1}] ", string.Join(",", typeof(DataRecord).GetProperties().Select(p => string.Format("[{0}]", p.Name))),sectionName);

                        var readDelete = comDelete.ExecuteReader();
                        if (readDelete != null)
                        {
                            tables.Add(string.Format("{0} [{1}] with (nolock)", commonSelect, sectionName));
                        }
                        else
                        {
                            tableForDelete.Add( $"'{sectionName}'");
                        }
                        readDelete.Close();
                    }
                    catch
                    {
                        tableForDelete.Add($"'{sectionName}'");
                    }
                }
                if (tableForDelete.Any())
                {
                    comDelete.CommandText = string.Format("delete from [{0}] where name in ({1})", TABLE_SECTIONS, string.Join(", ", tableForDelete));
                    var resultComDelete = comDelete.ExecuteNonQuery();
                }
                
                tables.Add(string.Format("{0} [{1}] with (nolock)", commonSelect, TABLE_DEFAULT));
                
                new SqlCommand(string.Format(@"if exists (select * from INFORMATION_SCHEMA.VIEWS where TABLE_NAME='{0}') drop view [{0}]", TABLE_VIEW), con).ExecuteNonQuery();
                com.Connection = con;
                //comDelete.ExecuteNonQuery();
                com.CommandText = string.Format(@"create view [{0}] as {1}", TABLE_VIEW, string.Join(" union all ", tables));
                var resultQuery = com.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                log.Error(string.Format("Ошибка при создании RecordView {0}", ex));
            }
            //*11.03.2019 new SqlCommand(string.Format(@"create view [{0}] as {1}", TABLE_VIEW, string.Join(" union all ", tables)), con).ExecuteNonQuery();
        }

        private void Insert(string tableName, IEnumerable<DataRecord> records, IEnumerable<string> index)
        {
            if (!records.Any()) return;

            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                //2100
                var sw = new Stopwatch();
                sw.Start();
                var properties = typeof(DataRecord).GetProperties();
                int bulkRows = MAX_PARAMETERS_COUNT / properties.Count();

                for (var start = 0; start < records.Count(); start += bulkRows)
                {
                    var part = records.Skip(start).Take(bulkRows);

                    var cmd = new SqlCommand();
                    cmd.Connection = con;
                    var parameterNumber = 0;

                    var query = new StringBuilder();
                    query.AppendFormat("insert into [{0}]({1})", tableName, string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))));

                    var rows = new List<string>();
                    foreach (var record in part)
                    {
                        var parameterNames = new List<string>();
                        foreach (var property in properties)
                        {
                            var parameter = new SqlParameter();
                            parameter.ParameterName = string.Format("@p{0}", parameterNumber++);
                            parameterNames.Add(parameter.ParameterName);
                            var value = property.GetValue(record, null);
                            if (value == null) value = DBNull.Value;
                            if (index.Contains(property.Name) && property.PropertyType == typeof(string) &&
                                value != DBNull.Value && value.ToString().Length > 255)
                            {
                                value = value.ToString().Substring(0, 255);
                            }
                            parameter.Value = value;
                            cmd.Parameters.Add(parameter);
                        }
                        rows.Add(string.Format("({0})", string.Join(",", parameterNames)));
                    }
                    query.Append("values");
                    query.Append(string.Join(",", rows));

                    var script = query.ToString();
                    //log.Debug(string.Format("скрипт вставки {0}", script));
                    cmd.CommandText = script;
                    cmd.ExecuteNonQuery();
                }
                sw.Stop();
            }
            //log.Debug(string.Format("вставка {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
        }

        public DataRecord Get(Guid id)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            DataRecord record = null;

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var clearSelect = string.Format("select {0} from [{1}] where [Id]=@id", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                cmd.CommandText = clearSelect;
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записи с кодом {0}", id), ex);
            }
            finally
            {
                con.Close();
            }
            return record;
        }

        public IEnumerable<DataRecord> Get(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в период с {1} по {2}", type, start, end));
                return null;
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var query = "";

                var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@end"].Value = end;

                var step = 1000;

                if (objectIds.Count() > 100)
                {
                    query = string.Format(@"create table #tmp(id uniqueidentifier);");
                    for (var offset = 0; offset < objectIds.Count(); offset += step)
                    {
                        query += string.Format("insert into #tmp(id) values{0};", string.Join(",", objectIds.Skip(offset).Take(step).Select(o => string.Format("('{0}')", o))));
                    }
                    query += string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and ObjectId in (select id from #tmp);drop table #tmp", clearSelect);
                }
                else
                {
                    var n = 0;
                    var parameterNames = new List<string>();
                    foreach (var objectId in objectIds)
                    {
                        var paramName = string.Format("@{0}", n++);
                        parameterNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, objectId);
                    }
                    query = string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and ObjectId in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));
                }

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записей типа {0} в период с {1} по {2} для {3} объектов", type, start, end, objectIds.Count()), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }
        public void RecordsDelete(List<Guid> Ids, string type)
        {

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            con.Open();
            var com = new SqlCommand();
            com.Connection = con;
            com.CommandText = string.Format("select name from [{0}]", TABLE_SECTIONS);
            var reader = com.ExecuteReader();
            var comDelete = new SqlCommand();
            comDelete.Connection = con;
            
            var tableForDelete = new List<string>();
            List<string> tableSectionName = new List<string>();
            while (reader.Read())
            {
                var sectionName = reader.GetString(0);
                tableSectionName.Add(sectionName);
            }
            reader.Close();

            var tables = tableSectionName.FindAll(x => x.Contains(type));

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                foreach(var table in tables)
                {
                    string clearDelete = string.Format("delete from [{0}]", table);

                    var cmd = new SqlCommand();
                    cmd.Connection = con;

                    var n = 0;
                    foreach (var Id in Ids)
                    {
                        var paramName = string.Format("@{0}", n++);
                        cmd.Parameters.AddWithValue(paramName, Id);
                    }

                    string query = string.Format(@"{0} where Id in ({1})", clearDelete, string.Join(",", Ids.Select(o => string.Format("'{0}'", o))));

                    cmd.CommandText = query;
                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new DataRecord();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            object val = reader.GetValue(i);
                            if (val == DBNull.Value) val = null;
                            properties[i].SetValue(record, val, null);
                        }
                        result.Add(record);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при записей записей типа {0}", type), ex);
            }
            finally
            {
                con.Close();
            }
        }
        public void DeleteRow(List<Guid> Ids)
        {

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            con.Open();
        
            var properties = typeof(DataRecord).GetProperties();

            try
            {
                string clearDelete = string.Format("delete from [{0}]", TABLE_ROWS);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                var n = 0;
                foreach (var Id in Ids)
                {
                    var paramName = string.Format("@{0}", n++);
                    cmd.Parameters.AddWithValue(paramName, Id);
                }

                string query = string.Format(@"{0} where Id in ({1})", clearDelete, string.Join(",", Ids.Select(o => string.Format("'{0}'", o))));

                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error("ошибка при удалении из RowsCache", ex);
            }
            finally
            {
                con.Close();
            }
        }
        //add in 21.01.2019 for PLC
        public IEnumerable<DataRecord> GetWithidsAndS1(List<Guid> objectIds, DateTime start, DateTime end, string type, string s1)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                string clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@s1", s1);
                cmd.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@end"].Value = end;

                var n = 0;
                foreach (var objectId in objectIds)
                {
                    var paramName = string.Format("@{0}", n++);
                    cmd.Parameters.AddWithValue(paramName, objectId);
                }
                
                string query = string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and [S1] like @s1 and ObjectId in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записей типа {0} в период с {1} по {2}", type, start, end), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }
        public IEnumerable<DataRecord> GetDataOnlyWithType(DateTime start, DateTime end, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                string clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@end"].Value = end;
                
                string query = string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type", clearSelect);

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записей типа {0} в период с {1} по {2}", type, start, end), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }
        public IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Get3D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type, string[] tableRange = null)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в период с {1} по {2}", type, start, end));
                return null;
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<Tuple<Guid, DateTime, string>, DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            if (tableRange == null || !tableRange.Any())
            {
                tableRange = new[] { TABLE_VIEW };
            }

            try
            {
                con.Open();
                foreach (string tableName in tableRange)
                {
                    var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), tableName);

                    var cmd = new SqlCommand();
                    cmd.Connection = con;
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);

                    var step = 1000;

                    var query = "";
                    if (objectIds.Count() > 100)
                    {
                        query = string.Format(@"create table #tmp(id uniqueidentifier);");
                        for (var offset = 0; offset < objectIds.Count(); offset += step)
                        {
                            query += string.Format("insert into #tmp(id) values{0};", string.Join(",", objectIds.Skip(offset).Take(step).Select(o => string.Format("('{0}')", o))));
                        }
                        query += string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and ObjectId in (select id from #tmp);drop table #tmp", clearSelect);
                    }
                    else
                    {
                        var n = 0;
                        var parameterNames = new List<string>();
                        foreach (var objectId in objectIds)
                        {
                            var paramName = string.Format("@{0}", n++);
                            parameterNames.Add(paramName);
                            cmd.Parameters.AddWithValue(paramName, objectId);
                        }
                        query = string.Format(@"{0} where [Date] >= @start and [Date] <= @end and [Type] = @type and ObjectId in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));
                    }

                    cmd.CommandText = query;
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new DataRecord();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            object val = reader.GetValue(i);
                            if (val == DBNull.Value) val = null;
                            properties[i].SetValue(record, val, null);
                        }
                        result.Add(new Tuple<Guid, DateTime, string>(record.ObjectId, record.Date, record.S1), record);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записей типа {0} в период с {1} по {2} для {3} объектов", type, start, end, objectIds.Count()), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> Get4D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в период с {1} по {2}", type, start, end));
                return null;
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<Tuple<string, Guid, DateTime, string>, DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var query = "";

                var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@end"].Value = end;

                var step = 1000;

                if (objectIds.Count() > 100)
                {
                    query = string.Format(@"create table #tmp(id uniqueidentifier);");
                    for (var offset = 0; offset < objectIds.Count(); offset += step)
                    {
                        query += string.Format("insert into #tmp(id) values{0};", string.Join(",", objectIds.Skip(offset).Take(step).Select(o => string.Format("('{0}')", o))));
                    }
                    query += string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and ObjectId in (select id from #tmp);drop table #tmp", clearSelect);
                }
                else
                {
                    var n = 0;
                    var parameterNames = new List<string>();
                    foreach (var objectId in objectIds)
                    {
                        var paramName = string.Format("@{0}", n++);
                        parameterNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, objectId);
                    }
                    query = string.Format(@"{0} where [Date]>=@start and [Date]<=@end and [Type]=@type and ObjectId in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));
                }

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(new Tuple<string, Guid, DateTime, string>(record.Type, record.ObjectId, record.Date, record.S1), record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении записей типа {0} в период с {1} по {2} для {3} объектов", type, start, end, objectIds.Count()), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public IDictionary<string, DataRecord> GetByParameter(DateTime date, Guid objectId, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<string, DataRecord>();
            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@objectid", objectId);

                con.Open();
                cmd.CommandText = string.Format(@"{0} where [Date] = @date and [Type] = @type and [ObjectId] = @objectid", clearSelect);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(record.S1, record);
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(ex, "ошибка при получении записей типа {0} за {1} для {2} объекта", type, date, objectId.ToString());
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(DateTime start, DateTime end, Guid objectId, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<DateTime, Dictionary<string, DataRecord>>();
            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Parameters.AddWithValue("@objectid", objectId);

                con.Open();
                cmd.CommandText = string.Format(@"{0} where [Date] >= @start and [Date] <= @end and [Type] = @type and [ObjectId] = @objectid", clearSelect);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }

                    if (!result.ContainsKey(record.Date))
                    {
                        result[record.Date] = new Dictionary<string, DataRecord>();
                    }
                    result[record.Date][record.S1] = record;
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(ex, "ошибка при получении записей типа {0} за период с {1} по {2} для {3} объекта", type, start, end, objectId.ToString());
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public DbConnection OpenDbConnection()
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            DbConnection con = new SqlConnection();
            con.ConnectionString = cs;
            try
            {
                con.Open();
            }
            catch (Exception ex)
            {

            }
            return con;
        }

        public void CloseDbConnection(DbConnection con)
        {
            try
            {
                con.Close();
            }
            catch (Exception ex)
            {

            }
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(string[] tableRange, DateTime start, DateTime end, Guid objectId, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<DateTime, Dictionary<string, DataRecord>>();

            if (tableRange != null && tableRange.Any())
            {
                var properties = typeof(DataRecord).GetProperties();

                try
                {
                    var cmd = new SqlCommand();
                    cmd.Connection = con;

                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    cmd.Parameters.AddWithValue("@objectid", objectId);

                    con.Open();

                    string props = string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name)));
                    cmd.CommandText = string.Join(" union ", tableRange.Select(t => $"select {props} from [{t}] where [Date] >= @start and [Date] <= @end and [Type] = @type and [ObjectId] = @objectid"));
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new DataRecord();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            object val = reader.GetValue(i);
                            if (val == DBNull.Value) val = null;
                            properties[i].SetValue(record, val, null);
                        }

                        if (!result.ContainsKey(record.Date))
                        {
                            result[record.Date] = new Dictionary<string, DataRecord>();
                        }
                        result[record.Date][record.S1] = record;
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    log.Error(ex, "ошибка при получении записей типа {0} за период с {1} по {2} для {3} объекта", type, start, end, objectId.ToString());
                }
                finally
                {
                    con.Close();
                }
            }
            return result;
        }



        public IDictionary<string, DataRecord> GetByParameter(string[] tableRange, DateTime date, Guid objectId, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new Dictionary<string, DataRecord>();
            if (tableRange != null && tableRange.Any())
            {
                var properties = typeof(DataRecord).GetProperties();

                try
                {
                    var cmd = new SqlCommand();
                    cmd.Connection = con;

                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@objectid", objectId);

                    con.Open();

                    string props = string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name)));
                    cmd.CommandText = string.Join(" union ", tableRange.Select(t => $"select {props} from [{t}] where [Date] = @date and [Type] = @type and [ObjectId] = @objectid"));
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new DataRecord();
                        for (int i = 0; i < properties.Length; i++)
                        {
                            object val = reader.GetValue(i);
                            if (val == DBNull.Value) val = null;
                            properties[i].SetValue(record, val, null);
                        }
                        result.Add(record.S1, record);
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    log.Error(ex, "ошибка при получении записей типа {0} за {1} для {2} объекта", type, date, objectId.ToString());
                }
                finally
                {
                    con.Close();
                }
            }
            return result;
        }

        public HashSet<DateTime> GetDateSet(DateTime start, DateTime end, Guid id, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;
            var dates = new HashSet<DateTime>();
            try
            {
                con.Open();

                var cmd = new SqlCommand();
                cmd.CommandText = @"select distinct [Date] from DataRecordView where [Type] = @type and [Date] >= @start and [Date] <= @end and [ObjectId] = @objectid";
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Parameters.AddWithValue("@objectid", id);
                cmd.Connection = con;

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    dates.Add(reader.GetDateTime(0));
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении сета дат"), ex);
            }
            finally
            {
                con.Close();
            }
            return dates;
        }

        public string[] TableRange(DateTime start, DateTime end, string type)
        {
            TableDef tableDefinition;
            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var properties = typeof(DataRecord).GetProperties();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    return tableDefinition.GetTablesRange(start, end);
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении списка таблиц записей типа {type} за период с {start} по {end}");
                }
                finally
                {
                    connection.Close();
                }
            }
            return null;
        }

        //public DataRecord Get(DateTime date, Guid objectId, string type)
        //{
        //    DataRecord result = null;
        //    TableDef tableDefinition;
        //    var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
        //    var properties = typeof(DataRecord).GetProperties();

        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        try
        //        {
        //            tableDefinition = GetTableDefinition(type, connection);
        //            if (tableDefinition.Format == null || tableDefinition.Dates == null)
        //            {
        //                throw new Exception($"не удалось получить список таблиц типа {type}");
        //            }

        //            string tableName = tableDefinition.GetTablesRange(date, date).FirstOrDefault();
        //            if (tableName == null || tableName == "")
        //            {
        //                throw new Exception($"не удалось получить таблицу типа {type} по дате {date:dd.MM.yyyy HH:mm:ss}");
        //            }

        //            var query = "";
        //            var clearSelect = string.Format("select {0} from [{1}]", string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), tableName);

        //            using (var command = new SqlCommand())
        //            {
        //                command.Connection = connection;
        //                command.Parameters.AddWithValue("@date", date);
        //                command.Parameters.AddWithValue("@type", type);
        //                command.Parameters.AddWithValue("@objectid", objectId);

        //                query = string.Format(@"{0} where [Date] = @date and [Type] = @type and [ObjectId] = @objectid", clearSelect);

        //                command.CommandText = query;
        //                var reader = command.ExecuteReader();

        //                if(reader.Read())
        //                { 
        //                    var record = new DataRecord();
        //                    for (int i = 0; i < properties.Length; i++)
        //                    {
        //                        object val = reader.GetValue(i);
        //                        if (val == DBNull.Value) val = null;
        //                        properties[i].SetValue(record, val, null);
        //                    }
        //                    result = record;
        //                }
        //                reader.Close();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            log.Error(ex, $"ошибка при получении записей типа {type} за дату {date} для {objectId.ToString()} объекта");
        //            return result;
        //        }
        //        finally
        //        {
        //            connection.Close();
        //        }
        //    }
        //    return result;
        //}

        public IEnumerable<DataRecord> Count(DateTime start, DateTime end, string type, IEnumerable<Guid> objectIds)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в спериод с {1} по {2}", type, start, end));
                return new DataRecord[] { };
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                cmd.Parameters["@end"].Value = end;

                var step = 1000;

                var prefix = "";
                var ids = "";

                if (objectIds.Count() > 100)
                {
                    prefix = string.Format(@"create table #tmp(id uniqueidentifier);");
                    for (var offset = 0; offset < objectIds.Count(); offset += step)
                    {
                        prefix += string.Format("insert into #tmp(id) values{0};", string.Join(",", objectIds.Skip(offset).Take(step).Select(o => string.Format("('{0}')", o))));
                    }
                    ids = "select [id] from #tmp";
                }
                else
                {
                    var n = 0;
                    var parameterNames = new List<string>();
                    foreach (var objectId in objectIds)
                    {
                        var paramName = string.Format("@{0}", n++);
                        parameterNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, objectId);
                    }
                    ids = string.Join(",", parameterNames);
                }

                var query = string.Format(@"{0}select [ObjectId], count(*) as Cnt from 
(select distinct [ObjectId], [Date] from [{1}]
where objectid in ({2})
and [type]=@type and [Date] >= @start and [Date] <= @end) t
group by ObjectId", prefix, TABLE_VIEW, ids);

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var record = new DataRecord();

                    record.Type = string.Format("{0}Count", type);
                    record.Id = Guid.NewGuid();
                    record.Date = end;
                    record.ObjectId = reader.GetGuid(0);
                    record.I1 = reader.GetInt32(1);

                    result.Add(record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении количества записей типа {0} в период с {1} по {2} для {3} объектов", type, start, end, objectIds.Count()), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public IEnumerable<DataRecordDate> GetDatesAll(string type, DateTime start, DateTime end)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;
            var dates = new List<DataRecordDate>();
            try
            {
                var cmd = new SqlCommand();

                cmd.CommandText = @"select distinct [Type],[ObjectId],[Date] from DataRecordView where [Type] = @type and [Date] >= @start and [Date] <= @end";
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Connection = con;

                con.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dates.Add(new DataRecordDate()
                    {
                        Type = reader.GetString(0),
                        ObjectId = reader.GetGuid(1),
                        Date = reader.GetDateTime(2)
                    });
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении дат"), ex);
            }
            finally
            {
                con.Close();
            }
            return dates;
        }

        public IEnumerable<DataRecordDate> GetDates(string type, DateTime start, DateTime end, IEnumerable<Guid> objectIds)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;
            var dates = new List<DataRecordDate>();
            try
            {
                var cmd = new SqlCommand();

                cmd.CommandText = string.Format(@"select distinct [Type],[ObjectId],[Date] from DataRecordView where [Type]=@type and [Date]>=@start and [Date]<=@end and ObjectId in ({0})", string.Join(",", objectIds.Select(g => string.Format("'{0}'", g))));
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Connection = con;

                con.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dates.Add(new DataRecordDate()
                    {
                        Type = reader.GetString(0),
                        ObjectId = reader.GetGuid(1),
                        Date = reader.GetDateTime(2)
                    });
                }
                reader.Close();

            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении дат"), ex);
            }
            finally
            {
                con.Close();
            }
            return dates;
        }

        public DateTime GetLastDate(string type, Guid objectId)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;
            var cmd = new SqlCommand();
            cmd.Connection = con;

            cmd.Parameters.AddWithValue("@objectId", objectId);
            cmd.Parameters.AddWithValue("@type", type);
            con.Open();
            cmd.CommandText = string.Format("select max(date) from DataRecordView where ObjectId=@objectId and Type=@type");
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value)
            {
                return (DateTime)res;
            }
            return DateTime.MinValue;
        }

        public DateTime GetLastDate1(string type, Guid objectId)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;
            var cmd = new SqlCommand();
            cmd.Connection = con;

            cmd.Parameters.AddWithValue("@objectId", objectId);
            cmd.Parameters.AddWithValue("@type", type);
            con.Open();
            cmd.CommandText = string.Format("select top 1 date from DataRecordView where ObjectId=@objectId and Type=@type order by date desc");
            var res = cmd.ExecuteScalar();
            if (res != null && res != DBNull.Value)
            {
                return (DateTime)res;
            }
            return DateTime.MinValue;
        }

        public IEnumerable<DataRecord> GetLast(string type, Guid[] objectIds, int count)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении последних записей типа {0} ", type));
                return null;
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new SqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var query = "";

                var clearSelect = string.Format("select top {0} {1} from [{2}]", count, string.Join(",", properties.Select(p => string.Format("[{0}]", p.Name))), TABLE_VIEW);

                var cmd = new SqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);

                var step = 1000;

                if (objectIds.Count() > 100)
                {
                    query = string.Format(@"create table #tmp(id uniqueidentifier);");
                    for (var offset = 0; offset < objectIds.Count(); offset += step)
                    {
                        query += string.Format("insert into #tmp(id) values{0};", string.Join(",", objectIds.Skip(offset).Take(step).Select(o => string.Format("('{0}')", o))));
                    }
                    query += string.Format(@"{0} where [Type]=@type and ObjectId in (select id from #tmp);drop table #tmp", clearSelect);
                }
                else
                {
                    var n = 0;
                    var parameterNames = new List<string>();
                    foreach (var objectId in objectIds)
                    {
                        var paramName = string.Format("@{0}", n++);
                        parameterNames.Add(paramName);
                        cmd.Parameters.AddWithValue(paramName, objectId);
                    }
                    query = string.Format(@"{0} where [Type]=@type and ObjectId in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));
                }

                con.Open();
                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var record = new DataRecord();
                    for (int i = 0; i < properties.Length; i++)
                    {
                        object val = reader.GetValue(i);
                        if (val == DBNull.Value) val = null;
                        properties[i].SetValue(record, val, null);
                    }
                    result.Add(record);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при получении последних записей типа {0} для {1} объектов", type, objectIds.Count()), ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }


        private TableDef GetTableDefinition(string type, SqlConnection connection)
        {
            List<DateTime> dates = null;
            string format = null;
            string typeFull = $"DataRecord{type}";
            string query = $@"SELECT [Name] FROM [DataRecordSections] WHERE [Name] LIKE '{typeFull}%';";

            using (var command = new SqlCommand())
            {
                dates = new List<DateTime>();
                command.Connection = connection;
                command.CommandText = query;

                //connection.Open();
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string nameFull = reader.GetString(0);
                    string dateStr = nameFull.Substring(typeFull.Length);
                    if (dateStr.Length == 6)
                    {
                        format = "MMyyyy";
                    }
                    else if (dateStr.Length == 4)
                    {
                        format = "yyyy";
                    }
                    else
                    {
                        format = "";
                    }

                    if (format != "")
                    {
                        try
                        {
                            dates.Add(DateTime.ParseExact(dateStr, format, new System.Globalization.CultureInfo("ru-RU")));
                        }
                        catch (Exception ex)
                        {
                            log.Warn(ex, $"строка \"{dateStr ?? "null"}\" не распознана как дата формата \"{format ?? "null"}\"");
                        }
                    }
                }
                reader.Close();

                if (dates.Count == 0)
                {
                    dates.Add(default(DateTime));
                }

                if (format == null)
                {
                    typeFull = "DataRecordDefault";
                    format = "";
                }
            }

            TableDef tdef = new TableDef();
            tdef.Type = type;
            tdef.TypeFull = typeFull;
            tdef.Format = format;
            tdef.Dates = dates == null ? null : dates.ToArray();
            return tdef;
        }

        public IEnumerable<DataRecord> GetLastRecords(string type, Guid[] objectIds, DateTime start = default(DateTime))
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении последних записей типа {0} ", type));
                return null;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;
            var result = new List<DataRecord>();

            TableDef tableDefinition;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    string[] tablesSortedDesc = tableDefinition.GetTablesSorted(desc: true);

                    foreach (var tableName in tablesSortedDesc)
                    {
                        string query = $@"SELECT 
                    v.Id,
	                v.Type,
	                v.Date,
                    v.ObjectId,
	                v.D1,
	                v.D2,
	                v.D3,
	                v.I1,
	                v.I2,
	                v.I3,
	                v.S1, 
	                v.S2,
	                v.S3,
	                v.Dt1,
	                v.Dt2,
	                v.Dt3,
	                v.G1, 
	                v.G2,
	                v.G3
                FROM
                    (SELECT
                        ObjectId,
                        Type,
                        MAX(Date) as lastdate
                      FROM [{tableName}]
                      where Type = '{type}' and ObjectId in ({string.Join(",", objectIds.Select(id => $"'{id.ToString()}'"))}){(start == default(DateTime) ? "" : " and Date >= @start")}
                      group by ObjectId, Type) as l
                      left join [{tableName}] as v
                on l.ObjectId = v.ObjectId and l.Type = v.Type and l.lastdate = v.Date";

                        var properties = typeof(DataRecord).GetProperties();

                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            if (start != default(DateTime))
                            {
                                command.Parameters.AddWithValue("@start", start);
                            }
                            command.CommandText = query;
                            var reader = command.ExecuteReader();

                            while (reader.Read())
                            {
                                var record = new DataRecord();
                                for (int i = 0; i < properties.Length; i++)
                                {
                                    object val = reader.GetValue(i);
                                    if (val == DBNull.Value) val = null;
                                    properties[i].SetValue(record, val, null);
                                }
                                result.Add(record);
                            }
                            reader.Close();
                            if (result.Any())
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении последних записей типа {type} для {objectIds.Count()} объектов");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }

            return result;
        }
        //    var cmd = new SqlCommand();
        //cmd.Connection = con;
        //con.Open();
        //cmd.CommandText = query;
        //var reader = cmd.ExecuteReader();

        //while (reader.Read())
        //{
        //    var record = new DataRecord();
        //    for (int i = 0; i < properties.Length; i++)
        //    {
        //        object val = reader.GetValue(i);
        //        if (val == DBNull.Value) val = null;
        //        properties[i].SetValue(record, val, null);
        //    }
        //    result.Add(record);
        //}
        //reader.Close();
    }
}

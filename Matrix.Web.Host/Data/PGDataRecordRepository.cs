using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;
using NLog;
using Npgsql;
using NpgsqlTypes;
using System.Globalization;
using Matrix.Web.Host.Data;

namespace Matrix.Domain.Infrastructure.EntityFramework.Repositories
{
    /// <summary>
    /// ручное чтение/запись архивов
    /// 
    /// при чтении используется хинт NOLOCK - "грязное" чтение, позволяет избежать блокировок
    /// запись в бд BULK INSERT-ом, при этом сначала пишем во временную таблицу, а потом MERGE-им ее с целевой
    /// </summary>
    public class PGDataRecordRepository : IDataRecordRepository
    {
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
        /// имя представления с объединением секций
        /// </summary>
        const string TABLE_ROWS = "RowsCache";

        /// <summary>
        /// имя таблицы именами секций
        /// </summary>
        const string TABLE_SECTIONS = "DataRecordSections";

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
        public PGDataRecordRepository()
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
                using (var con = new NpgsqlConnection(ConnectionString))
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
                                if (year <= 2010 || year >= MAX_YEAR)
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
                        con.Close();
                    }
                    sw.Stop();
                    log.Debug(string.Format("общее время обработки {0} записей {1} мс", cnt, sw.ElapsedMilliseconds));
                }
            }
        }
        public void DeleteRow(List<Guid> Ids)
        {
           
        }
        private void BulkUpdate(IEnumerable<DataRecord> records, string table, string[] uniqueFields, string[] allFields, NpgsqlConnection con)
        {
            using (var command = new NpgsqlCommand())
            {
                command.Connection = con;

                foreach (var record in records)
                {
                    try
                    {
                        command.Parameters.Clear();

                        command.CommandText = $"delete from \"{table}\" where {string.Join(" and ", uniqueFields.Select(f => $"\"{f}\"=@{f}"))};";

                        foreach (var uf in uniqueFields)
                        {
                            var pi = typeof(DataRecord).GetProperty(uf);
                            command.Parameters.AddWithValue($"@{uf}", pi.GetValue(record));
                        }

                        command.ExecuteNonQuery();

                        command.CommandText = $"insert into \"{table}\" ({string.Join(",", allFields.Select(f => $"\"{f}\""))}) values ({string.Join(",", allFields.Select(f => $"@{f}"))});";
                        command.Parameters.Clear();
                        foreach (var uf in allFields)
                        {
                            var pi = typeof(DataRecord).GetProperty(uf);
                            var val = pi.GetValue(record) ?? DBNull.Value;
                            command.Parameters.AddWithValue($"@{uf}", val);
                        }

                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }

            //var tempTable = string.Format("#tmp{0}{1}", table, DateTime.Now.ToString("ddMMyyHHmmssttt"));
            //CreateDataRecordTable(tempTable, new string[] { }, con);

            //bulkInserter.Insert(records, tempTable, con);

            //var mrg = new StringBuilder();
            //mrg.AppendFormat("merge [{0}] r ", table);
            //mrg.AppendFormat("using ");
            //mrg.AppendFormat("(select ");
            //mrg.AppendFormat("{0}", string.Join(",", uniqueFields.Select(p => string.Format("[{0}]", p))));
            //mrg.Append(uniqueFields.Any() && allFields.Where(ap => !uniqueFields.Contains(ap)).Any() ? "," : "");
            //mrg.AppendFormat("{0}", string.Join(",", allFields.Where(ap => !uniqueFields.Contains(ap)).Select(p => string.Format("max([{0}]) as [{0}]", p))));
            //mrg.AppendFormat(" from {0} group by {1}) t ", tempTable, string.Join(",", uniqueFields.Select(p => string.Format("[{0}]", p))));
            //mrg.AppendFormat("on {0} ", string.Join(" and ", uniqueFields.Select(p => string.Format("r.[{0}]=t.[{0}]", p))));
            //if (allFields.Any(ap => !uniqueFields.Contains(ap))) mrg.AppendFormat(" when matched then update set {0} ", string.Join(",", allFields.Where(ap => !uniqueFields.Contains(ap)).Select(p => string.Format("r.[{0}]=t.[{0}]", p))));
            //mrg.AppendFormat(" when not matched then insert({0})values({1});", string.Join(",", allFields.Select(p => string.Format("[{0}]", p))), string.Join(",", allFields.Select(p => string.Format("t.[{0}]", p))));
            //mrg.AppendFormat("drop table {0};", tempTable);
            //new NpgsqlCommand(mrg.ToString(), con).ExecuteNonQuery();
        }

        private static void CreateDataRecordTable(string tableName, string[] uniqueFields, NpgsqlConnection con)
        {
            log.Debug(string.Format("создание секции {0}", tableName));
            var sw = new Stopwatch();
            sw.Start();
            var properties = typeof(DataRecord).GetProperties();
            var columns = new List<string>();
            var notNulls = new string[] { "Id", "Date", "Type", "ObjectId" }.Union(uniqueFields).Distinct();
            foreach (var property in properties)
            {
                var column = $"\"{property.Name}\"";

                if (typeof(Guid) == property.PropertyType || typeof(Nullable<Guid>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "uuid");
                }
                if (typeof(DateTime) == property.PropertyType || typeof(Nullable<DateTime>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "timestamp");
                }
                if (typeof(string) == property.PropertyType)
                {
                    if (notNulls.Contains(property.Name))
                    {
                        column = string.Format("{0} {1}", column, "varchar(255)");
                    }
                    else
                    {
                        column = string.Format("{0} {1}", column, "text");
                    }
                }
                if (typeof(double) == property.PropertyType || typeof(Nullable<double>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "double precision");
                }
                if (typeof(int) == property.PropertyType || typeof(Nullable<int>) == property.PropertyType)
                {
                    column = string.Format("{0} {1}", column, "integer");
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

            var q1 = $"CREATE TABLE \"{tableName}\"({string.Join(",", columns)})";
            new NpgsqlCommand(q1, con).ExecuteNonQuery();

            if (uniqueFields.Any())
            {
                //var q2 = string.Format("create unique nonclustered index unique_row on [{0}]({1}) with (ignore_dup_key = on)", tableName, string.Join(",", uniqueFields.Select(u => string.Format("{0} asc", u))));
                //new NpgsqlCommand(q2, con).ExecuteNonQuery();
            }

            sw.Stop();
            log.Debug(string.Format("создание секции [{0}] за {1} мс", tableName, sw.ElapsedMilliseconds));
        }

        private static bool IsTableExists(string tableName, NpgsqlConnection con)
        {
            var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "select exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME=@name);";
            cmd.Parameters.AddWithValue("@name", tableName);
            return (bool)cmd.ExecuteScalar();
        }

        private void DeleteDublicates(string tableName, IEnumerable<string> fields, IEnumerable<DataRecord> records)
        {
            if (!fields.Any()) return;

            using (var con = new NpgsqlConnection(ConnectionString))
            {
                con.Open();
                var sw = new Stopwatch();
                sw.Start();

                int bulkRows = MAX_PARAMETERS_COUNT / fields.Count();

                for (var start = 0; start < records.Count(); start += bulkRows)
                {
                    var cmd = new NpgsqlCommand();
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
                            var parameter = new NpgsqlParameter();
                            parameter.ParameterName = string.Format("@p{0}", parameterNumber++);
                            var property = typeof(DataRecord).GetProperty(field);
                            var value = property.GetValue(record, null);
                            if (value == null) value = DBNull.Value;
                            parameter.Value = value;
                            cmd.Parameters.Add(parameter);

                            pairs.Add(string.Format("{0} = {1}", field, parameter.ParameterName));
                        }
                        deletes.Add(string.Format("delete from \"{0}\" where {1}", tableName, string.Join(" and ", pairs)));
                    }

                    log.Debug(string.Format("параметров {0}", parameterNumber));

                    cmd.CommandText = string.Join(";", deletes);
                    cmd.ExecuteNonQuery();
                }

                sw.Stop();
                log.Debug(string.Format("удаление возможных дубликатов для {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
            }
        }

        private static void CreateSectionsTable(NpgsqlConnection con)
        {
            log.Debug(string.Format("создание таблицы DataRecordSections"));

            var cmd = new NpgsqlCommand();
            cmd.Connection = con;
            cmd.CommandText = "create table \"DataRecordSections\"(\"Name\" varchar(255) not null primary key)";
            cmd.ExecuteNonQuery();
        }

        static PGDataRecordRepository()
        {
            using (var con = new NpgsqlConnection(ConnectionString))
            {
                con.Open();
                try
                {
                    if (!IsTableExists("DataRecordSections", con))
                    {
                        CreateSectionsTable(con);
                    }

                    if (!IsTableExists("DataRecordDefault", con))
                    {
                        CreateDataRecordTable("DataRecordDefault", new string[] { "id" }, con);
                    }
                }
                finally
                {
                    con.Close();
                }
            }
        }

        private void AddSection(NpgsqlConnection con, string tableName)
        {
            var q3 = $"insert into \"{TABLE_SECTIONS}\"(\"Name\")values('{tableName}')";
            new NpgsqlCommand(q3, con).ExecuteNonQuery();

            RebuildView(con);
        }

        private void RebuildView(NpgsqlConnection con)
        {
            try
            {
                var com = new NpgsqlCommand();
                com.Connection = con;
                com.CommandText = string.Format("select \"Name\" from \"{0}\"", TABLE_SECTIONS);
                var reader = com.ExecuteReader();
                var comDelete = new NpgsqlCommand();
                comDelete.Connection = con;
                var commonSelect = string.Format("select {0} from ", string.Join(",", typeof(DataRecord).GetProperties().Select(p => string.Format("\"{0}\"", p.Name))));

                var tables = new List<string>();
                var tableForDelete = new List<string>();
                List<string> tableSectionName = new List<string>();
                while (reader.Read())
                {
                    var sectionName = reader.GetString(0);
                    tables.Add(string.Format("{0} \"{1}\"", commonSelect, sectionName));
                }
                
                //------ start 11.03.2019
                /*
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
                        comDelete.CommandText = string.Format("select top 10 {0} from \"{1}\" ", string.Join(",", typeof(DataRecord).GetProperties().Select(p => string.Format("\"{0}\"", p.Name))), sectionName);

                        var readDelete = comDelete.ExecuteReader();
                        if (readDelete != null)
                        {
                            tables.Add(string.Format("{0} \"{1}\"", commonSelect, sectionName));
                        }
                        else
                        {
                            tableForDelete.Add($"\"{sectionName}\"");
                        }
                        readDelete.Close();
                    }
                    catch
                    {
                        tableForDelete.Add($"\"{sectionName}\"");
                    }
                }
                comDelete.CommandText = string.Format("delete from \"{0}\" where name in ({1})", TABLE_SECTIONS, string.Join(", ", tableForDelete));
                var resultComDelete = comDelete.ExecuteNonQuery();
                */
                //---------end 11.03.2019
                tables.Add(string.Format("{0} \"{1}\"", commonSelect, TABLE_DEFAULT));


                var exists = (bool)new NpgsqlCommand($"select exists (select 1 from INFORMATION_SCHEMA.VIEWS where TABLE_NAME='{TABLE_VIEW}')", con).ExecuteScalar();

                if (exists)
                    new NpgsqlCommand($"drop view \"{TABLE_VIEW}\"", con).ExecuteNonQuery();

                new NpgsqlCommand($"create view \"{TABLE_VIEW}\" as {string.Join(" union all ", tables)}", con).ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                log.Error(string.Format("Ошибка при создании RecordView {0}", ex));
            }
        }

        private void Insert(string tableName, IEnumerable<DataRecord> records, IEnumerable<string> index)
        {
            if (!records.Any()) return;

            using (var con = new NpgsqlConnection(ConnectionString))
            {
                con.Open();
                //2100
                try
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var properties = typeof(DataRecord).GetProperties();
                    int bulkRows = MAX_PARAMETERS_COUNT / properties.Count();

                    for (var start = 0; start < records.Count(); start += bulkRows)
                    {
                        var part = records.Skip(start).Take(bulkRows);

                        var cmd = new NpgsqlCommand();
                        cmd.Connection = con;
                        var parameterNumber = 0;

                        var query = new StringBuilder();
                        query.AppendFormat("insert into \"{0}\"({1})", tableName, string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))));

                        var rows = new List<string>();
                        foreach (var record in part)
                        {
                            var parameterNames = new List<string>();
                            foreach (var property in properties)
                            {
                                var parameter = new NpgsqlParameter();
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
                finally
                {
                    con.Close();
                }
            }
            //log.Debug(string.Format("вставка {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
        }

        public DataRecord Get(Guid id)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;

            DataRecord record = null;

            var properties = typeof(DataRecord).GetProperties();

            try
            {
                var clearSelect = string.Format("select {0} from \"{1}\" where \"Id\"=$1", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), TABLE_VIEW);

                var cmd = new NpgsqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("$1", id);

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
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в спериод с {1} по {2}", type, start, end));
                return null;
            }

            TableDef tableDefinition;
            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var result = new List<DataRecord>();
            var properties = typeof(DataRecord).GetProperties();


            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    string[] tablesInRange = tableDefinition.GetTablesRange(start, end);
                    foreach (var tableName in tablesInRange)
                    {
                        var query = "";
                        var clearSelect = string.Format("select {0} from \"{1}\"", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), tableName);

                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.AddWithValue("@start", start);
                            command.Parameters.AddWithValue("@end", end);
                            command.Parameters.AddWithValue("@type", type);

                            var n = 4;
                            //var parameterNames = new List<string>();
                            //foreach (var objectId in objectIds)
                            //{
                            //    var paramName = string.Format("${0}", n++);
                            //    parameterNames.Add(paramName);
                            //    command.Parameters.AddWithValue(paramName, objectId);
                            //}
                            query = string.Format(@"{0} where ""Date"">=@start and ""Date""<=@end and ""Type""=@type and ""ObjectId"" in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении записей типа {type} в период с {start} по {end} для {objectIds.Count()} объектов");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }
        public void RecordsDelete(List<Guid> Ids, string type)
        {
            
        }
        public IEnumerable<DataRecord> GetWithidsAndS1(List<Guid> objectIds, DateTime start, DateTime end, string type, string s1)
        {
            TableDef tableDefinition;
            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var result = new List<DataRecord>();
            var properties = typeof(DataRecord).GetProperties();
            
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    string[] tablesInRange = tableDefinition.GetTablesRange(start, end);
                    foreach (var tableName in tablesInRange)
                    {
                        var query = "";
                        var clearSelect = string.Format("select {0} from \"{1}\"", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), tableName);

                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.AddWithValue("@start", start);
                            command.Parameters.AddWithValue("@end", end);
                            command.Parameters.AddWithValue("@type", type);
                            command.Parameters.AddWithValue("@s1", s1);
                            query = string.Format(@"{0} where ""Date"">=@start and ""Date""<=@end and ""Type""=@type and ""S1""=@s1 and ""ObjectId"" in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));
                            
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении записей типа {type} в период с {start} по {end}");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }
        public IEnumerable<DataRecord> GetDataOnlyWithType(DateTime start, DateTime end, string type)
        {
            TableDef tableDefinition;
            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var result = new List<DataRecord>();
            var properties = typeof(DataRecord).GetProperties();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    string[] tablesInRange = tableDefinition.GetTablesRange(start, end);
                    foreach (var tableName in tablesInRange)
                    {
                        var query = "";
                        var clearSelect = string.Format("select {0} from \"{1}\"", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), tableName);

                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.AddWithValue("@start", start);
                            command.Parameters.AddWithValue("@end", end);
                            command.Parameters.AddWithValue("@type", type);
                            query = string.Format(@"{0} where ""Date"">=@start and ""Date""<=@end and ""Type""=@type ", clearSelect);

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
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении записей типа {type} в период с {start} по {end}");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }
        
        public IDictionary<Tuple<Guid, DateTime, string>, DataRecord> Get3D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type, string[] tableRange = null)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в спериод с {1} по {2}", type, start, end));
                return null;
            }

            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var result = new Dictionary<Tuple<Guid, DateTime, string>, DataRecord>();
            var properties = typeof(DataRecord).GetProperties();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                if (tableRange == null || !tableRange.Any())
                {
                    tableRange = new[] { TABLE_VIEW };
                }

                try
                {
                    connection.Open();
                    foreach (var tableName in tableRange)
                    {
                        var query = "";
                        var clearSelect = string.Format("select {0} from \"{1}\"", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), tableName);

                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.AddWithValue("@start", start);
                            command.Parameters.AddWithValue("@end", end);
                            command.Parameters.AddWithValue("@type", type);

                            query = string.Format(@"{0} where ""Date"">=@start and ""Date""<=@end and ""Type""=@type and ""ObjectId"" in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));

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
                                result.Add(new Tuple<Guid, DateTime, string>(record.ObjectId, record.Date, record.S1), record);
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении записей типа {type} в период с {start} по {end} для {objectIds.Count()} объектов");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }



        public IDictionary<Tuple<string, Guid, DateTime, string>, DataRecord> Get4D(DateTime start, DateTime end, IEnumerable<Guid> objectIds, string type)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в спериод с {1} по {2}", type, start, end));
                return null;
            }

            TableDef tableDefinition;
            var connectionString = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var result = new Dictionary<Tuple<string, Guid, DateTime, string>, DataRecord>();
            var properties = typeof(DataRecord).GetProperties();


            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                try
                {
                    tableDefinition = GetTableDefinition(type, connection);
                    if (tableDefinition.Format == null || tableDefinition.Dates == null)
                    {
                        throw new Exception($"не удалось получить список таблиц типа {type}");
                    }

                    string[] tablesInRange = tableDefinition.GetTablesRange(start, end);
                    foreach (var tableName in tablesInRange)
                    {
                        var query = "";
                        var clearSelect = string.Format("select {0} from \"{1}\"", string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name))), tableName);

                        using (var command = new NpgsqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.AddWithValue("@start", start);
                            command.Parameters.AddWithValue("@end", end);
                            command.Parameters.AddWithValue("@type", type);

                            var n = 4;
                            //var parameterNames = new List<string>();
                            //foreach (var objectId in objectIds)
                            //{
                            //    var paramName = string.Format("${0}", n++);
                            //    parameterNames.Add(paramName);
                            //    command.Parameters.AddWithValue(paramName, objectId);
                            //}
                            query = string.Format(@"{0} where ""Date"">=@start and ""Date""<=@end and ""Type""=@type and ""ObjectId"" in ({1})", clearSelect, string.Join(",", objectIds.Select(o => string.Format("'{0}'", o))));

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
                                result.Add(new Tuple<string, Guid, DateTime, string>(record.Type, record.ObjectId, record.Date, record.S1), record);
                            }
                            reader.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"ошибка при получении записей типа {type} в период с {start} по {end} для {objectIds.Count()} объектов");
                    return result;
                }
                finally
                {
                    connection.Close();
                }
            }
            return result;
        }


        public IDictionary<DateTime, DataRecord> GetByDate(DateTime start, DateTime end, Guid objectId, string type)
        {
            throw new NotImplementedException("нет реализации GetByDate для PGSQL");
        }

        public IDictionary<string, DataRecord> GetByParameter(DateTime date, Guid objectId, string type)
        {
            throw new NotImplementedException("нет реализации GetByParameter для PGSQL");
        }

        public IDictionary<string, DataRecord> GetByParameter(string[] tableRange, DateTime date, Guid id, string type)
        {
            throw new NotImplementedException("нет реализации GetByParameter для PGSQL");
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(DateTime start, DateTime end, Guid objectId, string type)
        {
            throw new NotImplementedException("нет реализации GetByDateParameter(4) для PGSQL");
        }

        public IDictionary<DateTime, Dictionary<string, DataRecord>> GetByDateParameter(string[] tableRange, DateTime start, DateTime end, Guid objectId, string type)
        {
            throw new NotImplementedException("нет реализации GetByDateParameter(5) для PGSQL");
        }

        public string[] TableRange(DateTime start, DateTime end, string type)
        {
            throw new NotImplementedException("нет реализации GetByParameter для TableRange");
        }

        public HashSet<DateTime> GetDateSet(DateTime start, DateTime end, Guid id, string type)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;
            var dates = new HashSet<DateTime>();
            try
            {
                con.Open();

                var cmd = new NpgsqlCommand();
                cmd.CommandText = @"select distinct ""Date"" from ""DataRecordView"" where ""Type"" = @type and ""Date"" >= @start and ""Date"" <= @end and ""ObjectId"" = @objectid";
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

        public IEnumerable<DataRecord> Count(DateTime start, DateTime end, string type, IEnumerable<Guid> objectIds)
        {
            if (objectIds == null || !objectIds.Any())
            {
                log.Warn(string.Format("недопустимое число объектов при получении записей типа {0} в спериод с {1} по {2}", type, start, end));
                return new DataRecord[] { };
            }

            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            con.Open();

            try
            {
                var cmd = new NpgsqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.Add("@start", NpgsqlDbType.Date);
                cmd.Parameters["@start"].Value = start;

                cmd.Parameters.Add("@end", NpgsqlDbType.Date);
                cmd.Parameters["@end"].Value = end;

                var ids = "";

                var n = 0;
                var parameterNames = new List<string>();
                foreach (var objectId in objectIds)
                {
                    var paramName = $"@{n++}";
                    parameterNames.Add(paramName);
                    cmd.Parameters.AddWithValue(paramName, objectId);
                }
                ids = string.Join(",", parameterNames);

                var query = $@"select ""ObjectId"", count(*) as Cnt from 
(select distinct ""ObjectId"", ""Date"" from ""{TABLE_VIEW}""
where ObjectId in ({ids})
and ""type""=@type and ""Date"" >= @start and ""Date"" <= @end) t
group by ""ObjectId""";

                cmd.CommandText = query;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var record = new DataRecord();

                    record.Type = $"{type}Count";
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
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;
            var dates = new List<DataRecordDate>();
            con.Open();
            try
            {
                var cmd = new NpgsqlCommand();

                cmd.CommandText = "select distinct \"Type\",\"ObjectId\",\"Date\" from \"DataRecordView\" where \"Type\"=@type and \"Date\">=@start and \"Date\"<=@end;";
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Connection = con;

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
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;
            var dates = new List<DataRecordDate>();
            con.Open();

            try
            {
                var cmd = new NpgsqlCommand();

                cmd.CommandText = $"select distinct \"Type\",\"ObjectId\",\"Date\" from \"DataRecordView\" where \"Type\"=@type and \"Date\">=@start and \"Date\"<=@end and \"ObjectId\" in ({string.Join(",", objectIds.Select(g => string.Format("'{0}'", g)))})";
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.Parameters.AddWithValue("@end", end);
                cmd.Connection = con;

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
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;
            var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.Parameters.AddWithValue("@objectId", objectId);
            cmd.Parameters.AddWithValue("@type", type);
            con.Open();
            cmd.CommandText = string.Format("select max(\"Date\") from \"DataRecordView\" where \"ObjectId\"=@objectId and \"Type\"=@type;");
            cmd.CommandTimeout = 90;

            try
            {
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return (DateTime)res;
                }
            }
            finally
            {
                con.Close();
            }
            return DateTime.MinValue;
        }

        public DateTime GetLastDate1(string type, Guid objectId)
        {
            var cs = ConfigurationManager.ConnectionStrings[CS_NAME].ConnectionString;
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;
            var cmd = new NpgsqlCommand();
            cmd.Connection = con;

            cmd.Parameters.AddWithValue("@objectId", objectId);
            cmd.Parameters.AddWithValue("@type", type);
            con.Open();
            cmd.CommandText = string.Format("select \"date\" from \"DataRecordView\" where \"ObjectId\"=@objectId and \"Type\"=@type order by \"date\" desc limit 1;");
            try
            {
                var res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return (DateTime)res;
                }
            }
            finally
            {
                con.Close();
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
            var con = new NpgsqlConnection();
            con.ConnectionString = cs;

            var result = new List<DataRecord>();

            var properties = typeof(DataRecord).GetProperties();

            con.Open();
            try
            {
                var query = "";

                var clearSelect = $"select {string.Join(",", properties.Select(p => string.Format("\"{0}\"", p.Name)))} from \"{TABLE_VIEW}\"";

                var cmd = new NpgsqlCommand();
                cmd.Connection = con;

                cmd.Parameters.AddWithValue("@type", type);

                var step = 1000;


                var n = 0;
                var parameterNames = new List<string>();
                foreach (var objectId in objectIds)
                {
                    var paramName = string.Format("@{0}", n++);
                    parameterNames.Add(paramName);
                    cmd.Parameters.AddWithValue(paramName, objectId);
                }
                query = $"{clearSelect} where \"Type\"=@type and \"ObjectId\" in ({string.Join(",", objectIds.Select(o => string.Format("'{0}'", o)))}) limit {count}";

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
                log.Error(ex, $"ошибка при получении последних записей типа {type} для {objectIds.Count()} объектов");
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        private TableDef GetTableDefinition(string type, NpgsqlConnection connection)
        {
            List<DateTime> dates = null;
            string format = null;
            string typeFull = $"DataRecord{type}";
            string query = $@"SELECT ""Name"" FROM ""DataRecordSections"" WHERE ""Name"" LIKE '{typeFull}%';";

            using (var command = new NpgsqlCommand())
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
                            dates.Add(DateTime.ParseExact(dateStr, format, new CultureInfo("ru-RU")));
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
                log.Warn($"недопустимое число объектов при получении последних записей типа {type}");
                return null;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["Context"].ConnectionString;
            var result = new List<DataRecord>();

            TableDef tableDefinition;
            using (var connection = new NpgsqlConnection(connectionString))
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
                    v.""Id"",
	                v.""Type"",
	                v.""Date"",
                    v.""ObjectId"",
	                v.""D1"",
	                v.""D2"",
	                v.""D3"",
	                v.""I1"",
	                v.""I2"",
	                v.""I3"",
	                v.""S1"", 
	                v.""S2"",
	                v.""S3"",
	                v.""Dt1"",
	                v.""Dt2"",
	                v.""Dt3"",
	                v.""G1"", 
	                v.""G2"",
	                v.""G3""
                FROM
                    (SELECT
                        ""ObjectId"",
                        ""Type"",
                        MAX(""Date"") as ""lastdate""
                      FROM ""{tableName}""
                      where ""Type"" = '{type}' and ""ObjectId"" in ({string.Join(",", objectIds.Select(id => $"'{id.ToString()}'"))}){(start == default(DateTime) ? "" : " and \"Date\" >= @start")}
                      group by ""ObjectId"", ""Type"") as l
                      left join ""{tableName}"" as v
                on l.""ObjectId"" = v.""ObjectId"" and l.""Type"" = v.""Type"" and l.""lastdate"" = v.""Date"";";

                        var properties = typeof(DataRecord).GetProperties();

                        using (var command = new NpgsqlCommand())
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
    }
}

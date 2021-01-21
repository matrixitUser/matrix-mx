//using log4net;
//using Npgsql;
//using NpgsqlTypes;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Diagnostics;
//using System.Dynamic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Matrix.PollServer.Storage
//{
//    class RecordsRepository2 : IDisposable
//    {
//        private const int MAX_COUNT = 1000;
//        private const int INTERVAL = 5000;
//        private readonly static ILog log = LogManager.GetLogger(typeof(RecordsRepository2));

//        Dictionary<string, NpgsqlDbType> columsHead = new Dictionary<string, NpgsqlDbType>()
//        {
//            {"timing", NpgsqlDbType.Boolean},
//            {"id", NpgsqlDbType.Uuid},
//            {"date", NpgsqlDbType.Timestamp},
//            {"type", NpgsqlDbType.Text},
//            {"objectId", NpgsqlDbType.Uuid},
//            {"d1", NpgsqlDbType.Real},
//            {"d2", NpgsqlDbType.Real},
//            {"d3", NpgsqlDbType.Real},
//            {"i1", NpgsqlDbType.Integer},
//            {"i2", NpgsqlDbType.Integer},
//            {"i3", NpgsqlDbType.Integer},
//            {"s1", NpgsqlDbType.Text},
//            {"s2", NpgsqlDbType.Text},
//            {"s3", NpgsqlDbType.Text},
//            {"dt1", NpgsqlDbType.Timestamp},
//            {"dt2", NpgsqlDbType.Timestamp},
//            {"dt3", NpgsqlDbType.Timestamp},
//            {"g1", NpgsqlDbType.Uuid},
//            {"g2", NpgsqlDbType.Uuid},
//            {"g3", NpgsqlDbType.Uuid}
//        };

//        private Dictionary<string, string[]> rules = new Dictionary<string, string[]>()
//        {
//            {"Day", new string[] {"objectId", "date", "s1"}},
//            {"Hour", new string[] {"objectId", "date", "s1"}},
//            {"Current", new string[] {"objectId", "date", "s1"}},
//            {"Constant", new string[] {"objectId", "s1"}},
//            {"Abnormal", new string[] {"objectId", "date", "s1"}}
//        };

//        //private Dictionary<string, List<dynamic>> recordCollections = new Dictionary<string, List<dynamic>>()
//        //{
//        //    {"Day", new List<dynamic>{}},
//        //    {"Hour", new List<dynamic>{}},
//        //    {"Current", new List<dynamic>{}},
//        //    {"Constant", new List<dynamic>{}},
//        //    {"Abnormal", new List<dynamic>{}}
//        //};

//        private void CreateSection(string section, string[] fields)
//        {
//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    string cmdText = string.Format(
//                        @"CREATE TABLE IF NOT EXISTS {0} 
//                    (
//                        timing BOOLEAN NOT NULL,
//                        id UUID NOT NULL PRIMARY KEY, 
//                        date TIMESTAMP NOT NULL,
//                        type TEXT NOT NULL,
//                        objectId UUID NOT NULL,
//                        d1 REAL,
//                        d2 REAL,
//                        d3 REAL,
//                        i1 INTEGER,
//                        i2 INTEGER,
//                        i3 INTEGER,
//                        s1 TEXT,
//                        s2 TEXT,
//                        s3 TEXT,
//                        dt1 TIMESTAMP,
//                        dt2 TIMESTAMP,
//                        dt3 TIMESTAMP,
//                        g1 UUID,
//                        g2 UUID,
//                        g3 UUID,
//                        UNIQUE ({1})                       
//                    )", section, string.Join(",", fields));

//                    NpgsqlCommand command = new NpgsqlCommand(cmdText, connection);
//                    connection.Open();
//                    var result = command.ExecuteNonQuery();
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при создании бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//        }

//        private void Create()
//        {
//            foreach (var rule in rules)
//            {
//                CreateSection(rule.Key, rule.Value);
//            }
//        }

//        private void OnElapsed(object sender, System.Timers.ElapsedEventArgs e)
//        {
//            dynamic[] clone = null;

//            lock (recordCollections)
//            {
//                clone = recordCollections.ToArray();
//                recordCollections.Clear();
//            }
//            if (clone != null && clone.Any())
//                InsertRecords(clone);
//        }

//        private void InsertRecords(IEnumerable<dynamic> records)
//        {
//            log.Debug(string.Format("InsertRecords {0}", records.Count()));
//            foreach (var rec in records.GroupBy(r => r.type))
//            {
//                if (rules.ContainsKey(rec.Key))
//                    BulkInsert(rec.Key, rec);
//            }
//        }

//        private System.Timers.Timer timer = new System.Timers.Timer();


//        private readonly List<dynamic> recordCollections = new List<dynamic>();

//        public void Save(IEnumerable<dynamic> records, bool sync = false)
//        {
//            foreach (var record in records)
//            {
//                record.timing = sync;
//            }

//            dynamic[] clone = null;
//            lock (recordCollections)
//            {
//                recordCollections.AddRange(records);
//                if (recordCollections.Count > MAX_COUNT)
//                {
//                    clone = recordCollections.ToArray();
//                    recordCollections.Clear();
//                }
//            }
//            if (clone != null)
//                InsertRecords(clone);
//        }

//        private void BulkInsert(string type, IEnumerable<dynamic> records)
//        {
//            timer.Stop();
//            var sw = new Stopwatch();
//            sw.Start();
//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    var tmpname = string.Format("tmp_{0}", Guid.NewGuid().ToString("N"));
//                    string script = string.Format("create temp table {0} as select * from {1} limit 1", tmpname, type);
//                    new NpgsqlCommand(script, connection).ExecuteNonQuery();
//                    script = string.Format("delete from {0}", tmpname);
//                    new NpgsqlCommand(script, connection).ExecuteNonQuery();

//                    string copyFromCommand = string.Format("COPY {0} ({1}) FROM STDIN BINARY", tmpname, string.Join(",", columsHead.Keys));
//                    using (var writer = connection.BeginBinaryImport(copyFromCommand))
//                    {
//                        int repetitions = 0;
//                        List<dynamic> writedrecords = new List<dynamic>();
//                        foreach (var record in records)
//                        {
//                            if (writedrecords.FirstOrDefault(r => r.s1 == record.s1 && r.date == record.date && r.objectId == record.objectId) != null)
//                            {
//                                //   log.Debug("повторюшка");
//                                repetitions++;
//                                continue;
//                            }
//                          //  log.Debug(string.Format("обнаружено повторяющихся записей при вставке {0}", repetitions));
//                            writedrecords.Add(record);

//                            var dictionary = record as IDictionary<string, object>;

//                            writer.StartRow();
//                            foreach (var key in columsHead.Keys)
//                            {
//                                if (dictionary.ContainsKey(key))
//                                    writer.Write(dictionary[key], columsHead[key]);
//                                else
//                                    writer.WriteNull();
//                            }
//                        }
//                    }
//                    script = string.Format("insert into {1} select t.* from {1} o right outer join {0} t on {2} where o.id is null", tmpname, type, string.Join(" and ", rules[type].Select(r => string.Format("t.{0}=o.{0}", r))));
//                    // log.Info(string.Format("скрипт {0}", script));
//                    new NpgsqlCommand(script, connection).ExecuteNonQuery();

//                    script = string.Format("drop table {0}", tmpname);
//                    new NpgsqlCommand(script, connection).ExecuteNonQuery();
//                    sw.Stop();
//                    log.Debug(string.Format("сохранено {0} записей за {1} мс", records.Count(), sw.ElapsedMilliseconds));
//                }
//                catch (Exception ex)
//                {
//                    log.Error(string.Format("ошибка при вставке в бд {0} записей {1}", records.Count(), type), ex);
//                }
//                finally
//                {
//                    connection.Close();
//                    timer.Start();
//                }
//            }
//        }

//        /// <summary>
//        /// обычная вставка, пока не используется
//        /// </summary>
//        /// <param name="type"></param>
//        /// <param name="records"></param>
//        /// <param name="sync"></param>
//        private void SimpleInsert(string type, IEnumerable<dynamic> records, bool sync = false)
//        {
//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();

//                    foreach (var record in records)
//                    {
//                        var dictionary = record as IDictionary<string, object>;
//                        if (!dictionary.ContainsKey("timing")) { dictionary.Add("timing", sync); };
//                        using (var command = new NpgsqlCommand())
//                        {
//                            string script = string.Format(@"DELETE FROM {0} where id in ({1});INSERT INTO {0} ({2}) values ({3})",
//                                type,
//                                string.Join(" and ", rules[type].Select(r => string.Format("{0}=:{0}", r))),
//                                string.Join(",", dictionary.Keys),
//                                string.Join(",", dictionary.Keys.Select(k => ":" + k)));

//                            command.CommandText = script;
//                            foreach (var key in dictionary.Keys)
//                            {
//                                command.Parameters.Add(new NpgsqlParameter(key, dictionary[key]));
//                            }
//                            command.ExecuteNonQuery();
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error(string.Format("ошибка при вставке в бд {0} записей {1}", records.Count(), type), ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//        }

//        public IEnumerable<dynamic> GetNotSyncedRecords()
//        {
//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    string cmdText = string.Format("SELECT * FROM Day where not timing");
//                    using (var command = new NpgsqlCommand(cmdText, connection))
//                    {
//                        using (var reader = command.ExecuteReader())
//                        {
//                            return ParseExecuteReader(reader);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при чтении из бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//            return new List<dynamic>(); ;
//        }

//        public void SetSyncedRecords(IEnumerable<dynamic> records)
//        {
//            if (records == null || !records.Any()) return;

//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    foreach (var groupRecords in records.GroupBy(r => r.type))
//                    {
//                        if (!rules.ContainsKey(groupRecords.Key)) continue;
//                        string cmdText = string.Format("UPDATE {0} SET timing=true where id in ({1});", groupRecords.Key, string.Join(",", groupRecords.Select(r => string.Format(@"'{0}'", r.id))));
//                        new NpgsqlCommand(cmdText, connection).ExecuteNonQuery();
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при обновлении в бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//        }

//        public IEnumerable<dynamic> GetLastRecords(string type, Guid objectId)
//        {
//            if (!rules.ContainsKey(type)) return new dynamic[] { };

//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    string cmdText = string.Format(@"select * from {0} where objectId=:objectId and date=(select max(date) from {0} where objectId=:objectId)", type);

//                    using (var command = new NpgsqlCommand(cmdText, connection))
//                    {
//                        command.Parameters.Add(new NpgsqlParameter("objectId", objectId));
//                        using (var reader = command.ExecuteReader())
//                        {
//                            return ParseExecuteReader(reader);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при чтении из бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//            return new dynamic[] { };
//        }

//        public IEnumerable<dynamic> GetRecords(string type, DateTime start, DateTime end, Guid objectId)
//        {
//            if (!rules.ContainsKey(type)) return new dynamic[] { };

//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    string cmdText = string.Format(@"select * from {0} where objectId=:objectId and date>=:start and date<=:end", type);

//                    using (var command = new NpgsqlCommand(cmdText, connection))
//                    {
//                        command.Parameters.Add(new NpgsqlParameter("objectId", objectId));
//                        command.Parameters.Add(new NpgsqlParameter("start", start));
//                        command.Parameters.Add(new NpgsqlParameter("end", end));
//                        using (var reader = command.ExecuteReader())
//                        {
//                            return ParseExecuteReader(reader);
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при чтении из бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }
//            return new dynamic[] { };
//        }

//        public DateTime? GetLastTime(string type, Guid objectId)
//        {
//            if (!rules.ContainsKey(type)) return null;

//            string connectionString = ConfigurationManager.ConnectionStrings["cache"].ConnectionString;
//            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
//            {
//                try
//                {
//                    connection.Open();
//                    string cmdText = string.Format(@"SELECT Max(date) FROM {0} WHERE objectId=:objectId", type);

//                    using (var command = new NpgsqlCommand(cmdText, connection))
//                    {
//                        command.Parameters.Add(new NpgsqlParameter("objectId", objectId));
//                        var result = command.ExecuteScalar();
//                        if (result == DBNull.Value) return null;
//                        return (DateTime)result;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    log.Error("ошибка при чтении из бд", ex);
//                }
//                finally
//                {
//                    connection.Close();
//                }
//            }

//            return null;
//        }

//        private IEnumerable<dynamic> ParseExecuteReader(NpgsqlDataReader reader)
//        {
//            List<dynamic> records = new List<dynamic>();

//            if (reader == null) return records;
//            while (reader.Read())
//            {
//                dynamic record = new ExpandoObject();
//                record.timing = reader.GetBoolean(0);
//                record.id = reader.GetGuid(1);
//                record.date = reader.GetTimeStamp(2);
//                record.type = reader.GetString(3);
//                record.objectId = reader.GetGuid(4);
//                if (!reader.IsDBNull(5))
//                    record.d1 = reader.GetDouble(5);
//                if (!reader.IsDBNull(6))
//                    record.d2 = reader.GetDouble(6);
//                if (!reader.IsDBNull(7))
//                    record.d3 = reader.GetDouble(7);
//                if (!reader.IsDBNull(8))
//                    record.i1 = reader.GetInt32(8);
//                if (!reader.IsDBNull(9))
//                    record.i2 = reader.GetInt32(9);
//                if (!reader.IsDBNull(10))
//                    record.i3 = reader.GetInt32(10);
//                if (!reader.IsDBNull(11))
//                    record.s1 = reader.GetString(11);
//                if (!reader.IsDBNull(12))
//                    record.s2 = reader.GetString(12);
//                if (!reader.IsDBNull(13))
//                    record.s3 = reader.GetString(13);
//                if (!reader.IsDBNull(14))
//                    record.dt1 = reader.GetTimeStamp(14);
//                if (!reader.IsDBNull(15))
//                    record.dt2 = reader.GetTimeStamp(15);
//                if (!reader.IsDBNull(16))
//                    record.dt3 = reader.GetTimeStamp(16);
//                if (!reader.IsDBNull(17))
//                    record.g1 = reader.GetGuid(17);
//                if (!reader.IsDBNull(18))
//                    record.g2 = reader.GetGuid(18);
//                if (!reader.IsDBNull(19))
//                    record.g3 = reader.GetGuid(19);
//                records.Add(record);
//            }
//            return records;
//        }

//        static RecordsRepository2() { }

//        private RecordsRepository2()
//        {
//            Create();
//            timer.Elapsed += OnElapsed;
//            timer.Interval = INTERVAL;
//            timer.Start();
//        }

//        private static RecordsRepository2 instance = new RecordsRepository2();
//        public static RecordsRepository2 Instance
//        {
//            get { return instance; }
//        }

//        public void Dispose()
//        {
//            timer.Stop();
//            timer.Dispose();
//            log.Info("работа репозитория завершена");
//        }
//    }
//}

//using log4net;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Data.SQLite;
//using System.Dynamic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Diagnostics;
//using System.Threading;

//namespace Matrix.PollServer
//{
//    class RecordsRepository2 : IDisposable
//    {
//        private readonly static ILog log = LogManager.GetLogger(typeof(RecordsRepository2));

//        private readonly string dbName = "cache.db";
//        private const int DB_SAVE_INTERVAL = 30 * 60 * 1000;
//        private const string DEFAULT_TABLE_NAME = "Default";
//        private const string RECORDS_VIEW = "record";

//        private EventWaitHandle wh = new AutoResetEvent(false);

//        private SQLiteConnection liteConnection;

//        private Dictionary<string, string> conditions = new Dictionary<string, string>()
//        {
//            {"Day", "type=@type and objectId=@objectId and date=@date and S1=@S1" },
//            {"Hour", "type=@type and objectId=@objectId and date=@date and S1=@S1" },
//            {"LogMessage", "type=@type and objectId=@objectId and date=@date and S1=@S1" },
//            {"Current", "type=@type and objectId=@objectId and date=@date and S1=@S1" },
//            {"Constant", "type=@type and objectId=@objectId and date=@date and S1=@S1" },
//            {"Abnormal", "type=@type and objectId=@objectId and date=@date and S1=@S1" }
//        };

//        private Dictionary<string, string[]> rules = new Dictionary<string, string[]>()
//        {
//            {"Day", new string[] {"objectId", "date", "s1"}},
//            {"Hour", new string[] {"objectId", "date", "s1"}},
//            {"Current", new string[] {"objectId", "date", "s1"}},
//            {"Constant", new string[] {"objectId", "s1"}},
//            {"Abnormal", new string[] {"objectId", "date", "s1"}}
//        };

//        private string[] fields = new string[] 
//        { 
//            "timing", "id", "date", 
//            "type", "objectId", 
//            "d1", "d2", "d3",
//            "i1", "i2", "i3",
//            "s1", "s2", "s3",
//            "dt1", "dt2", "dt3",
//            "g1", "g2", "g3"
//        };

//        private bool loop = true;
//        private void SaveDBIdle()
//        {
//            try
//            {
//                while (loop)
//                {
//                    Thread.Sleep(DB_SAVE_INTERVAL);
//                    Save();
//                }
//            }
//            catch (Exception ex)
//            {
//                log.Error("остановлен поток сохранения бд на диск", ex);
//            }
//        }

//        public void Save()
//        {
//            if (liteConnection.State != ConnectionState.Open) return;

//            var sw = new Stopwatch();
//            sw.Start();
//            using (var source = new SQLiteConnection(string.Format("Data Source={0};", dbName)))
//            {
//                source.Open();
//                liteConnection.BackupDatabase(source, "main", "main", -1, null, 0);
//                source.Close();
//            }
//            sw.Stop();
//            log.Info(string.Format("база сохранена в файл: за {0} мс", sw.ElapsedMilliseconds));
//        }

//        private void Open()
//        {
//            liteConnection = new SQLiteConnection(string.Format("Data Source={0};", dbName));
//            liteConnection.Open();
//            //var sw = new Stopwatch();
//            //sw.Start();
//            //using (var source = new SQLiteConnection(string.Format("Data Source={0};", dbName)))
//            //{
//            //    source.Open();
//            //    liteConnection = new SQLiteConnection("Data Source=:memory:");
//            //    liteConnection.Open();
//            //    source.BackupDatabase(liteConnection, "main", "main", -1, null, 0);
//            //    source.Close();
//            //}
//            //sw.Stop();
//            //log.Info(string.Format("База загружена в RAM: за {0} мс", sw.ElapsedMilliseconds));

//            //loop = true;

//            //saveThread = new Thread(SaveDBIdle);
//            //saveThread.IsBackground = true;
//            //saveThread.Name = "репозиторий: сохранеие бд в файл";
//            //saveThread.Start();
//        }

//        private void Load()
//        {

//        }

//        private Thread saveThread;

//        private void Close()
//        {
//            loop = false;
//            if (liteConnection.State == ConnectionState.Open)
//            {
//                // Save();
//                liteConnection.Close();
//            }
//            wh.Set();
//            //  saveThread.Join();
//            try
//            {
//                saveThread.Abort();
//            }
//            catch (Exception ex)
//            {

//            }
//            log.Info("соединение закрыто");
//        }

//        public static DateTime FromUnix(long utime)
//        {
//            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).AddSeconds(utime);
//        }

//        public static int ToUnix(DateTime time)
//        {
//            return (int)(time - new DateTime(1970, 1, 1)).TotalSeconds;
//        }

//        private void Create()
//        {
//            using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//            {
//                rules.Add(DEFAULT_TABLE_NAME, new string[] { "id" });

//                foreach (var rule in rules)
//                {
//                    command.CommandText = string.Format(@"CREATE TABLE IF NOT EXISTS [{0}]
//                      (timing INTEGER NOT NULL,
//                      id TEXT NOT NULL PRIMARY KEY,
//                      date INTEGER NOT NULL,
//                      type TEXT NOT NULL,
//                      objectId TEXT NOT NULL,
//                      d1 REAL,
//                      d2 REAL,
//                      d3 REAL,
//                      i1 INTEGER,
//                      i2 INTEGER,
//                      i3 INTEGER,
//                      s1 TEXT,
//                      s2 TEXT,
//                      s3 TEXT,
//                      dt1 BLOB,
//                      dt2 BLOB,
//                      dt3 BLOB,
//                      g1 TEXT,
//                      g2 TEXT,
//                      g3 TEXT, 
//                      UNIQUE ({1})
//                      ON CONFLICT REPLACE)", rule.Key, string.Join(",", rule.Value));

//                    command.ExecuteNonQuery();
//                    log.Info(string.Format("создана секция '{0}'", rule.Key));
//                }

//                command.CommandText = string.Format(@"CREATE VIEW IF NOT EXISTS {0} as {1}", RECORDS_VIEW, string.Join(" UNION ALL ", rules.Keys.Select(r => string.Format("SELECT * FROM [{0}]", r))));
//                command.ExecuteNonQuery();
//                log.Info(string.Format("создано представление '{0}'", RECORDS_VIEW));
//            }
//        }

//        private List<dynamic> datarecords = new List<dynamic>();

//        private readonly object insertLocker = new object();
//        //  private readonly object updateLocker = new object();

//        public void InsertRecords(IEnumerable<dynamic> records, bool sync = false)
//        {
//            lock (insertLocker)
//            {
//                var sw = new Stopwatch();
//                sw.Start();
//                using (var transaction = liteConnection.BeginTransaction())
//                {
//                    foreach (var groupRecords in records.GroupBy(r => r.type))
//                    {
//                        var tableName = DEFAULT_TABLE_NAME;
//                        if (rules.ContainsKey(groupRecords.Key))
//                        {
//                            tableName = groupRecords.Key;
//                        }

//                        foreach (var record in groupRecords)
//                        {
//                            var dictionary = record as IDictionary<string, object>;
//                            if (!dictionary.ContainsKey("timing")) { dictionary.Add("timing", sync ? 1 : 0); };

//                            var command = new SQLiteCommand(liteConnection);

//                            foreach (var key in dictionary.Keys)
//                            {
//                                switch (key)
//                                {
//                                    case "date":
//                                        {
//                                            command.Parameters.AddWithValue("@" + key, ToUnix((DateTime)dictionary[key]));
//                                            break;
//                                        }
//                                    case "id":
//                                        {
//                                            command.Parameters.AddWithValue("@" + key, dictionary[key].ToString());
//                                            break;
//                                        }
//                                    case "objectId":
//                                        {
//                                            command.Parameters.AddWithValue("@" + key, dictionary[key].ToString());
//                                            break;
//                                        }
//                                    default:
//                                        {
//                                            command.Parameters.AddWithValue("@" + key, dictionary[key]);
//                                            break;
//                                        }
//                                }
//                            }

//                            command.CommandText = string.Format("INSERT INTO [{2}]({0}) VALUES({1});",
//                                string.Join(",", dictionary.Keys),
//                                string.Join(",", dictionary.Keys.Select(x => "@" + x)),
//                                tableName);
//                            try
//                            {
//                                command.ExecuteNonQuery();
//                            }
//                            catch (Exception ex)
//                            {
//                                log.Error("ошибка при вставке в бд {0}", ex);
//                            }
//                        }
//                    }
//                    transaction.Commit();
//                }
//                sw.Stop();
//                log.Debug(string.Format("порция из {0} записей сохранена за {1} мс", records.Count(), sw.ElapsedMilliseconds));
//            }
//        }

//        public void SetSyncedRecords(IEnumerable<dynamic> records)
//        {
//            if (records == null || !records.Any()) return;

//            lock (insertLocker)
//            {
//                using (var transaction = liteConnection.BeginTransaction())
//                {
//                    foreach (var groupRecords in records.GroupBy(r => r.type))
//                    {
//                        var tableName = DEFAULT_TABLE_NAME;
//                        if (rules.ContainsKey(groupRecords.Key))
//                        {
//                            tableName = groupRecords.Key;
//                        }

//                        var command = new SQLiteCommand(liteConnection);

//                        command.CommandText = string.Format("UPDATE [{0}] SET timing=1 where id in ({1});", tableName, string.Join(",", groupRecords.Select(r => string.Format(@"'{0}'", r.id))));

//                        try
//                        {
//                            command.ExecuteNonQuery();
//                        }
//                        catch (Exception ex)
//                        {
//                            log.Error("ошибка при обновлении в бд {0}", ex);
//                        }
//                    }
//                    transaction.Commit();
//                }
//            }
//        }

//        public IEnumerable<dynamic> Select(Guid objectId)
//        {
//            List<dynamic> result = new List<dynamic>();

//            log.Info(string.Format("начато считывания записей по id={0}", objectId));
//            using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//            {
//                command.Parameters.AddWithValue("@objectId", objectId.ToString());
//                // command.CommandText = string.Format("select {0} from records where type=@type and objectId=@objectId", string.Join(",", fields));
//                command.CommandText = string.Format("SELECT {0} FROM [record] WHERE objectId=@objectId", string.Join(",", fields));

//                using (SQLiteDataReader rdr = command.ExecuteReader())
//                {
//                    while (rdr.Read())
//                    {
//                        dynamic record = new ExpandoObject();
//                        record.timing = rdr.GetInt32(0);
//                        record.id = rdr.GetGuid(1);
//                        record.date = FromUnix(rdr.GetInt32(2));
//                        record.type = rdr.GetString(3);
//                        record.objectId = rdr.GetGuid(4);
//                        if (!rdr.IsDBNull(5))
//                            record.d1 = rdr.GetDouble(5);
//                        if (!rdr.IsDBNull(6))
//                            record.d2 = rdr.GetDouble(6);
//                        if (!rdr.IsDBNull(7))
//                            record.d3 = rdr.GetDouble(7);
//                        if (!rdr.IsDBNull(8))
//                            record.i1 = rdr.GetInt32(8);
//                        if (!rdr.IsDBNull(9))
//                            record.i2 = rdr.GetInt32(9);
//                        if (!rdr.IsDBNull(10))
//                            record.i3 = rdr.GetInt32(10);
//                        if (!rdr.IsDBNull(11))
//                            record.s1 = rdr.GetString(11);
//                        if (!rdr.IsDBNull(12))
//                            record.s2 = rdr.GetString(12);
//                        if (!rdr.IsDBNull(13))
//                            record.s3 = rdr.GetString(13);
//                        if (!rdr.IsDBNull(14))
//                            record.dt1 = rdr.GetDateTime(14);
//                        if (!rdr.IsDBNull(15))
//                            record.dt2 = rdr.GetDateTime(15);
//                        if (!rdr.IsDBNull(16))
//                            record.dt3 = rdr.GetDateTime(16);
//                        if (!rdr.IsDBNull(17))
//                            record.g1 = rdr.GetGuid(17);
//                        if (!rdr.IsDBNull(18))
//                            record.g2 = rdr.GetGuid(18);
//                        if (!rdr.IsDBNull(19))
//                            record.g3 = rdr.GetGuid(19);

//                        result.Add(record);
//                    }
//                }
//            }
//            return result;
//        }

//        public DateTime? GetLastTime(string type, Guid objectId)
//        {
//            try
//            {
//                using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//                {
//                    command.Parameters.AddWithValue("@type", type);
//                    command.Parameters.AddWithValue("@objectId", objectId.ToString());

//                    command.CommandText = string.Format(@"SELECT Max(date) FROM [{0}] WHERE objectId=@objectId", type);

//                    var x = command.ExecuteScalar();
//                    if (x == DBNull.Value) return null;

//                    return FromUnix((long)x);
//                }
//            }
//            catch (Exception ex)
//            {
//                log.Error(ex.Message);
//            }

//            return null;
//        }

//        public IEnumerable<dynamic> GetLastRecords(string type, Guid objectId)
//        {
//            try
//            {
//                using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//                {
//                    command.Parameters.AddWithValue("@type", type);
//                    command.Parameters.AddWithValue("@objectId", objectId.ToString());
//                    command.CommandText = string.Format(
//                            @"select * from [{0}] where objectId=@objectId and type = @type and date=(select max(date) from [{0}] where objectId=@objectId and type = @type)", type);

//                    var result = new List<dynamic>();
//                    using (SQLiteDataReader rdr = command.ExecuteReader())
//                    {
//                        while (rdr.Read())
//                        {
//                            dynamic record = new ExpandoObject();
//                            record.timing = rdr.GetInt32(0);
//                            record.id = rdr.GetGuid(1);
//                            record.date = FromUnix(rdr.GetInt32(2));
//                            record.type = rdr.GetString(3);
//                            record.objectId = rdr.GetGuid(4);
//                            if (!rdr.IsDBNull(5))
//                                record.d1 = rdr.GetDouble(5);
//                            if (!rdr.IsDBNull(6))
//                                record.d2 = rdr.GetDouble(6);
//                            if (!rdr.IsDBNull(7))
//                                record.d3 = rdr.GetDouble(7);
//                            if (!rdr.IsDBNull(8))
//                                record.i1 = rdr.GetInt32(8);
//                            if (!rdr.IsDBNull(9))
//                                record.i2 = rdr.GetInt32(9);
//                            if (!rdr.IsDBNull(10))
//                                record.i3 = rdr.GetInt32(10);
//                            if (!rdr.IsDBNull(11))
//                                record.s1 = rdr.GetString(11);
//                            if (!rdr.IsDBNull(12))
//                                record.s2 = rdr.GetString(12);
//                            if (!rdr.IsDBNull(13))
//                                record.s3 = rdr.GetString(13);
//                            if (!rdr.IsDBNull(14))
//                                record.dt1 = rdr.GetDateTime(14);
//                            if (!rdr.IsDBNull(15))
//                                record.dt2 = rdr.GetDateTime(15);
//                            if (!rdr.IsDBNull(16))
//                                record.dt3 = rdr.GetDateTime(16);
//                            if (!rdr.IsDBNull(17))
//                                record.g1 = rdr.GetGuid(17);
//                            if (!rdr.IsDBNull(18))
//                                record.g2 = rdr.GetGuid(18);
//                            if (!rdr.IsDBNull(19))
//                                record.g3 = rdr.GetGuid(19);
//                            result.Add(record);
//                        }
//                        return result;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                log.Error(ex.Message);
//            }

//            return null;
//        }

//        public IEnumerable<dynamic> GetRecords(string type, DateTime start, DateTime end, Guid objectId)
//        {
//            List<dynamic> result = new List<dynamic>();
//            using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//            {
//                command.Parameters.AddWithValue("@start", ToUnix(start));
//                command.Parameters.AddWithValue("@end", ToUnix(end));
//                command.Parameters.AddWithValue("@objectId", objectId.ToString());
//                command.Parameters.AddWithValue("@type", type);
//                command.CommandText = string.Format("select {0} from {1} where type=@type and objectId=@objectId and date>=@start and date<=@end", string.Join(",", fields), RECORDS_VIEW);

//                using (SQLiteDataReader rdr = command.ExecuteReader())
//                {
//                    while (rdr.Read())
//                    {
//                        dynamic record = new ExpandoObject();
//                        record.timing = rdr.GetInt32(0);
//                        record.id = rdr.GetGuid(1);
//                        record.date = FromUnix(rdr.GetInt32(2));
//                        record.type = rdr.GetString(3);
//                        record.objectId = rdr.GetGuid(4);
//                        if (!rdr.IsDBNull(5))
//                            record.d1 = rdr.GetDouble(5);
//                        if (!rdr.IsDBNull(6))
//                            record.d2 = rdr.GetDouble(6);
//                        if (!rdr.IsDBNull(7))
//                            record.d3 = rdr.GetDouble(7);
//                        if (!rdr.IsDBNull(8))
//                            record.i1 = rdr.GetInt32(8);
//                        if (!rdr.IsDBNull(9))
//                            record.i2 = rdr.GetInt32(9);
//                        if (!rdr.IsDBNull(10))
//                            record.i3 = rdr.GetInt32(10);
//                        if (!rdr.IsDBNull(11))
//                            record.s1 = rdr.GetString(11);
//                        if (!rdr.IsDBNull(12))
//                            record.s2 = rdr.GetString(12);
//                        if (!rdr.IsDBNull(13))
//                            record.s3 = rdr.GetString(13);
//                        if (!rdr.IsDBNull(14))
//                            record.dt1 = rdr.GetDateTime(14);
//                        if (!rdr.IsDBNull(15))
//                            record.dt2 = rdr.GetDateTime(15);
//                        if (!rdr.IsDBNull(16))
//                            record.dt3 = rdr.GetDateTime(16);
//                        if (!rdr.IsDBNull(17))
//                            record.g1 = rdr.GetGuid(17);
//                        if (!rdr.IsDBNull(18))
//                            record.g2 = rdr.GetGuid(18);
//                        if (!rdr.IsDBNull(19))
//                            record.g3 = rdr.GetGuid(19);

//                        result.Add(record);
//                    }
//                }
//                return result;
//            }
//        }

//        public IEnumerable<dynamic> GetNotSyncedRecords()
//        {
//            List<dynamic> result = new List<dynamic>();

//            using (SQLiteCommand command = new SQLiteCommand(liteConnection))
//            {
//                command.CommandText = string.Format("SELECT {0} FROM [{1}] WHERE timing=0 ORDER BY date LIMIT 700", string.Join(",", fields), RECORDS_VIEW);

//                using (SQLiteDataReader rdr = command.ExecuteReader())
//                {
//                    while (rdr.Read())
//                    {
//                        dynamic record = new ExpandoObject();
//                        record.timing = rdr.GetInt32(0);
//                        record.id = rdr.GetGuid(1);
//                        record.date = FromUnix(rdr.GetInt32(2));
//                        record.type = rdr.GetString(3);
//                        record.objectId = rdr.GetGuid(4);
//                        if (!rdr.IsDBNull(5))
//                            record.d1 = rdr.GetDouble(5);
//                        if (!rdr.IsDBNull(6))
//                            record.d2 = rdr.GetDouble(6);
//                        if (!rdr.IsDBNull(7))
//                            record.d3 = rdr.GetDouble(7);
//                        if (!rdr.IsDBNull(8))
//                            record.i1 = rdr.GetInt32(8);
//                        if (!rdr.IsDBNull(9))
//                            record.i2 = rdr.GetInt32(9);
//                        if (!rdr.IsDBNull(10))
//                            record.i3 = rdr.GetInt32(10);
//                        if (!rdr.IsDBNull(11))
//                            record.s1 = rdr.GetString(11);
//                        if (!rdr.IsDBNull(12))
//                            record.s2 = rdr.GetString(12);
//                        if (!rdr.IsDBNull(13))
//                            record.s3 = rdr.GetString(13);
//                        if (!rdr.IsDBNull(14))
//                            record.dt1 = rdr.GetDateTime(14);
//                        if (!rdr.IsDBNull(15))
//                            record.dt2 = rdr.GetDateTime(15);
//                        if (!rdr.IsDBNull(16))
//                            record.dt3 = rdr.GetDateTime(16);
//                        if (!rdr.IsDBNull(17))
//                            record.g1 = rdr.GetGuid(17);
//                        if (!rdr.IsDBNull(18))
//                            record.g2 = rdr.GetGuid(18);
//                        if (!rdr.IsDBNull(19))
//                            record.g3 = rdr.GetGuid(19);
//                        result.Add(record);
//                    }
//                    rdr.Close();
//                }

//                return result;
//            }
//        }

//        static RecordsRepository2() { }

//        private RecordsRepository2()
//        {
//            Open();
//            Create();
//        }

//        private static RecordsRepository2 instance = new RecordsRepository2();
//        public static RecordsRepository2 Instance
//        {
//            get
//            {
//                return instance;
//            }
//        }

//        public void Dispose()
//        {
//            log.Info("Начато завершение работы репозитория");
//            Close();
//        }
//    }
//}

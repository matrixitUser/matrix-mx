using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;

namespace Matrix.RecordsSaver
{
    public class Saver
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Foo()
        {
            var repo = new Repository();
            repo.Foo();
        }

        private const string FILE_DB = "db.json";

        private readonly List<dynamic> batch = new List<dynamic>();

        public void Push(IEnumerable<dynamic> records)
        {
            if (records == null || !records.Any()) return;
            lock (batch)
            {
                batch.AddRange(records);
            }
            logger.Debug("порция записей из {0} шт. записана к кучу", records.Count());
        }

        public void Save()
        {
            if (!batch.Any()) return;
            var repo = new Repository();
            var copy = batch.ToArray();
            batch.Clear();
            File.AppendAllLines(FILE_DB, repo.Save(copy).Select(r => (string)JsonConvert.SerializeObject(r)));
        }
    }

    /// <summary>
    /// ручное чтение/запись архивов
    /// 
    /// при чтении используется хинт NOLOCK - "грязное" чтение, позволяет избежать блокировок
    /// запись в бд BULK INSERT-ом, при этом сначала пишем во временную таблицу, а потом MERGE-им ее с целевой
    /// </summary>   
    //private static readonly ILog log = LogManager.GetLogger(typeof(DataRecordRepository2));
    class Repository
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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

        public void Foo()
        {
            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var cmd = new SqlCommand();
                cmd.Connection = con;
                cmd.CommandText = "select * from hour";

                var reader = cmd.ExecuteReader();

                var records = new List<dynamic>();

                while (reader.Read())
                {
                    dynamic record = new ExpandoObject();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = reader.GetValue(i);
                        (record as IDictionary<string, object>).Add(name, value);
                    }
                    records.Add(record);
                }
                con.Close();
            }
        }

        public IEnumerable<dynamic> Save(IEnumerable<dynamic> records)
        {
            if (records == null || !records.Any())
            {
                logger.Warn("записи для сохранения отсутствуют");
                return new dynamic[] { };
            }

            using (var con = new SqlConnection(ConnectionString))
            {
                con.Open();
                var cnt = records.Count();

                //1. 
                foreach (RuleElement rule in SaveRulesSection.Instance.Rules)
                {
                    try
                    {
                        if (!records.Any()) break;

                        var allowToRuleRecords = records.Where(r => r.type == rule.Type).ToArray();
                        if (!allowToRuleRecords.Any()) continue;
                        
                        logger.Debug(string.Format("сработало правило {0}", rule.Type));
                        //проверка и создание секции
                        var cmd = new SqlCommand();
                        cmd.Connection = con;
                        cmd.CommandText = "select count(*) as cnt from INFORMATION_SCHEMA.TABLES where TABLE_NAME=@name";
                        cmd.Parameters.AddWithValue("@name", rule.Type);   //имя таблицы
                        if ((int)cmd.ExecuteScalar() == 0)   
                        {
                            var index = rule.GetIndexFields();
                            var fields = rule.GetFields();

                            var columns = fields.Select(f => string.Format("[{0}] {1} {2}", f.Key, f.Value, (index.Contains(f.Key) ? "not null" : "null")));

                            var q1 = string.Format(@"create table [dbo].[{0}]({1})", rule.Type, string.Join(",", columns));
                            new SqlCommand(q1, con).ExecuteNonQuery();

                            if (index.Any())
                            {
                                var q2 = string.Format("create unique nonclustered index unique_row on [{0}]({1}) with (ignore_dup_key = on)", rule.Type, string.Join(",", index.Select(u => string.Format("[{0}] asc", u))));
                                new SqlCommand(q2, con).ExecuteNonQuery();
                            }

                            logger.Debug("создана секция {0}", rule.Type);
                        }

                        bulkInserter.Insert(allowToRuleRecords, rule.Type,rule.GetFields().Keys.ToArray(), con);
                        records = records.Where(r => r.type != rule.Type).ToArray();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "ошибка при сохранении по правилу {0}", rule.Type);
                    }
                }

                if (records.Any())
                {
                    logger.Debug("записей не попавших под правила {0} шт", records.Count());                    
                }

                return records;
            }
        }

    }
}

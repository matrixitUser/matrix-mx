//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Data.Common;
//using Npgsql;
//using System.Runtime.Serialization;
//using System.Reflection;
//using Matrix.Domain.Structure;
//using log4net;

//namespace Matrix.Domain.Infrastructure.EntityFramework
//{
//    public class PostgreSqlBulkInserter : IBulkInserter
//    {
//        private static readonly ILog log = LogManager.GetLogger(typeof(PostgreSqlBulkInserter));

//        private static readonly object insertLocker = new object();

//        public void Insert<TEntity>(IEnumerable<TEntity> entities, DbConnection connection)
//        {
//            if (entities == null || !entities.Any())
//            {
//                return;
//            }
//            lock (insertLocker)
//            {
//                //определяем столбцы
//                var prototype = entities.FirstOrDefault();
//                if (prototype == null) return;

//                var properties = new List<PropertyInfo>();
//                var uniqueProperties = new List<PropertyInfo>();
//                var type = prototype.GetType();
//                foreach (var propertyInfo in type.GetProperties())
//                {
//                    if (!propertyInfo.CanWrite) continue;
//                    var attributes = propertyInfo.GetCustomAttributes(true);
//                    bool isIgnore = false;
//                    bool isUnique = false;

//                    foreach (var attribute in attributes)
//                    {
//                        if (attribute is IgnoreDataMemberAttribute)
//                        {
//                            isIgnore = true;
//                        }
//                        if (attribute is UniqueAttribute)
//                        {
//                            isUnique = true;
//                        }
//                    }

//                    if (!isIgnore) properties.Add(propertyInfo);
//                    if (isUnique) uniqueProperties.Add(propertyInfo);
//                }

//                var psCon = (NpgsqlConnection)connection;
//                psCon.Open();

//                var tempTableName = "temp";
//                var createTempTableCommand = new NpgsqlCommand(string.Format(@"drop table if exists {0}; create temporary table {0} as select * from dbo.""{1}"" limit 0;", tempTableName, type.Name), psCon);				
//                var prepareCommandResult = createTempTableCommand.ExecuteNonQuery();
//                log.DebugFormat("временная таблица {0} создана копированием из таблицы {1} ({2})", tempTableName, type.Name, prepareCommandResult);

//                var query = string.Format(@"copy {0}({1}) from stdin", tempTableName, string.Join(",", properties.Select(p => string.Format(@"""{0}""", p.Name))));
//                log.DebugFormat(@"подготовлен запрос на вставку во временную таблицу: {0}", query);
//                var command = new NpgsqlCommand(query, psCon);

//                var serializer = new NpgsqlCopySerializer(psCon);

//                var copier = new NpgsqlCopyIn(command, psCon, serializer.ToStream);

//                copier.Start();
//                log.DebugFormat("подготовка пакета для записи, количество записей",entities.Count());
//                foreach (var item in entities)
//                {
//                    foreach (var property in properties)
//                    {
//                        var value = property.GetValue(item, null);

//                        if (property.PropertyType == typeof(double))
//                        {
//                            value = value.ToString().Replace(",", ".");
//                        }

//                        serializer.AddString(value.ToString());
//                    }
//                    serializer.EndRow();
//                    serializer.Flush();
//                }
//                copier.End();

//                var copyQuery = string.Format(@"insert into dbo.""{0}"" (select t.* from {1} t left join dbo.""{0}"" d on {2} where d.* is null)", type.Name, tempTableName, string.Join(" and ", uniqueProperties.Select(p => string.Format(@"t.""{0}""=d.""{0}""", p.Name))));
//                log.DebugFormat("подготовлен запрос на копирование данных из временной таблицы в основную: {0}", copyQuery);
//                var copyCommand = new NpgsqlCommand(copyQuery, psCon);
//                var rowsAffected = copyCommand.ExecuteNonQuery();

//                log.DebugFormat("копирование завершено, изменено строк {0}", rowsAffected);

//                psCon.Close();
//            }
//        }
//    }
//}

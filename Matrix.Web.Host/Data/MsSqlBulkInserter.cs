using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.Domain.Entities;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using log4net;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
    public class MsSqlBulkInserter
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MsSqlBulkInserter));

        public void Insert<TEntity>(IEnumerable<TEntity> entities, string table, SqlConnection connection)
        {
            if (entities == null || !entities.Any()) return;
            log.Debug(string.Format("сохраняются архивы {0} шт.", entities.Count()));

            var prototype = entities.FirstOrDefault();
            try
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);

                bulkCopy.DestinationTableName = table;

                var reader = new EntityDataReader<TEntity>(entities);
                foreach (var property in reader.Properties)
                {
                    var name = property.Name;
                    bulkCopy.ColumnMappings.Add(name, name);
                }

                bulkCopy.WriteToServer(reader);
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при сохранении архивов"), ex);
            }
        }
    }
}

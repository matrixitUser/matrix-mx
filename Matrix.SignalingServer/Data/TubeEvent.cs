using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace Matrix.SignalingServer.Data
{
    /// <summary>
    /// работа с кешом строк в клиенте
    /// актуализация, уведомление об изменении
    /// </summary>
    class TubeEvent
    {
        public const string TABLENAME = "TubeEvent";
        private const string STORAGE_TYPE_MSSQL = "mssql";

        private static readonly ILog log = LogManager.GetLogger(typeof(TubeEvent));

        private readonly string tableFull;
        private readonly string Qs;
        private readonly string Qe;
        private readonly string likeString;
        private readonly string storageType;
        private readonly string trueString = "True";
        private readonly string falseString = "False";

        private TubeEvent()
        {
            storageType = ConfigurationManager.AppSettings["storage-type"];
            if (storageType == STORAGE_TYPE_MSSQL)
            {
                tableFull = TABLENAME;
                Qs = "[";
                Qe = "]";
                likeString = "like";
                CreateMsSql();
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        #region Создание таблицы TubeEvent
        private void CreateMsSql()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    var query = new StringBuilder();
                    query.AppendLine("if object_id('TubeEvent','U') is null ");
                    query.AppendLine("create table TubeEvent(");
                    query.AppendLine("[id] uniqueidentifier not null primary key,");
                    query.AppendLine("[objectId] uniqueidentifier not null,");
                    query.AppendLine("[message] nvarchar(255) not null,");
                    query.AppendLine("[dateStart] datetime2(7),");
                    query.AppendLine("[dateEnd] datetime2(7),");
                    query.AppendLine("[dateQuit] datetime2(7),");
                    query.AppendLine("[parameter] nvarchar(255) not null,");
                    query.AppendLine("[name] nvarchar(255) not null,");
                    query.AppendLine("[value] float not null,");
                    query.AppendLine("[tag] nvarchar(255) not null,");
                    query.AppendLine("[dtStart] datetime2(7),");
                    query.AppendLine("[dtEnd] datetime2(7),");
                    query.AppendLine("[setPoint] float,");
                    query.AppendLine("[replay] bit");
                    query.AppendLine(")");

                    command.Connection = connection;
                    command.CommandText = query.ToString();
                    connection.Open();
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        #endregion

        private static TubeEvent instance = new TubeEvent();
        public static TubeEvent Instance
        {
            get
            {
                return instance;
            }
        }

        private readonly string[] columns = new string[] {
            "id",
            "objectId",
            "message",
            "dateStart",
            "dateEnd",
            "dateQuit",
            "parameter",
            "name",
            "value",
            "tag",
            "dtStart",
            "dtEnd",
            "setPoint",
            "replay"
        };

        public List<dynamic> GetById(Guid id)
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[id] = '{id}'");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }

        public List<dynamic> GetActiveEvents(IEnumerable<Guid> objectIds)
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[objectId] in ({string.Join(",", objectIds.Select(i => string.Format("'{0}'", i)))})");
            conditions.Add($"([dateEnd] is null or [dateQuit] is null)");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }

        public List<dynamic> GetActiveEventByObjectId(Guid objectId)
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[objectId] = '{objectId}'");
            conditions.Add($"[dateEnd] is null");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }

        public List<dynamic> GetByObjectIdAndDateStart(IEnumerable<Guid> objectIds, DateTime dateStart, Guid userId)
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[objectId] in ({string.Join(",", objectIds.Select(i => string.Format("'{0}'", i)))})");
            conditions.Add($"([dateStart] >= {dateStart}");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }
        public IEnumerable<dynamic> GetByDateStart(DateTime dateStart, Guid userId)
        {
            var rows = new List<dynamic>();

            //права и папки

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                            command.Parameters["@start"].Value = dateStart;

                            var rowsQuery = new StringBuilder();
                            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
                            rowsQuery.AppendLine("where [dateStart] >= @start");
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }
        public List<dynamic> GetByDateStartEnd(DateTime sDateStart, DateTime eDateStart)
        {
            var rows = new List<dynamic>();

            //права и папки

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.Parameters.Add("@start", System.Data.SqlDbType.DateTime2);
                            command.Parameters["@start"].Value = sDateStart;
                            command.Parameters.Add("@end", System.Data.SqlDbType.DateTime2);
                            command.Parameters["@end"].Value = eDateStart;

                            var rowsQuery = new StringBuilder();
                            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
                            rowsQuery.AppendLine("where [dateStart] between @start and @end ");
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }
        public List<dynamic> GetActiveEventsAll()
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[dateEnd] is null or [dateQuit] is null");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }


        public IEnumerable<dynamic> GetActiveEventsAllOnlyDateEnd()
        {
            var rows = new List<dynamic>();

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"[dateEnd] is null");

            var rowsQuery = new StringBuilder();
            rowsQuery.AppendLine($"select {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}"))} from {tableFull}");
            rowsQuery.AppendLine(string.Format("where {0}", string.Join(" and ", conditions)));

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = rowsQuery.ToString();
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                dynamic row = new ExpandoObject();
                                var drow = row as IDictionary<string, object>;

                                for (var i = 0; i < columns.Length; i++)
                                {
                                    var col = columns[i];
                                    drow.Add(col, reader.GetValue(i));
                                }
                                rows.Add(row);
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }

            return rows;
        }

        public void CreateRow(DateTime DateStart, Guid objectId, string Message, string parameter, string name, double value, string tag, DateTime dtStart, double setPoint)
        {

            var commandText = $"INSERT INTO {tableFull} ({Qs}id{Qe},{Qs}objectId{Qe},{Qs}Message{Qe},{Qs}DateStart{Qe},{Qs}parameter{Qe},{Qs}name{Qe},{Qs}value{Qe},{Qs}tag{Qe},{Qs}dtStart{Qe},{Qs}setPoint{Qe},{Qs}replay{Qe}) VALUES (@id, @objectId, @Message, @DateStart, @parameter, @name, @value, @tag, @dtStart, @setPoint, @replay)";

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", Guid.NewGuid());
                            command.Parameters.AddWithValue("@objectId", objectId);
                            command.Parameters.AddWithValue("@Message", Message);
                            command.Parameters.AddWithValue("@DateStart", DateStart);
                            command.Parameters.AddWithValue("@parameter", parameter);
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@value", value);
                            command.Parameters.AddWithValue("@tag", tag);
                            command.Parameters.AddWithValue("@dtStart", dtStart);
                            command.Parameters.AddWithValue("@setPoint", setPoint);
                            command.Parameters.AddWithValue("@replay", 0);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось создать строку в TubeEvent"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        public void DeleteRow(Guid id)
        {
            var commandText = $"DELETE FROM {tableFull} where id=@id;";

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", id);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error($"не удалось удалить строку {id} в таблице {tableFull} ", ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        public void UpdateRow(Guid id, DateTime DateStart, double value, byte replay, double setPoint)
        {
            var commandText = $"update {tableFull} set {Qs}DateStart{Qe}=@DateStart, {Qs}value{Qe}=@value, {Qs}replay{Qe}=@replay, {Qs}setPoint{Qe}=@setPoint where id=@id;";

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", id);
                            command.Parameters.AddWithValue("@DateStart", DateStart);
                            command.Parameters.AddWithValue("@value", value);
                            command.Parameters.AddWithValue("@replay", replay);
                            command.Parameters.AddWithValue("@setPoint", setPoint);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось квитировать"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        public void UpdateDateQuit(DateTime DateQuit, Guid id)
        {
            var commandText = $"update {tableFull} set {Qs}DateQuit{Qe}=@DateQuit where id=@id;";

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", id);
                            command.Parameters.AddWithValue("@DateQuit", DateQuit);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось квитировать"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        public void UpdateDateEnd(DateTime DateEnd, Guid id)
        {

            var commandText = $"update {tableFull} set {Qs}DateEnd{Qe}=@DateEnd, {Qs}dtEnd{Qe}=@dtEnd  where id=@id;";

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", id);
                            command.Parameters.AddWithValue("@DateEnd", DateEnd);
                            command.Parameters.AddWithValue("@dtEnd", DateTime.Now);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось установить DateEnd"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using log4net;
using Matrix.Web.Host.Data.Sql;
using Npgsql;
using System.Data.SqlClient;

namespace Matrix.Web.Host.Data
{
    /// <summary>
    /// работа с кешом строк в клиенте
    /// актуализация, уведомление об изменении
    /// </summary>
    class RowsCache
    {
        public const string TABLENAME = "RowsCache";
        private const string STORAGE_TYPE_MSSQL = "mssql";
        private const string STORAGE_TYPE_PG = "pg";

        private static readonly ILog log = LogManager.GetLogger(typeof(RowsCache));
        
        private readonly string tableFull;
        private readonly string Qs;
        private readonly string Qe;
        private readonly string likeString;
        private readonly string storageType;
        private readonly string trueString = "True";
        private readonly string falseString = "False";

        private RowsCache()
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
            else if (storageType == STORAGE_TYPE_PG)
            {
                tableFull = $"public.\"{TABLENAME}\"";
                Qs = "\"";
                Qe = "\"";
                likeString = "ilike";
                CreateNpgSql();
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        #region Создание таблицы RowsCache
        private void CreateMsSql()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    var query = new StringBuilder();
                    query.AppendLine("if object_id('RowsCache','U') is null ");
                    query.AppendLine("create table RowsCache(");
                    query.AppendLine("[id] uniqueidentifier not null primary key,");
                    query.AppendLine(string.Format("{0}", string.Join(",", columns.Where(c => c != "id").Select(c => string.Format("[{0}] nvarchar(255)", c)))));
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
        private void CreateNpgSql()
        {
            using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
            {
                using (var command = new NpgsqlCommand())
                {
                    command.Connection = connection;
                    connection.Open();

                    try
                    {
                        command.CommandText = "select exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME='RowsCache');";
                        command.Parameters.AddWithValue("@name", tableFull);
                        var ex = (bool)command.ExecuteScalar();

                        if (!ex)
                        {
                            command.CommandText = $"create table {tableFull} (\"id\" uuid not null, {string.Join(",", columns.Where(c => c != "id").Select(c => $"\"{c}\" character varying(255)"))});";
                            command.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }
        #endregion

        private static RowsCache instance = new RowsCache();
        public static RowsCache Instance
        {
            get
            {
                return instance;
            }
        }

        private readonly string[] columns = new string[] {
            "id",
            "state",
            "description",
            "name",
            "pname",
            "city",
            "phone",
            "imei",
            "isDeleted",
            "device",
            "isDisabled",
            "abnormals",
            "number",
            "class",
            "deviceId",
            "resource",
            "fulness",
            "comment",
            "value",
            "controllerData",
            "valueUnitMeasurement",
            "coordinates",
            "event",
            "date",
            "fulnessHour"

            //,"fiasid","address"
        };

        public IEnumerable<dynamic> Get(IEnumerable<Guid> tubeIds, Guid userId)
        {
            var rows = new List<dynamic>();

            var ids = StructureGraph.Instance.GetTubeIds(userId).Where(id => tubeIds.Contains(id));

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"{Qs}id{Qe} in ({string.Join(",", ids.Select(i => string.Format("'{0}'", i)))})");

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
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        using (var command = new NpgsqlCommand())
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

        public List<dynamic> Get(Guid tubeId)
        {
            var rows = new List<dynamic>();
       
            var conditions = new List<string>();
            //права и папки
            conditions.Add($"{Qs}id{Qe} = '{tubeId}'");

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
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        using (var command = new NpgsqlCommand())
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
        /// <summary>
        /// получает список строк по фильтрам
        /// {
        ///     text:"some text",
        ///     folder:"guid",
        ///     states:[{min,max}],
        ///     devices:[{id}],
        ///     order:[{column,dir}],
        ///     page:{offset,count},
        ///     isDeleted:true|false,
        ///     isDisabled:true|false
        /// }
        /// </summary>
        /// <returns></returns>
        public dynamic Get(dynamic filter, Guid userId)
        {
            var rows = new List<dynamic>();

            var dfilter = filter as IDictionary<string, object>;
            IEnumerable<Guid> ids = null;
            if (dfilter.ContainsKey("folderId"))
            {
                var folderId = Guid.Parse(filter.folderId.ToString());
                ids = StructureGraph.Instance.GetTubeIdsByFolder(userId, folderId);
            }
            else
            {
                ids = StructureGraph.Instance.GetTubeIds(userId);
            }

            if (dfilter.ContainsKey("ids") && (filter.ids is IEnumerable<object>))
            {
                ids = (filter.ids as IEnumerable<object>).Where(id => (id is string) || (id is Guid)).Select(id => (id is string) ? new Guid((string)id) : (Guid)id).Intersect(ids);
            }

            var conditions = new List<string>();
            //права и папки
            conditions.Add($"{Qs}id{Qe} in ({ string.Join(",", ids.Union(new Guid[] { Guid.NewGuid() }).Select(i => string.Format("'{0}'", i))) })");

            if (dfilter.ContainsKey("states"))
            {
                var statesConds = new List<string>();
                foreach (var state in filter.states)
                {
                    if ((state as IDictionary<string, object>).ContainsKey("all"))
                    {
                        statesConds.Clear();
                        break;
                    }
                    string stateCast = storageType == STORAGE_TYPE_MSSQL ? $"{Qs}state{Qe}" : $"cast ({Qs}state{Qe} as integer)";
                    statesConds.Add($"{stateCast} between {state.min} and {state.max}");
                }
                conditions.AddRange(statesConds);
            }

            if (dfilter.ContainsKey("devices"))
            {
                var deviceIds = new List<string>();
                foreach (var device in filter.devices)
                {
                    if ((device as IDictionary<string, object>).ContainsKey("all"))
                    {
                        deviceIds.Clear();
                        break;
                    }
                    deviceIds.Add(string.Format("'{0}'", device.id));
                }

                if (deviceIds.Any())
                {
                    conditions.Add($"{Qs}deviceId{Qe} in ({string.Join(",", deviceIds)})");
                }
            }

            if (dfilter.ContainsKey("isDeleted"))
            {
                conditions.Add($"{Qs}isDeleted{Qe} = '{filter.isDeleted}'");
            }
            else
            {
                conditions.Add($"({Qs}isDeleted{Qe} is null or {Qs}isDeleted{Qe} = \'{falseString}\')");
            }

            if (dfilter.ContainsKey("isDisabled"))
            {
                conditions.Add($"{Qs}isDisabled{Qe} = '{filter.isDisabled}'");
            }
            else
            {
                //conditions.Add(string.Format("([isDisabled] is null or [isDisabled] = \'{falseString}\')"));
            }

            if (dfilter.ContainsKey("text") && !string.IsNullOrWhiteSpace(filter.text))
            {
                string text = filter.text;
                conditions.Add(
                    string.Format("{0}",
                      string.Join(" and ", text.Split(' ')
                        .Select(t => string.Format("({0})", string.Join(" or ", columns.Where(c => c != "id").Select(c => $"{Qs}{c}{Qe} {likeString} '%{t}%'"))))
                      )
                    )
                );
            }

            var rowsQuery = new StringBuilder();
            var countQuery = new StringBuilder();
            rowsQuery.AppendLine(string.Format("select {0} from {1} ", string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}")), tableFull));
            rowsQuery.AppendLine(string.Format("where {0} ", string.Join(" and ", conditions)));

            countQuery.AppendLine(string.Format("select count(*) as cnt from {0} ", tableFull));
            countQuery.AppendLine(string.Format("where {0} ", string.Join(" and ", conditions)));

            if (dfilter.ContainsKey("order") && (filter.order as IEnumerable<dynamic>).Any())
            {
                rowsQuery.AppendLine(string.Format("order by {0} ", string.Join(",", (filter.order as IEnumerable<dynamic>).Select(d => string.Format("{0} {1}", d.column, d.dir)))));
            }
            else
            {
                rowsQuery.AppendLine($"order by {Qs}name{Qe},{Qs}pname{Qe} ");
            }


            int count = 0;

            do
            {
                if (dfilter.ContainsKey("page"))
                {
                    if (filter.page.count == 0) break;
                    rowsQuery.AppendLine(string.Format("offset {0} rows fetch next {1} rows only ", filter.page.offset, filter.page.count));
                }

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
                                command.CommandText = countQuery.ToString();
                                var result = command.ExecuteScalar();
                                if (result != DBNull.Value)
                                {
                                    count = (int)result;
                                }
                            }

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
                else if (storageType == STORAGE_TYPE_PG)
                {
                    using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                    {
                        connection.Open();
                        try
                        {
                            using (var command = new NpgsqlCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = countQuery.ToString();
                                var result = command.ExecuteScalar();
                                if (result != DBNull.Value)
                                {
                                    count = Convert.ToInt32(result);
                                }
                            }

                            using (var command = new NpgsqlCommand())
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
            }
            while (false);

            dynamic res = new ExpandoObject();
            res.rows = rows;
            res.count = count;
            return res;
        }

        #region FillParameter
        private void FillParameterMssql(SqlCommand command, dynamic fatRow)
        {
            var df = (fatRow as IDictionary<string, object>);

            command.Parameters["@phone"].Value = string.Join(";",
                    ((df.ContainsKey("CsdConnection") ? (fatRow.CsdConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { }).
                Union(df.ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { })));
            command.Parameters["@imei"].Value = string.Join(";", (df.ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { }).Union(df.ContainsKey("SimpleMatrixConnection") ? (fatRow.SimpleMatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { }));
            command.Parameters["@name"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("name") ? a.name : "") : new string[] { });
            command.Parameters["@pname"].Value = df.ContainsKey("name") ? fatRow.name : DBNull.Value;

            command.Parameters["@deviceId"].Value = string.Join(";", df.ContainsKey("Device") ? (fatRow.Device as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("id") ? a.id : "") : new string[] { });
            command.Parameters["@resource"].Value = df.ContainsKey("resource") ? df["resource"] : DBNull.Value;
            command.Parameters["@fulness"].Value = DBNull.Value;
            command.Parameters["@fulnessHour"].Value = DBNull.Value;


            command.Parameters["@number"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Where(a => (a as IDictionary<string, object>).ContainsKey("number")).Select(a => a.number) ?? new string[] { } : new string[] { });
            command.Parameters["@city"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("city") ? a.city : "") : new string[] { });
            command.Parameters["@device"].Value = string.Join(";", df.ContainsKey("Device") ? (fatRow.Device as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("name") ? a.name : "") : new string[] { });
            command.Parameters["@isDeleted"].Value = df.ContainsKey("isDeleted") ? fatRow.isDeleted : DBNull.Value;

            command.Parameters["@isDisabled"].Value = df.ContainsKey("isDisabled") ? fatRow.isDisabled : DBNull.Value;

            if (columns.Contains("class"))
            {
                command.Parameters["@class"].Value = df.ContainsKey("class") ? df["class"] : DBNull.Value;
            }

            command.Parameters["@comment"].Value = df.ContainsKey("comment") ? fatRow.comment : DBNull.Value;

            //disable reason
            command.Parameters["@state"].Value = DBNull.Value;
            command.Parameters["@description"].Value = DBNull.Value;
            command.Parameters["@abnormals"].Value = DBNull.Value;
            command.Parameters["@value"].Value = DBNull.Value; 
            command.Parameters["@valueUnitMeasurement"].Value = DBNull.Value;
            command.Parameters["@controllerData"].Value = DBNull.Value;
            command.Parameters["@coordinates"].Value = DBNull.Value;
            command.Parameters["@event"].Value = DBNull.Value;
            command.Parameters["@date"].Value = DBNull.Value;

            bool isDisabled = false;
            if (df.ContainsKey("isDisabled") && bool.TryParse(fatRow.isDisabled.ToString(), out isDisabled))
            {
                if (isDisabled)
                {
                    command.Parameters["@state"].Value = "666";
                    command.Parameters["@description"].Value = df.ContainsKey("reason") ? fatRow.reason : "";
                }
            }
        }
        private void FillParameterPg(NpgsqlCommand command, dynamic fatRow)
        {
            var df = (fatRow as IDictionary<string, object>);

            command.Parameters["@phone"].Value = string.Join(";",
                    ((df.ContainsKey("CsdConnection") ? (fatRow.CsdConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { }).
                Union(df.ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { })));
            command.Parameters["@imei"].Value = string.Join(";", (df.ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { }).Union(df.ContainsKey("SimpleMatrixConnection") ? (fatRow.SimpleMatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { }));
            command.Parameters["@name"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("name") ? a.name : "") : new string[] { });

            command.Parameters["@pname"].Value = df.ContainsKey("name") ? fatRow.name : DBNull.Value;
            command.Parameters["@deviceId"].Value = string.Join(";", df.ContainsKey("Device") ? (fatRow.Device as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("id") ? a.id : "") : new string[] { });
            command.Parameters["@resource"].Value = df.ContainsKey("resource") ? df["resource"] : DBNull.Value;
            command.Parameters["@fulness"].Value = DBNull.Value;
            command.Parameters["@fulnessHour"].Value = DBNull.Value;

            command.Parameters["@number"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Where(a => (a as IDictionary<string, object>).ContainsKey("number")).Select(a => a.number) ?? new string[] { } : new string[] { });
            command.Parameters["@city"].Value = string.Join(";", df.ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("city") ? a.city : "") : new string[] { });
            command.Parameters["@device"].Value = string.Join(";", df.ContainsKey("Device") ? (fatRow.Device as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("name") ? a.name : "") : new string[] { });
            command.Parameters["@isDeleted"].Value = df.ContainsKey("isDeleted") ? fatRow.isDeleted : DBNull.Value;

            command.Parameters["@isDisabled"].Value = df.ContainsKey("isDisabled") ? fatRow.isDisabled : DBNull.Value;

            if (columns.Contains("class"))
            {
                command.Parameters["@class"].Value = df.ContainsKey("class") ? df["class"] : DBNull.Value;
            }

            command.Parameters["@comment"].Value = df.ContainsKey("comment") ? fatRow.comment : DBNull.Value;

            //disable reason
            command.Parameters["@state"].Value = DBNull.Value;
            command.Parameters["@description"].Value = DBNull.Value;
            command.Parameters["@abnormals"].Value = DBNull.Value;
            command.Parameters["@value"].Value = DBNull.Value;
            command.Parameters["@valueUnitMeasurement"].Value = DBNull.Value;
            command.Parameters["@controllerData"].Value = DBNull.Value;
            command.Parameters["@coordinates"].Value = DBNull.Value;
            command.Parameters["@event"].Value = DBNull.Value;
            command.Parameters["@date"].Value = DBNull.Value;
            bool isDisabled = false;
            if (df.ContainsKey("isDisabled") && bool.TryParse(fatRow.isDisabled.ToString(), out isDisabled))
            {
                if (isDisabled)
                {
                    command.Parameters["@state"].Value = "666";
                    command.Parameters["@description"].Value = df.ContainsKey("reason") ? fatRow.reason : "";
                }
            }
        }
        #endregion

        private int IndexByName(string name)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                if (columns[i] == name)
                    return i;
            }
            return 0;
        }

        public void UpdateRow(IEnumerable<Guid> tubeIds, Guid userId)
        {
            var fatRows = StructureGraph.Instance.GetRows(tubeIds, userId);

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        command.Connection = connection;
                        //command.CommandText = RowsCacheQueries.update;

                        var query = new StringBuilder();
                        query.AppendLine(string.Format("if not exists(select * from {0} where id=@id)", tableFull));
                        query.AppendLine(string.Format("insert into {0}({1})", tableFull, string.Join(",", string.Join(",", columns.Select(c => string.Format("[{0}]", c))))));
                        query.AppendLine(string.Format("values({0})", string.Join(",", string.Join(",", columns.Select(c => string.Format("@{0}", c))))));
                        query.AppendLine("else");
                        query.AppendLine(string.Format("update {0} set {1}", tableFull, string.Join(",", columns.Select(c => string.Format("[{0}]=isnull(@{0},[{0}])", c)))));
                        query.AppendLine("where id=@id");
                        command.CommandText = query.ToString();

                        command.Parameters.Add("@id", System.Data.SqlDbType.UniqueIdentifier);
                        columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add(string.Format("@{0}", c), System.Data.SqlDbType.NVarChar, 255));
                        columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[string.Format("@{0}", c)].Value = DBNull.Value);

                        connection.Open();
                        try
                        {
                            command.Prepare();

                            foreach (dynamic fatRow in fatRows)
                            {
                                command.Parameters["@id"].Value = Guid.Parse(fatRow.id.ToString());
                                //command.Parameters["@phone"].Value = string.Join(";", (((fatRow as IDictionary<string, object>).ContainsKey("CsdConnection") ? (fatRow.CsdConnection as IEnumerable<dynamic>).Select(c => c.phone) : new string[] { }).
                                //    Union((fatRow as IDictionary<string, object>).ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { })));
                                //command.Parameters["@imei"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { });
                                //command.Parameters["@name"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => a.name) : new string[] { });
                                //command.Parameters["@city"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("city") ? a.city : "") : new string[] { });
                                //command.Parameters["@device"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("Device") ? (fatRow.Device as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("name") ? a.name : "") : new string[] { });
                                //command.Parameters["@isDeleted"].Value = (fatRow as IDictionary<string, object>).ContainsKey("isDeleted") ? fatRow.isDeleted : DBNull.Value;
                                FillParameterMssql(command, fatRow);
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        command.Connection = connection;
                        connection.Open();
                        try
                        {
                            command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Uuid);
                            columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add($"@{c}", NpgsqlTypes.NpgsqlDbType.Varchar, 255));
                            foreach (dynamic fatRow in fatRows)
                            {
                                command.Parameters["@id"].Value = Guid.Parse(fatRow.id.ToString());
                                columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[$"@{c}"].Value = DBNull.Value);

                                command.CommandText = $"select exists(select 1 from {tableFull} where id=@id)";
                                var exists = (bool)command.ExecuteScalar();

                                var query = new StringBuilder();

                                if (!exists)
                                {
                                    query.AppendLine($"insert into {tableFull}({string.Join(",", string.Join(",", columns.Select(c => $"\"{c}\"")))})");
                                    query.AppendLine($"values({string.Join(",", string.Join(",", columns.Select(c => $"@{c}")))})");
                                }
                                else
                                {
                                    query.AppendLine($"update {tableFull} set {string.Join(",", columns.Select(c => $"\"{c}\"=coalesce(@{c},\"{c}\")"))}");
                                    query.AppendLine("where id=@id");
                                }

                                command.CommandText = query.ToString();

                                FillParameterPg(command, fatRow);
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        /// <summary>
        /// обновление кеша по графу
        /// </summary>
        /// <param name="relatedObjectId"></param>
        public void UpdateRow(dynamic relatedObject, Guid userId)
        {
            //строка представляет собой вот что:
            //{id,area,phones,imei,type...}

            Guid relatedObjectId = Guid.Parse(relatedObject.id.ToString());

            var tubeIds = StructureGraph.Instance.GetRelatedTubs(relatedObjectId);
            var fatRows = StructureGraph.Instance.GetRows(tubeIds, userId);

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new SqlCommand())
                    {
                        command.Connection = connection;
                        var query = new StringBuilder();
                        query.AppendLine(string.Format("if not exists(select * from {0} where id=@id)", tableFull));
                        query.AppendLine(string.Format("insert into {0}({1})", tableFull, string.Join(",", string.Join(",", columns.Select(c => string.Format("[{0}]", c))))));
                        query.AppendLine(string.Format("values({0})", string.Join(",", string.Join(",", columns.Select(c => string.Format("@{0}", c))))));
                        query.AppendLine("else");
                        query.AppendLine(string.Format("update {0} set {1}", tableFull, string.Join(",", columns.Select(c => string.Format("[{0}]=isnull(@{0},[{0}])", c)))));
                        query.AppendLine("where id=@id");
                        command.CommandText = query.ToString();
                        command.Parameters.Add("@id", System.Data.SqlDbType.UniqueIdentifier);
                        columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add(string.Format("@{0}", c), System.Data.SqlDbType.NVarChar, 255));
                        columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[string.Format("@{0}", c)].Value = DBNull.Value);

                        connection.Open();
                        try
                        {
                            command.Prepare();

                            foreach (dynamic fatRow in fatRows)
                            {
                                command.Parameters["@id"].Value = fatRow.id;
                                //command.Parameters["@phone"].Value = string.Join(";", (fatRow.CsdConnection as IEnumerable<dynamic>).Select(c => c.phone).Union((fatRow.MatrixConnection as IEnumerable<dynamic>).Select(c => c.phone)));
                                //command.Parameters["@imei"].Value = string.Join(";", (fatRow.Area as IEnumerable<dynamic>).Select(a => a.imei));
                                //command.Parameters["@name"].Value = string.Join(";", (fatRow.Area as IEnumerable<dynamic>).Select(a => a.name));
                                //command.Parameters["@city"].Value = string.Join(";", (fatRow.Area as IEnumerable<dynamic>).Select(a => a.city));
                                //command.Parameters["@isDeleted"].Value = (fatRow as IDictionary<string, object>).ContainsKey("isDeleted") ? fatRow.isDeleted : DBNull.Value;
                                FillParameterMssql(command, fatRow);
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        command.Connection = connection;
                        connection.Open();
                        try
                        {
                            command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Uuid);
                            columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add(string.Format("@{0}", c), NpgsqlTypes.NpgsqlDbType.Varchar, 255));
                            columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[string.Format("@{0}", c)].Value = DBNull.Value);

                            command.CommandText = $"select exists(select 1 from {tableFull} where id=@id)";
                            var exists = (bool)command.ExecuteScalar();

                            var query = new StringBuilder();

                            if (!exists)
                            {
                                query.AppendLine($"insert into {tableFull}({ string.Join(",", string.Join(",", columns.Select(c => $"\"{c}\"")))})");
                                query.AppendLine($"values({string.Join(",", string.Join(",", columns.Select(c => string.Format("@{0}", c))))})");
                            }
                            else
                            {
                                query.AppendLine($"update {tableFull} set {string.Join(",", columns.Select(c => $"\"{c}\"=coalesce(@{c},\"{c}\")"))}");
                                query.AppendLine("where id=@id");
                            }

                            command.CommandText = query.ToString();

                            command.Prepare();

                            foreach (dynamic fatRow in fatRows)
                            {
                                command.Parameters["@id"].Value = fatRow.id;
                                FillParameterPg(command, fatRow);
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        public void UpdateState(dynamic state, Guid tubeId, Guid userId)
        {
            //строка представляет собой вот что:
            //{id,area,phones,imei,type...}

            var commandText = $"update {tableFull} set {Qs}state{Qe}=@state, {Qs}description{Qe}=@description where id=@id;";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@state", state.code);
                            command.Parameters.AddWithValue("@description", state.description);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@state", state.code);
                            command.Parameters.AddWithValue("@description", state.description);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Функция обновления данных для освещения в RowCache
        /// </summary>
        /// <param name="controllerData">Строка с данными: cостояние контактора, выход контроллера, фотодатчик, способ управления, дата </param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateControllerData(string controllerData, Guid tubeId, Guid userId)
        {
            //строка представляет собой вот что:
            //{id,area,phones,imei,type...}

            var commandText = $"update {tableFull} set {Qs}controllerData{Qe}=@controllerData where id=@id;";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@controllerData", controllerData);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@controllerData", controllerData);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Функция обновления abnormals в RowCache 
        /// </summary>
        /// <param name="events">abnormals</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateEvents(int events, Guid tubeId, Guid userId)
        {
                
               //строка представляет собой вот что:
               //{id,area,phones,imei,type...}

               var commandText = $"update {tableFull} set {Qs}event{Qe}=@event where id=@id;";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@event", events.ToString());
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@event", events.ToString());
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Функция обновления данных для показаний счетчиков в RowCache
        /// </summary>
        /// <param name="value">показание</param>
        /// <param name="valueUnitMeasurement"> единица измерений</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateValue(double value, string valueUnitMeasurement, DateTime date, Guid tubeId, Guid userId)
        {
            //строка представляет собой вот что:
            //{id,area,phones,imei,type...}
            IDictionary<string, dynamic> tmp = new Dictionary<string, dynamic>();
            var commandText = $"update {tableFull} set {Qs}value{Qe}=@value, {Qs}valueUnitMeasurement{Qe}=@valueUnitMeasurement, {Qs}date{Qe}=@date where id=@id;";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@value", value);
                            command.Parameters.AddWithValue("@valueUnitMeasurement", valueUnitMeasurement);
                            command.Parameters.AddWithValue("@date", date);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@value", value);
                            command.Parameters.AddWithValue("@valueUnitMeasurement", valueUnitMeasurement);
                            command.Parameters.AddWithValue("@date", date);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Функция обновления данных для показаний счетчиков в RowCache
        /// </summary>
        /// <param name="data">даннные (название/значение)</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateData(IDictionary<string, dynamic> data, Guid tubeId, Guid userId)
        {
            var commandText = $"update {tableFull} set ";
            int countData = 0;
            foreach(var dataTmp in data)
            {
                commandText += (countData == 0) ? $"{Qs}{dataTmp.Key}{Qe}=@{dataTmp.Key}" : $", {Qs}{dataTmp.Key}{Qe}=@{dataTmp.Key}";
                countData++;
            }
            commandText += $" where id=@id;";
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
                            command.Parameters.AddWithValue("@id", tubeId);
                            foreach(var dataTmp in data)
                            {
                                command.Parameters.AddWithValue($"@{dataTmp.Key}", dataTmp.Value);
                            }
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            foreach (var dataTmp in data)
                            {
                                command.Parameters.AddWithValue($"@{dataTmp.Key}", dataTmp.Value);
                            }
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Обновление столбца abnormals по типу UpdateState(...)
        /// </summary>
        /// <param name="abnormals">dynamic abnormals={count: int [количество НС]}</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateAbnormals(dynamic abnormals, Guid tubeId, Guid userId)
        {
            var commandText = $"update {tableFull} set {Qs}abnormals{Qe}=@abnormals where id=@id;";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@abnormals", abnormals.count);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@abnormals", abnormals.count);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        /// <summary>
        /// Обновление столбца fulness по типу UpdateState(...)
        /// </summary>
        /// <param name="fulness">{dates: IEnumerable<DateTime> [даты за текущий месяц]}</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateFulness(dynamic fulness, Guid tubeId, Guid userId)
        {
            var days = (fulness.dates as IEnumerable<DateTime>).Distinct().Where(r => (fulness.start <= r && r < fulness.end)).OrderBy(r => r).Select(d => $"{(1 + (d - (DateTime)fulness.start).TotalDays):0}");
            var commandText = $"update {tableFull} set {Qs}fulness{Qe}=@fulness where id=@id;";
            var fullnessValue = $"{days.Count()};{fulness.day};{fulness.daysInPeriod};{string.Join(",", days)};{fulness.start:MM};{fulness.start:yyyy};{fulness.reportDay}";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@fulness", fullnessValue);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@fulness", fullnessValue);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        /// <summary>
        /// Обновление столбца fulnessHour по типу UpdateState(...)
        /// </summary>
        /// <param name="fulnessHour">{dates: IEnumerable<DateTime> [часы за текущий месяц]}</param>
        /// <param name="tubeId"></param>
        /// <param name="userId"></param>
        public void UpdateFulnessHour(dynamic fulnessHour, Guid tubeId, Guid userId)
        {
            var hours = (fulnessHour.dates as IEnumerable<DateTime>).Distinct().Where(r => (fulnessHour.start <= r && r < fulnessHour.end)).OrderBy(r => r).Select(d => $"{(1 + (d - (DateTime)fulnessHour.start).TotalHours):0}");
            var commandText = $"update {tableFull} set {Qs}fulnessHour{Qe}=@fulnessHour where id=@id;";
            var fullnessValue = $"{hours.Count()};{fulnessHour.currentHour};{fulnessHour.hoursInPeriod};{string.Join(",", hours)};{fulnessHour.start:MM};{fulnessHour.start:yyyy};{fulnessHour.reportDay}";

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
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@fulnessHour", fullnessValue);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    using (var command = new NpgsqlCommand())
                    {
                        connection.Open();
                        command.Connection = connection;
                        try
                        {
                            command.Parameters.AddWithValue("@id", tubeId);
                            command.Parameters.AddWithValue("@fulnessHour", fullnessValue);
                            command.CommandText = commandText;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            log.Error(string.Format("не удалось обновить кеш строк"), ex);
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"Неопределен тип хранилища {storageType}");
            }
        }

        internal void Update(Guid userId)
        {
            string commandCount = string.Format("select count(*) from {0} where id=@id;", tableFull);
            string commandIfCount0 = $"insert into {tableFull}({string.Join(",", string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}")))}) values({string.Join(",", string.Join(",", columns.Select(c => string.Format("@{0}", c))))});";
            string commandIfCountNot0 = $"update {tableFull} set {string.Join(",", columns.Select(c => $"{Qs}{c}{Qe}=@{c}"))} where id=$1;";

            var ids = StructureGraph.Instance.GetTubeIds(userId);

            if (storageType == STORAGE_TYPE_MSSQL)
            {
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        var partLen = 1000;
                        var emptyIds = new List<Guid>();
                        for (var part = 0; part < ids.Count(); part += partLen)
                        {
                            using (var command = new SqlCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = string.Format(
                                    @"create table tmp12(id uniqueidentifier);
                                    insert into tmp12(id) values {0};
                                    select t.id from [RowsCache] r right join tmp12 t on r.id = t.id where r.id is null;
                                    drop table [tmp12];", string.Join(",", ids.Skip(part).Take(partLen).Select(i => string.Format("('{0}')", i))));
                                var reader = command.ExecuteReader();
                                while (reader.Read())
                                {
                                    emptyIds.Add(reader.GetGuid(0));
                                }
                                reader.Close();
                            }
                        }

                        log.Debug(string.Format("новые коды получены {0}", emptyIds.Count));

                        //var fatRows = new List<dynamic>();
                        var size = 300;
                        for (var part = 0; part < emptyIds.Count; part += size)
                        {
                            var fatRows = new List<dynamic>();
                            fatRows.AddRange(StructureGraph.Instance.GetRows(emptyIds.Skip(part).Take(size), userId));
                            log.Debug($"порция жирных {part}-{(part + size - 1)} получена");

                            using (var command = new SqlCommand())
                            {
                                command.Connection = connection;
                                var query = new StringBuilder();
                                query.AppendLine("if not exists(select * from RowsCache where id=@id)");
                                query.AppendLine(string.Format("insert into RowsCache({0})", string.Join(",", string.Join(",", columns.Select(c => string.Format("[{0}]", c))))));
                                query.AppendLine(string.Format("values({0})", string.Join(",", string.Join(",", columns.Select(c => string.Format("@{0}", c))))));
                                query.AppendLine("else");
                                query.AppendLine(string.Format("update RowsCache set {0}", string.Join(",", columns.Select(c => string.Format("[{0}]=isnull(@{0},[{0}])", c)))));
                                query.AppendLine("where id=@id");
                                command.CommandText = query.ToString();
                                command.Parameters.Add("@id", System.Data.SqlDbType.UniqueIdentifier);

                                columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add(string.Format("@{0}", c), System.Data.SqlDbType.NVarChar, 255));
                                columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[string.Format("@{0}", c)].Value = DBNull.Value);

                                command.Prepare();

                                foreach (dynamic fatRow in fatRows)
                                {
                                    command.Parameters["@id"].Value = Guid.Parse(fatRow.id.ToString());
                                    //command.Parameters["@phone"].Value = string.Join(";", (((fatRow as IDictionary<string, object>).ContainsKey("CsdConnection") ? (fatRow.CsdConnection as IEnumerable<dynamic>).Select(c => c.phone) : new string[] { }).
                                    //    Union((fatRow as IDictionary<string, object>).ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(c => (c as IDictionary<string, object>).ContainsKey("phone") ? c.phone : "") : new string[] { })));
                                    //command.Parameters["@imei"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("MatrixConnection") ? (fatRow.MatrixConnection as IEnumerable<dynamic>).Select(a => a.imei) : new string[] { });
                                    //command.Parameters["@name"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => a.name) : new string[] { });
                                    //command.Parameters["@city"].Value = string.Join(";", (fatRow as IDictionary<string, object>).ContainsKey("Area") ? (fatRow.Area as IEnumerable<dynamic>).Select(a => (a as IDictionary<string, object>).ContainsKey("city") ? a.city : "") : new string[] { });
                                    //command.Parameters["@isDeleted"].Value = (fatRow as IDictionary<string, object>).ContainsKey("isDeleted") ? fatRow.isDeleted : DBNull.Value;
                                    FillParameterMssql(command, fatRow);
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            else if (storageType == STORAGE_TYPE_PG)
            {
                using (var connection = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["Context"].ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        var partLen = 1000;
                        var emptyIds = new List<Guid>();
                        for (var part = 0; part < ids.Count(); part += partLen)
                        {
                            using (var command = new NpgsqlCommand())
                            {
                                command.Connection = connection;
                                command.CommandText = string.Format(RowsCacheQueries.check, string.Join(",", ids.Skip(part).Take(partLen).Select(i => string.Format("('{0}')", i))));
                                var reader = command.ExecuteReader();
                                while (reader.Read())
                                {
                                    emptyIds.Add(reader.GetGuid(0));
                                }
                                reader.Close();
                            }
                        }

                        log.Debug(string.Format("новые коды получены {0}", emptyIds.Count));

                        //var fatRows = new List<dynamic>();
                        var size = 300;
                        for (var part = 0; part < emptyIds.Count; part += size)
                        {
                            var fatRows = new List<dynamic>();
                            fatRows.AddRange(StructureGraph.Instance.GetRows(emptyIds.Skip(part).Take(size), userId));
                            log.Debug($"порция жирных {part}-{(part + size - 1)} получена");

                            using (var command = new NpgsqlCommand())
                            {
                                command.Connection = connection;

                                foreach (dynamic fatRow in fatRows)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@id", Guid.Parse(fatRow.id.ToString()));
                                    command.CommandText = commandCount;
                                    var ifResult = Convert.ToInt32(command.ExecuteScalar());

                                    if (ifResult == 0)
                                    {
                                        command.CommandText = commandIfCount0;
                                    }
                                    else
                                    {
                                        command.CommandText = commandIfCountNot0;
                                    }


                                    columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters.Add(string.Format("@{0}", c), NpgsqlTypes.NpgsqlDbType.Varchar, 255));
                                    //columns.Where(c => c != "id").ToList().ForEach(c => command.Parameters[string.Format("@{0}", c)].Value = DBNull.Value);

                                    FillParameterPg(command, fatRow);
                                    command.ExecuteNonQuery();
                                }
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
        }
    }
}

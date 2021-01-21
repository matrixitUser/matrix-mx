using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Matrix.RecordsSaver
{
    public class MsSqlBulkInserter
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Insert(IEnumerable<dynamic> entities, string table,string[] fields, SqlConnection connection)
        {
            if (entities == null || !entities.Any()) return;
            logger.Debug(string.Format("сохраняются архивы {0} шт.", entities.Count()));
            
            try
            {
                SqlBulkCopy bulkCopy = new SqlBulkCopy(connection);

                bulkCopy.DestinationTableName =string.Format("[{0}]", table);

                var reader = new EntityDataReader(entities, fields);
                foreach (var property in reader.Keys)
                {                    
                    bulkCopy.ColumnMappings.Add(property, property);
                }

                bulkCopy.WriteToServer(reader);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ошибка при сохранении архивов");
                throw ex;
            }
        }
    }

    public class EntityDataReader : IDataReader
    {
        private IEnumerable<dynamic> data;

        private IEnumerator<dynamic> enumerator;

        public string[] Keys { get; private set; }

        public EntityDataReader(IEnumerable<dynamic> data,string[] fields)
        {
            this.data = data;
            enumerator = data.GetEnumerator();

            Keys = fields;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int FieldCount
        {
            get
            {
                return Keys.Count();
            }
        }

        public bool Read()
        {
            var canRead = enumerator.MoveNext();
            return canRead;
        }

        private readonly Regex guidRegex = new Regex(@"^[{(]?[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?$");

        public object GetValue(int i)
        {
            var item = enumerator.Current;
            if (item == null) return null;

            var ditem = (item as IDictionary<string, object>);

            if (!ditem.ContainsKey(Keys[i]))
            {
                return DBNull.Value;
            }
            var value = ditem[Keys[i]];
            if (value is double && double.IsNaN((double)value))
            {
                return double.MinValue;
            }
            if (value is float && float.IsNaN((float)value))
            {
                return float.MinValue;
            }
            if(guidRegex.IsMatch(value.ToString().ToUpper()))
            {
                return Guid.Parse(value.ToString());
            }

            return value;
        }

        public int GetOrdinal(string name)
        {
            for (var i = 0; i < Keys.Length; i++)
            {
                if (Keys[i] == name) return i;
            }
            return 0;
        }

        #region not realy used
        public void Close()
        {

        }

        public int Depth
        {
            get { throw new NotImplementedException(); }
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool IsClosed
        {
            get { throw new NotImplementedException(); }
        }

        public bool NextResult()
        {
            throw new NotImplementedException();
        }



        public int RecordsAffected
        {
            get { throw new NotImplementedException(); }
        }



        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }



        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public object this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public object this[int i]
        {
            get { throw new NotImplementedException(); }
        }
        #endregion
    }
}

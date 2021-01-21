using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Matrix.Domain.Entities;
using System.Runtime.Serialization;
using System.Reflection;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
	public class EntityDataReader<TEntity> : IDataReader
	{
		private IEnumerable<TEntity> data;

		public List<PropertyInfo> Properties { get; private set; }

		private IEnumerator<TEntity> enumerator;

		public EntityDataReader(IEnumerable<TEntity> data)
		{
			Properties = new List<PropertyInfo>();
			this.data = data;
			enumerator = data.GetEnumerator();

			var prototype = data.FirstOrDefault();

			if (prototype == null) return;

			var type = prototype.GetType();
			foreach (var propertyInfo in type.GetProperties())
			{
				if (!propertyInfo.CanWrite) continue;
				var attributes = propertyInfo.GetCustomAttributes(true);
				bool isIgnore = false;
				foreach (var attribute in attributes)
				{
					if (attribute is IgnoreDataMemberAttribute)
					{
						isIgnore = true;
						break;
					}
				}

				if (isIgnore) continue;

				Properties.Add(propertyInfo);
			}
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public int FieldCount
		{
			get
			{
				return Properties.Count;
			}
		}

		public bool Read()
		{
			var canRead = enumerator.MoveNext();
			return canRead;
		}

		public object GetValue(int i)
		{
			var item = enumerator.Current;
			if (item == null) return null;

			var value = Properties[i].GetValue(item, null);
			if (value is double && double.IsNaN((double)value))
			{
				return double.MinValue;
			}
			if (value is float && float.IsNaN((float)value))
			{
				return float.MinValue;
			}
			return value;
		}

		public int GetOrdinal(string name)
		{
			return Properties.FindIndex(p => p.Name == name);
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

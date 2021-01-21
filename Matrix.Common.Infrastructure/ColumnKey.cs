using Matrix.Common.Agreements;
using System.Collections.Generic;
using System.Linq;
using Matrix.Domain.Entities;
using System;

namespace Matrix.Common.Infrastructure
{
	/// <summary>
	/// столбец в таблице полученной поворотом данных
	/// 1. поворачивает "вертикальные" данные
	/// 2. преобразует все данные в КаноничЪский вид
	/// </summary>
	public class ColumnKey:IComparable
	{
		public string Name { get; private set; }
		
		public CalculationType CalculationType { get; private set; }
		public int Channel { get; private set; }
		public MeasuringUnitType MeasuringUnitType { get; private set; }

		public string GroupName { get; private set; }

		/// <summary>
		/// полное имя (с учетом способа расчета и канала
		/// </summary>
		public string FullName
		{
			get
			{
				if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(GroupName))
				{
					return string.Format("{0} [{1}]", Name, GroupName);
				}

				string ct = "";
				switch (CalculationType)
				{
					case Agreements.CalculationType.Total:
						ct = "нар";
						break;
					case Agreements.CalculationType.Average:
						ct = "ср";
						break;
					case Agreements.CalculationType.NotCalculated:
						break;
				}
				return string.Format("{0}[{1}]{2}", Name, Channel, ct);
			}
		}

		public ColumnKey(string name, CalculationType calculationType, int channel = 0, MeasuringUnitType measuringUnit = Agreements.MeasuringUnitType.Unknown)
		{
			CalculationType = calculationType;
			MeasuringUnitType = measuringUnit;
			Channel = channel;
			Name = name;
		}
		public ColumnKey(string name, string groupName, MeasuringUnitType measuringUnit = Agreements.MeasuringUnitType.Unknown)
		{
			MeasuringUnitType = measuringUnit;
			Name = name;
			GroupName = groupName;
		}

		[Obsolete("поддержка использования в отчетах")]
		public ColumnKey(CalculationType calculationType, MeasuringUnitType measuringUnit, string name, int channel = 0)
		{
			CalculationType = calculationType;
			MeasuringUnitType = measuringUnit;
			Channel = channel;
			Name = name;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ColumnKey)) return false;
			var other = obj as ColumnKey;
			if (ReferenceEquals(this, other)) return true;
			if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(GroupName))
			{
				return other.Name == Name && other.GroupName == GroupName;
			}
			return other.Name == Name && other.CalculationType == CalculationType && other.Channel == Channel;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = Name.GetHashCode();
				result = (result * 397) ^ CalculationType.GetHashCode();
				result = (result * 397) ^ Channel;
				return result;
			}
		}

		public int CompareTo(object obj)
		{
			return CompareToColumnKey(obj as ColumnKey);
		}
		private int CompareToColumnKey(ColumnKey columnKey)
		{
			if (columnKey == null) return 1;

			if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(GroupName))
			{
				if (!string.IsNullOrEmpty(columnKey.Name) && !string.IsNullOrEmpty(columnKey.GroupName))
				{
					var res = String.CompareOrdinal(GroupName, columnKey.GroupName);
					if (res != 0)
					{
						return res;
					}
					else
					{
						return String.CompareOrdinal(Name, columnKey.Name);
					}
				}
			}
			else
			{
				var res =  Channel.CompareTo(columnKey.Channel);
				if (res != 0)
				{
					return res;
				}
			}
			return String.CompareOrdinal(FullName, columnKey.FullName);
		}

		public override string ToString()
		{
			return FullName;
		}
	}
}

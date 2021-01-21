using System.Collections.Generic;
using System.Linq;
using Matrix.Common.Agreements;

namespace Matrix.Poll.Driver.Scylar
{
	class MapCollection : Dictionary<CompositeKey, UglyParameter>
	{
		public UglyParameter this[DriverParameter parameter, int index, int unit = 0]
		{
			get
			{
				foreach (var pair in this)
				{
					if (pair.Key.Tariff != index) continue;

					if (pair.Key.Unit != unit) continue;

					if (pair.Key.DriverParameters.Contains(parameter))
						return pair.Value;
				}
				return UglyParameter.Unknown;
			}
		}
	}

	class UglyParameter
	{
		public string ParameterType { get; set; }
		public CalculationType CalculationType { get; set; }
		public int Channel { get; set; }

		private static UglyParameter unknown;
		public static UglyParameter Unknown
		{
			get
			{
				return unknown ?? (unknown = new UglyParameter
												 {
													 ParameterType = "Unknown",
													 Channel = 0,
													 CalculationType = CalculationType.NotCalculated
												 });
			}
		}

		//public static bool operator ==(UglyParameter left, UglyParameter right)
		//{

		//    return left.Equals(right);
		//}

		//public static bool operator !=(UglyParameter left, UglyParameter right)
		//{
		//    if (null == left) return false;
		//    return !left.Equals(right);
		//}

		public override bool Equals(object obj)
		{
			var other = obj as UglyParameter;
			if (other == null) return false;

			return ParameterType == other.ParameterType &&
				CalculationType == other.CalculationType &&
				Channel == other.Channel;
		}

		public override int GetHashCode()
		{
			return ParameterType.GetHashCode() + CalculationType.GetHashCode() + Channel.GetHashCode();
		}
	}

	class CompositeKey
	{
		public IEnumerable<DriverParameter> DriverParameters { get; private set; }

		public int Tariff { get; private set; }

		public int Unit { get; private set; }


		public CompositeKey(IEnumerable<DriverParameter> driverParameters, int tariff, int unit = 0)
		{
			Tariff = tariff;
			DriverParameters = driverParameters;
			Unit = unit;
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
				return true;
			if (obj is CompositeKey)
				return Equals(obj as CompositeKey);
			return false;
		}

		public bool Equals(CompositeKey other)
		{
			if (other == null) return false;

			//return Item1 == other.Item1 && Item2 == other.Item2;
			if (Tariff == other.Tariff && DriverParameters == other.DriverParameters && Unit == other.Unit)
				return true;
			return false;
		}

		public override int GetHashCode()
		{
			return Tariff.GetHashCode() + DriverParameters.GetHashCode() + Unit.GetHashCode();
		}
	}
}

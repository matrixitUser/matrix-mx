using System;
using Matrix.Common.Agreements;

namespace Matrix.SurveyServer.Driver.Common
{
	/// <summary>
	/// архивная запись
	/// </summary>
	public class Data
	{
		public Data(string parameterName, MeasuringUnitType measuringUnit, DateTime date, double value)
		{
			ParameterName = parameterName;
			MeasuringUnit = measuringUnit;
			Date = date;
			Value = value;		
		}

		[Obsolete("не надо...")]
		public Data(string parameterName, MeasuringUnitType measuringUnit, DateTime date, double value, CalculationType calculationType = CalculationType.NotCalculated, int channel = 0)
		{
			ParameterName = parameterName;
			MeasuringUnit = measuringUnit;
			Date = date;
			Value = value;
			CalculationType = calculationType;
			Channel = channel;
		}

		public string ParameterName { get; set; }

		[Obsolete("не надо...")]
		public CalculationType CalculationType { get; set; }
		public DateTime Date { get; set; }
		public double Value { get; set; }
		public MeasuringUnitType MeasuringUnit { get; set; }

		[Obsolete("не надо...")]
		public int Channel { get; set; }

		public override string ToString()
		{
			return string.Format("{0:dd.MM.yyyy HH:mm:ss} {1} = {2} {3}",
				Date, ParameterName,Value, MeasuringUnit);
		}

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (!obj.GetType().IsAssignableFrom(typeof(Data))) return false;
			var other = obj as Data;
			if (other == null) return false;

			return this.Date == other.Date &&
				this.MeasuringUnit == other.MeasuringUnit &&
				this.ParameterName == other.ParameterName;
		}

		public override int GetHashCode()
		{
			return string.Format("{0}{1}{2}", Date, MeasuringUnit, ParameterName).GetHashCode();
		}
	}
}

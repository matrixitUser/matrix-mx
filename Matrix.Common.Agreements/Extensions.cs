using System;
using System.Linq;
using System.Reflection;

namespace Matrix.Common.Agreements
{
	public static class Extensions
	{
		//public static string GetString(this ParameterType value)
		//{
		//    string result = "unknown";

		//    var type = value.GetType();
		//    FieldInfo fieldInfo = type.GetField(value.ToString());
		//    if (fieldInfo != null)
		//    {
		//        var attributes = fieldInfo.GetCustomAttributes(typeof (ParameterAttribute), false);

		//        if (attributes.Any())
		//        {
		//            var attribute = (ParameterAttribute) attributes.First();
		//            result = attribute.Value;
		//        }
		//    }

		//    return result;
		//}

		//public static ParameterType OrFromString(this ParameterType obj, string value)
		//{
		//    var parameterType = ParameterType.Unknown;
		//    if (Enum.TryParse<ParameterType>(value, out parameterType))
		//    {
		//        return parameterType;
		//    }

		//    return obj;
		//}

        public static CalculationType OrFromString(this CalculationType obj, string value)
        {
            CalculationType parameterType;
            if (Enum.TryParse<CalculationType>(value, out parameterType))
            {
                return parameterType;
            }

            return obj;
        }

		public static string GetString(this MeasuringUnitType value)
		{
            if(value == MeasuringUnitType.Unknown) return string.Empty; 

            string result = "unknown";

            var type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            if (fieldInfo != null)
            {
                var attributes = fieldInfo.GetCustomAttributes(typeof(ParameterAttribute), false);

                if (attributes.Any())
                {
                    var attribute = (ParameterAttribute)attributes.First();
                    result = attribute.Value;
                }
            }

            return result;
		}

		public static MeasuringUnitType OrFromString(this MeasuringUnitType obj, string value)
		{
			var measuringUnitType = MeasuringUnitType.Unknown;
			if (Enum.TryParse<MeasuringUnitType>(value, out measuringUnitType))
			{
				return measuringUnitType;
			}

			return obj;
		}
	}
}

namespace Matrix.Common.Agreements
{
    public static class CommonHelper
    {
		//public static string GetDisplayName(ParameterType parameterType, int channel, CalculationType calculationType)
		//{
		//    string result = parameterType.GetString();
		//    if (calculationType == CalculationType.Total)
		//    {
		//        result = string.Format("{0} {1}", result, "накопл.");
		//    }
		//    if (channel > 0)
		//    {
		//        result = string.Format("{0}{1}", result, channel.ToString());
		//    }
		//    return result;
		//}
        public static string GetDisplayName(MeasuringUnitType unit)
        {
            return unit.GetString();
        }
    }
}

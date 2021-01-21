using Matrix.Common.Agreements;
namespace Matrix.SurveyServer.Driver.Common
{
	public class Constant
	{
		public Constant(ConstantType constantType, string value, bool isComposition = false)
		{
			Name = constantType.ToString();
			Value = value;
			IsComposition = isComposition;
		}

		public Constant(string name, string value, bool isComposition = false)
		{
			Name = name;
			Value = value;
			IsComposition = isComposition;
		}

		public string Name { get; set; }
		public string Value { get; set; }

		public bool IsComposition { get; set; }

		public override string ToString()
		{
			return string.Format("{0}={1}", Name, Value);
		}
	}
}

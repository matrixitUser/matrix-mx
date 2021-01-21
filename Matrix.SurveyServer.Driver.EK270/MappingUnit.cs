//using Matrix.Common.Agreements;

using System.Collections.Generic;
namespace Matrix.SurveyServer.Driver.EK270
{
	class MappingUnit
	{
		public string Address { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public string Parameter { get; set; }
        public string MeasureUnit { get; set; }
		public bool IsComposition { get; set; }
		public int Channel { get; set; }
        public List<DevType> Types { get; set; }

		public MappingUnit()
		{
			IsComposition = false;
			Channel = 1;
            Types = new List<DevType>();
		}
	}
}

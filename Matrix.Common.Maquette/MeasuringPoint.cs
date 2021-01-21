using System.Collections.Generic;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Содержит сведения о точке измерения/совокупности "малых" точек измерения 
	/// и результатах измерения по ней
	/// </summary>
	public class MeasuringPoint
	{
		#region Properties

		/// <summary>
		/// Содержит уникальный код, присвоенный КО данной точке измерения/совокупности «малых» точек измерения
		/// </summary>
		[XmlAttribute(AttributeName = "code")]
		public string Code { get; set; }

		/// <summary>
		/// Наименование данной точки измерения/совокупности "малых" точек измерения. 
		/// Длина наименования до 250 символов. 
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		/// <summary>
		/// Содержит информацию о результатах измерений по точкам измерений
		/// </summary>
		[XmlElement(ElementName = "measuringchannel")]
		public List<MeasuringChannel> MeasuringChannels { get; set; }

		#endregion

		#region Constructors

		public MeasuringPoint()
		{
			Code = string.Empty;
			Name = string.Empty;
			MeasuringChannels = new List<MeasuringChannel>();
		}

		#endregion
	}
}

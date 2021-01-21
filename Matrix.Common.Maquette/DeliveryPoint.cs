using System.Collections.Generic;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Точка поставки
	/// </summary>
	public class DeliveryPoint
	{
		/// <summary>
		/// Код точки измерений
		/// </summary>
		[XmlAttribute(AttributeName = "code")]
		public string Code { get; set; }

		/// <summary>
		/// Имя точки измерений
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Name { get; set; }

		/// <summary>
		/// Список каналов точки измерения
		/// </summary>
		[XmlElement(ElementName = "measuringchannel")]
		public List<MeasuringChannel> MeasuringChannels { get; set; }
	}
}

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Содержит информацию о результатах измерений субъекта ОРЭ
	/// </summary>    
	public class Area
	{
		#region Properties

		/// <summary>
		///Определяет в какой временной зоне ведется передача данных для данной area.
		///Атрибут timezone может принимать следующие значения:
		///1 – для первой и второй ценовых зон, для первой и третьей неценовых зон; 
		///3 – для второй неценовой зоны.
		///Отсутствие атрибута timezone эквивалентно записи timezone=1. 
		///Использование значений timezone отличных от 1 согласуется с  КО.
		/// </summary>
		[XmlAttribute(AttributeName = "timezone")]
		public int TimeZone { get; set; }

		/// <summary>
		///Является  обязательным  и  содержит  название  организации  Участника 
		///оптового рынка электроэнергии. Длина названия до 250 символов.
		/// </summary>
		[XmlElement(ElementName = "name")]
		public string Name { get; set; }

		/// <summary>
		///Является обязательным и содержит идентификатор, присваиваемый КО.
		/// </summary>
		[XmlElement(ElementName = "inn")]
		public string Inn { get; set; }

		/// <summary>
		///Содержит сведения о точке измерения/совокупности "малых" точек 
		///измерения и результатах измерения по ней
		/// </summary>
		[XmlElement(ElementName = "measuringpoint")]
		public List<MeasuringPoint> MeasuringPoints { get; set; }

		/// <summary>
		///Содержит сведения о группе точек поставки и результатах измерения в ней
		/// </summary>
		[XmlElement(ElementName = "deliverygroup")]
		public List<DeliveryGroup> DeliveryGroups { get; set; }

		/// <summary>
		///Содержит сведения о точке поставки/совокупности "малых" точек 
		///поставки и результатах измерения в ней
		/// </summary>
		[XmlElement(ElementName = "deliverypoint")]
		public List<DeliveryPoint> DeliveryPoints { get; set; }

		#endregion

		#region Constructors

		public Area()
		{
			TimeZone = 1;
			Name = string.Empty;
			Inn = string.Empty;
			MeasuringPoints = new List<MeasuringPoint>();
			DeliveryPoints = new List<DeliveryPoint>();
			DeliveryGroups = new List<DeliveryGroup>();
		}

		#endregion
	}
}

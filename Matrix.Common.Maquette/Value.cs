using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Содержимым является значение результата измерения.
	/// </summary>
	public class Value
	{
		private const string bypassSwitchStatus = "1114";

		#region Properties

		/// <summary>
		/// Значение (тавтология)
		/// </summary>
		[XmlText(Type = typeof(double))]
		public double Data { get; set; }

		/// <summary>
		/// Расширенный статус передаваемой информации
		/// (1114 - работал ОВ)
		/// </summary>
		[XmlAttribute(AttributeName = "extendedstatus")]
		public string ExtendedStatus { get; set; }

		/// <summary>
		/// Дополнительная информация (зависит от ExtendedStatus)
		/// </summary>
		[XmlAttribute(AttributeName = "param1")]
		public string Param1 { get; set; }

		/// <summary>
		/// Дополнительная информация (зависит от ExtendedStatus)
		/// </summary>
		[XmlAttribute(AttributeName = "param2")]
		public string Param2 { get; set; }

		/// <summary>
		/// Дополнительная информация (зависит от ExtendedStatus)
		/// </summary>
		[XmlAttribute(AttributeName = "param3")]
		public string Param3 { get; set; }

		/// <summary>
		/// Статус  передаваемой информации.  
		/// Статус 0 - передаваемая  информация  имеет  статус коммерческой. В этом случае атрибут статус может отсутствовать. 
		/// Статус 1 - некоммерческая информация.
		/// </summary>
		[XmlAttribute(AttributeName = "status")]
		public int Status { get; set; }

		/// <summary>
		/// Код замещаемой точки
		/// </summary>
		[XmlIgnore]
		public string BypassSwitchCode
		{
			get
			{
				string result = string.Empty;
				if (ExtendedStatus == bypassSwitchStatus)
				{
					result = Param1;
				}
				return result;
			}
			set
			{
				ExtendedStatus = "1114";
				Param1 = value;
			}
		}

		#endregion

		#region Constructors

		public Value()
		{
			Status = 0;
			Data = 0.0;
		}

		#endregion

		public override string ToString()
		{
			return string.Format("Значение {0}, статус {1}", Data, Status);
		}
	}
}

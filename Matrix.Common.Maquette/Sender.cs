using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Описывает  организацию, предоставляющую  информацию
	/// </summary>
	public class Sender
	{
		#region Properties

		/// <summary>
		/// Имя отправителя
		/// </summary>
		[XmlElement(ElementName = "name")]
		public string Name { get; set; }

		/// <summary>
		/// ИНН отправителя
		/// </summary>
		[XmlElement(ElementName = "inn")]
		public string Inn { get; set; }

		#endregion

		#region Constructors

		public Sender()
		{
			Name = string.Empty;
			Inn = string.Empty;
		}

		#endregion

		public override string ToString()
		{
			return Name;
		}
	}
}

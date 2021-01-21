using System.Collections.Generic;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Содержит информацию о результатах измерений по точкам измерений, 
	/// точкам поставки, 
	/// совокупности «малых» точек измерения, 
	/// совокупности «малых»  точек  поставки.
	/// </summary>
	public class MeasuringChannel
	{
		#region Properties

		/// <summary>
		/// Содержит код измерительного канала,
		/// присвоенный КО данному измерительному каналу
		/// В коде измерительного канала содержится информация о направлении передачи электроэнергии
		/// и типе измерительного канала;
		/// </summary>
		[XmlAttribute(AttributeName = "code")]
		public string Code { get; set; }

		/// <summary>
		/// Содержит описание измерительного канала
		/// </summary>
		[XmlAttribute(AttributeName = "desc")]
		public string Name { get; set; }

		/// <summary>
		/// Содержит номер версии алгоритма расчета  параметра точки поставки,
		/// является обязательным  при одновременной  передаче данных по точкам 
		/// поставки и сальдо перетоков по сечению в целом (по ГТП генерации) 
		/// по действующему и по новому Актам 
		/// либо при передаче данных только по Акту, имеющему номер версии больше 1. 
		/// Отсутствие атрибута algorithmversion эквивалентно записи algorithmversion=1
		/// </summary>
		[XmlAttribute(AttributeName = "algorithmversion")]
		public int AlgorithmVersion { get; set; }

		/// <summary>
		/// Содержит  временной  диапазон  измерения и  значения измерительных каналов 
		/// точки поставки и точки измерения
		/// </summary>
		[XmlElement(ElementName = "period")]
		public List<Period> Periods { get; set; }

		#endregion

		#region Constructors

		public MeasuringChannel()
		{
			Code = string.Empty;
			Name = string.Empty;
			AlgorithmVersion = 1;
			Periods = new List<Period>();
		}

		#endregion
	}
}

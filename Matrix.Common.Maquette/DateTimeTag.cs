using System;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
	/// <summary>
	/// Содержит информацию  о времени  создания документа
	/// </summary>
	public class DateTimeTag
	{
		private const string timestampFormat = "yyyyMMddHHmmss";
		private const string dayFormat = "yyyyMMdd";

		#region Properties

		/// <summary>
		/// Дата и время формирования данного документа в формате yyyyMMddHHmmss
		/// </summary>
		[XmlElement(ElementName = "timestamp")]
		public string Timestamp { get; set; }

		/// <summary>
		/// Дата и время формирования данного документа 
		/// </summary>
		[XmlIgnore]
		public DateTime TimestampAsDateTime
		{
			get
			{
				DateTime result = DateTime.Now;
				DateTime.TryParseExact(Timestamp, timestampFormat, null, System.Globalization.DateTimeStyles.None, out result);
				return result;
			}
			set
			{
				Timestamp = value.ToString(timestampFormat);
			}
		}

		/// <summary>
		/// Является обязательным и содержит 
		/// 0 - если используется зимнее время, 
		/// 1 - если используется летнее время,
		/// 2 - если документ сформирован для суток, в которые осуществлялся перевод часов с зимнего на летнее время и обратно
		/// с 2011 г. используется только 1
		/// </summary>
		[XmlElement(ElementName = "daylightsavingtime")]
		public int DayLightSavingTime { get; set; }

		/// <summary>
		/// является обязательным и  содержит дату, определяющую операционный 
		/// период, за который предоставляется информация, в формате yyyyMMdd
		/// </summary>
		[XmlElement(ElementName = "day")]
		public string Day { get; set; }

		/// <summary>
		/// Дата измерения
		/// </summary>
		[XmlIgnore]
		public DateTime DayAsDateTime
		{
			get
			{
				DateTime result = DateTime.Now;
				DateTime.TryParseExact(Day, dayFormat, null, System.Globalization.DateTimeStyles.None, out result);
				return result;
			}
			set
			{
				Day = value.Date.ToString(dayFormat);
			}
		}

		#endregion

		#region Constructors

		public DateTimeTag()
		{
			DayLightSavingTime = 1;
			TimestampAsDateTime = DateTime.Now;
			DayAsDateTime = DateTime.Now;
		}

		#endregion
	}
}

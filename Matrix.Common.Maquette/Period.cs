using System;
using System.Xml.Serialization;

namespace Matrix.Common.Maquette
{
    /// <summary>
    /// Содержит временной диапазон измерения и значения измерительных каналов 
    /// точки поставки и точки измерения
    /// </summary>
    public class Period
    {
        #region Properties

        /// <summary>
        /// Время начала периода (формат HHMM)
        /// </summary>
        [XmlAttribute(AttributeName = "start")]
        public String Start { get; set; }

        /// <summary>
        /// Смещение времени начала периода от начала дня в минутах
        /// </summary>
        [XmlIgnore]
        public int StartMinutesOffset
        {
            get
            {
                return MinuteOffsetCalculator(Start);
            }
        }

        /// <summary>
        /// Время окончания периода (формат HHMM)
        /// </summary>
        [XmlAttribute(AttributeName = "end")]
        public String End { get; set; }

        /// <summary>
        /// Смещение времени окончания периода от начала дня в минутах
        /// </summary>
        [XmlIgnore]
        public int EndMinutesOffset
        {
            get
            {
                return MinuteOffsetCalculator(End);
            }
        }

        /// <summary>
        /// Значение показания за данный период
        /// </summary>
        [XmlElement(ElementName = "value")]
        public Value Value { get; set; }

        #endregion

        /// <summary>
        /// Расчет смещения в минутах относительно начала дня
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        private int MinuteOffsetCalculator(string raw)
        {
            int result = 0;
            if (raw.Length == 4)
            {
                int hour = 0;
                int.TryParse(raw.Substring(0, 2), out hour);

                int minute = 0;
                int.TryParse(raw.Substring(2, 2), out minute);

                result = hour * 60 + minute;
            }
            return result;
        }

        #region Constructors

        public Period()
        {
            Value = new Value();
        }

        #endregion

        public static string NullSubstitutionPointCode
        {
            get
            {
                return "000000000000000";
            }
        }

		public override string ToString()
		{
			string result = string.Empty;

			result = Start;

			if (Value != null)
				result += " " + Value.ToString();

			return result;
		}
    }
}

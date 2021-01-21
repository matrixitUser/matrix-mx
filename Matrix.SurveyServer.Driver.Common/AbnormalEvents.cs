using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Common
{
    /// <summary>
    /// Нештатное событие
    /// </summary>
    public class AbnormalEvents
    {
        /// <summary>
        /// Время возникновения события
        /// </summary>
        public DateTime DateTime { get; set; }
        /// <summary>
        /// Длительность события
        /// </summary>
        public int Duration { get; set; }
        /// <summary>
        /// Описание события
        /// </summary>
        public string Description { get; set; }

		public override string ToString()
		{
			return string.Format("{0:dd.MM.yyyy HH:mm} {1} ({2} min)", DateTime, Description, Duration );
		}
    }
}

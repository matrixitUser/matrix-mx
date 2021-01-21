using System;

namespace Matrix.SurveyServer.Driver.CE102
{
    class Config
    {
        /// <summary>
        /// Положение точки
        /// </summary>
        public int Factor { get; set; }

        /// <summary>
        /// Интервал усреднения (в минутах)
        /// </summary>
        public int AveragingInterval { get; set; }
    }
}

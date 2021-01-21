using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    /// <summary>
    ///  коды сообщений протокола М4
    /// </summary>
    enum Codes
    {
        /// <summary>
        /// Ошибка
        /// </summary>
        Error = 0x21,
        /// <summary>
        /// Запрос сеанса связи
        /// </summary>
        Session = 0x3f,
        /// <summary>
        /// Запрос изменения скорости обмена
        /// </summary>
        Speed = 0x42,
        /// <summary>
        /// Запрос управления счетом
        /// </summary>
        Score = 0x4f,
        /// <summary>
        /// Запрос поиска архивной записи
        /// </summary>
        Archive = 0x61,
        /// <summary>
        /// Запрос чтения параметра  
        /// </summary>
        ParameterRead = 0x72,
        /// <summary>
        /// Запрос записи параметра  
        /// </summary>
        ParameterWrite = 0x77
    }
}

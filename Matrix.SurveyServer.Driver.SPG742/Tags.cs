using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    /// <summary>
    /// Набор тегов
    /// </summary>
    enum Tags: byte
    {
        /// <summary>
        /// Строка октетов
        /// </summary>
        OCTET_STRING = 0x04,
        /// <summary>
        /// Нет данных
        /// </summary>
        NULL = 0x05,
        /// <summary>
        /// Строка ASCII-символов
        /// </summary>
        ASCIIString = 0x16,
        /// <summary>
        /// Последовательность
        /// </summary>
        SEQUENCE = 0x30,
        /// <summary>
        /// Беззнаковое целое (unsigned int)
        /// </summary>
        IntU = 0x41,
        /// <summary>
        /// Целое со знаком (int)
        /// </summary>
        IntS = 0x42,
        /// <summary>
        /// Число с плавающей точкой IEEE 754 Float
        /// </summary>
        IEEFloat = 0x43,
        /// <summary>
        /// Параметр с комбинированным значением int+float
        /// </summary>
        MIXED = 0x44,
        /// <summary>
        /// Оперативный параметр настроечной БД
        /// </summary>
        Operative = 0x45,
        /// <summary>
        /// Подтверждение
        /// </summary>
        ACK = 0x46,
        /// <summary>
        /// Текущее время
        /// </summary>
        TIME = 0x47,
        /// <summary>
        /// Текущая календарная дата
        /// </summary>
        DATE = 0x48,
        /// <summary>
        /// Дата архивной записи
        /// </summary>
        ARCHDATE = 0x49,
        /// <summary>
        /// Номер параметра
        /// </summary>
        PNUM = 0x4a,
        /// <summary>
        /// Сборка флагов
        /// </summary>
        FLAGS = 0x4b,
        /// <summary>
        ///Ошибка 
        /// </summary>
        ERR = 0x55
    }
}

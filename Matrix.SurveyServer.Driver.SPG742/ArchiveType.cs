using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    enum ArchiveType : byte
    {
        /// <summary>
        /// часовые
        /// </summary>
        Hour = 0x00,
        /// <summary>
        /// суточные
        /// </summary>
        Day = 0x01,
        /// <summary>
        /// декадные
        /// </summary>
        Decade = 0x02,
        /// <summary>
        /// месячные
        /// </summary>
        Month = 0x03,
        /// <summary>
        /// изменения настроечной БД
        /// </summary>
        ChangDatabase = 0x04,
        /// <summary>
        /// перерывы электропитания
        /// </summary>
        PowerSupply = 0x05,
        /// <summary>
        /// события
        /// </summary>
        Event = 0x06,
        /// <summary>
        /// контрольные записи
        /// </summary>
        Audit = 0x07
    }

    public partial class Driver
    {
        private bool IsIntervalArchive(ArchiveType type)
        {
            return (type == ArchiveType.Hour
                || type == ArchiveType.Day
                || type == ArchiveType.Decade
                || type == ArchiveType.Month);
        }
    }
}

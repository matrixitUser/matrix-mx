using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
        /// <summary>
        /// Логирование процессов
        /// </summary>
#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

        /// <summary>
        /// запросы
        /// </summary>
        [Import("request")]
        private Action<byte[]> request;

        /// <summary>
        /// ответы
        /// </summary>
        [Import("response")]
        private Func<byte[]> response;

        /// <summary>
        /// передача коллекции записей 
        /// </summary>
        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        /// <summary>
        /// принудительное прерывание процесса опроса
        /// </summary>
        [Import("cancel")]
        private Func<bool> cancel;

        /// <summary>
        /// возвращает дату указанного типа архива, с которого необходимо начать опрос 
        /// </summary>
        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;


        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        /// <summary>
        /// архивы указанного типа за определенный интервал
        /// </summary>
        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        /// <summary>
        /// возвращает значение контрактного часа
        /// </summary>
        [Import("getContractHour")]
        private Func<int> getContractHour;

        /// <summary>
        /// задает значение контрактного часа
        /// </summary>
        [Import("setContractHour")]
        private Action<int> setContractHour;

        /// <summary>
        /// разница между временем на приборе и системным
        /// ('-' спешит, '+' отстает)
        /// </summary>
        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;
    }
}

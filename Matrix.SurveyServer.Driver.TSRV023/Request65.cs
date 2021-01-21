using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV023
{
    /// <summary>
    /// Функция 65
    /// </summary>
    abstract class Request65 : Request
    {
        public Request65(byte networkAddress, ArchiveType arrayNumber, short recordCount, RequestType requestType) :
            base(networkAddress, 65)
        {
            //номер массива
            Data.Add(Helper.GetHighByte((short)arrayNumber));
            Data.Add(Helper.GetLowByte((short)arrayNumber));

            //количество записей
            Data.Add(Helper.GetHighByte(recordCount));
            Data.Add(Helper.GetLowByte(recordCount));

            //тип запроса
            Data.Add((byte)requestType);
        }
    }
    enum RequestType : byte
    {
        ByIndex = 0,
        ByDate = 1
    }

    /// <summary>
    /// тип архива
    /// см. документация "структура архивов" стр. 1 (таблица)
    /// TODO дополнить всеми типами
    /// </summary>
    enum ArchiveType : short
    {
        Hourly = 1,
        Daily = 2,
        Monthly = 3,
        Log = 0
    }
}

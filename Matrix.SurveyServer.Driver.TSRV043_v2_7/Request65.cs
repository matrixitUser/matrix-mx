using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV043
{
	/// <summary>
	/// Функция 65
	/// </summary>
	abstract class Request65 : Request
	{
		public Request65(byte networkAddress, ArchiveType archiveIndex, short recordCount, RequestType requestType) :
			base(networkAddress, 65)
		{
			//индекс архива
			Data.Add(Helper.GetHighByte((short)archiveIndex));
			Data.Add(Helper.GetLowByte((short)archiveIndex));

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
		HourlyGrowing = 0,

		/// <summary>
		/// суточный нарастающим итогом
		/// </summary>
		DailyGrowing = 1,

		/// <summary>
		/// месячный нарастающим итогом
		/// </summary>
		MonthlyGrowing = 2
	}
}

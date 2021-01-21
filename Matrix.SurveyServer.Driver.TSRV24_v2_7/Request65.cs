using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TSRV24
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
		/// <summary>
		/// часовой (тс1)
		/// </summary>
		HourlySystem1 = 0,

		/// <summary>
		/// часовой (тс2)
		/// </summary>
		HourlySystem2 = 3,

		/// <summary>
		/// часовой (тс3)
		/// </summary>
		HourlySystem3 = 6,

		/// <summary>
		/// суточный (тс1)
		/// </summary>
		DailySystem1 = 1,

		/// <summary>
		/// суточный (тс2)
		/// </summary>
		DailySystem2 = 4,

		/// <summary>
		/// суточный (тс3)
		/// </summary>
		DailySystem3 = 7,

		MonthSystem1 = 2,
		MonthSystem2 = 5,
		MonthSystem3 = 8,

		HourlySumm = 9,

		/// <summary>
		/// часовой нарастающим итогом
		/// </summary>
		HourlyGrowing = 18,

		/// <summary>
		/// суточный нарастающим итогом
		/// </summary>
		DailyGrowing = 19,

		/// <summary>
		/// месячный нарастающим итогом
		/// </summary>
		MonthlyGrowing = 20
	}
}

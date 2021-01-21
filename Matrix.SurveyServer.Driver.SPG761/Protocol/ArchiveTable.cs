using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761.Protocol
{
	/// <summary>
	/// Тип архива
	/// </summary>
	public enum ArchiveTable : int
	{
		/// <summary>
		/// Минутный 
		/// </summary>
		Minutes = 65525,
		/// <summary>
		/// Часовой 
		/// </summary>
		Hours = 65530,
		/// <summary>
		/// (Полу)часовой, каналы 1 – 16 
		/// </summary>
		HalfHoursChannels1_16 = 65523,

		/// <summary>
		/// (Полу)часовой, каналы 17 – 32
		/// </summary> 
		HalfHoursChannels17_32 = 65522,

		/// <summary>
		/// (Полу)часовой, каналы 33 – 48 
		/// </summary>
		HalfHoursChannels33_48 = 65521,

		/// <summary>
		/// (Полу)часовой, каналы 49 – 64 
		/// </summary>
		HalfHoursChannels49_64 = 65520,

		/// <summary>
		/// (Полу)часовой, каналы 65 – 80 
		/// </summary>
		HalfHoursChannels65_80 = 65519,

		/// <summary>
		/// (Полу)часовой, каналы 81 – 96 
		/// </summary>
		HalfHoursChannels81_96 = 65518,

		/// <summary>
		/// (Полу)часовой, каналы 97 – 112 
		/// </summary>
		HalfHoursChannels97_112 = 65517,

		/// <summary>
		/// (Полу)часовой, каналы 113 – 128 
		/// </summary>
		HalfHoursChannels113_128 = 65516,

		/// <summary>
		/// (Полу)часовой, группы 1 – 16 
		/// </summary>
		HalfHoursGroups1_16 = 65499,

		/// <summary>
		/// (Полу)часовой, группы 17 – 32 
		/// </summary>
		HalfHoursGroups17_32 = 65498,

		/// <summary>
		/// Суточный 
		/// </summary>
		Daily = 65532,

		/// <summary>
		/// Суточный, каналы 1 – 16 
		/// </summary>
		DailyChannels1_16 = 65515,

		/// <summary>
		/// Суточный, каналы 17 – 32 
		/// </summary>
		DailyChannels17_32 = 65514,

		/// <summary>
		/// Суточный, каналы 33 – 48 
		/// </summary>
		DailyChannels33_48 = 65513,

		/// <summary>
		/// Суточный, каналы 49 – 64 
		/// </summary>
		DailyChannels49_64 = 65512,

		/// <summary>
		/// Суточный, каналы 65 – 80
		/// </summary>
		DailyChannels65_80 = 65511,

		/// <summary>
		/// Суточный, каналы 81 – 96 
		/// </summary>
		DailyChannels81_96 = 65510,

		/// <summary>
		/// Суточный, каналы 97 – 112 
		/// </summary>
		DailyChannels97_112 = 65509,

		/// <summary>
		/// Суточный, каналы 113 – 128 
		/// </summary>
		DailyChannels113_128 = 65508,

		/// <summary>
		/// Суточный, группы 1 – 16 
		/// </summary>
		DailyGroups1_16 = 65497,

		/// <summary>
		/// Суточный, группы 17 – 32 
		/// </summary>
		DailyGroups17_32 = 65496,

		/// <summary>
		/// Декадный 
		/// </summary>
		TenDayPeriod = 65528,

		/// <summary>
		/// Месячный 
		/// </summary>
		Monthly = 65534,

		/// <summary>
		/// Месячный, каналы 1 – 16 
		/// </summary>
		MonthlyChannels1_16 = 65507,

		/// <summary>
		/// Месячный, каналы 17 – 32 
		/// </summary>
		MonthlyChannels17_32 = 65506,

		/// <summary>
		/// Месячный, каналы 33 – 48 
		/// </summary>
		MonthlyChannels33_48 = 65505,

		/// <summary>
		/// Месячный, каналы 48 – 64 
		/// </summary>
		MonthlyChannels48_64 = 65504,

		/// <summary>
		/// Месячный, каналы 65 – 80 
		/// </summary>
		MonthlyChannels65_80 = 65503,

		/// <summary>
		/// Месячный, каналы 81 – 96 
		/// </summary>
		MonthlyChannels81_96 = 65502,

		/// <summary>
		/// Месячный, каналы 97 – 112 
		/// </summary>
		MonthlyChannels97_112 = 65502,

		/// <summary>
		/// Месячный, каналы 113 – 128 
		/// </summary>
		MonthlyChannels113_128 = 65500,

		/// <summary>
		/// Месячный, группы 1 – 16 
		/// </summary>
		MonthlyGroups1_16 = 65495,

		/// <summary>
		/// Месячный, группы 17 – 32
		/// </summary>
		MonthlyGroups17_32 = 65494
	}
}

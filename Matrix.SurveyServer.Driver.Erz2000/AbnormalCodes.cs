using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
	static class AbnormalCodes
	{
		private static Dictionary<int, string> description = new Dictionary<int, string>
		{
			{592,"GC6000 тайм-аут"},
			{593,"Старое показание счетч."},
			{594,"Новое показание счетч."},
			{595,"Луч 1 сбой"},
			{596,"Луч 2 сбой"},
			{597,"Луч 3 сбой"},
			{598,"Луч 4 сбой"},
			{599,"Луч 5 сбой"},
			{600,"Луч 6 сбой"},
			{601,"Луч 7 сбой"},
			{602,"Луч 8 сбой"},
			{603,"GC6000 !ошибка клибр."},
			{604,"T<>T-тандем"},
			{605,"P<>P-тандем"},
			{606,"VN<>VN-тандем"},
			{607,"VB<>VB-тандем"},
		};

		public static string GetAbnormal(int code)
		{
			if (description.ContainsKey(code))
			{
				return description[code];
			}
			return string.Format("неизвестная ошибка, код {0}", code);
		}
	}
}

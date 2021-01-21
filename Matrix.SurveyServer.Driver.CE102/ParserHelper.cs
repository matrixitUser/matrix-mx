using System;
using System.Collections.Generic;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.CE102
{
	static class ParserHelper
	{
		internal static int FromBcd(byte source)
		{
			var ten = source >> 4;
			var s = (byte)((byte)(source << 4) >> 4);
			return ten * 10 + s;
		}

		internal static byte ToBcd(int source)
		{
			var ten = (int)(source / 10);
			if (ten > 15) return 0;
			var s = source - ten * 10;
			return (byte)((ten << 4) + s);
		}
		internal static List<ValueUnit> Parse(List<byte> response, IEnumerable<MappingUnit> mappings)
		{
			var result = new List<ValueUnit>();
			int byteCounter = 0;

			foreach (var mapping in mappings)
			{
				if (byteCounter + mapping.Count > response.Count) break;

				switch (mapping.Type)
				{
					case MappingUnitType.integer:
						if (mapping.Count == 1)
							result.Add(new ValueUnit
							{
								MappingUnit = mapping,
								Value = (int)response[byteCounter],
							});
						if (mapping.Count == 3)
						{
							var vu = new ValueUnit
							{
								MappingUnit = mapping,
								Value = BitConverter.ToInt32(new byte[]
                                {
                                    response[byteCounter++],
                                    response[byteCounter++],
                                    response[byteCounter++],
                                    0, 
                                },
								0)
							};
							if ((int)vu.Value == 0xffffff) break;

							result.Add(vu);
						}
						if (mapping.Count == 4)
						{
							var vu = new ValueUnit
							{
								MappingUnit = mapping,
								Value = BitConverter.ToInt32(new byte[]
                                {
                                    response[byteCounter++],
                                    response[byteCounter++],
                                    response[byteCounter++],
                                    response[byteCounter++],
                                },
								0)
							};
							if ((int)vu.Value == 0xffffff) break;

							result.Add(vu);
						}
						break;
					case MappingUnitType.str:
						break;
					case MappingUnitType.date:
						if (mapping.Count == 3)
						{
							var day = FromBcd(response[byteCounter++]);
							var month = FromBcd(response[byteCounter++]);
							int year = FromBcd(response[byteCounter++]);
							if (year < 2000)
								year += 2000;
							if (month > 0 && day > 0)
							{
								result.Add(new ValueUnit
								{
									MappingUnit = mapping,
									Value = new DateTime(year, month, day)
								});
							}
						}
						break;
					case MappingUnitType.dateTime:
						if (mapping.Count == 7)
						{
							var sec = FromBcd(response[byteCounter++]);
							var min = FromBcd(response[byteCounter++]);
							var hour = FromBcd(response[byteCounter++]);
							var dayOfWeek = FromBcd(response[byteCounter++]);
							var day = FromBcd(response[byteCounter++]);
							var month = FromBcd(response[byteCounter++]);
							var year = FromBcd(response[byteCounter++]);
							if (year < 2000)
								year += 2000;

							if (month > 0 && day > 0)
							{
								var dt = new DateTime(year, month, day, hour, min, sec);
								result.Add(new ValueUnit
								{
									MappingUnit = mapping,
									Value = dt
								});
							}
						}
						break;
					default:
						break;
				}
			}

			return result;
		}

		internal static IEnumerable<Constant> ParseTarProg(List<byte> p, byte month, byte day, byte changePoint)
		{
			var res = new List<Constant>();

			if (p == null || p.Count < 2) return res;

			string dayName = string.Empty;
			if (day == 0)
				dayName = "рабочих";
			if (day == 1)
				dayName = "субботних";
			if (day == 2)
				dayName = "воскресных";
			if (day == 3)
				dayName = "особых";

			for (int i = 0; i + 1 < p.Count; i += 2)
			{
				var name = string.Format("Тарифная прогармма для {0} дней, месяц - {1}, точка смены архива - {2}, порядковый номер получаса - {3}",
				dayName, month, changePoint, p[i]);
				res.Add(new Constant(name, p[i + 1].ToString()));
			}
			return res;
		}

		internal static IEnumerable<Constant> ParseConstantConfig(List<byte> p, CounterType type)
		{
			switch (type)
			{
				case CounterType.S6:
					return ParseConfigS6R5(p);
				case CounterType.R5:
					return ParseConfigS6R5(p);
				case CounterType.S7:
					return ParseConfigS7R8R8Q(p);
				case CounterType.R8:
					return ParseConfigS7R8R8Q(p);
				case CounterType.R8Q:
					return ParseConfigS7R8R8Q(p);
				case CounterType.S31:
					return ParseConfigS31R33S7J5(p);
				case CounterType.R33:
					return ParseConfigS31R33S7J5(p);
				case CounterType.S7J5:
					return ParseConfigS31R33S7J5(p);
				case CounterType.S7J6:
					return ParseConfigS7J6(p);
				default:
					return null;
			}
		}
		internal static IEnumerable<Constant> ParseConfigS6R5(List<byte> p)
		{
			var res = new List<Constant>();

			//первый бит
			var bit = p[0];

			var b = (byte)(bit & 3);
			res.Add(new Constant("Положение точки", b.ToString()));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Заводской номер записан", b == 0 ? "Не записан" : "Записан"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Состояние пломбы", b == 0 ? "Не нарушена" : "Вскрыта"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Вывод сигнала контроля хода часов на телеметрию", b == 0 ? "Отключено" : "Включено"));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Заводской режим", b == 0 ? "Отключен" : "Включен"));

			bit = p[1];
			b = (byte)(bit & 3);
			string str = string.Empty;
			if (b == 0)
				str = "1 час";
			if (b == 1)
				str = "30 мин";
			if (b == 10)
				str = "15 мин";
			res.Add(new Constant("Интервал усреднения энергии", str));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Внешняя тарификация", b == 0 ? "По тарифной программе" : "Внешняя"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Автоматический переход на зимнее/летнее время", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Тарификация выходных и праздничных дней", b == 0 ? "Отключен" : "Включен"));

			bit = p[2];
			b = (byte)(bit & 7);
			res.Add(new Constant("Номер максимального действующего тарифа", (b + 1).ToString()));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Включение блокировки интерфейса, при трехкратном неверном вводе пароля", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 240) << 4);
			var speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена по интерфейсу", speed.ToString()));

			bit = p[3];
			res.Add(new Constant("Время индикации ", bit.ToString()));
			return res;
		}
		internal static IEnumerable<Constant> ParseConfigS7R8R8Q(List<byte> p)
		{
			var res = new List<Constant>();
			if (p.Count < 5) return res;
			
			var bit = p[0];

			var b = (byte)(bit & 3);
			res.Add(new Constant("Положение точки", b.ToString()));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Наличие реле 1", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Наличие реле 2", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Заводской номер записан", b == 0 ? "Не записан" : "Записан"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Состояние пломбы", b == 0 ? "Не нарушена" : "Вскрыта"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Вывод сигнала контроля хода часов на телеметрию", b == 0 ? "Отключено" : "Включено"));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Заводской режим", b == 0 ? "Отключен" : "Включен"));

			bit = p[1];
			b = (byte)(bit & 3);
			string str = string.Empty;
			if (b == 0)
				str = "1 час";
			if (b == 1)
				str = "30 мин";
			if (b == 10)
				str = "15 мин";
			res.Add(new Constant("Интервал усреднения энергии", str));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Внешняя тарификация", b == 0 ? "По тарифной программе" : "Внешняя"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Автоматический переход на зимнее/летнее время", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Тарификация выходных и праздничных дней", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Состояние реле 1", b.ToString()));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Состояние реле 2", b.ToString()));

			bit = p[2];
			b = (byte)(bit & 7);
			res.Add(new Constant("Номер максимального действующего тарифа", (b + 1).ToString()));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Включение блокировки интерфейса, при трехкратном неверном вводе пароля", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 240) << 4);
			var speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена по интерфейсу", speed.ToString()));

			bit = p[3];
			res.Add(new Constant("Время индикации ", bit.ToString()));

			bit = p[4];
			b = (byte)(bit & 3);
			res.Add(new Constant("Отключение первого реле", SwitchOffRele(b)));

			b = (byte)((bit & 12) << 2);
			res.Add(new Constant("Отключение второго реле", SwitchOffRele(b)));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Лимиты по энергии отключают нагрузку", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Лимиты по мощности отключают нагрузку", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Лимит по суммарной энергии отключает нагрузку ", b == 0 ? "Отключен" : "Включен"));
			return res;
		}
		internal static IEnumerable<Constant> ParseConfigS31R33S7J5(List<byte> p)
		{
			var res = new List<Constant>();
			var bit = p[0];

			var b = (byte)(bit & 3);
			res.Add(new Constant("Положение точки", b.ToString()));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Наличие реле 1", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Наличие подсветки ЖКИ", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Заводской номер записан", b == 0 ? "Не записан" : "Записан"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Состояние пломбы", b == 0 ? "Не нарушена" : "Вскрыта"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Вывод сигнала контроля хода часов на телеметрию", b == 0 ? "Отключено" : "Включено"));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Заводской режим", b == 0 ? "Отключен" : "Включен"));

			bit = p[1];
			b = (byte)(bit & 3);
			string str = string.Empty;
			if (b == 0)
				str = "1 час";
			if (b == 1)
				str = "30 мин";
			if (b == 10)
				str = "15 мин";
			res.Add(new Constant("Интервал усреднения энергии", str));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Внешняя тарификация", b == 0 ? "По тарифной программе" : "Внешняя"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Автоматический переход на зимнее/летнее время", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Тарификация выходных и праздничных дней", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Состояние реле 1", b.ToString()));

			bit = p[2];
			b = (byte)(bit & 7);
			res.Add(new Constant("Номер максимального действующего тарифа", (b + 1).ToString()));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Включение блокировки интерфейса, при трехкратном неверном вводе пароля", b == 0 ? "Отключен" : "Включен"));

			bit = p[3];
			res.Add(new Constant("Время индикации ", bit.ToString()));

			bit = p[4];
			b = (byte)(bit & 3);
			res.Add(new Constant("Отключение первого реле", SwitchOffRele(b)));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Лимиты по энергии отключают нагрузку", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Лимиты по мощности отключают нагрузку", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Лимит по суммарной энергии отключает нагрузку ", b == 0 ? "Отключен" : "Включен"));

			bit = p[5];
			b = (byte)(bit & 7);
			var speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена данными по дополнительному интерфейсу", speed.ToString()));

			b = (byte)((bit & 112) << 4);
			speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена данными по оптическому интерфейсу ", speed.ToString()));

			return res;
		}
		internal static IEnumerable<Constant> ParseConfigS7J6(List<byte> p)
		{
			var res = new List<Constant>();
			var bit = p[0];

			var b = (byte)(bit & 3);
			res.Add(new Constant("Положение точки", b.ToString()));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Наличие реле 1", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Наличие реле 2", b == 0 ? "Не установлео" : "Установлено"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Заводской номер записан", b == 0 ? "Не записан" : "Записан"));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Состояние пломбы", b == 0 ? "Не нарушена" : "Вскрыта"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Вывод сигнала контроля хода часов на телеметрию", b == 0 ? "Отключено" : "Включено"));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Заводской режим", b == 0 ? "Отключен" : "Включен"));

			bit = p[1];
			b = (byte)(bit & 3);
			string str = string.Empty;
			if (b == 0)
				str = "1 час";
			if (b == 1)
				str = "30 мин";
			if (b == 10)
				str = "15 мин";
			res.Add(new Constant("Интервал усреднения энергии", str));

			b = (byte)((bit & 4) << 2);
			res.Add(new Constant("Внешняя тарификация", b == 0 ? "По тарифной программе" : "Внешняя"));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Автоматический переход на зимнее/летнее время", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 16) << 4);
			res.Add(new Constant("Тарификация выходных и праздничных дней", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Состояние реле 1", b.ToString()));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Состояние реле 2", b.ToString()));

			bit = p[2];
			b = (byte)(bit & 7);
			res.Add(new Constant("Номер максимального действующего тарифа", (b + 1).ToString()));

			b = (byte)((bit & 8) << 3);
			res.Add(new Constant("Включение блокировки интерфейса, при трехкратном неверном вводе пароля", b == 0 ? "Отключен" : "Включен"));

			bit = p[3];
			res.Add(new Constant("Время индикации ", bit.ToString()));

			bit = p[4];
			b = (byte)(bit & 3);
			res.Add(new Constant("Отключение первого реле", SwitchOffRele(b)));

			b = (byte)((bit & 12) << 2);
			res.Add(new Constant("Отключение второго реле", SwitchOffRele(b)));

			b = (byte)((bit & 32) << 5);
			res.Add(new Constant("Лимиты по мощности отключают нагрузку", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 64) << 6);
			res.Add(new Constant("Лимит по суммарной энергии отключает нагрузку ", b == 0 ? "Отключен" : "Включен"));

			b = (byte)((bit & 128) << 7);
			res.Add(new Constant("Наличие подсветки ЖКИ", b == 0 ? "Не установлео" : "Установлено"));

			bit = p[5];
			b = (byte)(bit & 7);
			var speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена данными по дополнительному интерфейсу", speed.ToString()));

			b = (byte)((bit & 112) << 4);
			speed = 0;
			if (b == 0)
				speed = 2400;
			if (b == 1)
				speed = 4800;
			if (b == 2)
				speed = 9600;
			if (b == 3)
				speed = 14400;
			if (b == 4)
				speed = 19200;
			if (b == 5)
				speed = 38400;
			if (b == 6)
				speed = 57600;
			if (b == 7)
				speed = 115200;
			res.Add(new Constant("Скорость обмена данными по оптическому интерфейсу ", speed.ToString()));

			return res;
		}
		private static string SwitchOffRele(byte b)
		{
			if (b == 0)
				return "Не работает";
			if (b == 1)
				return "По команде интерфейса";
			if (b == 10)
				return "По превышению лимитов";
			if (b == 11)
				return "И интерфейс и превышение лимита";
			return string.Empty;
		}

        internal static Config ParseConfig(List<byte> p)
        {
            var result = new Config();
            var bit = p[0];
            var b = (byte)(bit & 3);

            if (b == 0) result.Factor = 1;
            else if (b == 1) result.Factor = 10;
            else if (b == 2) result.Factor = 100;
            else if (b == 3) result.Factor = 1000;

            bit = p[1];
            b = (byte)(bit & 3);
            if (b == 0)
                result.AveragingInterval = 60;
            if (b == 1)
                result.AveragingInterval = 30;
            if (b == 10)
                result.AveragingInterval = 15;

            return result;
        }
    }
	public enum CounterType
	{
		S6,
		R5,
		S7,
		R8,
		R8Q,
		S31,
		R33,
		S7J5,
		S7J6
	}
}

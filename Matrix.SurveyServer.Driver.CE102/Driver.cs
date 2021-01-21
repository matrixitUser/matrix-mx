using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Matrix.SurveyServer.Driver.CE102
{
	public class Driver : BaseDriver
	{
		private int _tryCount = 5;

		#region mappings
		private readonly List<MappingUnit> _version = new List<MappingUnit>
        {
            new MappingUnit{Description="Версия ядра"},
            new MappingUnit{Description="Тип прошивки"},
            new MappingUnit{Description="Версия прошивки"},
            new MappingUnit{Description="Дата создания прошивки", Count = 3, Type = MappingUnitType.date},
        };
		private readonly List<MappingUnit> _versionEx = new List<MappingUnit>
        {
            new MappingUnit{Description="Версия прошивки"},
            new MappingUnit{Description="Подверсия прошивки"},
        };
		private List<MappingUnit> _energyOfInterval = new List<MappingUnit>
        {
            new MappingUnit{Description="Первое значение энергии (при отсутствующем значении возвращается 0xFFFFFF)", Count= 3},
            new MappingUnit{Description="Второе значение энергии (при отсутствующем значении возвращается 0xFFFFFF)", Count= 3},
            new MappingUnit{Description="Третье значение энергии (при отсутствующем значении возвращается 0xFFFFFF)", Count= 3},
            new MappingUnit{Description="Четвертое значение энергии (при отсутствующем значении возвращается 0xFFFFFF)", Count= 3},
        };
		private readonly List<MappingUnit> _curentPower = new List<MappingUnit>
        {
            new MappingUnit{Description="Мгновенная мощность", Count= 3 },
        };
		private readonly List<MappingUnit> _halfHourEnergy = new List<MappingUnit>
        {
            new MappingUnit{Description="{Значение энергии за прошедший получасовой интервал", Count= 3 },
        };
		private readonly List<MappingUnit> _dateTime = new List<MappingUnit>
        {
            new MappingUnit{Description="Дата и время", Count= 7, Type=MappingUnitType.dateTime },
        };
		private readonly List<MappingUnit> _journal = new List<MappingUnit>
        {
            new MappingUnit{Description="Дата и время", Count= 7, Type=MappingUnitType.dateTime },
            new MappingUnit{Description="Код события", Count= 1 },
        };

		private readonly Dictionary<int, string> _journalEvents = new Dictionary<int, string>
        {
        {0xc0, "Самодиагностика прошла успешно" },
        {0xc1, "Сбой EEPROM"  }, 
        {0xc2, "Сбой RTC"}, 
        {0xc3, "Сбой I2C"  },
        {0xc4, "Ресурс батареи истекает"}, 
        {0x10, "Неверный ввод пароля" },
        {0x11 , "Блокировка интерфейса, пароль введен неверно более трех раз" },
        {0x12, "Вскрытие пломбы}"},
        {0x13, "Начала интервала, в который произошло вскрытие пломбы"},
        {0x14, "Конец интервала, в который произошло вскрытие пломбы"},
        {0x20, "Полная очистка EEPROM"}, 
        {0x21, "Обнуление тарифных накопителей"}, 
        {0x22, "Обнуление накоплений за интервалы, при переключении интервала сбора данных"},
        {0x23, "Сброс паролей"}, 
        {0x30, "Переход на зимнее время"}, 
        {0x31, "Переход на летнее время"},
        {0x40, "Отключение реле управления нагрузкой по интерфейсу"},
        {0x41,"Включение реле сигнализации по интерфейсу"},
        {0x42,"Включение дополнительного реле сигнализации по интерфейсу"},
        {0x43,"Разрешение включения реле управления нагрузкой"},
        {0x44,"Включение реле управления нагрузкой пользователем"},
        {0x45,"Отключение реле сигнализации"},
        {0x46,"Отключение дополнительного реле сигнализации"},
        {0x47,"Отключение реле управления нагрузкой по превышению лимита энергии по тарифу"},
        {0x48,"Отключение реле управления нагрузкой по превышению лимита мощности по тарифу"},
        {0x49,"Отключение реле управления нагрузкой по превышению лимита по суммарной энергии"},
        {0x4a,"Включение реле сигнализации по превышению лимита энергии по тарифу"},
        {0x4b,"Включение реле сигнализации по превышению лимита мощности по тарифу"},
        {0x4c,"Включение реле сигнализации по превышению лимита по суммарной энергии"},
        {0x4d,"Включение дополнительного реле сигнализации по превышению лимита энергии по тарифу"},
        {0x4e,"Включение дополнительного реле сигнализации по превышению лимита мощности по тарифу"},
        {0x4f,"Включение дополнительного реле сигнализации по превышению лимита по суммарной энергии"},
        {0x50,"Превышение лимита по энергии по тарифу"},
        {0x51,"Превышение лимита по мощности"},
        {0x52,"Превышение лимита по суммарной энергии"},
        {0xe0,"Нажата кнопка «ДСТП», отрыт доступ к опто-порту"},
        {0x80,"Изменение адреса счетчика"},
        {0x81,"Изменение заводского номера счетчика"},
        {0x82,"Изменение абонентского номера счетчика"},
        {0x83,"Изменение текущего тарифа по интерфейсу"},
        {0x84,"Запись тарифной программы"},
        {0x85,"Запись особых дат"},
        {0x86,"Запись ресурса батареи"},
        {0x87,"Изменение пароля 1 (чтение/запись)"},
        {0x88,"Изменение пароля 2 (чтение/запись)"},
        {0x89,"Изменение пароля 3 (чтение)"},
        {0x8a,"Запись лимита энергии"},
        {0x8b,"Запись лимита мощности"},
        {0x8c,"Запись лимита по суммарной энергии"},
        {0x8d,"Фиксация данных по широковещательной команде"},
        {0x8e,"Запись нового значения (DATA4) лимита по суммарной энергии. Формат: СС+ММ+ЧЧ+DATA4+0x8E"},
        {0xd0,"Отключение счетчика"}, 
        {0xd1,"Включение счетчика"}, 
        {0xb0,"Перезагрузка счетчика (сброс)"},
        };
		private readonly List<JournalUnit> _journalUnits = new List<JournalUnit>
        {
            new JournalUnit{Code = 0x0b, MaxEventNumber = 40},
            new JournalUnit{Code = 0x01, MaxEventNumber = 20},
            new JournalUnit{Code = 0x02, MaxEventNumber = 20},
            new JournalUnit{Code = 0x03, MaxEventNumber = 20},
            new JournalUnit{Code = 0x04, MaxEventNumber = 20},
            new JournalUnit{Code = 0x05, MaxEventNumber = 20},
            new JournalUnit{Code = 0x0d, MaxEventNumber = 40},
            new JournalUnit{Code = 0x07, MaxEventNumber = 20},
            new JournalUnit{Code = 0x0c, MaxEventNumber = 40},
            new JournalUnit{Code = 0x0a, MaxEventNumber = 20},
        };

		private readonly List<MappingUnit> _energyOfPeriod = new List<MappingUnit>
        {
            new MappingUnit {Count = 4, Type = MappingUnitType.integer}
        };
		#endregion

		public override SurveyResult Ping()
		{
			string number = string.Empty;
			int noResponse = 0;
			int notRecognize = 0;
			int i = 0;
			while (true)
			{
				var r = SendCommand(DriverHelper.ReadSerialNumber(NetworkAddress, 0, Password, i), 3000);
				if (r == null)
				{
					OnSendMessage("Ответа нет");
					noResponse++;
					if (noResponse + notRecognize > _tryCount)
						break;
					continue;
				}

				noResponse = 0;
				notRecognize = 0;

				var br = r.Any(n => n == 0);
				r.RemoveAll(n => n == 0);

				if (r.Any())
					number += Encoding.ASCII.GetString(r.ToArray().ToArray(), 0, r.Count);
				if (br) break;
				i++;
			}


			if (!string.IsNullOrEmpty(number))
				return new SurveyResult { State = SurveyResultState.Success };
			return new SurveyResult { State = notRecognize > 0 ? SurveyResultState.NotRecognized : SurveyResultState.NoResponse };
		}
		public override SurveyResultData ReadMonthlyArchive(IEnumerable<DateTime> dates)
		{
			var res = new List<Data>();

			OnSendMessage("Читаем конфигурацию счетчика");
			var r = SendCommand(DriverHelper.ReadConfig(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return new SurveyResultData { State = SurveyResultState.NoResponse };
			}
			Config config = ParserHelper.ParseConfig(r);

			var dateList = dates.OrderByDescending(d => d).ToList();

			foreach (var date in dateList)
			{
				Data vals = ReadMonth(date, config);

				if (vals != null)
					res.Add(vals);
			}
			return new SurveyResultData { Records = res, State = res.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse };
		}
		public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
		{
			var res = new List<Data>();

			OnSendMessage("Читаем конфигурацию счетчика");
			var r = SendCommand(DriverHelper.ReadConfig(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return new SurveyResultData { State = SurveyResultState.NoResponse };
			}
			Config config = ParserHelper.ParseConfig(r);

			var dateList = dates.OrderByDescending(d => d).ToList();

			foreach (var date in dateList)
			{
				Data vals = ReadDay(date, config);

				if (vals != null)
					res.Add(vals);
			}
			return new SurveyResultData { Records = res, State = res.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse };
		}
		private Data ReadDay(DateTime day, Config config)
		{
			if (config == null) return null;

			Thread.Sleep(1000);
			OnSendMessage("Читаем дату");
			//дату лучше опрашивать здесь, потому что если опрашивать несколько раз данные подряд, счетчик сходит с ума
			var dt = ReadDateTime();
			if (dt == null) return null;

			OnSendMessage("Считываем значение для " + day.ToString());
			var dayBack = (int)((dt.Value.Date - day.Date).TotalDays);

			if (dayBack > 45 || dayBack <= 0) return null;//счетчик хранит только послодние 45 дней

			Thread.Sleep(1000);

			var r = SendCommand(DriverHelper.ReadEnergyOfDay(NetworkAddress, 0, Password, day, dayBack));
			if (r == null) return null;

			List<ValueUnit> parsedVals = ParserHelper.Parse(r, _energyOfPeriod);

			if (!parsedVals.Any()) return null;

			double val = (int)parsedVals[0].Value;

			val = val / config.Factor;

			return new Data("Energy", MeasuringUnitType.kWtH, day, val)
			{
				Channel = 0,
				CalculationType = CalculationType.Total
			};
		}
		private Data ReadMonth(DateTime month, Config config)
		{
			if (config == null) return null;

			Thread.Sleep(1000);
			OnSendMessage("Читаем дату");
			//дату лучше опрашивать здесь, потому что если опрашивать несколько раз данные подряд, счетчик сходит с ума
			var dt = ReadDateTime();
			if (dt == null) return null;

			var hardWareDate = new DateTime(dt.Value.Year, dt.Value.Month, 1);
			var requestDate = new DateTime(month.Year, month.Month, 1);

			if (requestDate >= hardWareDate)
				return null;

			OnSendMessage("Считываем значение для " + month.ToString());

			var monthBack = DriverHelper.GetMonth(requestDate, hardWareDate);

			Thread.Sleep(1000);

			var r = SendCommand(DriverHelper.ReadEnergyOfMonth(NetworkAddress, 0, Password, month, monthBack));

			if (r == null) return null;

			List<ValueUnit> parsedVals = ParserHelper.Parse(r, _energyOfPeriod);
			if (!parsedVals.Any()) return null;

			double val = (int)parsedVals[0].Value;

			val = val / config.Factor;

			return new Data("Energy", MeasuringUnitType.kWtH, month, val)
			{
				Channel = 0,
				CalculationType = CalculationType.Total
			};
		}
		public override SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
		{
			var res = new List<AbnormalEvents>();
			foreach (var ju in _journalUnits)
			{
				for (int i = 0; i < ju.MaxEventNumber; i++)
				{
					var r = SendCommand(DriverHelper.ReadJournal(NetworkAddress, 0, Password, i, ju.Code));

					if (r == null)
						continue;

					var parsedVals = ParserHelper.Parse(r, _journal);
					if (parsedVals.Count != 2) break;

					var dt = (DateTime)parsedVals[0].Value;
					var code = (int)parsedVals[1].Value;

					if (_journalEvents.ContainsKey(code) && dt > dateStart && dt < dateEnd)
					{
						res.Add(new AbnormalEvents
						{
							DateTime = dt,
							Description = _journalEvents[code],
						});
					}
				}
			}

			return new SurveyResultAbnormalEvents { Records = res, State = res.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse };
		}
		public override SurveyResultData ReadCurrentValues()
		{
			var res = new List<Data>();

			OnSendMessage("Читаем конфигурацию счетчика");
			var r = SendCommand(DriverHelper.ReadConfig(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return new SurveyResultData { State = SurveyResultState.NoResponse };
			}

			Config config = ParserHelper.ParseConfig(r);

			//var pow = ReadCurrentPower(config);
			//if (pow != null)
			//    res.Add(pow);

			//res.AddRange(ReadRelayState());

			//var avEner = ReadHalHourEnergy(config);
			//if (avEner != null)
			//    res.Add(avEner);

			var avPow = ReadAveragePower(config);
			if (avPow != null)
				res.Add(avPow);
			return new SurveyResultData { Records = res, State = res.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse };
		}
		private Data ReadCurrentPower(Config config)
		{
			OnSendMessage("Чтение мгновенной мощности");
			var r = SendCommand(DriverHelper.ReadCurrentPower(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return null;
			}
			var rep = ParserHelper.Parse(r, _curentPower).ToList();

			double value = (int)rep[0].Value;
			if (config != null)
				value = value / config.Factor;

			if (rep.Any())
			{
				return new Data("InstantaneousPower", MeasuringUnitType.kWt, DateTime.Now, value)
						   {
							   CalculationType = CalculationType.NotCalculated
						   };
			}
			return null;
		}
		/// <summary>
		/// Чтение энергии, усредненной за послдений получасовой интервал
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		private Data ReadHalHourEnergy(Config config)
		{
			OnSendMessage("Чтение усредненной энергии");
			var r = SendCommand(DriverHelper.ReadLastHalfHourEnergy(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return null;
			}

			var rep = ParserHelper.Parse(r, _halfHourEnergy).ToList();
			if (rep.Any())
			{

				double value = (int)rep[0].Value;
				if (config != null)
					value = value / config.Factor;

				return new Data("Energy",
					MeasuringUnitType.kWtH, DateTime.Now, value) { CalculationType = CalculationType.NotCalculated };
			}
			return null;
		}
		private Data ReadAveragePower(Config config)
		{
			OnSendMessage("Чтение усредненной энергии");
			var r = SendCommand(DriverHelper.ReadCurrentPower(NetworkAddress, 0, Password), 3000);
			if (r == null)
			{
				return null;
			}

			var rep = ParserHelper.Parse(r, _curentPower).ToList();
			if (rep.Any())
			{

				double value = (int)rep[0].Value;
				if (config != null)
					value = value / config.Factor;

				return new Data("PowerAverage",
					MeasuringUnitType.kWt, DateTime.Now, value) { CalculationType = CalculationType.NotCalculated };
			}
			return null;
		}
		private IEnumerable<Data> ReadRelayState()
		{
			var res = new List<Data>();
			var r = SendCommand(DriverHelper.ReadRelayState(NetworkAddress, 0, Password), 3000);
			if (r == null) return res;
			var parsed = r;
			if (parsed.Any())
			{
				res.Add(new Data("положение реле №1", //положение реле №1 (1 –включено, 0 – выключено)
					MeasuringUnitType.Unknown,
					DateTime.Now,
					parsed[0] & 1
					));

				res.Add(new Data("положение реле №2", //положение реле №2 (1 –включено, 0 – выключено)
					MeasuringUnitType.Unknown,
					DateTime.Now,
					parsed[0] & 2
					));
			}
			return res;
		}
		private DateTime? ReadDateTime()
		{
			var r = SendCommand(DriverHelper.ReadDateTime(NetworkAddress, 0, Password), 3000);
			if (r == null) return null;

			var rep = ParserHelper.Parse(r, _dateTime).ToList();

			if (rep.Any() && rep[0].Value is DateTime)
				return (DateTime)rep[0].Value;

			return null;
		}
		public override SurveyResultConstant ReadConstants()
		{
			var res = new List<Constant>();
			OnSendMessage("Читаем версию");
			var r = SendCommand(DriverHelper.ReadVersion(NetworkAddress, 0, Password), 3000);
			if (r != null)
			{
				res.AddRange(ParserHelper.Parse(r, _version)
								 .Select(v => new Constant(v.MappingUnit.Description, v.Value.ToString())));
			}


			OnSendMessage("Читаем расширенную версию");
			r = SendCommand(DriverHelper.ReadVersionEx(NetworkAddress, 0, Password), 3000);
			if (r != null)
			{
				res.AddRange(ParserHelper.Parse(r, _versionEx)
								 .Select(v => new Constant(v.MappingUnit.Description, v.Value.ToString())));
			}

			OnSendMessage("Читаем конфигурацию");
			r = SendCommand(DriverHelper.ReadConfig(NetworkAddress, 0, Password), 3000);
			if (r != null)
			{
				res.AddRange(ParserHelper.ParseConstantConfig(r, CounterType.S6));
			}


			//OnSendMessage("Читаем конфигурацию");
			//r = SendMessage(DriverHelper.ReadConfig(NetworkAddress, 0, Password), 3000);
			//if (!DriverHelper.CheckCrc8(r))
			//    OnSendMessage("Не сошлась контрольная сумма");
			//else
			//    res.AddRange(ParserHelper.Parse(ParserHelper.ParseResponse(r), _version)
			//    .Select(v => new Constant(Imei, NetworkAddress, Port, v.MappingUnit.Description, v.Value.ToString())));



			//OnSendMessage("Читаем серийный номер");
			//var con = ReadSerialNumber();
			//if (con != null)
			//    res.Add(con);

			//OnSendMessage("Читаем абонентский номер");
			//con = ReadSubscriberNumber();
			//if (con != null)
			//    res.Add(con);

			//OnSendMessage("Читаем текущий тариф");
			//res.AddRange(ReadCurTarrif());

			//res.AddRange(ReadTarProg());

			return new SurveyResultConstant { Records = res, State = res.Any() ? SurveyResultState.Success : SurveyResultState.NoResponse };
		}

		private IEnumerable<Constant> ReadTarProg()
		{
			var res = new List<Constant>();
			for (byte month = 1; month < 13; month++)
			{
				for (byte day = 0; day < 4; day++)
				{
					for (byte changePoint = 0; changePoint < 16; changePoint++)
					{
						byte type = 0;
						if (day != 3)
						{
							type = (byte)((byte)(day << 4) + month);
						}
						var r = SendCommand(DriverHelper.ReadTarProg(NetworkAddress, 0, Password, type, changePoint, 6));
						if (r != null)
							res.AddRange(ParserHelper.ParseTarProg(r,
								month,
								day,
								changePoint));
					}
				}
			}
			return res;
		}
		private Constant ReadSerialNumber()
		{
			string number = string.Empty;
			int i = 0;
			OnSendMessage("Читаем серийный номер");
			while (true)
			{
				var r = SendCommand(DriverHelper.ReadSerialNumber(NetworkAddress, 0, Password, i), 3000);
				if (r != null)
				{
					var br = r.Any(n => n == 0);
					r.RemoveAll(n => n == 0);

					if (r.Any())
						number += Encoding.ASCII.GetString(r.ToArray().ToArray(), 0, r.Count);
					if (br) break;
				}
				i++;
				if (i > _tryCount) break;
			}
			if (!string.IsNullOrEmpty(number))
				return new Constant("Серийный номер", number);

			else return null;
		}
		private Constant ReadSubscriberNumber()
		{
			var number = string.Empty;
			var i = 0;
			OnSendMessage("Читаем абонентский номер");
			while (true)
			{
				var r = SendCommand(DriverHelper.ReadSubscriberNumber(NetworkAddress, 0, Password, i), 3000);
				if (r != null)
				{
					var br = r.Any(n => n == 0);
					r.RemoveAll(n => n == 0);

					if (r.Any())
						number += Encoding.ASCII.GetString(r.ToArray().ToArray(), 0, r.Count);
					if (br) break;
				}
				i++;
			}
			if (!string.IsNullOrEmpty(number))
				return new Constant("Абонентский номер", number);

			return null;
		}
		private IEnumerable<Constant> ReadCurTarrif()
		{
			var res = new List<Constant>();
			for (int i = 0; i < 8; i++)
			{
				var r = SendCommand(DriverHelper.ReadCurTarrif(NetworkAddress, 0, Password, i), 3000);
				if (r == null) continue;

				if (r.Count == 1)
					res.Add(new Constant("Тариф №" + (i + 1).ToString(), r[0].ToString()));
			}
			return res;
		}
		private List<byte> SendCommand(byte[] message, int wait = 1000)
		{
            OnSendMessage("Запрос на счетчик: " + string.Join(" ", (message.Select(b => b.ToString("X2")))) + " " + Encoding.Default.GetString(message));
			for (int i = 1; i < 4; i++)
			{
				OnSendMessage("Попытка №" + i.ToString());
				//Core.SendData(message, 1);
				RaiseDataSended(message);
				Wait(wait);
				if (isDataReceived)
				{
                    OnSendMessage("Ответ от счетчика: " + string.Join(" ", (receivedBuffer.Select(b => b.ToString("X2")))) + " " + Encoding.Default.GetString(receivedBuffer));
					isDataReceived = false;
					string errorMessage;
					var result = DriverHelper.CheckPacket(receivedBuffer.ToList(), out errorMessage);
					if (result == null) OnSendMessage(errorMessage);
					else
					{
						return result;
					}
				}
			}
			return null;
		}
	}
}

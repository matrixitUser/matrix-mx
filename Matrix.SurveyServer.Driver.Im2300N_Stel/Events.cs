using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        const int EVENT_BLOCKS = 1;

        private dynamic GetEventsA(byte na)
        {
            return ParseEventsA(GetBlocks(na, 0xcf, 1, 1, 1501));
        }

        private dynamic ParseEventsA(byte[] bytes)
        {
            dynamic events = new ExpandoObject();
            events.success = true;

            if (bytes == null || bytes.Length < 1501)
            {
                events.success = false;
                events.error = "не хватает данных";
                return events;
            }

            events.records = new List<dynamic>();

            for (var offset = 0; offset < 1051 - 5; offset += 5)
            {
                var seconds = BitConverter.ToInt32(bytes, offset);
                var date = new DateTime(2000, 01, 01).AddSeconds(seconds);
                var code = bytes[offset + 4];
                events.records.Add(MakeAbnormalRecord(GetEventName(code), 0, date));
            }

            return events;
        }

        private string GetEventName(byte code)
        {
            switch (code)
            {
                case 1: return "Запрос записи времени";
                case 2: return "Запись времени";
                case 3: return "Запись паспорта (коммерческая часть)";
                case 4: return "Запись паспорта (пользовательская часть)";
                case 5: return "Запись констант";
                case 6: return "Запись констант без смены КЗ";
                case 7: return "Запись блока описаний";
                case 8: return "Запись блока измерений";
                case 9: return "Запись параметров измерительных входов (датчиков)";
                case 10: return "Запись поправочных коэффициентов измерительных входов";
                case 11: return "Запись блока MicroLAN";
                case 12: return "Сброс аппаратного пароля";
                case 13: return "Запись аппаратного пароля";
                case 14: return "Запись нового адреса прибора в сети RS485";
                case 15: return "Запись поправочных коэффициентов выходов DAC";
                case 16: return "Установка скорости передачи";
                case 17: return "Запись новой версии ПО";
                case 18: return "Изменился номер внешнего измерительного блока";
                case 30: return "Сброс прибора (Очистка счетчиков)";
                case 31: return "Сброс прибора (Очистка полного архива)";
                case 32: return "Сброс прибора (Очистка посуточного архива)";
                case 33: return "Сброс прибора (Очистка помесячного архива)";
                case 34: return "Сброс прибора (Очистка журнала событий)";
                case 35: return "Сброс прибора (Очистка журнала ошибок конфигурации)";
                case 36: return "Сброс прибора (Очистка журнала ошибок измерений и вычислений)";
                case 37: return "Сброс прибора (Удаление блоков паспорта, констант и т.д.)";
                case 38: return "Завершение сброса прибора";
            }
            return string.Format("событие код {0}", code);
        }


        private dynamic GetEvents(byte na)
        {
            return ParseEvents(GetBlocks(na, 0x9f, 1, EVENT_BLOCKS, 704));
        }

        private dynamic ParseEvents(byte[] data)
        {
            dynamic events = new ExpandoObject();

            if (data == null || data.Length < 704 * EVENT_BLOCKS)
            {
                events.success = false;
                events.error = "недостаточно данных для разбора";
                return events;
            }

            //System.IO.File.WriteAllText(@"d:\im.txt", string.Join(",", data.Select(b => b.ToString("X2"))));

            events.records = new List<dynamic>();

            for (var p = 0; p < EVENT_BLOCKS; p++)
            {
                for (var i = 0; i < 100; i++)
                {
                    var sec = BinDecToInt(data[p * 704 + i * 7 + 6]);
                    var min = BinDecToInt(data[p * 704 + i * 7 + 5]);
                    var hour = BinDecToInt(data[p * 704 + i * 7 + 4]);
                    var day = BinDecToInt((byte)(data[p * 704 + i * 7 + 3] & 0x3f));

                    var curYear = DateTime.Today.Year;
                    var archiveRecordYearLeapOffset = ((data[p * 704 + i * 7 + 3] & 0xc0) >> 6);
                    var curYearLeapOffset = curYear % 4;
                    var year = curYear - curYearLeapOffset + archiveRecordYearLeapOffset - ((archiveRecordYearLeapOffset > curYearLeapOffset) ? 4 : 0);
                    var mon = BinDecToInt((byte)(data[p * 704 + i * 7 + 2] & 0x1f));

                    DateTime date;
                    try
                    {
                        date = new DateTime(year, mon, day, hour, min, sec);
                    }
                    catch (Exception ex)
                    {
                        break;
                    }

                    var code = BitConverter.ToInt16(new byte[] { data[i * 7 + 1], data[i * 7 + 0] }, 0);
                    var name = GetEventName(code);
                    if (!string.IsNullOrEmpty(name)) events.records.Add(MakeAbnormalRecord(name, 0, date));
                }
            }

            events.success = true;
            return events;
        }

        private string GetEventName(short code)
        {
            switch (code)
            {
                case 1: return "Запрос записи времени";
                case 2: return "Запись времени";
                case 4: return "Запись паспорта";
                case 8: return "Запись установок пользователя";
                case 10: return "Запись констант";
                case 20: return "Запись констант без смены КЗ";
                case 40: return "Сброс";
                case 80: return "Вкл. режима повыш. точности";
                case 100: return "Выкл. режима повыш. точности";
                case 200: return "Запись аппаратной конфигурации";
                case 400: return "Запись/Сброс флага оплаты";
                default: return "";// string.Format("Не определено, код {0}", code);
            }
        }
    }
}

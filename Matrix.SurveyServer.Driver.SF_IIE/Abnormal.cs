using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SF_IIE
{
    public partial class Driver
    {
        private dynamic GetAbnormal(byte na, byte channel, byte number, DateTime start, DateTime end)
        {
            return ParseAbnormal(Send(MakeAbnormalRequest(na, channel, number, start, end)));
        }

        private dynamic ParseAbnormal(byte[] bytes)
        {
            dynamic day = ParseResponse(bytes);
            if (!day.success)
            {
                return day;
            }

            day.channel = day.body[0];
            day.recordCount = day.body[1];

            day.state = day.body[2];

            day.records = new List<dynamic>();

            for (var rec = 0; rec < day.recordCount; rec++)
            {
                var offset = rec * 24;
                var date = new DateTime(2000 + day.body[offset + 5], day.body[offset + 3], day.body[offset + 4], day.body[offset + 6], day.body[offset + 7], day.body[offset + 8]);

                day.records.Add(MakeAbnormalRecord(GetAbnormalName(day.body[offset + 9]), 0, date));
            }

            return day;
        }

        private string GetAbnormalName(byte code)
        {
            switch (code)
            {
                case 0: return "Снят отказ аналогового входа";
                case 1: return "Окончание градуировки аналогового входа";
                case 2: return "Снята отсечка по отсутствию расхода";
                case 3: return "Переход на показания датчика";
                case 4: return "Загружен отчет";
                case 5: return "Напряжение питания в норме";
                case 6: return "Датчик в пределах градуировки";
                case 7: return "Снят флаг рестарта Суперфлоу";
                case 9: return "Аналоговый вход разморожен";
                case 10: return "Свойства газа в диапазоне";
                case 67: return "Переход на показания датчика (из Host’a)";
                case 128: return "Установлен отказ аналогового входа";
                case 129: return "Аналоговый вход в градуировке";
                case 130: return "Установлена отсечка по отсутствию расхода";
                case 131: return "Введена константа";
                case 133: return "Низкое напряжение питания";
                case 134: return "Превышение предела градуировки датчика";
                case 135: return "Рестарт Суперлоу";
                case 137: return "Аналоговый вход заморожен";
                case 138: return "Ошибка в свойствах газа";
                case 139: return "Сезонное изменение времени (летнее/зимнее)";
                case 195: return "Введена константа (из Host’а)";
            }
            return "не определено";
        }

        private byte[] MakeAbnormalRequest(byte na, byte channel, byte number, DateTime start, DateTime end)
        {
            return MakeRequest(na, 23, new byte[] { 
                channel,
                number,
                (byte)start.Month,
                (byte)start.Day,
                (byte)(start.Year-2000),
                (byte)start.Hour,
                (byte)end.Month,
                (byte)end.Day,
                (byte)(end.Year-2000),
                (byte)start.Hour
            });
        }
    }
}

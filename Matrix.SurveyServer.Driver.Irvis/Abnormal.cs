using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        private dynamic GetAbnormal(byte na, byte ch, short password, byte mode, DateTime date, byte[] codes)
        {
            byte[] req = MakeAbnormalRequest(na, ch, password, mode, date);
            var bytes = SendWithCrc(req);
            if (bytes.Any())
                return ParseAbnormalResponse(bytes, codes);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            answer.n = -1;
            return answer;
        }

        private dynamic GetAbnormalDay(byte na, byte ch, short password, byte mode, DateTime date)
        {
            byte[] req = MakeAbnormalRequest(na, ch, password, mode, date);
            var bytes = SendWithCrc(req);
            if (bytes.Any())
                return ParseAbnormalResponseDay(bytes);

            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = "не получен ответ на запрос";
            answer.n = -1;
            return answer;
        }

        private byte[] MakeAbnormalRequest(byte na, byte ch, short password, byte mode, DateTime date)
        {
            var bytes = new byte[] 
            { 
                ch,
                mode,
                (byte)date.Day,
                (byte)date.Month,
                (byte)(date.Year - 2000),
                (byte)(password >> 8), 
                (byte)(password & 0x00FF) 
            };
            return Make70Request(na, 2, bytes);
        }

        private dynamic ParseAbnormalResponseDay(byte[] bytes)
        {
            dynamic abnormal = Parse70Response(bytes);
            if (!abnormal.success)
            {
                abnormal.n = -1;
                return abnormal;
            }

            abnormal.records = new List<dynamic>();
            abnormal.channel = abnormal.body[0];
            abnormal.package = abnormal.body[1];
            abnormal.n = abnormal.body[2];
            if (abnormal.n == 0)
            {
                abnormal.success = false;
                abnormal.error = "данные отсутствуют";
                return abnormal;
            }
            
            //смещаем на 3 байта. другого решения пока не нашел
            for (int i = 0; i < abnormal.n; i++)
            {
                var date = new DateTime(abnormal.body[i * 12 + 7] + 2000, abnormal.body[i * 12 + 6], abnormal.body[i * 12 + 5], abnormal.body[i * 12 + 4], abnormal.body[i * 12 + 3], 0);

                var fl_a = abnormal.body[i * 12 + 12];
                var fl_b = (short)(abnormal.body[i * 12 + 14] * 0x0100 + abnormal.body[i * 12 + 13]); // инвертирование байтов

                dynamic record = new ExpandoObject();
                record.fl_a = fl_a;
                record.fl_b = fl_b;
                record.date = date;
                abnormal.records.Add(record);
            }

            return abnormal;
        }

        private dynamic ParseAbnormalResponse(byte[] bytes, byte[] codes)
        {
            dynamic abnormal = Parse70Response(bytes);
            if (!abnormal.success)
            {
                abnormal.n = -1;
                return abnormal;
            }
            abnormal.records = new List<dynamic>();
            abnormal.channel = abnormal.body[0];
            abnormal.package = abnormal.body[1];
            abnormal.n = abnormal.body[2];
            if (abnormal.n == 0)
            {
                abnormal.success = false;
                abnormal.error = "данные отсутствуют";
                return abnormal;
            }

            var oldCodes = codes;

            //смещаем на 3 байта. другого решения пока не нашел
            for (int i = 0; i < abnormal.n; i++)
            {
                var date = new DateTime(abnormal.body[i * 12 + 7] + 2000, abnormal.body[i * 12 + 6], abnormal.body[i * 12 + 5], abnormal.body[i * 12 + 4], abnormal.body[i * 12 + 3], 0);

                var fl_a = abnormal.body[i * 12 + 12];
                var fl_b = (short)(abnormal.body[i * 12 + 14] * 0x0100 + abnormal.body[i * 12 + 13]); // инвертирование байтов

                byte[] newCodes = ParseByMask(fl_a, fl_b);
               
                foreach (var code in newCodes.Except(oldCodes))
                {
                    abnormal.records.Add(MakeAbnormalRecord(code, string.Format("{0}, статус {1}", GetAbnormal(code), "появилась"), 0, date, newCodes));
                }

                foreach (var code in oldCodes.Except(newCodes))
                {
                    abnormal.records.Add(MakeAbnormalRecord(code, string.Format("{0}, статус {1}", GetAbnormal(code), "устранилась"), 0, date, newCodes));
                }
                oldCodes = newCodes;
            }

            abnormal.codes = oldCodes;
            return abnormal;
        }

        private byte[] ParseByMask(byte fl_a, short fl_b)
        {
            List<byte> codes = new List<byte>();

            if ((fl_a & 0x01) == 0x01) codes.Add(13);
            if ((fl_a & 0x02) == 0x01) codes.Add(14);
            if ((fl_a & 0x04) == 0x01) codes.Add(15);

            if ((short)(fl_b & 0x0003) == 0x0001)   //  0000 0000 0000 0001
                codes.Add(0);

            if ((short)(fl_b & 0x0003) == 0x0002)   //  0000 0000 0000 0010
                codes.Add(1);

            if ((short)(fl_b & 0x0003) == 0x0003)   //  0000 0000 0000 0011
                codes.Add(2);

            if ((short)(fl_b & 0x000C) == 0x0004)   //  0000 0000 0000 0100
                codes.Add(3);

            if ((short)(fl_b & 0x000C) == 0x0008)   //  0000 0000 0000 1000
                codes.Add(4);

            if ((short)(fl_b & 0x000C) == 0x000C)   //  0000 0000 0000 1100
                codes.Add(5);

            if ((short)(fl_b & 0x0030) == 0x0010)   //  0000 0000 0001 0000
                codes.Add(6);

            if ((short)(fl_b & 0x0030) == 0x0020)   //  0000 0000 0010 0000
                codes.Add(7);

            if ((short)(fl_b & 0x0040) == 0x0040)   //  0000 0000 0100 0000
                codes.Add(8);

            if ((short)(fl_b & 0x0080) == 0x0080)   //  0000 0000 1000 0000
                codes.Add(9);

            if ((short)(fl_b & 0x0100) == 0x0100)   //  0000 0001 0000 0000
                codes.Add(10);

            if ((short)(fl_b & 0x0200) == 0x0200)   //  0000 0010 0000 0000
                codes.Add(11);

            if ((short)(fl_b & 0x0400) == 0x0400)   //  0000 0100 0000 0000
                codes.Add(12);

            return codes.ToArray();
        }

        private string GetAbnormal(byte code)
        {
            switch (code)
            {
                case 0: return "Q ниже допуска";
                case 1: return "Q выше допуска";
                case 2: return "FQ выше допуска";
                case 3: return "вода в датчике Q";
                case 4: return "отказ датчика Q";
                case 5: return "нет расхода";
                case 6: return "плохой сигнал Q";
                case 7: return "анализ сигнала Q";
                case 8: return "T вне допуска";
                case 9: return "P вне допуска";
                case 10: return "нет данных";
                case 11: return "запись в архив констант: изменение договорных параметров; изменение параметров среды";
                case 12: return "учет газа при плохом сигнале";
                case 13: return "выключение питания";
                case 14: return "изменение даты/времени";
                case 15: return "отказ часов";
            }
            return string.Empty;
        }

        private bool IsEventImportant(int eventId)
        {
            return ((new int[] { 2, 4, 6, 10, 13, 15 }).Contains(eventId));
        }

        private dynamic MakeAbnormalRecord(int eventId, string name, int duration, DateTime date, byte[] codes)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId + (IsEventImportant(eventId)? 1000 : 0);
            record.s1 = name;
            record.s2 = Convert.ToBase64String(codes);
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}

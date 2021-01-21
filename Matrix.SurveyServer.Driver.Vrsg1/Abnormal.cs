using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Vrsg1
{
    public partial class Driver
    {
        private dynamic GetAbnormal(byte na, byte ch, short password, byte mode, DateTime date, byte[] codes)
        {
            var bytes = SendWithCrc(MakeAbnormalRequest(na, ch, password, mode, date));
            if (bytes.Any())
                return ParseAbnormalResponse(bytes, codes);

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

        private dynamic ParseAbnormalResponse(byte[] bytes, byte[] codes)
        {
            dynamic abnormal = Parse70Response(bytes);
            if (!abnormal.success) return abnormal;
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
                    abnormal.records.Add(MakeAbnormalRecord(string.Format("{0}, статус {1}", GetAbnormal(code), "появилась"), 0, date, newCodes));
                }

                foreach (var code in oldCodes.Except(newCodes))
                {
                    abnormal.records.Add(MakeAbnormalRecord(string.Format("{0}, статус {1}", GetAbnormal(code), "устранилась"), 0, date, newCodes));
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
                case 12: return "копия флага «Учет газа при плохом сигнале» на момент записи в архив";
                case 13: return "выключение питания";
                case 14: return "изменение даты/времени";
                case 15: return "отказ часов";
            }
            return string.Empty;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date, byte[] codes)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.s2 = Convert.ToBase64String(codes);
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        //private dynamic ParseAbnormalResponse(byte[] bytes)
        //{
        //    dynamic abnormal = Parse70Response(bytes);
        //    if (!abnormal.success) return abnormal;

        //    log(string.Format("СОбытие {0}", string.Join(",", (abnormal.body as IEnumerable<byte>).Select(x => x.ToString("X2")))));
        //    abnormal.records = new List<dynamic>();
        //    abnormal.channel = abnormal.body[0];
        //    abnormal.package = abnormal.body[1];
        //    abnormal.n = abnormal.body[2];
        //    if (abnormal.n == 0)
        //    {
        //        abnormal.success = false;
        //        abnormal.error = "данные отсутствуют";
        //        return abnormal;
        //    }

        //    for (int i = 0; i < abnormal.n; i++)
        //    {
        //        var date = new DateTime(abnormal.body[i * 12 + 7] + 2000, abnormal.body[i * 12 + 6], abnormal.body[i * 12 + 5], abnormal.body[i * 12 + 4], abnormal.body[i * 12 + 3], 0);

        //        var fl_a = abnormal.body[i * 12 + 9];
        //        if ((fl_a & 0x01) == 0x01)
        //            abnormal.records.Add(MakeAbnormalRecord("выключение питания", 0, date));
        //        if ((fl_a & 0x02) == 0x01)
        //            abnormal.records.Add(MakeAbnormalRecord("изменение даты/времени", 0, date));
        //        if ((fl_a & 0x04) == 0x01)
        //            abnormal.records.Add(MakeAbnormalRecord("отказ часов", 0, date));

        //        var fl_b = (short)(abnormal.body[i * 12 + 11] * 0x0100 + abnormal.body[i * 12 + 10]);

        //        if ((short)(fl_b & 0x0003) == 0x0001)   //  0000 0000 0000 0001
        //            abnormal.records.Add(MakeAbnormalRecord("Q ниже допуска", 0, date));

        //        if ((short)(fl_b & 0x0003) == 0x0002)   //  0000 0000 0000 0010
        //            abnormal.records.Add(MakeAbnormalRecord("Q выше допуска", 0, date));

        //        if ((short)(fl_b & 0x0003) == 0x0003)   //  0000 0000 0000 0011
        //            abnormal.records.Add(MakeAbnormalRecord("FQ выше допуска", 0, date));

        //        if ((short)(fl_b & 0x000C) == 0x0004)   //  0000 0000 0000 0100
        //            abnormal.records.Add(MakeAbnormalRecord("вода в датчике Q", 0, date));

        //        if ((short)(fl_b & 0x000C) == 0x0008)   //  0000 0000 0000 1000
        //            abnormal.records.Add(MakeAbnormalRecord("отказ датчика Q", 0, date));

        //        if ((short)(fl_b & 0x000C) == 0x000C)   //  0000 0000 0000 1100
        //            abnormal.records.Add(MakeAbnormalRecord("нет расхода", 0, date));

        //        if ((short)(fl_b & 0x0030) == 0x0010)   //  0000 0000 0001 0000
        //            abnormal.records.Add(MakeAbnormalRecord("плохой сигнал Q", 0, date));

        //        if ((short)(fl_b & 0x0030) == 0x0020)   //  0000 0000 0010 0000
        //            abnormal.records.Add(MakeAbnormalRecord("анализ сигнала Q", 0, date));

        //        if ((short)(fl_b & 0x0040) == 0x0040)   //  0000 0000 0100 0000
        //            abnormal.records.Add(MakeAbnormalRecord("T вне допуска", 0, date));

        //        if ((short)(fl_b & 0x0080) == 0x0080)   //  0000 0000 1000 0000
        //            abnormal.records.Add(MakeAbnormalRecord("P вне допуска", 0, date));

        //        if ((short)(fl_b & 0x0100) == 0x0100)   //  0000 0001 0000 0000
        //            abnormal.records.Add(MakeAbnormalRecord("нет данных", 0, date));

        //        if ((short)(fl_b & 0x0200) == 0x0200)   //  0000 0010 0000 0000
        //            abnormal.records.Add(MakeAbnormalRecord("запись в архив констант: изменение договорных параметров; изменение параметров среды", 0, date));

        //        if ((short)(fl_b & 0x0400) == 0x0400)   //  0000 0100 0000 0000
        //            abnormal.records.Add(MakeAbnormalRecord("копия флага «Учет газа при плохом сигнале» на момент записи в архив", 0, date));

        //        //if (fl_b == 0x0000) //0000 0000 0000 0000
        //        //    abnormal.records.Add(MakeAbnormalRecord("зарезервировано", 0, date));

        //    }

        //    return abnormal;
        //}

        //private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        //{
        //    dynamic record = new ExpandoObject();
        //    record.type = "Abnormal";
        //    record.i1 = duration;
        //    record.s1 = name;
        //    record.date = date;
        //    record.dt1 = DateTime.Now;
        //    return record;
        //}
    }
}

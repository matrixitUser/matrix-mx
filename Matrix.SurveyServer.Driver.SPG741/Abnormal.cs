using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        private dynamic GetAbnormal(byte na)
        {
            ///Архивы НС хранятся во FLASH-памяти начальный адрес 3894H,
            ///3894H по 64кб на странице => адрес первой страницы НС 0xE2
            ///Глубина архивов 100 записей по 8 байт = 12 страниц
            var pages = ReadFlash(na, 0xE2, 13);

            List<dynamic> records = new List<dynamic>();
            pages.records = records;

            if (!pages.success)
                return pages;


            //foreach (var page in pages.body)
            //{
            //    var p = (page as IEnumerable<byte>);
            //    for (int i = 0; i < 64; i += 8)
            //    {
            //        var abnormal = ParseAbnormal(p.Skip(i).Take(8).ToArray());
            //        if (!abnormal.success)
            //        {
            //            pages.success = false;
            //            pages.error = abnormal.error;
            //            return pages;
            //        }
            //        records.Add(abnormal);
            //    }
            //}

            List<byte> data = new List<byte>();
            foreach (var page in pages.body)
            {
                data.AddRange(page);
            }

            ///первые 20 байт несут непонятную информацию, пропускаем
            byte[] bytes = data.Skip(20).ToArray();
            for (int i = 0; i < bytes.Length; i += 8)
            {
                var abnormal = ParseAbnormal(bytes.Skip(i).Take(8).ToArray());
                if (!abnormal.success)
                {
                    pages.success = false;
                    pages.error = abnormal.error;
                    return pages;
                }
                records.Add(MakeAbnormalRecord(abnormal.code, abnormal.situation, abnormal.flag, abnormal.date));
            }
            return pages;
        }

        private dynamic ParseAbnormal(byte[] bytes)
        {
            dynamic abnormal = new ExpandoObject();

            if (bytes.Length != 8)
            {
                abnormal.success = false;
                abnormal.error = "тело параметра имеет не верное количество байт (8 байт)";
                return abnormal;
            }

            if (bytes[0] != 0x10)
            {
                abnormal.success = false;
                abnormal.error = "отстутствует запись НС";
                return abnormal;
            }

            try
            {
                int year = bytes[1];
                int month = bytes[2];
                int day = bytes[3];
                int hour = bytes[4];
                int minute = bytes[5];
                abnormal.date = new DateTime(2000 + year, month, day, hour, minute, 0);
            }
            catch (Exception ex)
            {
                abnormal.success = false;
                abnormal.error = "ошибка при чтении НС";
                return abnormal;
            }

            abnormal.code = bytes[6];
            abnormal.flag = bytes[7];
            string situations = "";
            if (Situations.ContainsKey(abnormal.code))
            {
                situations = Situations[abnormal.code];
            }
            else
            {
                situations = "сведения об ошибке отсутствует";
            }
            
            abnormal.situation = string.Format("(код {0}): {1}, статус: {2}", abnormal.code, situations, abnormal.flag == 1 ? "появилась" : "устранилась");
            abnormal.success = true;
            return abnormal;
        }

        private dynamic MakeAbnormalRecord(int eventId, string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.i2 = eventId + (IsEventImportant(eventId) ? 1000 : 0);
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        Dictionary<int, string> Situations = new Dictionary<int, string>()
        {
            {0, "Разряд батареи (напряжение батареи меньше 3,2 В)"},
            {2, "Перегрузка по цепям питания датчиков давления (только для модели 02)"},
            {3, "Активный уровень сигнала на дискретном входе D2"},
            {4, "Сигнал Qр по каналу т1 меньше нижнего предела"},
            {5, "Сигнал Qр по каналу т2 меньше нижнего предела"},
            {6, "Сигнал Qр по каналу т1 превысил верхний предел"},
            {7, "Сигнал Qр по каналу т2 превысил верхний предел"},
            {9, "Сигнал на входе ПД1 вне диапазона"},
            {10, "Сигнал на входе ПД2 вне диапазона"},
            {11, "Сигнал на входе ПД3 вне диапазона"},
            {12, "Сигнал на входе ПД4 вне диапазона"},
            {13, "Сигнал на входе ПД5 вне диапазона"},
            {14, "Температура t1 вне диапазона -52...+92 °C"},
            {15, "Температура t2 вне диапазона -52...+92 °C"},
            {16, "Параметр P1 вышел за пределы уставок Ув, Ун"},
            {17, "Параметр ∆P1 вышел за пределы уставок Ув, Ун"},
            {18, "Параметр Qр1 вышел за пределы уставок Ув, Ун"},
            {19, "Параметр P2 вышел за пределы уставок Ув, Ун"},
            {20, "Параметр ∆P2 вышел за пределы уставок Ув, Ун"},
            {21, "Параметр Qр2 вышел за пределы уставок Ув, Ун"},
            {22, "Параметр ∆P3 вышел за пределы уставок Ув, Ун"},
            {23, "Параметр P3 вышел за пределы уставок Ув, Ун"},
            {24, "Параметр P4 вышел за пределы уставок Ув, Ун"},
            {25, "Текущее суточное значение V по каналу ОБЩ превышает норму поставки"},
            {26, "Отрицательное значение Кп по каналу 1"},
            {27, "Отрицательное значение Кп по каналу 2"}
        };

        private bool IsEventImportant(int eventId)
        {
            return ((new int[] { 0, 2, 3, 26, 27 }).Contains(eventId));
        }
    }

}

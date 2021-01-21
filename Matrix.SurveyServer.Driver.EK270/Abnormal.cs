using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.EK270
{
    public partial class Driver
    {
        private dynamic GetAbnormals(DateTime start, DateTime end)
        {
            dynamic archive = new ExpandoObject();
            archive.records = new List<dynamic>();

            while (true)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    return archive;
                }

                //log(string.Format("запрос НС записи {0:dd.MM.yy HH:mm:ss} - {1:dd.MM.yy HH:mm:ss}", startAbnormal, endAbnormal));
                var data = Send(MakeArchiveRequest(4, start, end, 1));
                //var str = Encoding.ASCII.GetString(data, 1, data.Length - 1);
                //log(str);

                var rsp = ParseArchiveResponse(data);
                if (!rsp.success) return rsp;

                var abn = ParseAbnormalRecords(rsp.rows);
                if (!abn.success) return abn;

                records(abn.records);

                List<dynamic> recs = abn.records;
                archive.records.AddRange(recs);

                if (recs.Count == 0)
                {
                    break;
                }

                start = ((DateTime)recs[0].date).AddSeconds(1);

                if (start >= end)
                {
                    break;
                }
            }

            archive.success = true;
            archive.error = string.Empty;
            return archive;
        }


        private string GetAbnormalText(long evt)
        {
            var res = "";
            switch (evt)
            {
                case 0x8104:
                    res = "Новый интервал архива";
                    break;
                case 0x8004:
                    res = "Изменение интервала назад (перевод часов назад)";
                    break;

                case 0x8203:
                    res = "Уместное значение для архива изменилось – значение после замены";
                    break;

                case 0x8303:
                    res = "Уместное значение для архива изменилось – значение перед заменой";
                    break;

                case 0x8503:
                    res = "команда Заморозить";
                    break;

                case 0x0005:
                    res = GetStatusMessage(5, 1, false);//"Сообщение '1' исчезает из статуса 5";
                    break;

                case 0x0006:
                    res = GetStatusMessage(6, 1, false);//"Сообщение '1' исчезает из статуса 6";
                    break;

                case 0x0007:
                    res = GetStatusMessage(7, 1, false);//"Сообщение '1' исчезает из статуса 7";
                    break;

                case 0x0008:
                    res = GetStatusMessage(8, 1, false);//"Сообщение '1' исчезает из статуса 8";
                    break;

                case 0x0009:
                    res = GetStatusMessage(9, 1, false);//"Сообщение '1' исчезает из статуса 9";
                    break;

                case 0x0105:
                    res = GetStatusMessage(5, 2, false);//"Сообщение '2' исчезает из статуса 5";
                    break;

                case 0x0106:
                    res = GetStatusMessage(6, 2, false);//"Сообщение '2' исчезает из статуса 6";
                    break;

                case 0x0301:
                    res = GetStatusMessage(1, 4, false);//"Сообщение '4' исчезает из статуса 1";
                    break;

                case 0x0302:
                    res = GetStatusMessage(2, 4, false);//"Сообщение '4' исчезает из статуса 2";
                    break;

                case 0x0303:
                    res = GetStatusMessage(3, 4, false);//"Сообщение '4' исчезает из статуса 3";
                    break;

                case 0x0304:
                    res = GetStatusMessage(4, 4, false);//"Сообщение '4' исчезает из статуса 4";
                    break;

                case 0x0402:
                    res = GetStatusMessage(2, 5, false);//"Сообщение '5' исчезает из статуса 2";
                    break;

                case 0x0502:
                    res = GetStatusMessage(2, 6, false);//"Сообщение '6' исчезает из статуса 2";
                    break;

                case 0x0504:
                    res = GetStatusMessage(4, 6, false);//"Сообщение '6' исчезает из статуса 4";
                    break;

                case 0x0506:
                    res = GetStatusMessage(6, 6, false);//"Сообщение '6' исчезает из статуса 6";
                    break;

                case 0x0507:
                    res = GetStatusMessage(7, 6, false);//"Сообщение '6' исчезает из статуса 7";
                    break;

                case 0x0702:
                    res = GetStatusMessage(2, 8, false);//"Сообщение '8' исчезает из статуса 2";
                    break;

                case 0x0703:
                    res = GetStatusMessage(3, 8, false);//"Сообщение '8' исчезает из статуса 3";
                    break;

                case 0x0905:
                    res = GetStatusMessage(5, 10, false);//"Сообщение '10' исчезает из статуса 5";
                    break;

                case 0x0906:
                    res = GetStatusMessage(6, 10, false);//"Сообщение '10' исчезает из статуса 6";
                    break;

                case 0x0C02:
                    res = GetStatusMessage(2, 13, false);//"Сообщение '13' исчезает из статуса 2";
                    break;

                case 0x0C03:
                    res = GetStatusMessage(3, 13, false);//"Сообщение '13' исчезает из статуса 3";
                    break;

                case 0x0D01:
                    res = GetStatusMessage(1, 14, false);//"Сообщение '14' исчезает из статуса 1";
                    break;

                case 0x0D02:
                    res = GetStatusMessage(2, 14, false);//"Сообщение '14' исчезает из статуса 2";
                    break;

                case 0x0D03:
                    res = GetStatusMessage(3, 14, false);//"Сообщение '14' исчезает из статуса 3";
                    break;

                case 0x0D04:
                    res = GetStatusMessage(4, 14, false);//"Сообщение '14' исчезает из статуса 4";
                    break;

                case 0x0F01:
                    res = GetStatusMessage(1, 16, false);//"Сообщение '16' исчезает из статуса 1";
                    break;

                case 0x0F02:
                    res = GetStatusMessage(2, 16, false);//"Сообщение '16' исчезает из статуса 2";
                    break;

                case 0x1002:
                    res = GetStatusMessage(0, 1, false);//"Сообщение '1' исчезает из системного статуса";
                    break;

                case 0x1202:
                    res = GetStatusMessage(0, 3, false);//"Сообщение '3' исчезает из системного статуса";
                    break;

                case 0x1302:
                    res = GetStatusMessage(0, 4, false);//"Сообщение '4' исчезает из системного статуса";
                    break;

                case 0x1402:
                    res = GetStatusMessage(0, 5, false);//"Сообщение '5' исчезает из системного статуса";
                    break;

                case 0x1702:
                    res = GetStatusMessage(0, 8, false);//"Сообщение '8' исчезает из системного статуса";
                    break;

                case 0x1802:
                    res = GetStatusMessage(0, 9, false);//"Сообщение '9' исчезает из системного статуса";
                    break;

                case 0x1A02:
                    res = GetStatusMessage(0, 11, false);//"Сообщение '11' исчезает из системного статуса";
                    break;

                case 0x1C02:
                    res = GetStatusMessage(0, 13, false);//"Сообщение '13' исчезает из системного статуса";
                    break;

                case 0x1E02:
                    res = GetStatusMessage(0, 15, false);//"Сообщение '15' исчезает из системного статуса";
                    break;

                case 0x1F02:
                    res = GetStatusMessage(0, 16, false);//"Сообщение '16' исчезает из системного статуса";
                    break;

                case 0x2005:
                    res = GetStatusMessage(5, 1, true);//"Сообщение '1' появилось в статусе 5";
                    break;

                case 0x2006:
                    res = GetStatusMessage(6, 1, true);//"Сообщение '1' появилось в статусе 6";
                    break;

                case 0x2007:
                    res = GetStatusMessage(7, 1, true);//"Сообщение '1' появилось в статусе 7";
                    break;

                case 0x2008:
                    res = GetStatusMessage(8, 1, true);//"Сообщение '1' появилось в статусе 8";
                    break;

                case 0x2009:
                    res = GetStatusMessage(9, 1, true);//"Сообщение '1' появилось в статусе 9";
                    break;

                case 0x2105:
                    res = GetStatusMessage(5, 2, true);//"Сообщение '2' появилось в статусе 5";
                    break;

                case 0x2106:
                    res = GetStatusMessage(6, 2, true);//"Сообщение '2' появилось в статусе 6";
                    break;

                case 0x2301:
                    res = GetStatusMessage(1, 4, true);//"Сообщение '4' появилось в статусе 1";
                    break;

                case 0x2302:
                    res = GetStatusMessage(2, 4, true);//"Сообщение '4' появилось в статусе 2";
                    break;

                case 0x2303:
                    res = GetStatusMessage(3, 4, true);//"Сообщение '4' появилось в статусе 3";
                    break;

                case 0x2304:
                    res = GetStatusMessage(4, 4, true);//"Сообщение '4' появилось в статусе 4";
                    break;

                case 0x2402:
                    res = GetStatusMessage(2, 5, true);//"Сообщение '5' появилось в статусе 2";
                    break;

                case 0x2502:
                    res = GetStatusMessage(2, 6, true);//"Сообщение '6' появилось в статусе 2";
                    break;

                case 0x2504:
                    res = GetStatusMessage(4, 6, true);//"Сообщение '6' появилось в статусе 4";
                    break;

                case 0x2506:
                    res = GetStatusMessage(6, 6, true);//"Сообщение '6' появилось в статусе 6";
                    break;

                case 0x2507:
                    res = GetStatusMessage(7, 6, true);//"Сообщение '6' появилось в статусе 7";
                    break;

                case 0x2702:
                    res = GetStatusMessage(2, 8, true);//"Сообщение '8' появилось в статусе 2";
                    break;

                case 0x2703:
                    res = GetStatusMessage(3, 8, true);//"Сообщение '8' появилось в статусе 3";
                    break;

                case 0x2905:
                    res = GetStatusMessage(5, 10, true);//"Сообщение '10' появилось в статусе 5";
                    break;

                case 0x2906:
                    res = GetStatusMessage(6, 10, true);//"Сообщение '10' появилось в статусе 6";
                    break;

                case 0x2C02:
                    res = GetStatusMessage(2, 13, true);//"Сообщение '13' появилось в статусе 2";
                    break;

                case 0x2C03:
                    res = GetStatusMessage(3, 13, true);//"Сообщение '13' появилось в статусе 3";
                    break;

                case 0x2D01:
                    res = GetStatusMessage(1, 14, true);//"Сообщение '14' появилось в статусе 1";
                    break;

                case 0x2D02:
                    res = GetStatusMessage(2, 14, true);//"Сообщение '14' появилось в статусе 2";
                    break;

                case 0x2D03:
                    res = GetStatusMessage(3, 14, true);//"Сообщение '14' появилось в статусе 3";
                    break;

                case 0x2D04:
                    res = GetStatusMessage(4, 14, true);//"Сообщение '14' появилось в статусе 4";
                    break;

                case 0x2F01:
                    res = GetStatusMessage(1, 16, true);//"Сообщение '16' появилось в статусе 1";
                    break;

                case 0x2F02:
                    res = GetStatusMessage(2, 16, true);//"Сообщение '16' появилось в статусе 2";
                    break;

                case 0x3002:
                    res = GetStatusMessage(0, 1, true);//"Сообщение '1' появилось в системном статусе";
                    break;

                case 0x3202:
                    res = GetStatusMessage(0, 3, true);//"Сообщение '3' появилось в системном статусе";
                    break;

                case 0x3302:
                    res = GetStatusMessage(0, 4, true);//"Сообщение '4' появилось в системном статусе";
                    break;

                case 0x3402:
                    res = GetStatusMessage(0, 5, true);//"Сообщение '5' появилось в системном статусе";
                    break;

                case 0x3702:
                    res = GetStatusMessage(0, 8, true);//"Сообщение '8' появилось в системном статусе";
                    break;

                case 0x3802:
                    res = GetStatusMessage(0, 9, true);//"Сообщение '9' появилось в системном статусе";
                    break;

                case 0x3A02:
                    res = GetStatusMessage(0, 11, true);//"Сообщение '11' появилось в системном статусе";
                    break;

                case 0x3C02:
                    res = GetStatusMessage(0, 13, true);//"Сообщение '13' появилось в системном статусе";
                    break;

                case 0x3E02:
                    res = GetStatusMessage(0, 15, true);//"Сообщение '15' появилось в системном статусе";
                    break;

                case 0x3F02:
                    res = GetStatusMessage(0, 16, true);//"Сообщение '16' появилось в системном статусе";
                    break;

                default:
                    res = string.Format("Нераспознанное событие 0x{0:X4}", evt);
                    break;
            }
            return res;
        }

        private string GetStatusMessage(byte status, byte msgi, bool isAppear)
        {
            var res = "";
            switch (msgi)
            {
                //Тревоги
                case 1:
                    switch (status)
                    {
                        case 0:
                            res = "Перезапуск корректора";
                            break;

                        case 4:
                            res = "Нарушены границы рабочего расхода";
                            break;

                        case 5:
                            res = "Невозможно вычислить коэффициент коррекции";
                            break;

                        case 6:
                            res = "Нарушены границы тревоги для температуры";
                            break;

                        case 7:
                            res = "Нарушены границы тревоги для давления";
                            break;

                        case 8:
                            res = "Невозможно вычислить коэффициент сжимаемости газа";
                            break;

                        case 9:
                            res = "Невозможно вычислить коэффициент реального газа";
                            break;
                    }
                    break;

                case 2:
                    switch (status)
                    {
                        case 1:
                            res = "Ошибка на входе 1";
                            break;

                        case 5:
                            res = "Выходной сигнал с датчика температуры вне пределов допустимых значений";
                            break;

                        case 6:
                            res = "Выходной сигнал с датчика давления выходит за пределы установленных значений";
                            break;
                    }
                    break;

                //Предупреждение
                case 3:
                    switch (status)
                    {
                        case 0:
                            res = "Данные восстановлены";
                            break;
                    }
                    break;

                case 4:
                    switch (status)
                    {
                        case 0:
                            res = "Низкое напряжение питания";
                            break;

                        case 1:
                            res = "Ошибка на выходе 1";
                            break;

                        case 2:
                            res = "Ошибка на выходе 2";
                            break;

                        case 3:
                            res = "Ошибка на выходе 2";
                            break;

                        case 4:
                            res = "Ошибка на выходе 4";
                            break;
                    }
                    break;

                case 5:
                    switch (status)
                    {
                        case 0:
                            res = "Ошибка данных";
                            break;

                        case 2:
                            res = "Ошибка во время сравнения кол-ва импульсов на Входе 2";
                            break;
                    }
                    break;

                case 6:
                    switch (status)
                    {
                        case 1:
                            res = "Предел предупреждения по W";
                            break;

                        case 2:
                            res = "Нарушены границы предупреждения для стандартного расхода";
                            break;

                        case 4:
                            res = "Нарушены границы предупреждения для рабочего расхода";
                            break;

                        case 6:
                            res = "Нарушены границы предупреждения для температуры";
                            break;

                        case 7:
                            res = "Нарушены границы предупреждения для давления";
                            break;
                    }
                    break;

                case 7:
                    switch (status)
                    {
                        case 0:
                            res = "Ошибка программного обеспечения";
                            break;
                    }
                    break;

                case 8:
                    switch (status)
                    {
                        case 0:
                            res = "Ошибка установок";
                            break;

                        case 2:
                            res = "Сигнал предупреждения на Входе 2";
                            break;

                        case 3:
                            res = "Сигнал предупреждения на Входе 3";
                            break;
                    }
                    break;

                //Отчеты
                case 9:
                    switch (status)
                    {
                        case 0:
                            res = "Нижний предел остаточного срока службы элементов питания";
                            break;
                    }
                    break;

                case 10:
                    switch (status)
                    {
                        case 0:
                            res = "Ремонтный режим включен";
                            break;
                    }
                    break;

                case 11:
                    switch (status)
                    {
                        case 0:
                            res = "Часы не установлены";
                            break;

                        case 1:
                            res = "Ошибка энкодера";
                            break;
                    }
                    break;

                case 13:
                    switch (status)
                    {
                        case 0:
                            res = "Интерфейс активен";
                            return "";//!!!! Отключен !!!! 
                            //break;

                        case 2:
                            res = "Сигнал отчета на Входе 2";
                            break;

                        case 3:
                            res = "Сигнал отчета на Входе 3";
                            break;
                    }
                    break;

                case 14:
                    switch (status)
                    {
                        case 0:
                            res = "Дистанционная синхронизация времени началась";
                            break;

                        case 1:
                            res = "Открыт калибровочный замок";
                            break;

                        case 3:
                            res = "Замок поставщика открыт";
                            return "";//!!!! Отключен !!!! 
                            //break;

                        case 4:
                            res = "Замок потребителя открыт";
                            return "";//!!!! Отключен !!!! 
                            //break;
                    }
                    break;

                case 15:
                    switch (status)
                    {
                        case 0:
                            res = "Работа от внутренних элементов питания";
                            break;

                        case 1:
                            res = "Активен дополнительный временной интервал подтверждения запроса данных";
                            break;
                    }
                    break;

                case 16:
                    switch (status)
                    {
                        case 0:
                            res = "Летнее время";
                            break;

                        case 1:
                            res = "Активен временной интервал 1 подтверждения запроса данных";
                            break;

                        case 2:
                            res = "Активен временной интервал 2 подтверждения запроса";
                            break;
                    }
                    break;
            }
            if (res == "")
            {
                res = string.Format("[Сообщение {0} появилось в статусе {1}]", msgi, status);
            }

            return (isAppear ? "Начало события: " : "Конец события: ") + res;
        }
    }
}

// закомментируйте следующую строку, если ДАННЫЕ_ЗА_ЭТОТ_ЧАС = (ЭТОТ_ЧАС:30 + СЛЕД_ЧАС:00) / 2,
//*#define HALF_NEXT // ДАННЫЕ_ЗА_ЭТОТ_ЧАС = (ЭТОТ_ЧАС:00 + ЭТОТ_ЧАС:30) / 2   //Закомментировал 27/06/1917 ЭСКБ-Айрат утверждает, что это усредненная мощность выдается для конца периода

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        private DateTime GetHourFromHalf(DateTime date)
        {
#if HALF_NEXT
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0);
#else
            return (new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0)).AddHours(-1);
#endif
        }

        private dynamic GetHours(DateTime start, DateTime end, DateTime current, Version version, dynamic variant)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var halfs = new List<dynamic>();
            var hours = new List<dynamic>();
            var status = new Dictionary<DateTime, int>();

            var last = ParseLastPowerRecordInfo(Send(MakeParametersRequest(0x13)));
            if (!last.success) return last;

            log(string.Format("последняя запись {0:dd.MM.yyyy HH:mm}; версия ПО: {1}; постоянная счётчика: {2}; размер памяти №3: {3}x8", last.Date, version.ToString(), variant.A, variant.mem3 == 1 ? "131" : "65,5"));
            byte profile = last.Profile;

            List<dynamic> journal = null;

            Func<DateTime, int, UInt32> OffsetCalculator = (d, delta) =>
            {
                byte recordLen = 16;

                /*if (debugMode)
                {
                    log(string.Format("Вычисление смещения, lastInx={0}, mins={1}, delta={2}", last.Index, (last.Date - d).TotalMinutes, delta));
                    log(string.Format("Формула 1: {0:X4}, 2: {1:X4}", (UInt32)(last.Index - recordLen * (Math.Round(((last.Date - d).TotalMinutes + delta) / 30.0, MidpointRounding.ToEven))),
                        (UInt32)(recordLen * (last.Index - Math.Round(((last.Date - d).TotalMinutes + delta) / 30.0, MidpointRounding.ToEven)))));
                }*/

                if (version.IsLessThan(7, 0, 0))//7.2.6
                {
                    //OnSendMessage("версия ПО младше 7.0.0");
                    return (UInt32)(last.Index - recordLen * (Math.Round(((last.Date - d).TotalMinutes + delta) / 30.0, MidpointRounding.ToEven)));
                }
                else
                {
                    //OnSendMessage("версия ПО старше 7.0.0");
                    return (UInt32)(recordLen * (last.Index - Math.Round(((last.Date - d).TotalMinutes + delta) / 30.0, MidpointRounding.ToEven)));
                }
            };

            ////эксперементальным путем вычислены версии, где 17-й байт постоянен
            //if (version.IsLessThan(3, 0, 0))
            //{
            //    profile = 0;
            //}

            log(string.Format("профиль: {0}", profile));

            var retry = false;
#if HALF_NEXT
            DateTime date = start.Date.AddHours(start.Hour);
#else
            // Пример: старт был задан как 1:00 (= 1:30 + 2:00) тогда начальная дата 1:30
            DateTime date = start.Date.AddHours(start.Hour).AddMinutes(30);
            DateTime dateReadTo = end;//.AddHours(1);
#endif
            var deltaOffset = 0;

            var lastDateH = last.Date.Date.AddHours(last.Date.Hour);

            //сбор получасовок
            while (date <= dateReadTo)
            {
                if (cancel())
                {
                    archive.success = false;
                    archive.error = "опрос отменен";
                    archive.errorcode = DeviceError.NO_ERROR;
                    break;
                }

                if (date > lastDateH)
                {
                    log(string.Format("запись за {0:dd.MM.yyyy HH:mm} еще не сформирована", date));
                    break;
                }

                uint offset = (uint)(OffsetCalculator(date, deltaOffset));

                ////экспериментальным путем вычислены версии, где 17-й байт постоянен
                if (version.IsLessThan(3, 0, 0))
                {
                    profile = 0;
                }
                else
                {
                    profile = (byte)(offset >> 16);
                }

                var half = ParsePowerProfileResponse(Send(MakePowerProfileRequest(offset, 0x0f, profile)), variant.A);
                if (!half.success)
                {
                    archive.success = false;
                    archive.errorcode = half.errorcode;
                    archive.error = string.Format("получасовка {0:dd.MM.yyyy HH:mm} не прочитана - {1}", date, half.error);
                    break; //  06.02.2020  //continue; // 05.03.2019 //break;
                }

                if (!half.IsEmpty)//запись прочитана
                {
                    log(string.Format("прочитана запись {0:dd.MM.yyyy HH:mm}: A+={1} P+={2}", half.Date, half.ap1, half.pp1));
                    //continue; // 05.03.2019
                }

                if (!half.IsEmpty && half.Date == date)//запись прочитана верно
                {
                    /*расчет часовых сразу*/
                    if (half.Date.Minute == 00)// расчёт часовых значений
                    {
                        /*
                        foreach (var record in half.records)
                        {
                         var second = halfs.Where(r => r.s1 == record.s1 && r.date == record.date.AddMinutes(30)).FirstOrDefault();
                        if (second != null)
                        {
                            var hour = MakeHourRecord(record.s1, record.d1, record.s2, record.date);
                            hour.d1 = (record.d1 + second.d1) / 2.0;
                            hours.Add(hour);
                            //OnSendMessage(string.Format("запись {0}",hour)); 
                        }
                            records(fullhour);
                        */
                    }
                    else// сохранение получасовок
                    {

                        //halfs.AddRange(half.records);
                    }

                    /*расчет часовых сразу - вместо*/
                    halfs.AddRange(half.records);

                    retry = false;
                }
                else
                {
                    if (!half.IsEmpty)//прочитана какая-то запись
                    {
                        deltaOffset += (int)(half.Date - date).TotalMinutes;
                    }
                    else//запись пуста
                    {
                        log(string.Format("пустая запись за {0:dd.MM.yy HH:mm}", date));
                    }

                    #region поиск в журнале вкл/выкл
                    //ищем дату в журнале вкл/выкл
                    log(string.Format("не найдена запись за {0:dd.MM.yyyy HH:mm}, анализ журнала вкл./выкл. прибора", date));

                    //чтение журнала вкл/выкл (если уже не был прочитан)
                    if (journal == null)
                    {
                        journal = new List<dynamic>();
                        for (byte i = 0; i < 10; i++)
                        {
                            // OnSendMessage(string.Format("[{0}]", string.Join(",", b.Select(bb => bb.ToString("X2")))));
                            var jr = ParseJournalResponse(Send(MakeJournalRequest(i)), last.Date);
                            if (!jr.success) return jr;
                            if (jr.IsEmpty)
                            {
                                log("пустая запись журнала вкл - выкл");
                            }
                            else
                            {
                                log(string.Format("запись журнала вкл {0:dd.MM.yy HH:mm} - выкл {1:dd.MM.yy HH:mm}", jr.TurnOn, jr.TurnOff));
                                if (jr.TurnOn != DateTime.MinValue)
                                {
                                    journal.Add(jr);
                                }
                            }
                        }
                    }


                    var x = journal.SelectMany(j => new DateTime[] { j.TurnOn, j.TurnOff }).OrderBy(d => d).ToList();

                    //поиск даты в журнале
                    bool onOff = false;
                    for (int i = 1; i < x.Count - 1; i += 2)
                    {
                        var off = x[i];
                        var on = x[i + 1];

                        //OnSendMessage(string.Format("диапазон отключения [{0:dd.MM.yy HH:mm},{1:dd.MM.yy HH:mm}]", off, on));
                        onOff = (off <= date) && (date < on);
                        if (onOff)
                        {
                            break;
                        }
                    }

                    if (onOff)
                    {
                        log(string.Format("запись {0:dd.MM.yy HH:mm} в журнале вкл/выкл, чтение отменено", date));
                        status[GetHourFromHalf(date)] = 1;
                    }
                    else
                    {
                        retry = !retry;
                        log(string.Format("запись {0:dd.MM.yy HH:mm} не найдена в счетчике и в журнале вкл/выкл", date));
                        if (!retry)
                        {
                            status[GetHourFromHalf(date)] = 2;
                        }
                    }
                    #endregion
                }

                if (!retry)
                {
                    date = date.AddMinutes(30);
                }
            }

            foreach (var record in halfs)
            {
                if (record.date.Minute == 00)
                {
#if HALF_NEXT
                    var second = halfs.Where(r => (r.s1 == record.s1) && (r.date == record.date.AddMinutes(30))).FirstOrDefault();
#else
                    // Пример: record.date = 1:00, тогда second = 0:30; hour.date должен быть 0:00!!!
                    var second = halfs.Where(r => (r.s1 == record.s1) && (r.date == record.date.AddMinutes(-30))).FirstOrDefault();
#endif
                    if (second != null)
                    {
#if HALF_NEXT
                        var hour = MakeHourRecord(record.s1, record.d1, record.s2, record.date);
#else
                        var hour = MakeHourRecord(record.s1, record.d1, record.s2, GetHourFromHalf(record.date));
#endif
                        hour.d1 = (record.d1 + second.d1) / 2.0;
                        //log(string.Format("добавлена запись за {0:dd.MM.yy HH:mm}: {1}", hour.date, hour.d1));
                        hours.Add(hour);
                        status[GetHourFromHalf(record.date)] = 0;

                        //log(string.Format("данные на {0:dd.MM.yy HH:mm} записаны в БД", GetHourFromHalf(record.date)));
                    }
                }
            }

            foreach (var stat in status)
            {
                //log(string.Format("запись {0:dd.MM.yy HH:mm} статус {1}", stat.Key, stat.Value));
                hours.Add(MakeHourRecord("Статус", stat.Value, "", stat.Key));
            }

            records(hours);
            //log(string.Format("Часовые Q+ ({0}):", hours.Count));
            //foreach (var data in hours)
            //{
            //    if (data.s1 == "Q+")
            //    {
            //        log(string.Format("{0:dd.MM.yyyy HH:mm} {1} = {2} {3}", data.date, data.s1, data.d1, data.s2));
            //    }
            //}


            archive.halfs = halfs;
            archive.records = hours;
            return archive;
        }
    }
}

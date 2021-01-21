using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        dynamic GetHours(DateTime start, DateTime end, DateTime current, dynamic constants)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var recs = new List<dynamic>();

            int maxProfiles = ((string)constants.aType).Contains("СЭТ-4ТМ.03М") ? 3 : 2;

            dynamic arr = GetArray(maxProfiles);
            if (!arr.success)
            {
                return arr;
            }

            log(string.Format("Наиболее подходящий массив профилей мощности с номером:{0} и периодом интегрирования:{1} минут.", arr.select, arr.TimeInterval));
            /*
             *  Как сейчас?
             *  Поиск подходящего профиля мощности - 30 или 60 минут
             *  Для каждой даты:
             *      Поиск адреса по дате
             *      Ожидание
             *      Запрос по адресу одной записи
             *  Конец
             *  
             * 
             * Надо
             * 
             *  Для каждой даты
             *      Если адрес неизвестен
             *          Поиск адреса по дате
             *          Ожидание
             *      Конец
             *      Запрос по адресу трёх записей
             *      
             *  Конец
             */

            if (arr.select < maxProfiles)
            {
                //2.4.3.6  Расширенное чтение текущего указателя массива профиля мощности  
                var requestCurrent = MakeRequestParameters(0x04, new byte[] { arr.select });
                var rspCurrent = Send(requestCurrent);
                if (!rspCurrent.success)
                {
                    return rspCurrent;
                }

                var dtCurrent = dtCurrentMemory(rspCurrent.Body, 0);
                //                        OnSendMessage(string.Format(" Время начала текущего среза: {0:dd.MM.yyyy HH:mm}", dtCurrent));
                uint currAddr = Helper.ToUInt16(rspCurrent.Body, 5);
                byte addrHigh = rspCurrent.Body[5];
                byte addrLow = rspCurrent.Body[6];
                //                        OnSendMessage(string.Format(" Адрес текущего среза:{0:X} и {1:X} => {2:X}",  addrHigh,addrLow,currAddr));

                //Ограничение на одновременное считывание часовых данных - устанавливаем 10 суток * 24 = 240 часов
                int countMax = 240;
                //Считывание профилей мощности

                var step = 1;
                UInt16 addrMemory = 0;

                for (var date0 = start.Date.AddHours(start.Hour); date0 < end; date0 = date0.AddHours(step))
                {
                    step = 0;

                    log($"-----------------------------------{date0}");
                    if (cancel())
                    {
                        archive.success = false;
                        archive.errorcode = DeviceError.NO_ERROR;
                        archive.error = "опрос отменен";
                        break;
                    }

                    if (date0 >= current)
                    {
                        log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date0));
                        break;
                    }

                    if (date0 >= dtCurrent)
                    {
                        log(string.Format("данные за {0:dd.MM.yyyy HH:mm} еще не собраны", date0));
                        break;
                    }

                    if (countMax == 0)
                    {
                        log(string.Format("Прерывание опроса по причине превышения числа одновременно считываемых данных. Последнее прочитанное за {0:dd.MM.yyyy HH:mm}. Считано {1} значений", date0, 240 - countMax));
                        break;
                    }

                    countMax--;

                    for (var i = 0; i < 3; i++)
                    {
                        if (_j != null)
                        {
                            var date = date0.AddHours(i);
                            if (isInJournal(date))
                            {
                                log(string.Format("Данных за {0:dd.MM.yyyy HH:mm} нет по причине выключения счетчика ", date));
                                // !!!
                                continue;
                            }
                        }
                    }

                    var readCount = (byte)(arr.TimeInterval == 30 ? 0x18 : 0x10);

                    //log(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm} ", date));

                    
                    byte[] aMemory = new byte[] { 0x02, 0x03, 0x08, 0x09 };

                    if (addrMemory == 0)
                    {
                        step = 1;
                        //2.3.1.23  Поиск адреса заголовка массива профиля мощности
                        var request = MakeRequestOnWriteParameters(0x28, new byte[] { arr.select, 0xFF, 0xFF, Helper.IntToBinDec(date0.Hour), Helper.IntToBinDec(date0.Day), Helper.IntToBinDec(date0.Month), Helper.IntToBinDec(date0.Year - 2000), 0xFF, arr.TimeInterval });
                        var rspAddrRequest = Send(request);
                        if (!rspAddrRequest.success)
                        {
                            return rspAddrRequest;
                        }

                        dynamic rspAddress;

                        var inProcess = true;
                        do
                        {
                            rspAddress = Send(MakeRequestParameters(0x18, new byte[] { 0x00 })); // 2.4.3.31.1  Чтение слова состояния задачи поиска адреса заголовка массива профиля
                            if (!rspAddress.success) return rspAddress;

                            if (rspAddress.Body[0] == 0)
                            {
                                inProcess = false;
                            }
                            else if (rspAddress.Body[0] > 1)
                            {
                                rspAddress.errorcode = DeviceError.DEVICE_EXCEPTION;
                                rspAddress.success = false;
                                switch ((byte)rspAddress.Body[0])   
                                {
                                    case 0x2:
                                        rspAddress.error = "Запрошенный заголовок не найден";  
                                        break;
                                    case 0x3:
                                        rspAddress.error = "Внутренняя аппаратная ошибка счетчика. Не отвечает память указателя поиска";
                                        break;
                                    case 0x4:
                                        rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы указателя поиска";
                                        break;
                                    case 0x5:
                                        rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы дескриптора поиска";
                                        break;
                                    case 0x6:
                                        rspAddress.error = "Внутренняя аппаратная ошибка счетчика. Не отвечает память массива профиля";
                                        break;
                                    case 0x7:
                                        rspAddress.error = "Внутренняя логическая ошибка счетчика. Ошибка контрольной суммы заголовка в массиве профиля";
                                        break;
                                    case 0x8:
                                        rspAddress.error = "Внутренняя логическая ошибка счетчика. Заголовок находится по адресу, где должна быть запись среза";
                                        break;
                                    case 0x9:
                                        rspAddress.error = "Недопустимый номер массива поиска";
                                        break;
                                    case 0xA:
                                        rspAddress.error = "Недопустимое время интегрирования профиля мощности в дескрипторе запроса (не соответствует времени интегрирования счетчика)";
                                        break;
                                    default:
                                        rspAddress.error = string.Format("Неизвестная ошибка {0:X}h", rspAddress.Body[0]);
                                        break;
                                }
                                inProcess = false;
                            }
                        }
                        while (inProcess);

                        if (!rspAddress.success)
                        {
                            log(string.Format("Ошибка.Данные за {0:dd.MM.yyyy HH:mm} не найдены в профилях мощности счетчика.", date0));
                            if (isInJournal(date0))
                            {
                                log(string.Format("Данных за {0:dd.MM.yyyy HH:mm} нет по причине выключения счетчика ", date0));
                            }
                        }
                        else
                        {
                            addrMemory = (UInt16)(((UInt16)rspAddress.Body[3] << 8) | rspAddress.Body[4]);
                        }
                    }
                    // 2.4.4.2 Расширенный запрос на чтение информации по физическим адресам физиче-ской памяти
                    var rsp = Send(MakeRequestProfiles(0x00, aMemory[arr.select + 1], (byte)(addrMemory >> 8), (byte)(addrMemory & 0xFF), (byte)(readCount * 3)));
                    //var rsp = ReadProfiles(arr.select, date, readCount, arr.TimeInterval);
                    if (rsp.success)
                    {
                        //  DateTime dateReaded0 = dtfromCounter(rsp.Body, 1);
                        DateTime dateReadedi = dtfromCounter(rsp.Body, 1);
                        int count = 0;

                        for (var i = 0; i < 3; i++)
                        {
                            //var dateReaded = dateReaded0.AddHours(i);
                           
                            var dateReaded = dtfromCounter(rsp.Body, (byte)(1 + i * (8 + 8 * 60/ arr.TimeInterval)));

                            var date = date0.AddHours(i);

                            log($"{date0}-----------------------------------{i}");
                            if (date >= current)
                            {
                                break;
                            }

                            if (date >= dtCurrent)
                            {
                                break;
                            }
                            log($"dateReaded:{dateReaded}   date:{ date}");
                            if (dateReaded == date)
                            {
                                var currec = new List<dynamic>();
                                //стр 109 2.4.3.17
                                if (arr.TimeInterval == 30)
                                {
                                    var offset = i * 0x18;
                                    currec.Add(MakeHourRecord("P+", (PQ(rsp.Body, offset + 9, constants.constA, arr.TimeInterval) + PQ(rsp.Body, offset + 17, constants.constA, arr.TimeInterval)) / 2.0, "кВт", date));
                                    currec.Add(MakeHourRecord("P-", (PQ(rsp.Body, offset + 11, constants.constA, arr.TimeInterval) + PQ(rsp.Body, offset + 19, constants.constA, arr.TimeInterval)) / 2.0, "кВт", date));
                                    currec.Add(MakeHourRecord("Q+", (PQ(rsp.Body, offset + 13, constants.constA, arr.TimeInterval) + PQ(rsp.Body, offset + 21, constants.constA, arr.TimeInterval)) / 2.0, "кВт", date));
                                    currec.Add(MakeHourRecord("Q-", (PQ(rsp.Body, offset + 15, constants.constA, arr.TimeInterval) + PQ(rsp.Body, offset + 23, constants.constA, arr.TimeInterval)) / 2.0, "кВт", date));
                                }
                                else
                                {
                                    var offset = i * 0x10;
                                    currec.Add(MakeHourRecord("P+", PQ(rsp.Body, offset + 9, constants.constA, arr.TimeInterval), "кВт", date));
                                    currec.Add(MakeHourRecord("P-", PQ(rsp.Body, offset + 11, constants.constA, arr.TimeInterval), "кВт", date));
                                    currec.Add(MakeHourRecord("Q+", PQ(rsp.Body, offset + 13, constants.constA, arr.TimeInterval), "кВт", date));
                                    currec.Add(MakeHourRecord("Q-", PQ(rsp.Body, offset + 15, constants.constA, arr.TimeInterval), "кВт", date));
                                }

                                currec.Add(MakeHourRecord("Статус", 0, "", date));

                                records(currec);
                                log(string.Format("часовые данные за {0:dd.MM.yyyy HH:mm} P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}", date, currec[0].d1, currec[1].d1, currec[2].d1, currec[3].d1));
                                recs.AddRange(currec);
                                count++;
                            }
                            else
                            {
                                log(string.Format("Ошибка.Прочитаны данные за {0:dd.MM.yyyy HH:mm} вместо ожидаемых за {1:dd.MM.yyyy HH:mm}", dateReaded, date));
                                if (isInJournal(date))
                                {
                                    log(string.Format("Данных за {0:dd.MM.yyyy HH:mm} нет по причине выключения счетчика ", date));

                                    var currec = new List<dynamic>();
                                    currec.Add(MakeHourRecord("Статус", 1, "", date));
                                    records(currec);
                                }
                                else
                                {

                                }
                            }

                            log($"{date0}+++++++++++++++++++++++++++++++++++++++++++{i}");
                        }

                        if (count > 0)
                        {
                            step = count;
                        }
                    }
                    else
                    {
                        log(string.Format("Ошибка.Данные за {0:dd.MM.yyyy HH:mm}  не найдены в профилях мощности счетчика.", date0));
                        if (isInJournal(date0))
                        {
                            log(string.Format("Данных за {0:dd.MM.yyyy HH:mm} нет по причине выключения счетчика ", date0));
                        }
                    }

                    if(step == 0)
                    {
                        addrMemory = 0;
                    }
                    else
                    {
                        addrMemory += (UInt16)(step * readCount);
                    }
                }
            }


            //log(string.Format("Часовые Q+ ({0}):", hours.Count));
            //foreach (var data in hours)
            //{
            //    if (data.s1 == "Q+")
            //    {
            //        log(string.Format("{0:dd.MM.yyyy HH:mm} {1} = {2} {3}", data.date, data.s1, data.d1, data.s2));
            //    }
            //}

            archive.records = recs;
            return archive;
        }

        //2.4.3.10  Расширенное чтение времени интегрирования мощности для массива профиля
        dynamic GetArray(int maxProfile)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.select = maxProfile; //нет такого массива профиля
            //2.4.3.10  Расширенное чтение времени интегрирования мощности для массива профиля

            byte[] aTimeInterval = new byte[maxProfile];

            byte maxN = 0;
            byte maxTI = 0;
            for (byte nArray = 0; nArray < maxProfile; nArray++)
            {
                var rsp = Send(MakeRequestParameters(0x06, new byte[] { nArray }));
                if (!rsp.success) return rsp;
                aTimeInterval[nArray] = rsp.Body[1];
                if (aTimeInterval[nArray] <= 60 && aTimeInterval[nArray] > maxTI)
                {
                    maxN = nArray;
                    maxTI = aTimeInterval[nArray];
                }
            }

            //выбираем массив профилей не большее часа и менее отличаюшейся от часа 
            /*for (byte nArray = 0; nArray < maxProfile; nArray++)
                if (aTimeInterval[nArray] <= 60)
                {
                    answer.select = nArray;
                    answer.TimeInterval = aTimeInterval[answer.select];
                }*/
            if (maxTI > 0)
            {
                answer.select = maxN;
                answer.TimeInterval = maxTI;
            }

            if (answer.select >= maxProfile)
            //{
            //    byte minTime = (byte)(60 - aTimeInterval[answer.select]);
            //    for (byte nArray = 0; nArray < maxProfile; nArray++)
            //    {
            //        if ((minTime > aTimeInterval[nArray]) && (aTimeInterval[nArray] <= 60))
            //        {
            //            answer.select = nArray;
            //            minTime = (byte)(60 - aTimeInterval[answer.select]);
            //            answer.TimeInterval = aTimeInterval[answer.select];
            //        }
            //    }
            //}
            //else
            {
                answer.success = false;
                answer.error = string.Format("Нет профилей мощности с периодои интегрирования менее или равно 60 минут. Чтение часовых профилей невозможно.");
                answer.errorcode = DeviceError.NO_ERROR;
            }
            return answer;
        }

    }

}

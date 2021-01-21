using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;

namespace Matrix.Poll.Driver.Milur107
{
    public partial class Driver
    {
        #region GetHour
        private dynamic GetHours(DateTime startDate, DateTime endDate)
        {
            if (ModelVersion.Contains("107"))
            {
                int sMin = startDate.Minute;
                int sHour = startDate.Hour;
                int sDay = startDate.Day;
                int sMonth = startDate.Month;
                int sYear = startDate.Year - 2000;
                int eMin = endDate.Minute;
                int eHour = endDate.Hour;
                int eDay = endDate.Day;
                int eMonth = endDate.Month;
                int eYear = endDate.Year - 2000;
                int n = 0;
                if (sMin == 0) sMin = 30;
                byte[] bytes = new byte[] { Convert.ToByte(sMin), Convert.ToByte(sHour), Convert.ToByte(sDay), Convert.ToByte(sMonth), Convert.ToByte(sYear), Convert.ToByte(eMin), Convert.ToByte(eHour), Convert.ToByte(eDay), Convert.ToByte(eMonth), Convert.ToByte(eYear) };
                dynamic dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                if (!dt.success)
                {
                    return dt;
                }
                while (n < 3)
                {
                    switch ((int)dt.Body[0])
                    {
                        case 1:
                            log($"Запрос на поиск индексов принят, начат поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                            if (!dt.success)
                            {
                                return dt;
                            }
                            break;
                        case 2:
                            log($"Процесс поиска индексов активен, идет поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                            if (!dt.success)
                            {
                                return dt;
                            }
                            break;
                        case 3:
                            log($"Найдены более ранние ранние записи");
                            n = 3;
                            break;
                        case 4:
                            log($"Найдены более поздние записи");
                            n = 3;
                            break;
                        case 5:
                            log($"Найдены записи вне диапозона");
                            n = 3;
                            break;
                        case 6:
                            log($"Список пустой");
                            n = 3;
                            break;
                        case 7:
                            log($"Ошибка при работе со списком");
                            n = 3;
                            break;
                        default:
                            n = 3;
                            break;
                    }
                    n++;
                }

                int startIndex = dt.Body[2] << 8 | dt.Body[1];
                int endeIndex = dt.Body[4] << 8 | dt.Body[3];
                log($"Получен начальный индекс: {startIndex} ");
                log($"Получен конечный индекс: {endeIndex} ");

                log($"Считаем PerfectTime");
                List<DateTime> listPerfectTime = new List<DateTime>();
                DateTime tmpStart = startDate;
                listPerfectTime.Add(tmpStart);
                for (int i = 0; tmpStart < endDate; i++)
                {
                    tmpStart = tmpStart.AddMinutes(30);
                    listPerfectTime.Add(tmpStart);
                }

                List<dynamic> listHalfHour = new List<dynamic>();
                List<dynamic> listFullHour = new List<dynamic>();

                dynamic answer = new ExpandoObject();
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                while (startIndex <= endeIndex)
                {
                    if (cancel())
                    {
                        answer.success = false;
                        answer.error = "опрос отменен";
                        answer.errorcode = DeviceError.NO_ERROR;
                        return answer;
                    }

                    byte b0 = (byte)(startIndex & 0xFF);
                    byte b1 = (byte)(startIndex >> 8);
                    byte[] addBytes = new byte[] { b0, b1 };
                    dynamic oneHalfRecord = new ExpandoObject();
                    dt = Send(MakePackage(0x0D, 0x10, 0xff, addBytes), 0x0D);
                    if (!dt.success)
                    {
                        return dt;
                    }
                    
                    DateTime date = new DateTime((int)dt.Body[4] + 2000, (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], (int)dt.Body[0], 0);

                    //double activeEnergy = (double)(dt.Body[6] << 8 | dt.Body[5]) / 5000;
                    //double reactiveEnergy = (double)(dt.Body[8] << 8 | dt.Body[7]) / 5000;

                    double activeEnergy = (double)BitConverter.ToUInt16(dt.Body, 5) / 5000;
                    double reactiveEnergy = (double)BitConverter.ToUInt16(dt.Body, 7) / 5000;

                    log($"Получена запись с индексом {startIndex}: {date} : активная энергия {activeEnergy} кВт*ч, реактивная энергия {reactiveEnergy} квар*ч");

                    oneHalfRecord.date = date;
                    oneHalfRecord.valueA = activeEnergy;
                    oneHalfRecord.valueR = reactiveEnergy;
                    listHalfHour.Add(oneHalfRecord);
                    startIndex++;
                }
                //log($"Складываем получасовки");
                for (int i = 0, j = 0; i < listPerfectTime.Count - 1 && j < listHalfHour.Count - 1;)
                {
                    dynamic oneFullRecord = new ExpandoObject();
                    if (listPerfectTime[i] == listHalfHour[j].date && listPerfectTime[i + 1] == listHalfHour[j + 1].date)
                    {
                        if (listHalfHour[j].date.Minute == 30)
                        {
                            oneFullRecord.date = listHalfHour[j + 1].date;
                            oneFullRecord.valueA = (listHalfHour[j].valueA + listHalfHour[j + 1].valueA) / 2;
                            oneFullRecord.valueR = (listHalfHour[j].valueR + listHalfHour[j + 1].valueR) / 2;
                            oneFullRecord.status = 0;
                            listFullHour.Add(oneFullRecord);
                            j += 2;
                            i += 2;
                        }
                        else
                        {
                            i++; j++;
                        }
                    }
                    else // лезем в журнал вкл. и выкл.
                    {
                        if (listHalfHour[j].date > listPerfectTime[i])
                        {
                            i += 2;
                        }
                        else if (listHalfHour[j].date < listPerfectTime[i])
                        {
                            if (listHalfHour[j].date.Minute == 0) j++;
                            else if (listHalfHour[j].date.Minute == 30) j += 2;
                        }
                        else if (listHalfHour[j+1].date > listPerfectTime[i+1])
                        {
                            i += 2; j++;
                        }
                        if (listPerfectTime[i + 1].Minute == 0)
                        {
                            if (CheckBetween(listPerfectTime[i]))
                            {
                                oneFullRecord.date = listPerfectTime[i + 1];
                                oneFullRecord.status = 1;
                                listFullHour.Add(oneFullRecord);
                            }
                            else
                            {
                                oneFullRecord.date = listPerfectTime[i + 1];
                                oneFullRecord.status = 2;
                                listFullHour.Add(oneFullRecord);
                            }
                        }
                    }
                }

                foreach (var item in listFullHour)
                {
                    DateTime dateTime = item.date;
                    dateTime = dateTime.AddHours(-1);
                    if (item.status == 0)
                    {

                        answer.records.Add(MakeHourRecord(hourP, item.valueA, "кВт*ч", dateTime));
                        answer.records.Add(MakeHourRecord(hourQ, item.valueR, "кВт*ч", dateTime));
                        answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                    }
                    else
                    {
                        answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                    }
                }
                return answer;
            }

            if (ModelVersion.Contains("307"))
            {
                int sMin = startDate.Minute;
                int sHour = startDate.Hour;
                int sDay = startDate.Day;
                int sMonth = startDate.Month;
                int sYear = startDate.Year - 2000;
                int eMin = endDate.Minute;
                int eHour = endDate.Hour;
                int eDay = endDate.Day;
                int eMonth = endDate.Month;
                int eYear = endDate.Year - 2000;
                int n = 0;
                if (sMin == 0) sMin = 30;
                byte[] bytes = new byte[] { Convert.ToByte(sMin), Convert.ToByte(sHour), Convert.ToByte(sDay), Convert.ToByte(sMonth), Convert.ToByte(sYear), Convert.ToByte(eMin), Convert.ToByte(eHour), Convert.ToByte(eDay), Convert.ToByte(eMonth), Convert.ToByte(eYear) };
                dynamic dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                if (!dt.success)
                {
                    return dt;
                }
                while (n < 3)
                {
                    switch ((int)dt.Body[0])
                    {
                        case 1:
                            log($"Запрос на поиск индексов принят, начат поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                            if (!dt.success)
                            {
                                return dt;
                            }
                            break;
                        case 2:
                            log($"Процесс поиска индексов активен, идет поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x10, 0xff, bytes), 0x10);
                            if (!dt.success)
                            {
                                return dt;
                            }
                            break;
                        case 3:
                            log($"Найдены более ранние ранние записи");
                            n = 3;
                            break;
                        case 4:
                            log($"Найдены более поздние записи");
                            n = 3;
                            break;
                        case 5:
                            log($"Найдены записи вне диапозона");
                            n = 3;
                            break;
                        case 6:
                            log($"Список пустой");
                            n = 3;
                            break;
                        case 7:
                            log($"Ошибка при работе со списком");
                            n = 3;
                            break;
                        default:
                            n = 3;
                            break;
                    }
                    n++;
                }

                int startIndex = dt.Body[2] << 8 | dt.Body[1];
                int endeIndex = dt.Body[4] << 8 | dt.Body[3];
                log($"Получен начальный индекс: {startIndex} ");
                log($"Получен конечный индекс: {endeIndex} ");

                log($"Считаем PerfectTime");
                List<DateTime> listPerfectTime = new List<DateTime>();
                DateTime tmpStart = startDate.AddMinutes(sMin);
                listPerfectTime.Add(tmpStart);
                for (int i = 0; tmpStart < endDate; i++)
                {
                    tmpStart = tmpStart.AddMinutes(30);
                    listPerfectTime.Add(tmpStart);
                }

                List<dynamic> listHalfHour = new List<dynamic>();
                List<dynamic> listFullHour = new List<dynamic>();

                dynamic answer = new ExpandoObject();
                answer.success = true;
                answer.error = string.Empty;
                answer.errorcode = DeviceError.NO_ERROR;
                answer.records = new List<dynamic>();

                int softWareVersion = Int32.Parse(SoftwareVersion);

                if ((softWareVersion >= 300 && softWareVersion <= 399) || (softWareVersion >= 100 && softWareVersion <= 199))
                {
                    while (startIndex <= endeIndex)
                    {
                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        byte b0 = (byte)(startIndex & 0xFF);
                        byte b1 = (byte)(startIndex >> 8);
                        byte[] addBytes = new byte[] { b0, b1 };
                        dynamic oneHalfRecord = new ExpandoObject();
                        dt = Send(MakePackage(0x0D, 0x10, 0xff, addBytes), 0x0D);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        DateTime date = new DateTime((int)dt.Body[4] + 2000, (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], (int)dt.Body[0], 0);

                        double ActiveEnergy = ConvertFromBcd(dt.Body[5]) * 0.01 + ConvertFromBcd(dt.Body[6]) * 1 + ConvertFromBcd(dt.Body[7]) * 100 + ConvertFromBcd(dt.Body[8]) * 10000;
                        //answer.records.Add(MakeHourRecord("Суммарная активная энергия", ActiveEnergy, "кВт*ч", date));

                        double ReactiveEnergy = ConvertFromBcd(dt.Body[9]) * 0.01 + ConvertFromBcd(dt.Body[10]) * 1 + ConvertFromBcd(dt.Body[11]) * 100 + ConvertFromBcd(dt.Body[12]) * 10000;
                        //answer.records.Add(MakeHourRecord("Суммарная реактивная энергия", ReactiveEnergy, "квар*ч", date));

                        log($"Получена запись с индексом {startIndex}: {date} : активная энергия {ActiveEnergy} кВт*ч : реактивная энергия {ReactiveEnergy} квар*ч");

                        oneHalfRecord.date = date;
                        oneHalfRecord.valueAct = ActiveEnergy;
                        oneHalfRecord.valueReac = ReactiveEnergy;
                        listHalfHour.Add(oneHalfRecord);
                        startIndex++;
                    }
                    //log($"Складываем получасовки");
                    for (int i = 0, j = 0; i < listPerfectTime.Count - 1 && j < listHalfHour.Count - 1;)
                    {
                        dynamic oneFullRecord = new ExpandoObject();
                        if (listPerfectTime[i] == listHalfHour[j].date && listPerfectTime[i + 1] == listHalfHour[j + 1].date)
                        {
                            if (listHalfHour[j].date.Minute == 30)
                            {
                                oneFullRecord.date = listHalfHour[j + 1].date;
                                oneFullRecord.valueAct = (listHalfHour[j].valueAct + listHalfHour[j + 1].valueAct()) / 2;
                                oneFullRecord.valueReac = listHalfHour[j].valueReac + listHalfHour[j + 1].valueReac;
                                oneFullRecord.status = 0;
                                listFullHour.Add(oneFullRecord);
                                j += 2;
                                i += 2;
                            }
                            else
                            {
                                i++; j++;
                            }
                        }
                        else // лезем в журнал вкл. и выкл.
                        {
                            if (listHalfHour[j].date > listPerfectTime[i])
                            {
                                i += 2;
                            }
                            else if (listHalfHour[j].date < listPerfectTime[i])
                            {
                                if (listHalfHour[j].date.Minute == 00) j++;
                                else if (listHalfHour[j].date.Minute == 30) j += 2;
                            }
                            else if (listHalfHour[j + 1].date > listPerfectTime[i + 1])
                            {
                                i += 2; j++;
                            }
                            if (listPerfectTime[i + 1].Minute == 0)
                            {
                                if (CheckBetween(listPerfectTime[i]))
                                {
                                    oneFullRecord.date = listPerfectTime[i + 1];
                                    oneFullRecord.status = 1;
                                    listFullHour.Add(oneFullRecord);
                                }
                                else
                                {
                                    oneFullRecord.date = listPerfectTime[i + 1];
                                    oneFullRecord.status = 2;
                                    listFullHour.Add(oneFullRecord);
                                }
                            }
                        }
                    }

                    foreach (var item in listFullHour)
                    {
                        DateTime dateTime = item.date;
                        dateTime = dateTime.AddHours(-1);
                        if (item.status == 0)
                        {
                            
                            answer.records.Add(MakeHourRecord(hourP, item.valueAct, "кВт*ч", dateTime));
                            answer.records.Add(MakeHourRecord(hourQ, item.valueReac, "квар*ч", dateTime));
                            answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                        }
                        else
                        {
                            answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                        }
                    }
                    return answer;
                }
                else if ((softWareVersion >= 400 && softWareVersion <= 499) || (softWareVersion >= 200 && softWareVersion <= 299))
                {
                    while (startIndex <= endeIndex)
                    {
                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        byte b0 = (byte)(startIndex & 0xFF);
                        byte b1 = (byte)(startIndex >> 8);
                        byte[] addBytes = new byte[] { b0, b1 };
                        dynamic oneHalfRecord = new ExpandoObject();
                        dt = Send(MakePackage(0x0D, 0x10, 0xff, addBytes), 0x0D);
                        if (!dt.success)
                        {
                            return dt;
                        }
                       
                        DateTime date = new DateTime((int)dt.Body[4] + 2000, (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], (int)dt.Body[0], 0);

                        double ImpActiveEnergy = ConvertFromBcd(dt.Body[6]) * 0.01 + ConvertFromBcd(dt.Body[7]) * 1 + ConvertFromBcd(dt.Body[8]) * 100 + ConvertFromBcd(dt.Body[9]) * 10000;
                        log($"Получена запись с индексом {startIndex}: {date} : Суммарная импортируемая активная энергия {ImpActiveEnergy}");
                        //double ExpActiveEnergy = ConvertFromBcd(dt.Body[10]) * 0.01 + ConvertFromBcd(dt.Body[11]) * 1 + ConvertFromBcd(dt.Body[12]) * 100 + ConvertFromBcd(dt.Body[13]) * 10000;
                        double ImpReactiveEnergy = ConvertFromBcd(dt.Body[14]) * 0.01 + ConvertFromBcd(dt.Body[15]) * 1 + ConvertFromBcd(dt.Body[16]) * 100 + ConvertFromBcd(dt.Body[17]) * 10000;
                        //double ExpReactiveEnergy = ConvertFromBcd(dt.Body[18]) * 0.01 + ConvertFromBcd(dt.Body[19]) * 1 + ConvertFromBcd(dt.Body[20]) * 100 + ConvertFromBcd(dt.Body[21]) * 10000;

                        oneHalfRecord.date = date;
                        oneHalfRecord.valuePI = ImpActiveEnergy;
                        //oneHalfRecord.valuePE = ExpActiveEnergy;
                        oneHalfRecord.valueQI = ImpReactiveEnergy;
                        //oneHalfRecord.valueQE = ExpReactiveEnergy;
                        listHalfHour.Add(oneHalfRecord);
                        startIndex++;
                    }
                    //log($"Складываем получасовки");
                    for (int i = 0, j = 0; i < listPerfectTime.Count - 1 && j < listHalfHour.Count - 1;)
                    {
                        dynamic oneFullRecord = new ExpandoObject();
                        if (listPerfectTime[i] == listHalfHour[j].date && listPerfectTime[i + 1] == listHalfHour[j + 1].date)
                        {
                            if (listHalfHour[j].date.Minute == 30)
                            {
                                oneFullRecord.date = listHalfHour[j + 1].date;
                                oneFullRecord.valuePI = (listHalfHour[j].valuePI + listHalfHour[j + 1].valuePI) / 2;
                                //oneFullRecord.valuePE = listHalfHour[j].valuePE + listHalfHour[j + 1].valuePE;
                                oneFullRecord.valueQI = listHalfHour[j].valueQI + listHalfHour[j + 1].valueQI;
                               // oneFullRecord.valueQE = listHalfHour[j].valueQE + listHalfHour[j + 1].valueQE;
                                oneFullRecord.status = 0;
                                listFullHour.Add(oneFullRecord);
                                j += 2;
                                i += 2;
                            }
                            else
                            {
                                i++; j++;
                            }
                        }
                        else // лезем в журнал вкл. и выкл.
                        {
                            if (listHalfHour[j].date > listPerfectTime[i])
                            {
                                i += 2;
                            }
                            else if (listHalfHour[j].date < listPerfectTime[i])
                            {
                                if (listHalfHour[j].date == 00) j++;
                                else if (listHalfHour[j].date == 30) j += 2;
                            }
                            else if (listHalfHour[j + 1].date > listPerfectTime[i + 1])
                            {
                                i += 2; j++;
                            }
                            if (listPerfectTime[i + 1].Minute == 0)
                            {
                                if (CheckBetween(listPerfectTime[i]))
                                {
                                    oneFullRecord.date = listPerfectTime[i + 1];
                                    oneFullRecord.status = 1;
                                    listFullHour.Add(oneFullRecord);
                                }
                                else
                                {
                                    oneFullRecord.date = listPerfectTime[i + 1];
                                    oneFullRecord.status = 2;
                                    listFullHour.Add(oneFullRecord);
                                }
                            }
                        }
                    }

                    foreach (var item in listFullHour)
                    {
                        DateTime dateTime = item.date;
                        dateTime = dateTime.AddHours(-1);

                        if (item.status == 0)
                        {
                            answer.records.Add(MakeHourRecord(hourP, item.valuePI, "кВт*ч", dateTime));
                            //answer.records.Add(MakeHourRecord("P-", item.valuePE, "кВт*ч", dateTime));
                            answer.records.Add(MakeHourRecord(hourQ, item.valueQI, "квар*ч", dateTime));
                            //answer.records.Add(MakeHourRecord("Q-", item.valueQE, "квар*ч", dateTime));
                            answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                        }
                        else
                        {
                            answer.records.Add(MakeHourRecord("Статус", item.status, "", dateTime));
                        }
                    }
                    return answer;
                }
                else
                {
                    log($"Для модели счетичика  {ModelVersion} с версией прошивки {SoftwareVersion} чтение часовых не реализовано");
                }
                
            }
            dynamic ans = new ExpandoObject();
            ans.success = false;
            ans.error = "для данной прошивки чтение часовых не реализовано";
            ans.errorcode = DeviceError.NO_ANSWER;
            return ans;
        }
        private bool CheckBetween(DateTime date)
        {
            for (int i = 0; i < listJournalOnOff.Count(); i++)
            {
                if (date > listJournalOnOff[i].dateOff && date < listJournalOnOff[i].dateOn) return true;
            }
            return false;
        }   
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;

namespace Matrix.Poll.Driver.Milur107
{
    public partial class Driver
    {
        private dynamic GetDays(DateTime startDate, DateTime endDate)
        {
            if (ModelVersion.Contains("107"))
            {
                int sHour = startDate.Hour;
                int sDay = startDate.Day;
                int sMonth = startDate.Month;
                int sYear = startDate.Year - 2000;
                int eHour = endDate.Hour;
                int eDay = endDate.Day;
                int eMonth = endDate.Month;
                int eYear = endDate.Year - 2000;
                int n = 0;
                byte[] bytes = new byte[] { 0, Convert.ToByte(sHour), Convert.ToByte(sDay), Convert.ToByte(sMonth), Convert.ToByte(sYear), 0, Convert.ToByte(eHour), Convert.ToByte(eDay), Convert.ToByte(eMonth), Convert.ToByte(eYear) };
                dynamic dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
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
                            dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
                            break;
                        case 2:
                            log($"Процесс поиска индексов активен, идет поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
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
                    dt = Send(MakePackage(0x0C, 0x42, 0xff, addBytes), 0x0C);
                    if (!dt.success)
                    {
                        continue;
                    }
                    DateTime date1 = new DateTime((int)dt.Body[5] + 2000, (int)dt.Body[4], (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], 0);
                    DateTime date = date1.AddDays(-1);

                    double sumActiveEnergy = ConvertFromBcd(dt.Body[6]) * 0.01 + ConvertFromBcd(dt.Body[7]) * 1 + ConvertFromBcd(dt.Body[8]) * 100 + ConvertFromBcd(dt.Body[9]) * 10000;
                    answer.records.Add(MakeDayRecord(dayP, sumActiveEnergy, "кВт*ч", date));

                    for (int i = 1; i <= 4; i++)
                    {
                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        double ActiveEnergy = ConvertFromBcd(dt.Body[6 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[7 + i * 4]) * 1 + ConvertFromBcd(dt.Body[8 + i * 4]) * 100 + ConvertFromBcd(dt.Body[9 + i * 4]) * 10000;
                        answer.records.Add(MakeDayRecord(dayArrP[i-1], ActiveEnergy, "кВт*ч", date));
                        //log($"индекс {i}: {ActiveEnergy}");

                    }

                    double sumReactiveEnergy = ConvertFromBcd(dt.Body[26]) * 0.01 + ConvertFromBcd(dt.Body[27]) * 1 + ConvertFromBcd(dt.Body[28]) * 100 + ConvertFromBcd(dt.Body[29]) * 10000;
                    answer.records.Add(MakeDayRecord(dayQ, sumReactiveEnergy, "квар*ч", date));

                    log($"Получена запись с индексом {startIndex}: {date} : активная энергия {sumActiveEnergy} кВт*ч, реактивная энергия {sumReactiveEnergy} квар*ч");

                    for (int i = 1; i <= 4; i++)
                    {
                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        double ReactiveEnergy = ConvertFromBcd(dt.Body[26 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[27 + i * 4]) * 1 + ConvertFromBcd(dt.Body[28 + i * 4]) * 100 + ConvertFromBcd(dt.Body[29 + i * 4]) * 10000;
                        answer.records.Add(MakeDayRecord(dayArrQ[i-1], ReactiveEnergy, "квар*ч", date));
                    }

                    startIndex++;
                }
                return answer;
            }
            if (ModelVersion.Contains("307"))
            {
                int sHour = startDate.Hour;
                int sDay = startDate.Day;
                int sMonth = startDate.Month;
                int sYear = startDate.Year - 2000;
                int eHour = endDate.Hour;
                int eDay = endDate.Day;
                int eMonth = endDate.Month;
                int eYear = endDate.Year - 2000;
                int n = 0;
                byte[] bytes = new byte[] { 0, Convert.ToByte(sHour), Convert.ToByte(sDay), Convert.ToByte(sMonth), Convert.ToByte(sYear), 0, Convert.ToByte(eHour), Convert.ToByte(eDay), Convert.ToByte(eMonth), Convert.ToByte(eYear) };
                dynamic dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
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
                            dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
                            break;
                        case 2:
                            log($"Процесс поиска индексов активен, идет поиск. Повтор запроса через 2 секунды");
                            Thread.Sleep(2000);
                            dt = Send(MakePackage(0x10, 0x42, 0xff, bytes), 0x10);
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
                    dt = Send(MakePackage(0x0C, 0x42, 0xff, addBytes), 0x0C);
                    if (!dt.success)
                    {
                        continue;
                    }
                    DateTime date1 = new DateTime((int)dt.Body[5] + 2000, (int)dt.Body[4], (int)dt.Body[3], (int)dt.Body[2], (int)dt.Body[1], 0);
                    DateTime date = date1.AddDays(-1);

                    if (Int32.TryParse(SoftwareVersion, out int softWareVersion)){
                        if((softWareVersion == 124) || (softWareVersion >= 300 && softWareVersion <= 399))
                        {
                            double sumActiveEnergy = ConvertFromBcd(dt.Body[6]) * 0.01 + ConvertFromBcd(dt.Body[7]) * 1 + ConvertFromBcd(dt.Body[8]) * 100 + ConvertFromBcd(dt.Body[9]) * 10000;
                            answer.records.Add(MakeDayRecord("EnergyP+ (сумма тарифов)", sumActiveEnergy, "кВт*ч", date));
                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ActiveEnergy = ConvertFromBcd(dt.Body[6 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[7 + i * 4]) * 1 + ConvertFromBcd(dt.Body[8 + i * 4]) * 100 + ConvertFromBcd(dt.Body[9 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyP+ (тариф {i})", ActiveEnergy, "кВт*ч", date));
                                //log($"индекс {i}: {ActiveEnergy}");
                            }

                            double sumReactiveEnergy = ConvertFromBcd(dt.Body[42]) * 0.01 + ConvertFromBcd(dt.Body[43]) * 1 + ConvertFromBcd(dt.Body[44]) * 100 + ConvertFromBcd(dt.Body[45]) * 10000;
                            answer.records.Add(MakeDayRecord("EnergyQ+ (сумма тарифов)", sumReactiveEnergy, "квар*ч", date));
                            log($"Получена запись с индексом {startIndex}: {date} : активная энергия {sumActiveEnergy} кВт*ч, реактивная энергия {sumReactiveEnergy} квар*ч");
                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ReactiveEnergy = ConvertFromBcd(dt.Body[42 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[43 + i * 4]) * 1 + ConvertFromBcd(dt.Body[44 + i * 4]) * 100 + ConvertFromBcd(dt.Body[45 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyQ+ (тариф {i})", ReactiveEnergy, "квар*ч", date));
                            }

                            startIndex++;
                        }
                        else if (softWareVersion == 123)
                        {
                            double sumActiveEnergy = ConvertFromBcd(dt.Body[6]) * 0.001 + ConvertFromBcd(dt.Body[7]) * 0.1 + ConvertFromBcd(dt.Body[8]) * 10 + ConvertFromBcd(dt.Body[9]) * 1000;
                            answer.records.Add(MakeDayRecord("EnergyP+ (сумма тарифов)", sumActiveEnergy, "кВт*ч", date));
                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ActiveEnergy = ConvertFromBcd(dt.Body[6 + i * 4]) * 0.001 + ConvertFromBcd(dt.Body[7 + i * 4]) * 0.1 + ConvertFromBcd(dt.Body[8 + i * 4]) * 10 + ConvertFromBcd(dt.Body[9 + i * 4]) * 1000;
                                answer.records.Add(MakeDayRecord($"EnergyP+ (тариф {i})", ActiveEnergy, "кВт*ч", date));
                                //log($"индекс {i}: {ActiveEnergy}");
                            }

                            double sumReactiveEnergy = ConvertFromBcd(dt.Body[42]) * 0.001 + ConvertFromBcd(dt.Body[43]) * 0.1 + ConvertFromBcd(dt.Body[44]) * 10 + ConvertFromBcd(dt.Body[45]) * 1000;
                            answer.records.Add(MakeDayRecord("EnergyQ+ (сумма тарифов)", sumReactiveEnergy, "квар*ч", date));
                            log($"Получена запись с индексом {startIndex}: {date} : активная энергия {sumActiveEnergy} кВт*ч, реактивная энергия {sumReactiveEnergy} квар*ч");
                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ReactiveEnergy = ConvertFromBcd(dt.Body[42 + i * 4]) * 0.001 + ConvertFromBcd(dt.Body[43 + i * 4]) * 0.1 + ConvertFromBcd(dt.Body[44 + i * 4]) * 10 + ConvertFromBcd(dt.Body[45 + i * 4]) * 1000;
                                answer.records.Add(MakeDayRecord($"EnergyQ+ (тариф {i})", ReactiveEnergy, "квар*ч", date));
                            }

                            startIndex++;
                        }
                        else if(softWareVersion >= 400 && softWareVersion <= 499)
                        {
                            if (cancel())
                            {
                                answer.success = false;
                                answer.error = "опрос отменен";
                                answer.errorcode = DeviceError.NO_ERROR;
                                return answer;
                            }

                            double sumImpActiveEnergy = ConvertFromBcd(dt.Body[6]) * 0.01 + ConvertFromBcd(dt.Body[7]) * 1 + ConvertFromBcd(dt.Body[8]) * 100 + ConvertFromBcd(dt.Body[9]) * 10000;
                            answer.records.Add(MakeDayRecord("EnergyP+ (сумма тарифов)", sumImpActiveEnergy, "кВт*ч", date));

                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ActiveEnergy = ConvertFromBcd(dt.Body[6 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[7 + i * 4]) * 1 + ConvertFromBcd(dt.Body[8 + i * 4]) * 100 + ConvertFromBcd(dt.Body[9 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyP+ (тариф {i})", ActiveEnergy, "кВт*ч", date));
                                log($"индекс {i}: {ActiveEnergy}");
                            }

                            double sumExpActiveEnergy = ConvertFromBcd(dt.Body[42]) * 0.01 + ConvertFromBcd(dt.Body[43]) * 1 + ConvertFromBcd(dt.Body[44]) * 100 + ConvertFromBcd(dt.Body[45]) * 10000;
                            answer.records.Add(MakeDayRecord($"EnergyP- (сумма тарифов)", sumExpActiveEnergy, "кВт*ч", date));
                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ExpActiveEnergy = ConvertFromBcd(dt.Body[42 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[43 + i * 4]) * 1 + ConvertFromBcd(dt.Body[44 + i * 4]) * 100 + ConvertFromBcd(dt.Body[45 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyP- (тариф {i})", ExpActiveEnergy, "кВт*ч", date));
                            }

                            double sumReactiveEnergy = ConvertFromBcd(dt.Body[78]) * 0.01 + ConvertFromBcd(dt.Body[79]) * 1 + ConvertFromBcd(dt.Body[80]) * 100 + ConvertFromBcd(dt.Body[81]) * 10000;
                            answer.records.Add(MakeDayRecord("EnergyQ+ (сумма тарифов)", sumReactiveEnergy, "квар*ч", date));

                            log($"Получена запись с индексом {startIndex}: {date} : активная энергия {sumImpActiveEnergy} кВт*ч, реактивная энергия {sumReactiveEnergy} квар*ч");

                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ReactiveEnergy = ConvertFromBcd(dt.Body[78 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[79 + i * 4]) * 1 + ConvertFromBcd(dt.Body[80 + i * 4]) * 100 + ConvertFromBcd(dt.Body[81 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyQ+ (тариф {i})", ReactiveEnergy, "квар*ч", date));
                            }

                            double sumExpReactiveEnergy = ConvertFromBcd(dt.Body[114]) * 0.01 + ConvertFromBcd(dt.Body[115]) * 1 + ConvertFromBcd(dt.Body[116]) * 100 + ConvertFromBcd(dt.Body[117]) * 10000;
                            answer.records.Add(MakeDayRecord("EnergyQ- (сумма тарифов)", sumExpReactiveEnergy, "квар*ч", date));

                            for (int i = 1; i <= 4; i++)
                            {
                                if (cancel())
                                {
                                    answer.success = false;
                                    answer.error = "опрос отменен";
                                    answer.errorcode = DeviceError.NO_ERROR;
                                    return answer;
                                }

                                double ReactiveEnergy = ConvertFromBcd(dt.Body[114 + i * 4]) * 0.01 + ConvertFromBcd(dt.Body[115 + i * 4]) * 1 + ConvertFromBcd(dt.Body[116 + i * 4]) * 100 + ConvertFromBcd(dt.Body[117 + i * 4]) * 10000;
                                answer.records.Add(MakeDayRecord($"EnergyQ- (тариф {i})", ReactiveEnergy, "квар*ч", date));
                            }
                            startIndex++;
                        }
                        else
                        {
                            log($"Для модели счетичика  {ModelVersion} с версией прошивки {SoftwareVersion} чтение суточных не реализовано");
                        }
                    }
                    
                }
                return answer;
            }

            dynamic ans = new ExpandoObject();
            ans.success = false;
            ans.error = "для данной прошивки чтение часовых не реализовано";
            ans.errorcode = DeviceError.NO_ANSWER;
            return ans;
        }

    }
}

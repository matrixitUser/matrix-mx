using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Matrix.Poll.Driver.Rim384
{
    public partial class Driver
    {
        #region GetCurrent
        private dynamic GetCurrent()
        {
            DateTime dateNow = DateTime.Now;
            if (ModelVersion.Contains("107"))
            {
                dynamic answer = new ExpandoObject();
                dynamic dt = Send(MakePackage(0x01, 0x04, 0xff), 0x01);
                if (!dt.success)
                {
                    log($"Не удалось прочитать: Энергия активная суммарная");
                }
                else
                {
                    answer.success = true;
                    answer.error = string.Empty;
                    answer.errorcode = DeviceError.NO_ERROR;
                    answer.records = new List<dynamic>();

                    if (cancel())
                    {
                        answer.success = false;
                        answer.error = "опрос отменен";
                        answer.errorcode = DeviceError.NO_ERROR;
                        return answer;
                    }

                    double sumActiveTarifs = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                    log($"Энергия активная суммарная: {sumActiveTarifs} кВт*ч");
                    answer.records.Add(MakeCurrentRecord("Энергия активная суммарная", sumActiveTarifs, "кВт*ч", dateNow));
                    double indication = sumActiveTarifs;
                    setIndicationForRowCache(indication, "кВт*ч", dateNow);
                }

                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

               

                for (int i = 1; i <= 2; i++)
                {
                    dt = Send(MakePackage(0x01, (byte)(i + 4), 0xff), 0x01);
                    if (!dt.success)
                    {
                        log($"Не удалось прочитать: Энергия активная по тарифу {i}");
                    }
                    else
                    {
                        double activeTarif = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия активная по тарифу {i}: {activeTarif} кВт*ч");
                        answer.records.Add(MakeCurrentRecord($"Энергия активная по тарифу {i}", activeTarif, "кВт*ч", dateNow));
                    }
                }

                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                dt = Send(MakePackage(0x01, 0x48, 0xff), 0x01);
                if (!dt.success)
                {
                    log($"Не удалось прочитать: Энергия реактивная суммарная");
                }
                else
                {
                    double sumReactiveTarifs = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                    log($"Энергия реактивная суммарная: {sumReactiveTarifs} квар*ч");
                    answer.records.Add(MakeCurrentRecord("Энергия реактивная суммарная", sumReactiveTarifs, "кВт*ч", dateNow));
                }

                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                for (int i = 1; i <= 2; i++)
                {
                    dt = Send(MakePackage(0x01, (byte)(73 + i), 0xff), 0x01);
                    if (!dt.success)
                    {
                        log($"Не удалось прочитать: Энергия реактивная по тарифу {i}");
                    }
                    else
                    {
                        double reactiveTarif = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия реактивная по тарифу {i}: {reactiveTarif} квар*ч");
                        answer.records.Add(MakeCurrentRecord($"Энергия реактивная по тарифу {i}", reactiveTarif, "кВт*ч", dateNow));
                    }
                }

                if (cancel())
                {
                    answer.success = false;
                    answer.error = "опрос отменен";
                    answer.errorcode = DeviceError.NO_ERROR;
                    return answer;
                }

                dt = Send(MakePackage(0x01, 0x03, 0xff), 0x01);

                double activePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.001;
                log($"Активная мощность: {activePower} Вт");
                answer.records.Add(MakeCurrentRecord("Активная мощность", activePower, "Вт", dateNow));

                return answer;
            }
            else if (ModelVersion.Contains("307"))
            {
                if (Int32.TryParse(SoftwareVersion, out int softWareVersion))
                {
                    if (softWareVersion == 124)
                    {
                        dynamic dt = Send(MakePackage(0x01, 0x76, 0xff), 0x01);

                        dynamic answer = new ExpandoObject();
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                        answer.records = new List<dynamic>();

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        double sumActiveImp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия активная импортируемая суммарная: {sumActiveImp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная импортируемая суммарная", sumActiveImp, "кВт*ч", dateNow));
                        dt = Send(MakePackage(0x01, 0x95, 0xff), 0x01);

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        double sumActiveExp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия активная экспортируемая суммарная: {sumActiveExp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная экспортируемая суммарная", sumActiveExp, "кВт*ч", dateNow));

                        dt = Send(MakePackage(0x01, 0x7F, 0xff), 0x01);

                        double sumReactiveImp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия реактивная импортируемая суммарная: {sumReactiveImp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная импортируемая суммарная", sumReactiveImp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x9F, 0xff), 0x01);

                        double sumReactiveExp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия реактивная экспортируемая суммарная: {sumReactiveExp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная экспортируемая суммарная", sumReactiveExp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x6d, 0xff), 0x01);

                        double activePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Активная мощность суммарная: {activePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Активная мощность суммарная", activePower, "Вт", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x71, 0xff), 0x01);

                        double reactivePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Реактивная мощность суммарная: {reactivePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Реактивная мощность суммарная", reactivePower, "Вт", dateNow));

                        double indication = sumActiveImp;
                        setIndicationForRowCache(indication, "кВт*ч", dateNow);
                        return answer;
                    }
                    else if (softWareVersion == 123)
                    {
                        dynamic dt = Send(MakePackage(0x01, 0x76, 0xff), 0x01);

                        dynamic answer = new ExpandoObject();
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                        answer.records = new List<dynamic>();

                        double sumActiveImp = ConvertFromBcd(dt.Body[0]) * 0.001 + ConvertFromBcd(dt.Body[1]) * 0.1 + ConvertFromBcd(dt.Body[2]) * 10 + ConvertFromBcd(dt.Body[3]) * 1000;
                        log($"Энергия активная импортируемая суммарная: {sumActiveImp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная импортируемая суммарная", sumActiveImp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x95, 0xff), 0x01);
                        double sumActiveExp = ConvertFromBcd(dt.Body[0]) * 0.001 + ConvertFromBcd(dt.Body[1]) * 0.1 + ConvertFromBcd(dt.Body[2]) * 10 + ConvertFromBcd(dt.Body[3]) * 1000;
                        log($"Энергия активная экспортируемая суммарная: {sumActiveExp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная экспортируемая суммарная", sumActiveExp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x7F, 0xff), 0x01);
                        double sumReactiveImp = ConvertFromBcd(dt.Body[0]) * 0.001 + ConvertFromBcd(dt.Body[1]) * 0.1 + ConvertFromBcd(dt.Body[2]) * 10 + ConvertFromBcd(dt.Body[3]) * 1000;
                        log($"Энергия реактивная импортируемая суммарная: {sumReactiveImp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная импортируемая суммарная", sumReactiveImp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x9F, 0xff), 0x01);
                        double sumReactiveExp = ConvertFromBcd(dt.Body[0]) * 0.001 + ConvertFromBcd(dt.Body[1]) * 0.1 + ConvertFromBcd(dt.Body[2]) * 10 + ConvertFromBcd(dt.Body[3]) * 1000;
                        log($"Энергия реактивная экспортируемая суммарная: {sumReactiveExp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная экспортируемая суммарная", sumReactiveExp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x6d, 0xff), 0x01);
                        double activePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Активная мощность суммарная: {activePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Активная мощность суммарная", activePower, "Вт", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x71, 0xff), 0x01);
                        double reactivePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Реактивная мощность суммарная: {reactivePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Реактивная мощность суммарная", reactivePower, "Вт", dateNow));

                        double indication = sumActiveImp;
                        setIndicationForRowCache(indication, "кВт*ч", dateNow);
                        return answer;
                    }
                    else if (softWareVersion >= 300 && softWareVersion <= 399)
                    {
                        dynamic dt = Send(MakePackage(0x01, 0x76, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        dynamic answer = new ExpandoObject();
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                        answer.records = new List<dynamic>();

                        double sumActiveImp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия активная импортируемая суммарная: {sumActiveImp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная импортируемая суммарная", sumActiveImp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x95, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        double sumActiveExp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия активная экспортируемая суммарная: {sumActiveExp} кВт*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия активная экспортируемая суммарная", sumActiveExp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x7F, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        double sumReactiveImp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия реактивная импортируемая суммарная: {sumReactiveImp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная импортируемая суммарная", sumReactiveImp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x9F, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        double sumReactiveExp = ConvertFromBcd(dt.Body[0]) * 0.01 + ConvertFromBcd(dt.Body[1]) * 1 + ConvertFromBcd(dt.Body[2]) * 100 + ConvertFromBcd(dt.Body[3]) * 10000;
                        log($"Энергия реактивная экспортируемая суммарная: {sumReactiveExp} квар*ч");
                        answer.records.Add(MakeCurrentRecord("Энергия реактивная экспортируемая суммарная", sumReactiveExp, "кВт*ч", dateNow));

                        if (cancel())
                        {
                            answer.success = false;
                            answer.error = "опрос отменен";
                            answer.errorcode = DeviceError.NO_ERROR;
                            return answer;
                        }

                        dt = Send(MakePackage(0x01, 0x6d, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        double activePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Активная мощность суммарная: {activePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Активная мощность суммарная", activePower, "Вт", dateNow));

                        dt = Send(MakePackage(0x01, 0x71, 0xff), 0x01);
                        if (!dt.success)
                        {
                            return dt;
                        }
                        double reactivePower = (dt.Body[3] << 24 | dt.Body[2] << 16 | dt.Body[1] << 8 | dt.Body[0]) * 0.01;
                        log($"Реактивная мощность суммарная: {reactivePower} Вт");
                        answer.records.Add(MakeCurrentRecord("Реактивная мощность суммарная", reactivePower, "Вт", dateNow));

                        double indication = sumActiveImp;
                        setIndicationForRowCache(indication, "кВт*ч", dateNow);
                        return answer;
                    }
                    dynamic ans1 = new ExpandoObject();
                    ans1.success = false;
                    ans1.error = "для данной прошивки чтение часовых не реализовано";
                    ans1.errorcode = DeviceError.NO_ANSWER;
                    return ans1;
                }
                dynamic ans2 = new ExpandoObject();
                ans2.success = false;
                ans2.error = "не удалось прочитать версию прошивки";
                ans2.errorcode = DeviceError.NO_ANSWER;
                return ans2;
            }
            dynamic ans3 = new ExpandoObject();
            ans3.success = false;
            ans3.error = "нет ответа";
            ans3.errorcode = DeviceError.NO_ANSWER;
            return ans3;
        }

        private DateTime GetCurrentTime()
        {
            dynamic dt = Send(MakePackage(0x01, 0x0E, 0xff), 0x01);
            if (!dt.success)
            {
                return dt;
            }

            dt.date = DateTime.Now;

            if (dt.Body.Length == 7)
            {
                int sec = dt.Body[0];
                int min = dt.Body[1];
                int hour = dt.Body[2];

                int day = dt.Body[4];
                int mon = dt.Body[5];
                int year = dt.Body[6];

                try
                {
                    dt.date = new DateTime(year + 2000, mon, day, hour, min, sec);
                    log($"Текущее время на счетчике: {dt.date}");
                    return dt;
                }
                catch
                {
                }
            }

            log("Не удалось прочитать текущую дату");
            return dt.date;
        }
        #endregion
    }
}

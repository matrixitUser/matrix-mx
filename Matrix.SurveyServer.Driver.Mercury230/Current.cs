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
        dynamic GetCurrent(DateTime date)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();


            byte parameter = 0x11;
            //читаем напряжение			
            for (var u = 1; u <= 3; u++)
            {
                var voltage = ParseVoltageResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Voltage, 1))), date, u);
                if (voltage.success)
                {
                    records.AddRange(voltage.records);
                }
                else if ((voltage.errorcode != DeviceError.DEVICE_EXCEPTION) || (voltage.exceptioncode != 0x01))
                {
                    return voltage;
                }
            }

            for (var i = 1; i <= 3; i++)
            {
                var cur = ParseCurrentResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Current, 1))), date, i);
                if (cur.success)
                {
                    records.AddRange(cur.records);
                }
                else if ((cur.errorcode != DeviceError.DEVICE_EXCEPTION) || (cur.exceptioncode != 0x01))
                {
                    return cur;
                }
            }

            parameter = 0x14;
            var freq = Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Frequency, 0)));
            if (freq.success)
            {
                var freqData = freq.Body;

                if (freqData.Length < 2)
                {
                    log("Не удалось получить частоту");
                }
                else
                {
                    var freqVal = Helper.MercuryStrange(freqData, 0) / 100.0;
                    records.Add(MakeCurrentRecord("Частота", freqVal, "Гц", date));
                    log(string.Format("Частота = {0} Гц", freqVal));
                }
            }
            else if ((freq.errorcode != DeviceError.DEVICE_EXCEPTION) || (freq.exceptioncode != 0x01))
            {
                return freq;
            }


            //Угол между фазными напряжениями
            var fi1_2Answer = Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Fi, 1)));
            if (fi1_2Answer.success)
            {
                var fi1_2 = fi1_2Answer.Body;
                if (fi1_2.Length < 8)
                {
                    log("Не удалось получить углы между фазами");
                }
                else
                {
                    var fi1_2Val = Helper.MercuryStrange(fi1_2, 0) / 100.0;
                    var fi1_3Val = Helper.MercuryStrange(fi1_2, 3) / 100.0;
                    var fi2_3Val = Helper.MercuryStrange(fi1_2, 6) / 100.0;
                    log(string.Format("Углы м/у фазными U: 1 и 2 фазы->{0} град.,1 и 3 фазы>{1} град.,2 и 3 фазы->{2} град.,", fi1_2Val, fi1_3Val, fi2_3Val));
                }
            }
            else if ((fi1_2Answer.errorcode != DeviceError.DEVICE_EXCEPTION) || (fi1_2Answer.exceptioncode != 0x01))
            {
                return fi1_2Answer;
            }


            ///Чтение вспомогательных пара-
            ///метров: мгновенной активной, ре-
            ///активной, полной мощности, на-
            ///пряжения
            ///тока,
            ///коэффициента
            ///мощности и частоты (см. формат)
            ///3 двоичных байта. Два старших
            ///разряда старшего байта указывают
            ///положение вектора полной мощ-
            ///ности и должны маскироваться.
            ///(См. формат ответа).

            parameter = 0x11;

            var cosFiAll = ParsePowerCoefficientResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.PowerCoefficient, 0))), 0, date);
            if (cosFiAll.success)
            {
                records.AddRange(cosFiAll.records);
            }
            else if ((cosFiAll.errorcode != DeviceError.DEVICE_EXCEPTION) || (cosFiAll.exceptioncode != 0x01))
            {
                return cosFiAll;
            }

            var cosFi1 = ParsePowerCoefficientResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.PowerCoefficient, 1))), 1, date);
            if (cosFi1.success)
            {
                records.AddRange(cosFi1.records);
            }
            else if ((cosFi1.errorcode != DeviceError.DEVICE_EXCEPTION) || (cosFi1.exceptioncode != 0x01))
            {
                return cosFi1;
            }

            var cosFi2 = ParsePowerCoefficientResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.PowerCoefficient, 2))), 2, date);
            if (cosFi2.success)
            {
                records.AddRange(cosFi2.records);
            }
            else if ((cosFi2.errorcode != DeviceError.DEVICE_EXCEPTION) || (cosFi2.exceptioncode != 0x01))
            {
                return cosFi2;
            }

            var cosFi3 = ParsePowerCoefficientResponse(Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.PowerCoefficient, 3))), 3, date);
            if (cosFi3.success)
            {
                records.AddRange(cosFi3.records);
            }
            else if ((cosFi3.errorcode != DeviceError.DEVICE_EXCEPTION) || (cosFi3.exceptioncode != 0x01))
            {
                return cosFi3;
            }

            //var power = new PowerResponse(SendMessageToDevice(new AdditionalParametersRequest(NetworkAddress, parameter,
            //    new BWRI(AdditionalParameterNumber.Power, 1))), date, NetworkAddress);
            try
            {
                var power1Answer = Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Power, 1)));
                if (!power1Answer.success) return power1Answer;
                var power1 = power1Answer.Body;
                var powerVal1 = Helper.MercuryStrange(power1, 0, true) / 100.0;

                var power2Answer = Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Power, 2)));
                if (!power2Answer.success) return power2Answer;
                var power2 = power2Answer.Body;
                var powerVal2 = Helper.MercuryStrange(power2, 0, true) / 100.0;

                var power3Answer = Send(MakeAdditionalParametersRequest(parameter, new BWRI(AdditionalParameterNumber.Power, 3)));
                if (!power3Answer.success) return power3Answer;
                var power3 = power3Answer.Body;
                var powerVal3 = Helper.MercuryStrange(power3, 0, true) / 100.0;
                
                var recs = new List<dynamic>();
                recs.Add(MakeCurrentRecord("Мощность P по фазе 1", powerVal1, "Вт", date));
                recs.Add(MakeCurrentRecord("Мощность P по фазе 2", powerVal2, "Вт", date));
                recs.Add(MakeCurrentRecord("Мощность P по фазе 3", powerVal3, "Вт", date));
                records.AddRange(recs);

                log(string.Format("Мощность P по фазе 1:{0}Вт; Мощность P по фазе 2:{1}Вт; Мощность P по фазе 3:{2}Вт", powerVal1, powerVal2, powerVal3));

            }
            catch (Exception e)
            {
                log("Не удалось получить мощности по фазам: нет ответа");
            }




            //Энергия от сброса
            //    var response = SendMessageToDevice(new AdditionalParametersRequest(NetworkAddress, parameter,
            //    new BWRI(AdditionalParameterNumber.FixEnergy, 0)));

            var energy = ParseEnergyResponse(Send(MakeDataRequest(0x00, 0, 0)), date);

            


            //var energy = new EnergyResponse(SendMessageToDevice(new AdditionalParametersRequest(NetworkAddress, parameter,
            //    new BWRI(AdditionalParameterNumber.FixEnergy, 0))), date);
            if (energy.success)
            {
                foreach (var data in energy.records)
                {
                    log(string.Format("{0} = {1} {2}", data.s1, data.d1, data.s2));
                }
                records.AddRange(energy.records);
            }
            else if ((energy.errorcode != DeviceError.DEVICE_EXCEPTION) || (energy.exceptioncode != 0x01))
            {
                return energy;
            }



            //?var recordsConst = new List<Constant>();
            //?recordsConst.AddRange(ReadConst());
            //if (recordsConst != null)
            //{
            //    OnSendMessage(string.Format("{0}", " Константы"));
            //    foreach (var data in recordsConst)
            //    {
            //        if (data.Name == "ProductionDate")
            //        {
            //            DateTime dt = new DateTime();
            //            dt = DateTime.ParseExact(data.Value.Substring(0, 10), "dd.MM.yyyy",null);
            //            records.Add(new Data(data.Name, Matrix.Common.Agreements.MeasuringUnitType.day, DateTime.Now, dt.Year, Matrix.Common.Agreements.CalculationType.NotCalculated));

            //        }
            //        //records.Add(new Data(data.Name, Matrix.Common.Agreements.MeasuringUnitType.day, DateTime.Now, Convert.ToDouble(data.Value),Matrix.Common.Agreements.CalculationType.NotCalculated));
            //     }
            //}

            current.records = records;
            return current;
        }
    }
}

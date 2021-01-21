
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SwitchADC
{
    /// <summary>
    /// Драйвер для регистраторов РИ-3|4|5
    /// </summary>
    public partial class Driver
    {
        /// <summary>
        /// Регистр выбора страницы
        /// </summary>
        private const short RVS = 0x0084;
        [Import("log")]
        private Action<string> log;

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        private Func<byte[]> response;

        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        [Import("cancel")]
        private Func<bool> cancel;

        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;

        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            byte networkAddress = 1;
            byte channel = 1;
            var param = (IDictionary<string, object>)arg;

            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out networkAddress))
            {
                log(string.Format("Отсутствуют сведения о сетевом адресе, принят по-умолчанию {0}", networkAddress));
            }

            if (!param.ContainsKey("channel") || !byte.TryParse(arg.channel.ToString(), out channel))
            {
                log(string.Format("Отсутствуют сведения о канале, принят по-умолчанию {0}", channel));
            }

            dynamic result;

            switch (what.ToLower())
            {
                case "all":
                    result = Current(networkAddress, (byte)(channel | 0x40));
                    break;
                default:
                    {
                        var description = string.Format("неопознаная команда {0}", what);
                        log(description);
                        result = MakeResult(201, description);
                    }
                    break;
            }

            return result;
        }

        private byte[] Send(byte[] data)
        {
            request(data);
            //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            byte[] buffer = new byte[] { };
            var timeout = 10000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }
            //log(string.Format("пришло {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            return buffer;
        }

        private readonly Random rnd = new Random();

        private double Approximate(double value)
        {
            if (value < 2) //для 1мА
            {
                return value * 1.0122038488760429555589404282459; //1.000564244655077; //1.002682747466943;//1.001574872322941;
            }
            //else if (value < 10)
            //{
            //    return value * 1.000564244655077;//1.000615302947691;//1.001046355715868;
            //}
            //else if (value < 12)
            //{
            //    return value * 1.000465970338486;
            //}
            //else if (value < 15)
            //{
            //    return value * 1.00035602520773;//1.000175536140862;
            //}
            return value * 0.96279464144939278757982788369771;//5мА //0.96287586737856478766560747570078;//1.000525003484028;
        }

        private double GetPercent(double target, double value)
        {
            return (value - target) / target * 100.0;
        }

        private double GetValueFromAdc(uint adc)
        {
            return 0.00523834 * (double)adc / 1000.0;
        }

        private dynamic Current(byte na, byte ch)
        {
            try
            {
                var op = OpenPort(na, ch, 0x01);
                if (!op.success)
                {
                    log(string.Format("не удалось открыть порт, {0}", op.error));
                    return MakeResult(101, string.Format("не удалось открыть порт, {0}", op.error));
                }
                log(string.Format("порт открыт"));

                var isMultipoint = true;
                if (isMultipoint)
                {
                    int points = 100;
                    int skip = 3;

                    int pointsAfterSkip = points - skip * 2;

                    var currentValues = new List<double>();
                    var sum = 0.0;

                    for (var i = 0; i < points; i++)
                    {
                        var adc = GetADC(na, ch, 0x03, true);
                        if (!adc.success)
                        {
                            log(string.Format("значение не получено, {0}", adc.error));
                            return MakeResult(102, string.Format("значение не получено, {0}", adc.error)); ;
                        }

                        var currentValue = GetValueFromAdc(adc.value);
                        currentValues.Add(currentValue);
                        sum += currentValue;

                        var percents = false;
                        if (percents)
                        {
                            var point = i + 1;
                            if ((point == points) || (point % 4 == 0))
                            {
                                log(string.Format("получение значения, {0}%...", Math.Round(100.0 * point / points)));
                            }
                        }
                        else
                        {
                            var t = Math.Round(currentValue);
                            var currentValueApprox = Approximate(currentValue);
                            log(string.Format("{0}) ADC={1} Ток={2}=>{3} Отклонение={4:0.####}%", i, adc.value, currentValue, currentValueApprox, GetPercent(t, currentValueApprox)));
                        }
                    }

                    currentValues.Sort();

                    var mediana = currentValues[(int)(points / 2)];
                    var middle = sum / points;

                    var medianaMiddleSum = currentValues.Skip(skip).Take(currentValues.Count - skip * 2).Sum();
                    var medianaMiddle = medianaMiddleSum / (points - skip * 2);

                    var medianaApprox = Approximate(mediana);
                    var middleApprox = Approximate(middle);
                    var medianaMiddleApprox = Approximate(medianaMiddle);

                    var target = Math.Round(medianaMiddle);
                    var medianaMiddlePerc = GetPercent(target, medianaMiddle);
                    var medianaMiddleApproxPerc = GetPercent(target, medianaMiddleApprox);

                    log(string.Format("Значение получено, {0} {2:0.###}%-(approx.)->{1} {3:0.####}%", medianaMiddle, medianaMiddleApprox, medianaMiddlePerc, medianaMiddleApproxPerc));
                    //log(string.Format("значение получено, {2} (среднее={0}, медиана={1})", middle, mediana, medianaMiddle));
                    //log(string.Format("после аппроксимации, {2} (среднее={0}, медиана={1})", middleApprox, medianaApprox, medianaMiddleApprox));

                    records(new dynamic[] { MakeCurrentRecord("__medianaValue", medianaApprox, "мА", DateTime.Now) });
                    records(new dynamic[] { MakeCurrentRecord("__middleValue", middleApprox, "мА", DateTime.Now) });
                    records(new dynamic[] { MakeCurrentRecord("Величина тока", medianaMiddleApprox, "мА", DateTime.Now) });
                }
                else//single value
                {
                    var adc = GetADC(na, ch, 0x03, true);
                    if (!adc.success)
                    {
                        log(string.Format("значение не получено, {0}", adc.error));
                        return MakeResult(102, string.Format("значение не получено, {0}", adc.error)); ;
                    }

                    var currentValue = GetValueFromAdc(adc.value);
                    var approx = Approximate(currentValue);
                    var target = Math.Round(approx);
                    var approxPerc = GetPercent(target, approx);
                    log(string.Format("значение получено, ADC={0} => {1}mA => {2} {3:0.####}%", adc.value, currentValue, approx, approxPerc));
                    //records(new dynamic[] { MakeCurrentRecord("__medianaValue", currentValue, "мА", DateTime.Now) });
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
            }

            return MakeResult(0);
        }



        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeResult(int code, string description = "")
        {
            dynamic result = new ExpandoObject();
            result.code = code;
            result.success = code == 0 ? true : false;
            result.description = description;
            return result;
        }
    }

}

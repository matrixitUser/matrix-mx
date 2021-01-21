using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        dynamic GetCurrent(DateTime date, DateTime dateNow, UInt16 devid, byte counters, byte digitals, Dictionary<int, Parameter> parameterConfiguration)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            var countersText = new List<string>();
            var valuesText = new List<string>();

            var bkp = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).Counter, (UInt16)(counters * 4))));
            if (!bkp.success) return bkp;

            for (var i = 0; i < counters; i++)
            {
                var counter = Helper.ToUInt32(bkp.Register, i * 4);
                Parameter p;
                if (parameterConfiguration.ContainsKey(i + 1))
                {
                    p = parameterConfiguration[i + 1];
                    valuesText.Add($"{p.name}={p.GetView(counter)}");
                    if(p.name == "ХВС")
                    {
                        setIndicationForRowCache(p.GetValue(counter), p.unit, dateNow);
                    }
                }
                else
                {
                    p = new Parameter(i + 1);
                    countersText.Add($"{p.name}={p.GetView(counter)}");
                }
                records.Add(MakeCurrentRecord(p.name, p.GetValue(counter), p.unit, date, dateNow));

                //var param = string.Format("Канал {0}", i + 1);
                //if (parameterConfiguration.ContainsKey(i + 1))
                //{
                //    var pcfg = parameterConfiguration[i + 1];
                //    var name = pcfg.name == "" ? param : pcfg.name;
                //    var view = pcfg.k * value + pcfg.start;
                //    valuesText.Add(string.Format("{0}={1}{2}", name, view, ((pcfg.unit == "")? "" : (" " + pcfg.unit)) ));
                //    records.Add(MakeCurrentRecord(name, view, pcfg.unit, date));
                //}
                //else
                //{
                //    records.Add(MakeCurrentRecord(param, value, "", date));
                //    countersText.Add(string.Format("{0}={1}", param, value));
                //}
            }

            //


            var adcText = "";
            var inputsText = "";

            if (GetRegisterSet(devid).name == "new")
            {
               
                var stateNAdc = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).State, 12)));
                if (!stateNAdc.success) return stateNAdc;

                var stateReg = Helper.ToUInt16(stateNAdc.Register, 0);
                var stateReg2 = Helper.ToUInt16(stateNAdc.Register, 2);
                List<int> inputs = null;

                if (devid < (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 13, 14 };
                }
                else if (devid == (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 6, 7 };
                }
                else if (devid == (int)DeviceType.TYPE_MINI)
                {
                    inputs = new List<int>() { 4, 5 };
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13 }; //Только счётные
                }
                else if (devid == (int)DeviceType.TYPE_MX1005R4_P16D16I_01)
                {
                    inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                }
                else if (devid == (int)DeviceType.TYPE_IC485_03)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 0, 1, 2, 3 }; //, 4, 5, 6, 7}; //Только счётные
                }
                else
                {
                    current.success = false;
                    current.error = string.Format("Неизвестный тип устройства = {0}", devid);
                    current.errorcode = DeviceError.NO_ERROR;
                    return current;
                }

                
                for (int i = 0; i < inputs.Count; i++)
                {
                    var param = string.Format("Вход {0}", i + 1);
                    var value = (stateReg & (1 << inputs[i])) > 0 ? 1 : 0;
                    records.Add(MakeCurrentRecord(param, value, "", date, dateNow));
                    inputsText += (inputsText == "" ? "" : ",") + value;
                }

                if((inputs.Count > 0) && (digitals > 0))
                {
                    inputsText += "; ";
                }

                for (int i = 0; i < digitals; i++)
                {
                    var param = string.Format("Д.Вход {0}", i + 1);
                    var value = (stateReg2 & (1 << i)) > 0 ? 1 : 0;
                    records.Add(MakeCurrentRecord(param, value, "", date, dateNow));
                    inputsText += value + (i == (digitals - 1)? "" : ",");
                }

                //for (var i = 0; i < ADC_ATTEMPTS_COUNT; i++)
                {
                    var j = 0;
                    for (; j < 8; j++)
                    {
                        if (stateNAdc.Register[j + 4] != 0) break;
                    }

                    //if (j == 8) break; //АЦП не работает

                    var temp = Helper.ToInt16(stateNAdc.Register, 0 + 4) / 100.0;
                    var bat = Helper.ToUInt16(stateNAdc.Register, 2 + 4) / 1000.0;
                    var vbat = Helper.ToUInt16(stateNAdc.Register, 4 + 4) / 1000.0;
                    var mains = Helper.ToUInt16(stateNAdc.Register, 6 + 4) / 1000.0;

                    //if ((bat >= ADC_VMIN) && (bat < ADC_VMAX))
                    {
                        records.Add(MakeCurrentRecord("Температура", temp, "°C", date, dateNow));
                        records.Add(MakeCurrentRecord("Питание", bat, "В", date, dateNow));
                        records.Add(MakeCurrentRecord("Часовая батарея", vbat, "В", date, dateNow));
                        records.Add(MakeCurrentRecord("Внешнее питание", mains, "В", date, dateNow));

                        adcText += string.Format("темп.={0}, пит.={1}, внешнее пит.={2}, часовая бат.={3}", temp, bat, mains, vbat);
                        //break;
                    }
                }
            }
            else
            {

                var state = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).State, 2)));
                if (!state.success) return state;

                var stateReg = Helper.ToUInt16(state.Register, 0);
                List<int> inputs = null;

                if (devid < (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 13, 14 };
                }
                else if (devid == (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 6, 7 };
                }
                else if (devid == (int)DeviceType.TYPE_MINI)
                {
                    inputs = new List<int>() { 4, 5 };
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13 }; //Только счётные
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                }
                else if (devid == (int)DeviceType.TYPE_IC485_03)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 0, 1, 2, 3 }; //, 4, 5, 6, 7}; //Только счётные
                }
                else
                {
                    current.success = false;
                    current.error = string.Format("Неизвестный тип устройства = {0}", devid);
                    current.errorcode = DeviceError.NO_ERROR;
                    return current;
                }


                for (var i = 0; i < inputs.Count; i++)
                {
                    var param = string.Format("Вход {0}", i + 1);
                    var value = (stateReg & (1 << inputs[i])) > 0 ? 1 : 0;
                    records.Add(MakeCurrentRecord(param, value, "", date, dateNow));
                    inputsText += (inputsText == "" ? "" : ",") + value;
                }
            }

            current.counters = string.Join("; ", countersText); ;
            current.values = string.Join("; ", valuesText);
            current.inputs = inputsText;
            current.adc = adcText;
            current.date = date;
            current.records = records;

            return current;
        }
        dynamic GetDay(DateTime date, UInt16 devid, byte counters, byte digitals, Dictionary<int, Parameter> parameterConfiguration)
        {
            dynamic day = new ExpandoObject();
            day.success = true;
            day.error = string.Empty;
            day.errorcode = DeviceError.NO_ERROR;
            
            var records = new List<dynamic>();

            var countersText = new List<string>();
            var valuesText = new List<string>();

            var bkp = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).Counter, (UInt16)(counters * 4))));
            if (!bkp.success) return bkp;

            for (var i = 0; i < counters; i++)
            {
                var counter = Helper.ToUInt32(bkp.Register, i * 4);
                Parameter p;
                if (parameterConfiguration.ContainsKey(i + 1))
                {
                    p = parameterConfiguration[i + 1];
                    valuesText.Add($"{p.name}={p.GetView(counter)}");
                }
                else
                {
                    p = new Parameter(i + 1);
                    countersText.Add($"{p.name}={p.GetView(counter)}");
                }
                records.Add(MakeDayRecord(p.name, p.GetValue(counter), p.unit, date));

                //var param = string.Format("Канал {0}", i + 1);
                //if (parameterConfiguration.ContainsKey(i + 1))
                //{
                //    var pcfg = parameterConfiguration[i + 1];
                //    var name = pcfg.name == "" ? param : pcfg.name;
                //    var view = pcfg.k * value + pcfg.start;
                //    valuesText.Add(string.Format("{0}={1}{2}", name, view, ((pcfg.unit == "")? "" : (" " + pcfg.unit)) ));
                //    records.Add(MakeCurrentRecord(name, view, pcfg.unit, date));
                //}
                //else
                //{
                //    records.Add(MakeCurrentRecord(param, value, "", date));
                //    countersText.Add(string.Format("{0}={1}", param, value));
                //}
            }

            //


            var adcText = "";
            var inputsText = "";

            if (GetRegisterSet(devid).name == "new")
            {
                var stateNAdc = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).State, 12)));
                if (!stateNAdc.success) return stateNAdc;

                var stateReg = Helper.ToUInt16(stateNAdc.Register, 0);
                var stateReg2 = Helper.ToUInt16(stateNAdc.Register, 2);
                List<int> inputs = null;

                if (devid < (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 13, 14 };
                }
                else if (devid == (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 6, 7 };
                }
                else if (devid == (int)DeviceType.TYPE_MINI)
                {
                    inputs = new List<int>() { 4, 5 };
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13 }; //Только счётные
                }
                else if (devid == (int)DeviceType.TYPE_MX1005R4_P16D16I_01)
                {
                    inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                }
                else if (devid == (int)DeviceType.TYPE_IC485_03)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 0, 1, 2, 3 }; //, 4, 5, 6, 7}; //Только счётные
                }
                else
                {
                    day.success = false;
                    day.error = string.Format("Неизвестный тип устройства = {0}", devid);
                    day.errorcode = DeviceError.NO_ERROR;
                    return day;
                }


                for (int i = 0; i < inputs.Count; i++)
                {
                    var param = string.Format("Вход {0}", i + 1);
                    var value = (stateReg & (1 << inputs[i])) > 0 ? 1 : 0;
                    records.Add(MakeDayRecord(param, value, "", date));
                    inputsText += (inputsText == "" ? "" : ",") + value;
                }

                if ((inputs.Count > 0) && (digitals > 0))
                {
                    inputsText += "; ";
                }

                for (int i = 0; i < digitals; i++)
                {
                    var param = string.Format("Д.Вход {0}", i + 1);
                    var value = (stateReg2 & (1 << i)) > 0 ? 1 : 0;
                    records.Add(MakeDayRecord(param, value, "", date));
                    inputsText += value + (i == (digitals - 1) ? "" : ",");
                }

                //for (var i = 0; i < ADC_ATTEMPTS_COUNT; i++)
                {
                    var j = 0;
                    for (; j < 8; j++)
                    {
                        if (stateNAdc.Register[j + 4] != 0) break;
                    }

                    //if (j == 8) break; //АЦП не работает

                    var temp = Helper.ToInt16(stateNAdc.Register, 0 + 4) / 100.0;
                    var bat = Helper.ToUInt16(stateNAdc.Register, 2 + 4) / 1000.0;
                    var vbat = Helper.ToUInt16(stateNAdc.Register, 4 + 4) / 1000.0;
                    var mains = Helper.ToUInt16(stateNAdc.Register, 6 + 4) / 1000.0;

                    //if ((bat >= ADC_VMIN) && (bat < ADC_VMAX))
                    {
                        records.Add(MakeDayRecord("Температура", temp, "°C", date));
                        records.Add(MakeDayRecord("Питание", bat, "В", date));
                        records.Add(MakeDayRecord("Часовая батарея", vbat, "В", date));
                        records.Add(MakeDayRecord("Внешнее питание", mains, "В", date));

                        adcText += string.Format("темп.={0}, пит.={1}, внешнее пит.={2}, часовая бат.={3}", temp, bat, mains, vbat);
                        //break;
                    }
                }
            }
            else
            {

                var state = ParseRegisterResponse(Send(MakeRegisterRequest((UInt32)GetRegisterSet(devid).State, 2)));
                if (!state.success) return state;

                var stateReg = Helper.ToUInt16(state.Register, 0);
                List<int> inputs = null;

                if (devid < (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 13, 14 };
                }
                else if (devid == (int)DeviceType.TYPE_KURCHATOVA13)
                {
                    inputs = new List<int>() { 4, 5, 6, 7 };
                }
                else if (devid == (int)DeviceType.TYPE_MINI)
                {
                    inputs = new List<int>() { 4, 5 };
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 10, 11, 12, 13 }; //Только счётные
                }
                else if (devid == (int)DeviceType.TYPE_MX1001R4_P12_01)
                {
                    inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                }
                else if (devid == (int)DeviceType.TYPE_IC485_03)
                {
                    //inputs = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }; //Всё
                    inputs = new List<int>() { 0, 1, 2, 3 }; //, 4, 5, 6, 7}; //Только счётные
                }
                else
                {
                    day.success = false;
                    day.error = string.Format("Неизвестный тип устройства = {0}", devid);
                    day.errorcode = DeviceError.NO_ERROR;
                    return day;
                }


                for (var i = 0; i < inputs.Count; i++)
                {
                    var param = string.Format("Вход {0}", i + 1);
                    var value = (stateReg & (1 << inputs[i])) > 0 ? 1 : 0;
                    records.Add(MakeDayRecord(param, value, "", date));
                    inputsText += (inputsText == "" ? "" : ",") + value;
                }
            }

            day.counters = string.Join("; ", countersText); ;
            day.values = string.Join("; ", valuesText);
            day.inputs = inputsText;
            day.adc = adcText;
            day.date = date;
            day.records = records;

            return day;
        }
    }
}

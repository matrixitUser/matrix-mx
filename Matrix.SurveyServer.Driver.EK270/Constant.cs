using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.EK270
{
    enum DevType
    {
        Unknown,
        EK260,
        EK270,
        TC210,
        TC215,
        TC220
    }

    public partial class Driver
    {

        #region Константы
        /// <summary>
        /// Маппинг для констант. Никакие зачения не будут игнорироваться. В качестве имени константы будут 
        /// ичспользоваться Name либо Description
        /// </summary>
        private readonly List<MappingUnit> _main = new List<MappingUnit>
            {
                new MappingUnit{Address="1:180.0",Description="Серийный номер устройства"}, //ТС220;ТС215
                new MappingUnit{Address="1:181.0",Description="Тип устройства"},
                new MappingUnit{Address="2:190.0",Description="Версия ПО"}, //ТС215
                new MappingUnit{Address="2:191.0",Description="Контрольная сумма ПО"},  //ТС215
                new MappingUnit{Address="2:141.0",Description="Граница дня (начало газового дня) 2 (час)"},  //ТС220;ТС215
                new MappingUnit { Address = "4:150.0", Description = "Период архивации" },  //ТС220;ТС215
            };

        #endregion

        public static bool CheckBcc(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 3) return false;
            //первым симолом должен идти <STX> или <SOH> - их учитывать не надо
            char bcc = Encoding.ASCII.GetChars(new byte[] { buffer[1] })[0];

            if (buffer.Length > 2)
                for (int i = 2; i < buffer.Length - 1; i++)
                {
                    bcc ^= Encoding.ASCII.GetChars(new byte[] { buffer[i] })[0];
                }

            return bcc == Encoding.ASCII.GetChars(new byte[] { buffer[buffer.Length - 1] })[0];
        }

        /// <summary>
        /// Распарсить ответ контроллера при запросе отдельного значения.
        /// В возвращаемом значении возможно будет размерность (если оа пришла с контроллера)
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string ParseSingleValue(byte[] response)
        {
            if (response == null) return null;

            var message = Encoding.ASCII.GetString(response);

            //log(string.Format("message={0}", message));

            string _pattern = @"\((?<val>.*)\)";
            Regex _rgx = new Regex(_pattern, RegexOptions.IgnoreCase);

            var match = _rgx.Match(message);
            if (!match.Success) return null;

            //var res = match.Groups["num"].Value;
            //return res.Substring(1, res.Length - 2);
            //log(string.Format("val={0}", match.Groups["val"].Value));
            return match.Groups["val"].Value;
        }

        private dynamic SmallConst()
        {
            dynamic constant = new ExpandoObject();
            constant.success = false;
            constant.error = string.Empty;
            constant.records = new List<dynamic>();
            constant.period = "";

            var constants = new List<MappingUnit>(_main);

            //new MappingUnit { Address = "4:150.0", Description = "Период архивации", Types = { DevType.TC210, DevType.TC215, DevType.TC220 } },  //ТС220;ТС215



            foreach (var unit in _main)
            {
                try
                {

                    if (cancel())
                    {
                        constant.success = false;
                        constant.error = string.Format("отмена опроса");
                        return constant;
                    }

                    if (string.IsNullOrEmpty(unit.Address)) continue;

                    var values = ParseSingleValue(Send(MakeSingleValueRequest(unit.Address))).Split('*');

                    if (values == null)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("Ошибка при считывании константы {0}", unit.Description);
                        //return constant;
                        log(string.Format("Ошибка при считывании константы {0}", unit.Description));
                        continue;
                    }

                    int errcode = 0;
                    if (values[0].StartsWith("#"))
                    {
                        if (!int.TryParse(values[0].Substring(1), out errcode)) errcode = -1;
                    }

                    if (errcode == 4)
                    {
                        log(string.Format("Константа '{0}' в данном счетчике не представлена", unit.Description));
                        continue;
                    }
                    else if (errcode < 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        //return constant;
                        string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        continue;
                    }
                    else if (errcode > 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode));
                        //return constant;
                        log(string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode)));
                        continue;
                    }

                    var value = values[0];

                    //        if (unit.Description == "Период архивации")
                    //{
                    //    value += values[1];
                    //    constant.period = value;
                    //    log("Период архивации " + value);
                    //}



                    switch (unit.Address)
                    {
                        case "2:141.0"://contractHour
                            {
                                int i;
                                if (!int.TryParse(value, out i))
                                {
                                    constant.success = false;
                                    constant.error = string.Format("не удалось распарсить контрактный час={0}", value);
                                    return constant;
                                }
                                constant.contractHour = i;
                            }
                            break;
                        case "1:180.0"://SN
                            {
                                constant.serial = value;
                            }
                            break;
                        case "4:150.0"://period
                            {
                                constant.period = value+ values[1];
                            }
                            break;
                        case "1:181.0"://DevType
                            {
                                constant.devType = DevType.Unknown;
                                if (value.Contains("EK260")) constant.devType = DevType.EK260;
                                if (value.Contains("EK270")) constant.devType = DevType.EK270;
                                if (value.Contains("TC210")) constant.devType = DevType.TC210;
                                if (value.Contains("TC215")) constant.devType = DevType.TC215;
                                if (value.Contains("TC220")) constant.devType = DevType.TC220;

                                if (constant.devType == DevType.Unknown)
                                {
                                    constant.success = false;
                                    constant.error = string.Format("неизвестный тип устройства", value);
                                    return constant;
                                }
                            }
                            break;
                        case "2:190.0"://version
                            {
                                float f;
                                if (!float.TryParse(value.Replace('.', ','), out f))
                                {
                                    constant.success = false;
                                    constant.error = string.Format("не удалось распарсить версию={0}", value);
                                    return constant;
                                }
                                constant.version = f;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    log(string.Format("ошибка при чтении константы {0}", unit.Description));
                }
            }

            return constant;
        }

        private dynamic GetConstant(DateTime date, bool full = true)
        {
            dynamic constant = new ExpandoObject();
            constant.success = false;
            constant.error = string.Empty;
            constant.records = new List<dynamic>();
            constant.period = "";

            var constants = new List<MappingUnit>(_main);
            if (full) constants.AddRange(_constants);

            foreach (var unit in _main)
            {
                try
                {

                    if (cancel())
                    {
                        constant.success = false;
                        constant.error = string.Format("отмена опроса");
                        return constant;
                    }

                    if (string.IsNullOrEmpty(unit.Address)) continue;

                    var values = ParseSingleValue(Send(MakeSingleValueRequest(unit.Address))).Split('*');

                    if (values == null)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("Ошибка при считывании константы {0}", unit.Description);
                        //return constant;
                        log(string.Format("Ошибка при считывании константы {0}", unit.Description));
                        continue;
                    }

                    int errcode = 0;
                    if (values[0].StartsWith("#"))
                    {
                        if (!int.TryParse(values[0].Substring(1), out errcode)) errcode = -1;
                    }

                    if (errcode == 4)
                    {
                        log(string.Format("Константа '{0}' в данном счетчике не представлена", unit.Description));
                        continue;
                    }
                    else if (errcode < 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        //return constant;
                        string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        continue;
                    }
                    else if (errcode > 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode));
                        //return constant;
                        log(string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode)));
                        continue;
                    }

                    var value = values[0];

                    switch (unit.Address)
                    {
                        case "2:141.0"://contractHour
                            {
                                int i;
                                if (!int.TryParse(value, out i))
                                {
                                    constant.success = false;
                                    constant.error = string.Format("не удалось распарсить контрактный час={0}", value);
                                    return constant;
                                }
                                constant.contractHour = i;
                            }
                            break;
                        case "1:180.0"://SN
                            {
                                constant.serial = value;
                            }
                            break;
                        case "1:181.0"://DevType
                            {
                                constant.devType = DevType.Unknown;
                                if (value.Contains("EK260")) constant.devType = DevType.EK260;
                                if (value.Contains("EK270")) constant.devType = DevType.EK270;
                                if (value.Contains("TC210")) constant.devType = DevType.TC210;
                                if (value.Contains("TC215")) constant.devType = DevType.TC215;
                                if (value.Contains("TC220")) constant.devType = DevType.TC220;

                                if (constant.devType == DevType.Unknown)
                                {
                                    constant.success = false;
                                    constant.error = string.Format("неизвестный тип устройства", value);
                                    return constant;
                                }
                            }
                            break;
                        case "2:190.0"://version
                            {
                                float f;
                                if (!float.TryParse(value.Replace('.', ','), out f))
                                {
                                    constant.success = false;
                                    constant.error = string.Format("не удалось распарсить версию={0}", value);
                                    return constant;
                                }
                                constant.version = f;
                            }
                            break;
                    }

                    //log(string.Format("Считали константу {0}={1}", unit.Description, value));
                    constant.records.Add(MakeConstRecord(unit.Name ?? unit.Description, value, date));
                }
                catch (Exception ex)
                {
                    log(string.Format("ошибка при чтении константы {0}", unit.Description));
                }
            }


            foreach (var unit in _constants.Where(c => c.Types.Contains((DevType)constant.devType)))
            {
                try
                {
                    if (cancel())
                    {
                        constant.success = false;
                        constant.error = string.Format("отмена опроса");
                        return constant;
                    }

                    if (unit == null || string.IsNullOrEmpty(unit.Address)) continue;

                    string[] values;
                    values = ParseSingleValue(Send(MakeSingleValueRequest(unit.Address))).Split('*');

                    if (values == null)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("Ошибка при считывании константы {0}", unit.Description);
                        //return constant;
                        log(string.Format("Ошибка при считывании константы {0}", unit.Description));
                        continue;
                    }

                    int errcode = 0;
                    if (values[0].StartsWith("#"))
                    {
                        if (!int.TryParse(values[0].Substring(1), out errcode)) errcode = -1;
                    }

                    if (errcode == 4)
                    {
                        log(string.Format("Константа '{0}' в данном счетчике не представлена", unit.Description));
                        continue;
                    }
                    else if (errcode < 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        //return constant;
                        string.Format("неизвестная ошибка при запросе {0}", unit.Description);
                        continue;
                    }
                    else if (errcode > 0)
                    {
                        //constant.success = false;
                        //constant.error = string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode));
                        //return constant;
                        log(string.Format("ошибка при запросе {0} - {1}", unit.Description, GetErrorText(errcode)));
                        continue;
                    }

                    var value = values[0];

                    if (unit.Description == "Период архивации")
                    {
                        value += values[1];
                        constant.period = value;
                        log("Период архивации " + value);
                    }

                    //log(string.Format("Считали константу {0}={1}", unit.Description, value));
                    constant.records.Add(MakeConstRecord(unit.Name ?? unit.Description, value, date));
                }
                catch (Exception ex)
                {
                    log(string.Format("ошибка при чтении константы {0}", unit.Description));
                }
            }
            constant.success = true;
            return constant;
        }
    }
}

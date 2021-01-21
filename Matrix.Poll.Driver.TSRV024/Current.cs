using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.TSRV024
{
    public partial class Driver
    {
        private dynamic ReadCurrent(int register, string parameterName, bool isLongFloat = false)
        {

            dynamic bytes = Send(MakeRequestRegister(register, isLongFloat ? 4 : 2));
            if(!bytes.success)
            {
                bytes.error = $"не удалось прочитать параметр {parameterName} (регистр 0x{register:X})";
                return bytes;
            }
                //OnSendMessage(string.Format("0x{0:X} => {1}", register, string.Join(",", bytes.Select(b => b.ToString("X2")))));

            dynamic result;
            if (isLongFloat)
            {
                result = ParseResponseLongFloat(bytes);
            }
            else if (register >= 0xC000)
            {
                result = ParseResponseFloat(bytes);
            }
            else /*if(register >= 0xC000)*/
            {
                result = ParseResponseWord(bytes);
            }
            return result;
        }

        private void ReadCurrentAndSave(ref List<dynamic> records, UInt16 register, string parameter, double k, string unit, DateTime date, bool isLongFloat = false, bool isRequired = true)
        {
            if (cancel()) throw new Exception("опрос отменен");
            dynamic current = ReadCurrent(register, parameter, isLongFloat);
            if (!current.success)
            {
                if(isRequired) throw new Exception(current.error);
                log($"{current.error}, пропуск");
                return;
            }
            records.Add(MakeCurrentRecord(parameter, current.values[0] * k, unit, date));
        }
        private void ReadCurrentEventAndSave(ref List<dynamic> records, UInt16 register1, string parameter1, UInt16 register2, string parameter2, string parameterDiff, double k, string unit, DateTime date, bool isLongFloat = false, bool isRequired = true)
        {
            if (cancel()) throw new Exception("опрос отменен");
            dynamic current1 = ReadCurrent(register1, parameter1, isLongFloat);
            if (!current1.success)
            {
                if (isRequired) throw new Exception(current1.error);
                log($"{current1.error}, пропуск");
                return;
            }
            dynamic current2 = ReadCurrent(register2, parameter2, isLongFloat);
            if (!current2.success)
            {
                if (isRequired) throw new Exception(current2.error);
                log($"{current2.error}, пропуск");
                return;
            }
            records.Add(MakeCurrentRecord(parameterDiff, (current1.values[0] - current2.values[0]) * k, unit, date));
        }

        dynamic GetCurrentEvent()
        {
            dynamic result = new ExpandoObject();
            result.success = true;
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;
            DateTime date = DateTime.Now;
            var records = new List<dynamic>();
            try
            {
                ReadCurrentEventAndSave(ref records, 0xC05A, "Q(0)", 0xC05C, "Q(1)", "dV (ТС 1)", 1.0, "м³/ч", date); // тепло
                ReadCurrentEventAndSave(ref records, 0xC05E, "Q(2)", 0xC060, "Q(3)", "dV (ТС 2)", 1.0, "м³/ч", date); // гвс
                ReadCurrentAndSave(ref records, 0xC062, "dV (ТС 3)", 1.0, "м³/ч", date); // хвс
            }
            catch (Exception ex)
            {
                result.error = ex.Message;
                result.success = false;
                return result;
            }
            result.date = date;
            result.records = records;
            return result;
        }

        dynamic GetCurrent(DateTime date)
        {
            dynamic result = new ExpandoObject();
            result.success = true;
            result.error = string.Empty;
            result.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();

            //

            try
            {
                ReadCurrentAndSave(ref records, 0xC6AC, "Eтс(0)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6AE, "Eгв(0)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6B0, "Gтс(0)", 1.0, "т/ч", date);
                ReadCurrentAndSave(ref records, 0xC6B2, "Eтс(1)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6B4, "Eгв(1)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6B6, "Gтс(1)", 1.0, "т/ч", date);
                ReadCurrentAndSave(ref records, 0xC6B8, "Eтс(2)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6BA, "Eгв(2)", 1.0, "ГКал/ч", date);
                ReadCurrentAndSave(ref records, 0xC6BC, "Gтс(2)", 1.0, "т/ч", date);
                log("Опрошены текущие параметры Eтс, Eгв, Gтс");

                ReadCurrentAndSave(ref records, 0xC048, "t(0)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC04A, "t(1)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC04C, "t(2)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC04E, "t(3)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC050, "t(4)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC052, "t(5)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC054, "t(6)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC056, "t(7)", 1.0, "°C", date);
                ReadCurrentAndSave(ref records, 0xC058, "t(8)", 1.0, "°C", date);
                log("Опрошены текущие t");

                DateTime dtNow = DateTime.Now;
                ReadCurrentEventAndSave(ref records, 0xC05A, "Q(0)", 0xC05C, "Q(1)", "dV (ТС 1)", 1.0, "м³/ч", dtNow); // тепло
                ReadCurrentEventAndSave(ref records, 0xC05E, "Q(2)", 0xC060, "Q(3)", "dV (ТС 2)", 1.0, "м³/ч", dtNow); // гвс
                ReadCurrentAndSave(ref records, 0xC062, "dV (ТС 3)", 1.0, "м³/ч", dtNow); // хвс
                log("Опрошены текущие dV (ТС N)");
                ReadCurrentAndSave(ref records, 0xC05A, "Q(0)", 1.0, "м³/ч", date); // тепло вход
                ReadCurrentAndSave(ref records, 0xC05C, "Q(1)", 1.0, "м³/ч", date); // тепло выход
                ReadCurrentAndSave(ref records, 0xC05E, "Q(2)", 1.0, "м³/ч", date); // гвс входи
                ReadCurrentAndSave(ref records, 0xC060, "Q(3)", 1.0, "м³/ч", date); // гвс выход
                ReadCurrentAndSave(ref records, 0xC062, "Q(4)", 1.0, "м³/ч", date); // хвс
                ReadCurrentAndSave(ref records, 0xC064, "Q(5)", 1.0, "м³/ч", date);
                ReadCurrentAndSave(ref records, 0xC066, "Q(6)", 1.0, "м³/ч", date);
                ReadCurrentAndSave(ref records, 0xC068, "Q(7)", 1.0, "м³/ч", date);
                ReadCurrentAndSave(ref records, 0xC06A, "Q(8)", 1.0, "м³/ч", date);
                log("Опрошены текущие Q");

                ReadCurrentAndSave(ref records, 0xC03C, "P(0)", 1.0, "МПа", date);
                ReadCurrentAndSave(ref records, 0xC03E, "P(1)", 1.0, "МПа", date);
                ReadCurrentAndSave(ref records, 0xC040, "P(2)", 1.0, "МПа", date);
                ReadCurrentAndSave(ref records, 0xC042, "P(3)", 1.0, "МПа", date);
                ReadCurrentAndSave(ref records, 0xC044, "P(4)", 1.0, "МПа", date);
                ReadCurrentAndSave(ref records, 0xC046, "P(5)", 1.0, "МПа", date);
                log("Опрошены текущие P");

                ReadCurrentAndSave(ref records, 0xC0C6, "Mтс(1)", 1.0, "т", date, isRequired: false);
                //ReadCurrentAndSave(ref records, 0xC0CE);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Mтс(1) сервис");
                //ReadCurrentAndSave(ref records, 0xC0DE);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Mтс(2)");
                //ReadCurrentAndSave(ref records, 0xC0E6);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Mтс(2) сервис");
                
                ReadCurrentAndSave(ref records, 0xC238, "MтрТР1ТС1", 1.0, "т", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC242);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Mтр1(1) сервис");
                ReadCurrentAndSave(ref records, 0xC274, "MтрТР2ТС1", 1.0, "т", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC27E);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Mтр2(1) сервис");
                ReadCurrentAndSave(ref records, 0xC328, "MтрТР1ТС2", 1.0, "т", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC332, "Mтр1(2) сервис", "т", 1.0, CalculationType.Average, dateResponse.Date);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);
                ReadCurrentAndSave(ref records, 0xC364, "MтрТР2ТС2", 1.0, "т", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC36E, "Mтр2(2) сервис", "т", 1.0, CalculationType.Average, dateResponse.Date);
                ////if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);

                ReadCurrentAndSave(ref records, 0xC418, "MтрТР1ТС3", 1.0, "т", date, isRequired: false, isLongFloat: true);
                ReadCurrentAndSave(ref records, 0xC454, "MтрТР2ТС3", 1.0, "т", date, isRequired: false, isLongFloat: true);
                log("Опрошены текущие Mтс, Mтр");

                ReadCurrentAndSave(ref records, 0xC234, "WтрТР1ТС1", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC240);
                //if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", "Wтр1(1) сервис");
                ReadCurrentAndSave(ref records, 0xC270, "WтрТР2ТС1", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                ////if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);
                //ReadCurrentAndSave(ref records, 0xC27C, "Wтр2(1) сервис", "т", 1.0, CalculationType.Average, dateResponse.Date);
                ////if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);
                ReadCurrentAndSave(ref records, 0xC324, "WтрТР1ТС2", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC330, "Wтр1(2) сервис", "т", 1.0, CalculationType.Average, dateResponse.Date);
                ////if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);
                ReadCurrentAndSave(ref records, 0xC360, "WтрТР2ТС2", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                //ReadCurrentAndSave(ref records, 0xC36C, "Wтр2(2) сервис", "т", 1.0, CalculationType.Average, dateResponse.Date);
                ////if (current != null) OnSendMessage(string.Format("Параметр {0}={1}", current.ParameterName, current.Value);
                ReadCurrentAndSave(ref records, 0xC420, "WтрТР1ТС3", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                ReadCurrentAndSave(ref records, 0xC450, "WтрТР2ТС3", 1.0, "ГКал", date, isRequired: false, isLongFloat: true);
                log("Опрошены текущие Wтр");
                dynamic WтрТР1ТС1 = records.Find(x => x.s1 == "WтрТР1ТС1");
                dynamic WтрТР2ТС1 = records.Find(x => x.s1 == "WтрТР2ТС1");
                double indication = (double)WтрТР1ТС1.d1 - (double)WтрТР2ТС1.d1;
                log($"indication={indication}");
                setIndicationForRowCache(indication, (string)WтрТР1ТС1.s2, (DateTime)WтрТР1ТС1.date);
                //Чистое время работы ТС в штатном режиме, ч
                ReadCurrentAndSave(ref records, 0x8016, "ТнарТС1", 1.0 / 3600.0, "ч", date);
                ReadCurrentAndSave(ref records, 0x8024, "ТнарТС2", 1.0 / 3600.0, "ч", date);
                ReadCurrentAndSave(ref records, 0x8032, "ТнарТС3", 1.0 / 3600.0, "ч", date);
            }
            catch(Exception ex)
            {
                result.error = ex.Message;
                result.success = false;
                return result;
            }

            //

            result.date = date;
            result.records = records;
            return result;
        }
    }
}

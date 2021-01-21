using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        byte[] MakeJournalRequest(byte number)
        {
            var Data = new List<byte>();
            Data.Add(0x01);
            Data.Add(number);
            return MakeBaseRequest(0x04, Data);
        }
        byte[] MakeJournalEventAndPKERequest(byte parameter) // Запрос на чтение журналов событий и ПКЭ
        {
            var Data = new List<byte>();
            Data.Add(parameter);
            Data.Add(0xFE);
            return MakeBaseRequest(0x04, Data);
        }

        public string ParameterName(byte param)
        {
            switch (param)
            {
                case 0x01:
                    return "Чтение времени включения/ выключения прибора";
                case 0x02:
                    return "Чтение времени коррекции часов прибора";
                case 0x07:
                    return "Чтение времени коррекции тарифного расписания";
                case 0x08:
                    return "Чтение времени коррекции расписания праздничных дней";
                case 0x0A:
                    return "Чтение времени инициализации массива средних мощностей";
                case 0x12:
                    return "Чтение времени вскрытия/закрытия прибора";
                case 0x13:
                    return "Чтение времени и кода перепрограммирования прибора";
                default:
                    return $"Неизвестный параметер: {param}";
            }
        }

        dynamic ParseJournalResponse(dynamic answer, DateTime lastDate)
        {
            if (!answer.success) return answer;

            byte[] body = answer.Body;

            answer.IsEmpty = true;
            foreach (byte b in body)
            {
                if (b != 0x00)
                {
                    answer.IsEmpty = false;
                    break;
                }
            }

            var ssOn = Helper.FromBCD(body[0]);
            var mmOn = Helper.FromBCD(body[1]);
            var HHOn = Helper.FromBCD(body[2]);
            var ddOn = Helper.FromBCD(body[3]);
            var MMOn = Helper.FromBCD(body[4]);
            var yyOn = Helper.FromBCD(body[5]);
            
            try
            {
                answer.TurnOn = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
            }
            catch(Exception ex)
            {
                answer.TurnOn = DateTime.MinValue;
            }

            var ssOff = Helper.FromBCD(body[6]);
            var mmOff = Helper.FromBCD(body[7]);
            var HHOff = Helper.FromBCD(body[8]);
            var ddOff = Helper.FromBCD(body[9]);
            var MMOff = Helper.FromBCD(body[10]);
            var yyOff = Helper.FromBCD(body[11]);
            
            try
            {
                answer.TurnOff = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
            }
            catch
            {
                answer.TurnOff = lastDate;
            }

            return answer;
        }
        dynamic ParseJournalEventAndPKEResponse(byte param, dynamic answer)
        {
            if (!answer.success) return answer;
            byte[] body = answer.Body;

            answer.IsEmpty = true;
            List<dynamic> records = new List<dynamic>();
            foreach (byte b in body)
            {
                if (b != 0x00)
                {
                    answer.IsEmpty = false;
                    break;
                }
            }
            switch (param)
            {
                case 0x01:
                    records.AddRange(TurnOffOnn(body));
                    break;
                case 0x02:
                case 0x12:
                    records.AddRange(ParseByParam120Bytes(body, param));
                    break;
                case 0x07:
                case 0x08:
                case 0x0A:
                    records.AddRange(ParseByParam60Bytes(body, param));
                    break;
                case 0x13:
                    records.Add(ParseTimeReprogramming(body));
                    break;
            }
            answer.records = records;
            return answer;
        }
        private List<dynamic> TurnOffOnn(byte[] body)
        {
            List<dynamic> recs = new List<dynamic>();
            List<DateTime> listTurnOn = new List<DateTime>();
            List<DateTime> listTurnOff = new List<DateTime>();
            for (int i = 0; i < 10; i++)
            {

                DateTime TurnOn, TurnOff;
                TurnOn = GetDateFromData(body, i * 12);
                listTurnOn.Add(TurnOn);
                TurnOff = GetDateFromData(body, i * 12 + 6);
                listTurnOff.Add(TurnOff);

                //var ssOn = Helper.FromBCD(body[i * 12 + 0]);
                //var mmOn = Helper.FromBCD(body[i * 12 + 1]);
                //var HHOn = Helper.FromBCD(body[i * 12 + 2]);
                //var ddOn = Helper.FromBCD(body[i * 12 + 3]);
                //var MMOn = Helper.FromBCD(body[i * 12 + 4]);
                //var yyOn = Helper.FromBCD(body[i * 12 + 5]);
                //try
                //{
                //    TurnOn = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
                //    listTurnOn.Add(TurnOn);
                //}
                //catch (Exception ex)
                //{
                //    TurnOn = DateTime.MinValue;
                //}

                //var ssOff = Helper.FromBCD(body[i * 12 + 6]);
                //var mmOff = Helper.FromBCD(body[i * 12 + 7]);
                //var HHOff = Helper.FromBCD(body[i * 12 + 8]);
                //var ddOff = Helper.FromBCD(body[i * 12 + 9]);
                //var MMOff = Helper.FromBCD(body[i * 12 + 10]);
                //var yyOff = Helper.FromBCD(body[i * 12 + 11]);

                //try
                //{
                //    TurnOff = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
                //    listTurnOff.Add(TurnOff);
                //}
                //catch
                //{
                //    TurnOff = DateTime.MinValue;
                //}
            }
            while (listTurnOff.Any())
            {
                var firstTurnOff = listTurnOff.Min();
                listTurnOff.Remove(firstTurnOff);
                if (listTurnOff.Any())
                {
                    var lastFirstTurnOff = listTurnOff.Min();
                    var listTurnOnTmp = listTurnOn.FindAll(x => (x >= firstTurnOff) && (x <= lastFirstTurnOff));
                    if (listTurnOnTmp.Any())
                    {
                        if(listTurnOnTmp.Count() > 1)
                        {
                            log($"между записями вкл {string.Join(",", listTurnOnTmp)} нет записи выкл", level: 1);
                        }
                        DateTime firstTurnOn = listTurnOnTmp.Max();
                        listTurnOn.RemoveAll(x => x <= lastFirstTurnOff);
                        log($"время выкл:{firstTurnOff} - вкл:{firstTurnOn}", level: 1);
                        recs.Add(MakeAbnormalRecord("turnOffOn", $"время выкл:{firstTurnOff} - вкл:{firstTurnOn}", firstTurnOff, 0));
                    }
                    else
                    {
                        log($"между записями выкл {firstTurnOff} и {lastFirstTurnOff} нет записи вкл", level: 1);
                    }
                }
                else
                {
                    var listTurnOnTmp = listTurnOn.FindAll(x => x >= firstTurnOff);
                    if (listTurnOnTmp.Any())
                    {
                        if (listTurnOnTmp.Count() > 1)
                        {
                            log($"между записями вкл {string.Join(",", listTurnOnTmp)} нет записи выкл", level: 1);
                        }
                        DateTime firstTurnOn = listTurnOnTmp.Max();
                        log($"время выкл:{firstTurnOff} - вкл:{firstTurnOn}", level: 1);
                        recs.Add(MakeAbnormalRecord("turnOffOn", $"время выкл:{firstTurnOff} - вкл:{firstTurnOn}", firstTurnOff, 1));
                    }
                    else
                    {
                        log($"после записи выкл {firstTurnOff} нет записи вкл", level: 1);
                    }
                }
            }
            return recs;
        }

        private dynamic ParseTimeReprogramming(byte[] body)
        {

            var ddOn = Helper.FromBCD(body[0]);
            var MMOn = Helper.FromBCD(body[1]);
            var yyOn = Helper.FromBCD(body[2]);

            var ddOn1 = body[0];
            var MMOn1 = body[1];
            var yyOn1 = body[2];

            DateTime dateFirst, dateSecond;
            try
            {
                dateFirst = new DateTime(2000 + yyOn, MMOn, ddOn);
                dateSecond = new DateTime(2000 + yyOn1, MMOn1, ddOn1);
            }
            catch
            {
                dateFirst = DateTime.MinValue;
                dateSecond = DateTime.MinValue;
            }

            return null;
        }

        private List<dynamic> ParseByParam120Bytes(byte[] body, byte param)
        {
            List<dynamic> recs = new List<dynamic>();
            IDictionary<DateTime, string> dicDateMessage = new Dictionary<DateTime, string>();
            for (int i = 0; i < 10; i++)
            {

                DateTime dateFirst, dateSecond;
                dateFirst = GetDateFromData(body, i * 12);
                dateSecond = GetDateFromData(body, i * 12 + 6);

                //var ssOn = Helper.FromBCD(body[i * 12 + 0]);
                //var mmOn = Helper.FromBCD(body[i * 12 + 1]);
                //var HHOn = Helper.FromBCD(body[i * 12 + 2]);
                //var ddOn = Helper.FromBCD(body[i * 12 + 3]);
                //var MMOn = Helper.FromBCD(body[i * 12 + 4]);
                //var yyOn = Helper.FromBCD(body[i * 12 + 5]);
                //try
                //{
                //    dateFirst = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
                //}
                //catch (Exception ex)
                //{
                //    dateFirst = DateTime.MinValue;
                //}

                //var ssOff = Helper.FromBCD(body[i * 12 + 6]);
                //var mmOff = Helper.FromBCD(body[i * 12 + 7]);
                //var HHOff = Helper.FromBCD(body[i * 12 + 8]);
                //var ddOff = Helper.FromBCD(body[i * 12 + 9]);
                //var MMOff = Helper.FromBCD(body[i * 12 + 10]);
                //var yyOff = Helper.FromBCD(body[i * 12 + 11]);

                //try
                //{
                //    dateSecond = new DateTime(2000 + yyOff, MMOff, ddOff, HHOff, mmOff, ssOff);
                //}
                //catch
                //{
                //    dateSecond = DateTime.MinValue;
                //}
                string messageValue = "";
                switch (param)
                {
                    case 0x02:
                        messageValue = $"время коррекции часов до:{dateFirst}; после:{dateSecond}";
                        break;
                    case 0x12:
                        messageValue = $"время вскрытия:{dateFirst}; закрытия:{dateSecond} прибора";
                        break;
                }
                log(messageValue, level: 1);
                if (dicDateMessage.ContainsKey(dateSecond)) continue;
                dicDateMessage.Add(dateSecond, messageValue);
            }
            DateTime dtKey = dicDateMessage.Max(x => x.Key);
            var message =  dicDateMessage[dtKey];
            recs.Add(MakeAbnormalRecord(ParamName(param), message, dtKey, 1));
            return recs;
        }

        public DateTime GetDateFromData(byte[] body, int i)
        {
            DateTime date;
            var ssOn = Helper.FromBCD(body[i + 0]);
            var mmOn = Helper.FromBCD(body[i + 1]);
            var HHOn = Helper.FromBCD(body[i + 2]);
            var ddOn = Helper.FromBCD(body[i + 3]);
            var MMOn = Helper.FromBCD(body[i + 4]);
            var yyOn = Helper.FromBCD(body[i + 5]);
            try
            {
                date = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
            }
            catch (Exception ex)
            {
                date = DateTime.MinValue;
            }
            return date;
        }


        private List<dynamic> ParseByParam60Bytes(byte[] body, byte param)
        {
            List<dynamic> recs = new List<dynamic>();
            List<DateTime> listDates = new List<DateTime>();
            for (int i = 0; i < 10; i++)
            {
                DateTime date = GetDateFromData(body, i * 6);
                listDates.Add(date);
                //var ssOn = Helper.FromBCD(body[i * 6 + 0]);
                //var mmOn = Helper.FromBCD(body[i * 6 + 1]);
                //var HHOn = Helper.FromBCD(body[i * 6 + 2]);
                //var ddOn = Helper.FromBCD(body[i * 6 + 3]);
                //var MMOn = Helper.FromBCD(body[i * 6 + 4]);
                //var yyOn = Helper.FromBCD(body[i * 6 + 5]);
                //try
                //{
                //    date = new DateTime(2000 + yyOn, MMOn, ddOn, HHOn, mmOn, ssOn);
                //    listDates.Add(date);
                //}
                //catch (Exception ex)
                //{
                //    date = DateTime.MinValue;
                //}
                string messageValue = $"{MessageByParam(param)}:{date}";
                log(messageValue, level: 1);
            }
            if (!listDates.Any()) return recs;
            DateTime lastDt = listDates.Max();
            string message = $"{MessageByParam(param)}:{lastDt}";
            log(message, level: 1);
            recs.Add(MakeAbnormalRecord(ParamName(param), message, lastDt, 1));
            return recs;
        }
        
        public string MessageByParam(byte param)
        {
            switch (param)
            {
                case 0x07:
                    return "время коррекции тарифного расписания";
                case 0x08:
                    return "время коррекции расписания праздничных дней";
                case 0x0A:
                    return "время инициализации массива средних мощностей";
                default:
                    return $"параметер 0x:{param:x}";
            }
        }
        public string ParamName(byte param)
        {
            switch (param)
            {
                case 0x02:
                    return "beforeAfterCurrectTime";
                case 0x07:
                    return "currectTariff";
                case 0x08:
                    return "currectScheduleHoliday";
                case 0x0A:
                    return "mediumPowerArray";
                case 0x12:
                    return "timeOpenCloseDevice";
                case 0x13:
                    return "timePereprogramming";
                default:
                    return $"параметер 0x:{param:x}";
            }
        }
    }
}

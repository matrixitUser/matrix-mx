using System;
using System.Collections.Generic;
using System.Linq;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using log4net;
using System.Timers;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Text;
using System.Text.RegularExpressions;

namespace Matrix.SurveyServer.Driver.TSRV043
{
    /// <summary>
    /// драйвер для теплосчетчиков ТСРВ024
    /// счетчики имеют три теплосистемы, по 4 трубы в каждой
    /// нумерация каналов сквозная (тс1-1,2,3,4; тс2-5,6,7,8; тс3-9,10,11,12)
    /// </summary>
    public class Driver : BaseDriver
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Driver));

        private bool debug = false;
        private int mid = 0;

        public static byte ByteLow(int getLow)
        {
            return (byte)(getLow & 0xFF);
        }
        public static byte ByteHigh(int getHigh)
        {
            return (byte)((getHigh >> 8) & 0xFF);
        }

        public override SurveyResult Ping()
        {
            try
            {
                var req = new Request17(NetworkAddress);
                var resp = new Response17(SendMessageToDevice(req));
                if (resp == null) return new SurveyResult { State = SurveyResultState.NoResponse };
                OnSendMessage(resp.Version);
                return new SurveyResult { State = SurveyResultState.Success };
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
            }
            return new SurveyResult { State = SurveyResultState.NotRecognized };
        }

        public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
        {
            var data = new List<Data>();
            try
            {
                byte[] bytes = null;
                foreach (var date in dates)
                {
                    try
                    {
                        OnSendMessage(string.Format("чтение суточных данных нарастающим итогом за {0:dd.MM.yyyy} ", date));
                        bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.DailyGrowing));
                        var responseTotals = new Response65(bytes, (short) ArchiveType.DailyGrowing);
                        foreach (var d in responseTotals.Data)
                        {
                            //убираем лишние 23:59:59
                            d.Date = d.Date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                            data.Add(d);
                        }
                    }
                    catch (Exception ex2)
                    {
                        OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} нарастающим итогом", date));
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
                return new SurveyResultData { Records = data, State = SurveyResultState.PartialyRead };
            }
            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
        }

        public  SurveyResultData ReadHourlyArchive_(IEnumerable<DateTime> dates)  //не работающий
        {
            var data = new List<Data>();
               foreach (var date in dates)
                {
                try
                {

                    OnSendMessage(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm} нарастающим итогом {1}", date));

                    var bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.HourlyGrowing));
                    var dataResponse = new Response65(bytes, (short) ArchiveType.HourlyGrowing);
                    foreach (var d in dataResponse.Data)
                    {
                        //убираем лишние 59:59
                       // d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
                        data.Add(d);
                    }
                }
                catch (Exception ex)
                {
                    OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy  HH:mm} нарастающим итогом", date));
                    return new SurveyResultData { Records = data, State = SurveyResultState.PartialyRead };
                }

                //OnSendMessage(string.Format("{0:dd.MM.yyyy HH:mm} {1}", date, dataResponse.Text));
                }
             return new SurveyResultData { Records = data, State = SurveyResultState.Success };
        }

        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
        {
            var data = new List<Data>();
            try
            {
                byte[] bytes = null;
                foreach (var date in dates)
                {
                    try
                    {
                        OnSendMessage(string.Format("чтение часовых данных нарастающим итогом за {0:dd.MM.yyyy HH:mm} ", date));
                        bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.HourlyGrowing));
                        var responseTotals = new Response65(bytes, (short)ArchiveType.HourlyGrowing);
                        foreach (var d in responseTotals.Data)
                        {
                            //убираем лишние 59:59
                            d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
                            data.Add(d);
                        }
                    }
                    catch (Exception ex2)
                    {
                        OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} нарастающим итогом", date));
                    }
                }
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
                return new SurveyResultData { Records = data, State = SurveyResultState.PartialyRead };
            }
            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
        }

        /// <summary>
        /// читает регистр текущих показаний
        /// </summary>
        /// <param name="register"></param>
        /// <param name="name"></param>
        /// <param name="measuringUnit"></param>
        /// <param name="channel"></param>
        /// <param name="calculationType"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private Response4 ReadCurrent(int register, bool isLongFloat = false)
        {
            Response4 result = null;
            try
            {
                var bytes = SendMessageToDevice(new Request4(NetworkAddress, register, isLongFloat ? 4 : 2));
                OnSendMessage(string.Format("0x{0:X} => {1}", register, string.Join(",", bytes.Select(b => b.ToString("X2")))));

                if (isLongFloat)
                {
                    result = new ResponseLongFloat(bytes);
                }
                else if (register >= 0xC000)
                {
                    result = new ResponseFloat(bytes);
                }
                else /*if(register >= 0xC000)*/
                {
                    result = new ResponseWord(bytes);
                }
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("не удалось прочитать регистр 0x{0:X}", register));
            }
            return result;
        }

        public override SurveyResultData ReadCurrentValues()
        {
            var data = new List<Data>();
            try
            {
                OnSendMessage(string.Format("чтение мгновенных данных"));

                var dateResponse = new ResponseDateTime(SendMessageToDevice(new Request4(NetworkAddress, 0x8000, 2)));

                Response4 current = null;
                 
                /*
                current = ReadCurrent(0xC097, true);  // 349303-> 49303 ->0xC097
                if (current != null) data.Add(new Data("Eтс(0)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6AE);
                if (current != null) data.Add(new Data("Eгв(0)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6B0);
                if (current != null) data.Add(new Data("Gтс(0)", MeasuringUnitType.tonn_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6B2);
                if (current != null) data.Add(new Data("Eтс(1)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6B4);
                if (current != null) data.Add(new Data("Eгв(1)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6B6);
                if (current != null) data.Add(new Data("Gтс(1)", MeasuringUnitType.tonn_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6B8);
                if (current != null) data.Add(new Data("Eтс(2)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6BA);
                if (current != null) data.Add(new Data("Eгв(2)", MeasuringUnitType.Gkal_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC6BC);
                if (current != null) data.Add(new Data("Gтс(2)", MeasuringUnitType.tonn_h, dateResponse.Date, current.Values.ElementAt(0)));


                current = ReadCurrent(0xC015); //349173
                if (current != null) data.Add(new Data("t(0)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC04A);
                if (current != null) data.Add(new Data("t(1)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC04C);
                if (current != null) data.Add(new Data("t(2)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC04E);
                if (current != null) data.Add(new Data("t(3)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC050);
                if (current != null) data.Add(new Data("t(4)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC052);
                if (current != null) data.Add(new Data("t(5)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC054);
                if (current != null) data.Add(new Data("t(6)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC056);
                if (current != null) data.Add(new Data("t(7)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC058);
                if (current != null) data.Add(new Data("t(8)", MeasuringUnitType.C, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC05A);
                if (current != null) data.Add(new Data("Q(0)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC05C);
                if (current != null) data.Add(new Data("Q(1)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC05E);
                if (current != null) data.Add(new Data("Q(2)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC060);
                if (current != null) data.Add(new Data("Q(3)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC062);
                if (current != null) data.Add(new Data("Q(4)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC064);
                if (current != null) data.Add(new Data("Q(5)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC066);
                if (current != null) data.Add(new Data("Q(6)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC068);
                if (current != null) data.Add(new Data("Q(7)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC06A);
                if (current != null) data.Add(new Data("Q(8)", MeasuringUnitType.m3_h, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC03C);
                if (current != null) data.Add(new Data("P(0)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC03E);
                if (current != null) data.Add(new Data("P(1)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC040);
                if (current != null) data.Add(new Data("P(2)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC042);
                if (current != null) data.Add(new Data("P(3)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC044);
                if (current != null) data.Add(new Data("P(4)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC046);
                if (current != null) data.Add(new Data("P(5)", MeasuringUnitType.MPa, dateResponse.Date, current.Values.ElementAt(0)));


                current = ReadCurrent(0xC0C6);
                if (current != null) data.Add(new Data("Mтс(1)", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));
                

                current = ReadCurrent(0xC238, true);
                if (current != null) data.Add(new Data("MтрТР1ТС1", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC274, true);
                if (current != null) data.Add(new Data("MтрТР2ТС1", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC328, true);//, "Mтр1(2)", MeasuringUnitType.tonn, 1, CalculationType.Average, dateResponse.Date);
                if (current != null) data.Add(new Data("MтрТР1ТС2", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC364, true);
                if (current != null) data.Add(new Data("MтрТР2ТС2", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC418, true);
                if (current != null) data.Add(new Data("MтрТР1ТС3", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC454, true);
                if (current != null) data.Add(new Data("MтрТР2ТС3", MeasuringUnitType.tonn, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC234, true);
                if (current != null) data.Add(new Data("WтрТР1ТС1", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC270, true);
                if (current != null) data.Add(new Data("WтрТР2ТС1", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC324, true);
                if (current != null) data.Add(new Data("WтрТР1ТС2", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC360, true);
                if (current != null) data.Add(new Data("WтрТР2ТС2", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));

                current = ReadCurrent(0xC420, true);
                if (current != null) data.Add(new Data("WтрТР1ТС3", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));
                current = ReadCurrent(0xC450, true);
                if (current != null) data.Add(new Data("WтрТР2ТС3", MeasuringUnitType.Gkal, dateResponse.Date, current.Values.ElementAt(0)));

                */
                //Чистое время работы ТС в штатном режиме, ч
                current = ReadCurrent(0x8016);
                if (current != null) data.Add(new Data("ТнарТС1", MeasuringUnitType.h, dateResponse.Date, current.Values.ElementAt(0) / 3600));
                current = ReadCurrent(0x8024);
                if (current != null) data.Add(new Data("ТнарТС2", MeasuringUnitType.h, dateResponse.Date, current.Values.ElementAt(0) / 3600));
                current = ReadCurrent(0x8032);
                if (current != null) data.Add(new Data("ТнарТС3", MeasuringUnitType.h, dateResponse.Date, current.Values.ElementAt(0) / 3600));


                //var date = dateResponse.Date.AddDays(-1).Date;
                //OnSendMessage(string.Format("чтение итоговых часовых данных за {0:dd.MM.yyyy HH:mm}", date));

                //var bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.DailyGrowing));
                //var dataResponse = new Response65Totals(bytes);
                ////Response65.Channel = channel.Key;
                ////var dataResponse = SendMessageToDevice<Response65>(new Request65ByDate(NetworkAddress, date, channel.Value));
                ////foreach (var d in dataResponse.Data)
                ////{
                ////    //убираем лишние 59:59
                ////    d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
                ////    data.Add(d);
                ////}

                //OnSendMessage(dataResponse.Text);
            }
            catch (Exception ex)
            {
                var iex = ex;
                var message = "";
                do
                {
                    message += "->" + iex.Message;
                    iex = iex.InnerException;
                }
                while (iex != null);
                OnSendMessage(string.Format("ошибка: {0}", message));
            }
            return new SurveyResultData { Records = data, State = SurveyResultState.Success };
        }

        /// <summary>
        /// отправка сообщения прибору
        /// </summary>		
        /// <param name="request">запрос</param>		
        /// <returns>ответ</returns>	
        private byte[] SendMessageToDevice(Request request)
        {
            byte[] response = null;

            bool success = false;
            int attemtingCount = 0;

            while (!success && attemtingCount < 5)
            {
                attemtingCount++;
                isDataReceived = false;
                receivedBuffer = null;
                var bytes = request.GetBytes();

                if(debug) OnSendMessage(string.Format("{1:X}> {0}", string.Join(",", bytes.Select(b => b.ToString("X2"))), (mid & 0x0F)));

                RaiseDataSended(bytes);
                Wait(7000);
                if (isDataReceived)
                {
                    response = receivedBuffer;
                    success = true;
                }
            }

            mid++;
            if (debug) OnSendMessage(string.Format("{1:X}< {0}", string.Join(",", response.Select(b => b.ToString("X2"))), (mid & 0x0F)));

            return response;
        }
    }
}
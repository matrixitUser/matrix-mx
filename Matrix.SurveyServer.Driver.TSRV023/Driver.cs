using System;
using System.Collections.Generic;
using System.Linq;
using Matrix.Common.Agreements;
using Matrix.SurveyServer.Driver.Common;
using System.Timers;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Text;
using System.Text.RegularExpressions;


namespace Matrix.SurveyServer.Driver.TSRV023
{
    /// <summary>
    /// драйвер для теплосчетчиков ТСРВ024
    /// счетчики имеют три теплосистемы, по 4 трубы в каждой
    /// нумерация каналов сквозная (тс1-1,2,3,4; тс2-5,6,7,8; тс3-9,10,11,12)
    /// </summary>
    public class Driver : BaseDriver
    {


        public override SurveyResult Ping()
        {
            try
            {
                OnSendMessage(string.Format("новый драйвер5"));
                var req = new Request17(NetworkAddress);
                var resp = new Response17(SendMessageToDevice(req));
                if (resp == null) return new SurveyResult { State = SurveyResultState.NoResponse };
                OnSendMessage(resp.ToString());
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
                        OnSendMessage(string.Format("чтение суточных данных за {0:dd.MM.yyyy}", date));

                        bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.Daily));

                        var dataResponse = new Response65(bytes, this);
                        foreach (var d in dataResponse.Data)
                        {
                            //убираем лишние 23:59:59
                            d.Date = d.Date.AddHours(-23).AddMinutes(-59).AddSeconds(-59);
                            data.Add(d);
                        }
                    }
                    catch (Exception ex1)
                    {
                        OnSendMessage(string.Format("ошибка при разборе ответа, запись за {0:dd.MM.yy} будет пропущена", date));
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

        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
        {
            var data = new List<Data>();
            try
            {

                foreach (var date in dates)
                {
                    OnSendMessage(string.Format("чтение часовых данных за {0:dd.MM.yyyy HH:mm}", date));

                    var bytes = SendMessageToDevice(new Request65ByDate(NetworkAddress, date, ArchiveType.Hourly));
                    var dataResponse = new Response65(bytes, this);
                    //Response65.Channel = channel.Key;
                    //var dataResponse = SendMessageToDevice<Response65>(new Request65ByDate(NetworkAddress, date, channel.Value));
                    foreach (var d in dataResponse.Data)
                    {
                        //убираем лишние 59:59
                        d.Date = d.Date.AddMinutes(-59).AddSeconds(-59);
                        data.Add(d);
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

        private int ReadCharConst(int register, string name)
        {
            try
            {
                var result = new ResponseByte(SendMessageToDevice(new Request3(NetworkAddress, register, 2)));
                OnSendMessage(string.Format("Регистр 0x{0:X} константа '{1}'={2} ", register, name, result.OneValue));
                return (int)result.OneValue;
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("не удалось прочитать регистр 0x{0:X}", register));
            }
            return -1;
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
        private Data ReadCurrent(int register, string name, MeasuringUnitType measuringUnit, DateTime date)
        {
            Data data = null;
            try
            {
                var result = new ResponseFloat(SendMessageToDevice(new Request4(NetworkAddress, register, 2)));
                data = new Data(name, measuringUnit, date, result.OneValue);
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("не удалось прочитать регистр 0x{0:X}", register));
            }
            return data;
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
        private Data ReadCurrent3(int register, string name, MeasuringUnitType measuringUnit, DateTime date)
        {
            Data data = null;
            try
            {
                var result = new ResponseFloat(SendMessageToDevice(new Request3(NetworkAddress, register, 2)));
                data = new Data(name, measuringUnit, date, result.OneValue);
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("не удалось прочитать регистр 0x{0:X}", register));
            }
            return data;
        }
        private int RegisterToPhys(int logRegister)
        {
            if (logRegister > 400000) return logRegister - 400001;
            return logRegister - 300001;
        }

        public override SurveyResultData ReadCurrentValues()
        {
            var data = new List<Data>();
            try
            {
                Data current = null;                

                OnSendMessage(string.Format("чтение мгновенных данных"));

                var dateResponse = new ResponseDateTime(SendMessageToDevice(new Request3(NetworkAddress, 0x8002, 2)));
                var date = dateResponse.Date;
                OnSendMessage(string.Format("Системное время {0:dd.MM.yyyy HH:mm}:", date));
                
                //нигде не используются
                //var heatSUnits = new MeasuringUnitType[] { MeasuringUnitType.GDj, MeasuringUnitType.Gkal };         // 0-Гдж; 1- Гкал
                //var heatOnHourSUnits = new MeasuringUnitType[] { MeasuringUnitType.MWt, MeasuringUnitType.GDj_h, MeasuringUnitType.Gkal_h };   // 0- Мвт; 1- Гдж/час; 2- Гкал/час
                //var volumeOnHourSUnits = new MeasuringUnitType[] { MeasuringUnitType.l_sek, MeasuringUnitType.m3_h }; // 0- литр/мин(!!! надо умножить на 60); 1- м3/час
                //var measuringScaleS = new int[] { 1, 1000 }; //0 - 1:1 ; 1- 1:1000

                //var heatUnit = heatSUnits[ReadCharConst(0x00D3, "Размерность теплоты")];

                //var heatOnHourUnit = heatOnHourSUnits[ReadCharConst(0x00D4, "Размерность тепловой мощности")];

                //var volumeOnHourUnit = volumeOnHourSUnits[ReadCharConst(0x00D5, "Размерность объемного расхода")];

                //int measuringScale = measuringScaleS[ReadCharConst(0x00D6, "Масштаб измерений")];

                #region другой способ опроса?
                ////ТЕПЛО

                ////Теплосистема 1
                //currentInt = ReadCurrent(RegisterToPhys(449153), "Тепло 1  в теплосистеме 1.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null)  //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449159), "Тепло 1  в теплосистеме 1", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449155), "Тепло 2  в теплосистеме 1.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449161), "Тепло 2  в теплосистеме 1", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449157), "Тепло 3  в теплосистеме 1.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449163), "Тепло 3  в теплосистеме 1", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                ////Теплосистема 2
                //currentInt = ReadCurrent(RegisterToPhys(449165), "Тепло 1  в теплосистеме 2.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null)  //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449171), "Тепло 1  в теплосистеме 2", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449173), "Тепло 2  в теплосистеме 2.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449161), "Тепло 2  в теплосистеме 2", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449175), "Тепло 3  в теплосистеме 2.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449163), "Тепло 3  в теплосистеме 2", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}


                ////Теплосистема 3
                //currentInt = ReadCurrent(RegisterToPhys(449177), "Тепло 1  в теплосистеме 3.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null)  //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449183), "Тепло 1  в теплосистеме 3", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449179), "Тепло 2  в теплосистеме 3.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449185), "Тепло 2  в теплосистеме 3", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}

                //currentInt = ReadCurrent(RegisterToPhys(449181), "Тепло 3  в теплосистеме 3.", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //if (currentInt != null) //Дробная часть
                //{
                //    current = ReadCurrent(RegisterToPhys(449187), "Тепло 3  в теплосистеме 3", MeasuringUnitTypeHeat, 1, CalculationType.Average, dateResponse.Date);
                //    if (current != null)
                //    {
                //        current.Value = currentInt.Value + current.Value;
                //        data.Add(current);
                //    }
                //}
                #endregion

                //Тепловая мощность по каналам 

                current = ReadCurrent(RegisterToPhys(349153), "Тепловая мощность 1 теплосистемы 1", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349155), "Тепловая мощность 2 теплосистемы 1", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349157), "Тепловая мощность 3 теплосистемы 1", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349159), "Тепловая мощность 1 теплосистемы 2", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349161), "Тепловая мощность 2 теплосистемы 2", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349163), "Тепловая мощность 3 теплосистемы 2", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349165), "Тепловая мощность 1 теплосистемы 3", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349167), "Тепловая мощность 2 теплосистемы 3", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349169), "Тепловая мощность 3 теплосистемы 3", MeasuringUnitType.MWt, date);
                if (current != null) data.Add(current);

                //Текущая температура в каналах

                current = ReadCurrent(RegisterToPhys(349175), "Текущая температура воды в канале 0", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349183), "Текущая температура воды в канале 1", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349193), "Текущая температура воды в канале 2", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349203), "Текущая температура воды в канале 3", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349213), "Текущая температура воды в канале 4", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349223), "Текущая температура воды в канале 5", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349233), "Текущая температура воды в канале 6", MeasuringUnitType.C, date);
                if (current != null) data.Add(current);

                //Текущая температура в каналах

                current = ReadCurrent(RegisterToPhys(349177), "Текущая давление воды в канале 0", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349185), "Текущая давление воды в канале 1", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349195), "Текущая давление воды в канале 2", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349205), "Текущая давление воды в канале 3", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349215), "Текущая давление воды в канале 4", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349225), "Текущая давление воды в канале 5", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                current = ReadCurrent(RegisterToPhys(349235), "Текущая давление воды в канале 6", MeasuringUnitType.MPa, date);
                if (current != null) data.Add(current);

                //Расход массовый воды по каналам

                current = ReadCurrent(RegisterToPhys(349187), "Расход массовый воды в канале 1", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                current = ReadCurrent(RegisterToPhys(349187), "Расход массовый воды в канале 2", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                current = ReadCurrent(RegisterToPhys(349207), "Расход массовый воды в канале 3", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                current = ReadCurrent(RegisterToPhys(349217), "Расход массовый воды в канале 4", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                current = ReadCurrent(RegisterToPhys(349227), "Расход массовый воды в канале 5", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                current = ReadCurrent(RegisterToPhys(349237), "Расход массовый воды в канале 6", MeasuringUnitType.g_sec, date);
                if (current != null)
                {
                    current.Value = (double)(1000.0 * current.Value);
                    data.Add(current);
                }

                for (int nElement = 0; nElement <= (data.Count - 1); nElement++)
                {
                    OnSendMessage(string.Format(" {0} Value :{1} {2}", data[nElement].ParameterName, data[nElement].Value, data[nElement].MeasuringUnit));
                }
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
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
                RaiseDataSended(bytes);
                Wait(7000);
                if (isDataReceived)
                {
                    response = receivedBuffer;
                    success = true;
                }
            }
            return response;
        }
    }
}
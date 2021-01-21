//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    /// <summary>
//    /// драйвер газовых корректоров РМГ
//    /// </summary>
//    public class Driver2 : BaseDriver
//    {
//        /// <summary>
//        /// отправка сообщения устройству
//        /// </summary>
//        /// <typeparam name="TResponse">тип ожидаемого ответа</typeparam>
//        /// <param name="request">запрос</param>
//        /// <returns></returns>
//        private IEnumerable<byte> SendMessageToDevice(Request request)// where TResponse : Response
//        {
//            IEnumerable<byte> response = null;

//            bool success = false;
//            int attemtingCount = 0;

//            while (!success && attemtingCount < 5)
//            {
//                attemtingCount++;
//                //OnSendMessage(string.Format("отправка запроса {0}, попытка {1}", request, attemtingCount));

//                isDataReceived = false;
//                receivedBuffer = null;
//                RaiseDataSended(request.GetBytes());
//                Wait(7000);

//                if (isDataReceived)
//                {
//                    response = receivedBuffer;
//                    success = true;
//                }
//            }

//            //OnSendMessage(success ? string.Format("ответ получен: {0}", string.Join(",", response.Select(b => b.ToString("X")))) : "ответ не получен");
//            return response;
//        }

//        public override SurveyResult Ping()
//        {
//            try
//            {
//                var request = new DataRequest(NetworkAddress, 1, 2, 1);
//                var data = SendMessageToDevice(request);
//                var response = new DataResponse(data.ToArray());
//                if (response != null)
//                {
//                    OnSendMessage("температура за {0:dd.MM.yyyy} равна {1}", response.Date, response.Value);
//                    return new SurveyResult() { State = SurveyResultState.Success };
//                }
//                return new SurveyResult() { State = SurveyResultState.NoResponse };
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResult() { State = SurveyResultState.NoResponse };
//        }

//        public override SurveyResultConstant ReadConstants()
//        {
//            var result = new List<Constant>();
//            try
//            {
//                var data = SendMessageToDevice(new Request3(NetworkAddress, 1206, 2));
//                var floatValue = new ResponseFloat(data.ToArray());
//                var value = floatValue.Value;
//                result.Add(new Constant(ConstantType.Density, value.ToString() + " кг/м3"));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 1210, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Рабочая плотность", value.ToString() + " кг/м3"));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 400, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant(ConstantType.Carbondioxide, value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 402, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Водород", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 404, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant(ConstantType.Nitrogen, value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 406, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Mетан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 408, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Этан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 410, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Пропан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 412, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("N-бутан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 414, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("I-бутан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 416, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("N-пентан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 418, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("I-пентан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 420, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Нео-пентан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 422, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Гексан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 424, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Гептан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 426, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Октан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 428, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Нонан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 430, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Декан", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 432, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Сероводород", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 434, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Вода", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 436, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Гелий", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 438, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Кислород", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 440, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Окись углерода", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 442, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Этилен", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 444, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Пропен", value.ToString()));

//                data = SendMessageToDevice(new Request3(NetworkAddress, 446, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                value = floatValue.Value;
//                result.Add(new Constant("Аргон", value.ToString()));
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResultConstant { State = SurveyResultState.Success, Records = result };
//        }

//        public override SurveyResultData ReadCurrentValues()
//        {
//            var result = new List<Data>();
//            try
//            {

//                var data = SendMessageToDevice(new Request3(NetworkAddress, 475, 1));
//                var shortValue = new ResponseShort(data.ToArray());
//                var year = shortValue.Value;
//                data = SendMessageToDevice(new Request3(NetworkAddress, 476, 1));
//                shortValue = new ResponseShort(data.ToArray());
//                var month = shortValue.Value;
//                data = SendMessageToDevice(new Request3(NetworkAddress, 477, 1));
//                shortValue = new ResponseShort(data.ToArray());
//                var day = shortValue.Value;
//                data = SendMessageToDevice(new Request3(NetworkAddress, 478, 1));
//                shortValue = new ResponseShort(data.ToArray());
//                var hour = shortValue.Value;

//                data = SendMessageToDevice(new Request3(NetworkAddress, 479, 1));
//                shortValue = new ResponseShort(data.ToArray());
//                var minute = shortValue.Value;
//                data = SendMessageToDevice(new Request3(NetworkAddress, 480, 1));
//                shortValue = new ResponseShort(data.ToArray());
//                var second = shortValue.Value;
//                var date = new DateTime(year, month, day, hour, minute, second);
//                OnSendMessage(string.Format("текущая дата на счетчике {0:dd.MM.yyyy HH:mm:ss}", date));

//                //Норм. объем. расх.
//                data = SendMessageToDevice(new Request3(NetworkAddress, 204, 2));
//                var floatValue = new ResponseFloat(data.ToArray());
//                result.Add(new Data("VolumeConsumptionNormal", MeasuringUnitType.m3_h, date, floatValue.Value));

//                //Рабочий расход
//                data = SendMessageToDevice(new Request3(NetworkAddress, 206, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                result.Add(new Data("VolumeConsumptionWork", MeasuringUnitType.m3_h, date, floatValue.Value));

//                //Абсолютное давл.
//                data = SendMessageToDevice(new Request3(NetworkAddress, 300, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                result.Add(new Data("Pressure", MeasuringUnitType.MPa, date, floatValue.Value));

//                //Температура газа
//                data = SendMessageToDevice(new Request3(NetworkAddress, 302, 2));
//                floatValue = new ResponseFloat(data.ToArray());
//                result.Add(new Data("Temperature", MeasuringUnitType.C, date, floatValue.Value));
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResultData { State = SurveyResultState.Success, Records = result };
//        }

//        private DataResponse CheckHour(byte group, byte channel, int number, IEnumerable<int> numberHistory, DateTime targetDate, Func<DateTime, DateTime, int> GetDelta, int maxDelta = 0)
//        {
//            //запрашиваем данные
//            DataResponse response = GetResponse(group, channel, number);

//            if (number < response.Number) return null;

//            //определяем разницу между полученной и целевой датами
//            var delta = GetDelta(targetDate, response.Date);

//            //если укладывается в нужный диапазон - значит все нормально
//            if (Math.Abs(delta) <= Math.Abs(maxDelta))
//            {
//                if (maxDelta != 0)//если отклонение возможно попробуем поискать вариант получше чем найденный
//                {
//                    while (true)
//                    {
//                        if (delta > 0)
//                        {
//                            number++;
//                        }
//                        else
//                        {
//                            number--;
//                        }

//                        var tempResponse = GetResponse(group, channel, number);
//                        var tempDelta = GetDelta(targetDate, response.Date);
//                        if (Math.Abs(tempDelta) >= Math.Abs(delta))
//                        {
//                            break;
//                        }
//                        else
//                        {
//                            delta = tempDelta;
//                            response = tempResponse;
//                        }
//                    }
//                }
//                return response;
//            }

//            //вычисляем скорректированный номер записи
//            if (delta > 0)
//            {
//                if (delta > 100)
//                    number += 100;
//                else
//                    number += delta;
//            }
//            else
//            {
//                if (delta < -100)
//                    number = response.Number - 100;
//                else
//                    number = response.Number + delta;
//            }

//            //добавляем номер записи в историю
//            var checkedNumbers = new List<int>(numberHistory);
//            //checkedNumbers.Add(number);

//            //если такой номер уже встречался, мы ходим по кругу
//            if (checkedNumbers.Contains(response.Number) || number < 0)
//            {
//                return null;
//            }
//            else
//            {
//                //пробуем еще раз
//                checkedNumbers.Add(response.Number);
//                return CheckHour(group, channel, number, checkedNumbers, targetDate, GetDelta);
//            }
//        }

//        private DataResponse Check(byte group, byte channel, int number, IEnumerable<int> numberHistory, DateTime targetDate, Func<DateTime, DateTime, int> GetDelta)
//        {
//            //запрашиваем данные
//            var request = new DataRequest(NetworkAddress, group, channel, number);
//            var data = SendMessageToDevice(request);
//            var response = new DataResponse(data.ToArray());

//            //if (response.Date.Minute != 0)
//            //{
//            //    return response;
//            //}

//            if (number < response.Number) return null;

//            byte normalGroup = 0;
//            var n = response.Number;
//            var dt = response.Date;
//            while (dt.Minute != 0)
//            {                
//                DataResponse foo = GetResponse(normalGroup, 2, n);
//                if (response != null)
//                {
//                    double volumeConsumptionNormalTotal = foo.Value;
//                    OnSendMessage(string.Format("прочитан объемный расход при н.у. (нарастающий) за {0:dd.MM.yyyy HH:mm} ({1})", foo.Date, volumeConsumptionNormalTotal));
//                }
//                OnSendMessage(string.Format("прочитана запись #{0} за {1:dd.MM.yyyy HH:mm}", response.Number, response.Date));
//                dt = foo.Date;
//                n++;
//            }

//            //определяем разницу между полученной и целевой датами
//            var delta = GetDelta(targetDate, response.Date);

//            //если разницы нет - т.е. получена ожидаемая дата, возвращаем ответ
//            if (delta == 0)
//            {
//                return response;
//            }

//            ///тут
//            //вычисляем скорректированный номер записи
//            if (delta > 0)
//            {
//                number += delta;
//            }
//            else
//            {
//                number = response.Number + delta;
//            }

//            //добавляем номер записи в историю
//            var checkedNumbers = new List<int>(numberHistory);

//            //если такой номер уже встречался, мы ходим по кругу
//            if (checkedNumbers.Contains(response.Number) || number < 0)
//            {
//                return null;
//            }
//            else
//            {
//                //пробуем еще раз
//                checkedNumbers.Add(response.Number);
//                return Check(group, channel, number, checkedNumbers, targetDate, GetDelta);
//            }
//        }

//        private Tuple<DataResponse, bool> Check1(byte group, byte channel, int number, IEnumerable<int> numberHistory, DateTime targetDate, Func<DateTime, DateTime, int> GetDelta)
//        {
//            //запрашиваем данные
//            var request = new DataRequest(NetworkAddress, group, channel, number);
//            var data = SendMessageToDevice(request);
//            var response = new DataResponse(data.ToArray());

//            if (response.Date.Minute != 0)
//            {
//                return new Tuple<DataResponse, bool>(response, false);
//            }

//            if (number < response.Number) return null;

//            //определяем разницу между полученной и целевой датами
//            var delta = GetDelta(targetDate, response.Date);

//            //если разницы нет - т.е. получена ожидаемая дата, возвращаем ответ
//            if (delta == 0)
//            {
//                return new Tuple<DataResponse, bool>(response, true);
//            }

//            ///тут
//            //вычисляем скорректированный номер записи
//            if (delta > 0)
//            {
//                number += delta;
//            }
//            else
//            {
//                number = response.Number + delta;
//            }

//            //добавляем номер записи в историю
//            var checkedNumbers = new List<int>(numberHistory);

//            //если такой номер уже встречался, мы ходим по кругу
//            if (checkedNumbers.Contains(response.Number) || number < 0)
//            {
//                return null;
//            }
//            else
//            {
//                //пробуем еще раз
//                checkedNumbers.Add(response.Number);
//                return Check1(group, channel, number, checkedNumbers, targetDate, GetDelta);
//            }
//        }

//        private DataResponse GetResponse(byte group, byte channel, int number)
//        {
//            var request = new DataRequest(NetworkAddress, group, channel, number);
//            var data = SendMessageToDevice(request);
//            if (data != null)
//                return new DataResponse(data.ToArray());
//            return null;
//        }

//        public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
//        {
//            var result = new List<Data>();
//            try
//            {
//                //группа настраиваемых архивов
//                //см. док. стр.4 (архив настраиваемый, запись газовых суток)
//                byte group = 8;
//                var number = 1;

//                //var allDates = new List<DateTime>(dates);
//                //добавляем дату первого дня
//                //allDates.Insert(0, dates.Min().AddDays(-1));

//                foreach (var date in dates)
//                {
//                    var record = Check(group, 1, number, new int[] { }, date.AddDays(1), ((d1, d2) => (d1.Date - d2.Date).Days));
//                    if (record == null)
//                    {
//                        OnSendMessage(string.Format("записи за {0:dd.MM.yy} отсутствуют", date));
//                    }
//                    else
//                    {
//                        //todo читаем все параметры хере
//                        number = record.Number;

//                        var volConTotCelyaChast = record.Value;

//                        //запрашиваем данные
//                        //температура
//                        var request = new DataRequest(NetworkAddress, group, 11, number);
//                        IEnumerable<byte> data = SendMessageToDevice(request);
//                        var response = new DataResponse(data.ToArray());
//                        result.Add(new Data("Temperature", MeasuringUnitType.C, date, response.Value));
//                        OnSendMessage(string.Format("прочитана температура за {0:dd.MM.yyyy} ({1})", date, response.Value));

//                        //Нештатный расход накопленный при стандартных условиях
//                        request = new DataRequest(NetworkAddress, group, 12, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        double volumeConsumptionNormalTotalNs = response.Value;
//                        request = new DataRequest(NetworkAddress, group, 13, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        volumeConsumptionNormalTotalNs += response.Value;
//                        result.Add(new Data("VolumeConsumptionNormalNs", MeasuringUnitType.m3, date, volumeConsumptionNormalTotalNs));
//                        OnSendMessage(string.Format("прочитан НЕШТАТНЫЙ объемный расход при н.у. за {0:dd.MM.yyyy} ({1})", date, volumeConsumptionNormalTotalNs));

//                        //Расход накопленный при стандартных условиях
//                        request = new DataRequest(NetworkAddress, group, 0, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        double volumeConsumptionNormalTotal = response.Value;
//                        request = new DataRequest(NetworkAddress, group, 1, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        volumeConsumptionNormalTotal += response.Value;
//                        //volumeConsumptionNormalTotal += volumeConsumptionNormalTotalNs;
//                        result.Add(new Data("VolumeConsumptionNormalTotal", MeasuringUnitType.m3, date, volumeConsumptionNormalTotal));
//                        OnSendMessage(string.Format("прочитан объемный расход при н.у. (нарастающий) за {0:dd.MM.yyyy} ({1})", date, volumeConsumptionNormalTotal));

//                        //Расход за прошедшие сутки при стандартных условиях
//                        request = new DataRequest(NetworkAddress, group, 4, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        double volumeConsumptionNormal = response.Value;
//                        request = new DataRequest(NetworkAddress, group, 5, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        volumeConsumptionNormal += response.Value;
//                        result.Add(new Data("VolumeConsumptionNormal", MeasuringUnitType.m3, date, volumeConsumptionNormal));
//                        OnSendMessage(string.Format("прочитан объемный расход при н.у. (за сутки) за {0:dd.MM.yyyy} ({1})", date, volumeConsumptionNormal));

//                        //Расход накопленный при рабочих условиях
//                        request = new DataRequest(NetworkAddress, group, 2, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        double volumeConsumptionWorkTotal = response.Value;
//                        request = new DataRequest(NetworkAddress, group, 3, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        volumeConsumptionWorkTotal += response.Value;
//                        result.Add(new Data("VolumeConsumptionWorkTotal", MeasuringUnitType.m3, date, volumeConsumptionWorkTotal));
//                        OnSendMessage(string.Format("прочитан объемный расход при р.у. (нарастающий) за {0:dd.MM.yyyy} ({1})", date, volumeConsumptionWorkTotal));

//                        //Расход за прошедшие сутки при стандартных условиях
//                        request = new DataRequest(NetworkAddress, group, 6, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        double volumeConsumptionWork = response.Value;
//                        request = new DataRequest(NetworkAddress, group, 7, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        volumeConsumptionWork += response.Value;
//                        result.Add(new Data("VolumeConsumptionWork", MeasuringUnitType.m3, date, volumeConsumptionWork));
//                        OnSendMessage(string.Format("прочитан объемный расход при р.у. (за сутки) за {0:dd.MM.yyyy} ({1})", date, volumeConsumptionWork));

//                        //Измеренное давление, абсолютное
//                        //request = new DataRequest(NetworkAddress, group, 8, number);
//                        //data = SendMessageToDevice(request);
//                        //response = new DataResponse(data.ToArray());
//                        //result.Add(new Data(ParameterType.Pressure, MeasuringUnitType.Bar, date, response.Value, CalculationType.Average, 1));

//                        //Среднесуточное давление, абсолютное
//                        request = new DataRequest(NetworkAddress, group, 9, number);
//                        data = SendMessageToDevice(request);
//                        response = new DataResponse(data.ToArray());
//                        result.Add(new Data("Pressure", MeasuringUnitType.Bar, date, response.Value));
//                        OnSendMessage(string.Format("прочитано давление за {0:dd.MM.yyyy} ({1})", date, response.Value));
//                        number++;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResultData { State = SurveyResultState.Success, Records = result };
//        }

//        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
//        {
//            var result = new List<Data>();

//            try
//            {
//                //группа настраиваемых архивов
//                //см. док. стр.4 (архив настраиваемый, запись газовых суток)
//                byte normalGroup = 0;
//                int number = int.MaxValue;

//                foreach (var date in dates.OrderByDescending(d => d))
//                {
//                    var record = Check(normalGroup, 1, number, new int[] { }, date.AddHours(1), ((d1, d2) => (int)((d1 - d2).TotalHours)));

//                    if (record == null)
//                    {
//                        //попробуем поискать среди аварийныйх записей
//                        OnSendMessage(string.Format("записи за {0:dd.MM.yy HH:mm} отсутствуют", date));
//                    }
//                    else
//                    {

//                        //todo читаем все параметры хере
//                        number = record.Number;

//                        OnSendMessage(string.Format("прочитана запись #{0} за {1:dd.MM.yyyy HH:mm}", number, date));

//                        //Расход при рабочих условиях
//                        double volumeConsumptionWorkTotal = record.Value;
//                        result.Add(new Data("VolumeConsumptionWorkTotal", MeasuringUnitType.m3, date,
//                                            volumeConsumptionWorkTotal));
//                        //OnSendMessage(string.Format("прочитан объемный расход при р.у. (нарастающий) за {0:dd.MM.yyyy HH:mm} ({1})", date, volumeConsumptionWorkTotal));

//                        //Расход при стандартных условиях
//                        DataResponse response = GetResponse(normalGroup, 2, number);
//                        if (response != null)
//                        {
//                            double volumeConsumptionNormalTotal = response.Value;
//                            result.Add(new Data("VolumeConsumptionNormalTotal", MeasuringUnitType.m3, date,
//                                                volumeConsumptionNormalTotal));
//                            // OnSendMessage(string.Format("прочитан объемный расход при н.у. (нарастающий) за {0:dd.MM.yyyy HH:mm} ({1})", date, volumeConsumptionNormalTotal));
//                        }

//                        //Измеренное давление, абсолютное
//                        response = GetResponse(normalGroup, 4, number);
//                        if (response != null)
//                        {
//                            result.Add(new Data("Pressure", MeasuringUnitType.Bar, date, response.Value));
//                            // OnSendMessage(string.Format("прочитано давление за {0:dd.MM.yyyy HH:mm} ({1})", date, response.Value));
//                        }

//                        //температура
//                        response = GetResponse(normalGroup, 5, number);
//                        if (response != null)
//                        {
//                            result.Add(new Data("Temperature", MeasuringUnitType.C, date, response.Value));
//                            //OnSendMessage(string.Format("прочитана температура за {0:dd.MM.yyyy HH:mm} ({1})", date, response.Value));
//                        }
//                    }
//                }

//                foreach (var date in dates.OrderByDescending(d => d))
//                {
//                    var foo = Check1(1, 1, number, new int[] { }, date.AddHours(1), ((d1, d2) => (int)((d1 - d2).TotalHours)));

//                    if (foo == null)
//                    {
//                        //попробуем поискать среди аварийныйх записей
//                        OnSendMessage(string.Format("НЕШТАТНЫЕ записи за {0:dd.MM.yy HH:mm} отсутствуют", date));
//                    }
//                    else
//                    {
//                        //todo читаем все параметры хере
//                        var record = foo.Item1;
//                        number = record.Number;
//                        OnSendMessage(string.Format("прочитана запись #{0} за {1:dd.MM.yyyy HH:mm}", number, date));
//                        //Расход при рабочих условиях
//                        double volumeConsumptionWorkTotal = record.Value;
//                        result.Add(new Data("VolumeConsumptionWorkNsTotal", MeasuringUnitType.m3, date,
//                                            volumeConsumptionWorkTotal));
//                        //OnSendMessage(string.Format("прочитан НЕШТАТНЫЙ объемный расход при р.у. (нарастающий) за {0:dd.MM.yyyy HH:mm} ({1})", date, volumeConsumptionWorkTotal));

//                        //Расход при стандартных условиях
//                        DataResponse response = GetResponse(normalGroup, 2, number);
//                        if (response != null)
//                        {
//                            double volumeConsumptionNormalTotal = response.Value;
//                            result.Add(new Data("VolumeConsumptionNormalNsTotal", MeasuringUnitType.m3, date,
//                                                volumeConsumptionNormalTotal));
//                            //OnSendMessage(string.Format("прочитан НЕШТАТНЫЙ объемный расход при н.у. (нарастающий) за {0:dd.MM.yyyy HH:mm} ({1})", date, volumeConsumptionNormalTotal));
//                        }
//                    }
//                }
//                ////преобразуем к дате старту
//                //foreach (var record in result)
//                //{
//                //    record.Date = record.Date.AddHours(-1);
//                //}
//            }
//            catch (Exception ex)
//            {
//                var iex = ex;
//                var message = "";
//                do
//                {
//                    message += "->" + iex.Message;
//                    iex = iex.InnerException;
//                }
//                while (iex != null);
//                OnSendMessage(string.Format("ошибка: {0}", message));
//            }
//            return new SurveyResultData { State = SurveyResultState.Success, Records = result };
//        }

//        public override SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
//        {
//            var result = new List<AbnormalEvents>();
//            try
//            {
//                //группа настраиваемых архивов
//                //см. док. стр.4 (архив настраиваемый, запись газовых суток)
//                byte group = 20;
//                var record = int.MaxValue;
//                DataResponse response;

//                var checkedRecords = new List<int>();
//                AbnormalEvents last = null;
//                int joined = 0;
//                do
//                {
//                    response = new DataResponse(SendMessageToDevice(new DataRequest(NetworkAddress, group, 1, record)).ToArray());
//                    record = response.Number - 1;
//                    if (checkedRecords.Contains(record)) break;
//                    checkedRecords.Add(record);

//                    var ae = new AbnormalEvents() { DateTime = response.Date, Description = AbnormalCodes.GetAbnormal((int)response.Value), Duration = 0 };
//                    if (last != null && last.Description == ae.Description && (ae.DateTime - last.DateTime).TotalSeconds <= 10)
//                    {
//                        last.Duration = (int)(ae.DateTime - last.DateTime).TotalSeconds;
//                        joined++;
//                    }
//                    else
//                    {
//                        last = ae;
//                        result.Add(last);
//                        OnSendMessage(string.Format("прочитана запись №{0} за {1:dd.MM.yyyy HH:mm:ss} ({2})", response.Number, ae.DateTime, ae.Description));
//                        joined = 0;
//                    }

//                } while (response != null && response.Date >= dateStart);
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }
//            return new SurveyResultAbnormalEvents { State = SurveyResultState.Success, Records = result };
//        }
//    }
//}

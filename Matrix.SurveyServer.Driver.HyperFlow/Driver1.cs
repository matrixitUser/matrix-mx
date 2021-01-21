//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    public class Driver1 : BaseDriver
//    {
//        //private byte[] SendMessageToDevice(byte[] request)
//        //{
//        //    int attemtingCount = 0;

//        //    while (attemtingCount < 5)
//        //    {
//        //        attemtingCount++;

//        //        isDataReceived = false;
//        //        receivedBuffer = null;

//        //        RaiseDataSended(request);
//        //        Wait(7000);

//        //        if (isDataReceived)
//        //        {
//        //            return receivedBuffer;
//        //        }
//        //    }
//        //    return null;
//        //}

//        //public override SurveyResult Ping()
//        //{
//        //    try
//        //    {
//        //        var request = new Request(Direction.MasterToSlave, NetworkAddress, 0x21, new byte[] { 0x00, 0x01, 0x02, 0x03 });
//        //        //рукопожатие
//        //        //var request = new byte[]
//        //        //{
//        //        //    0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,	//преамбула
//        //        //    0x02,	//стартовый байт
//        //        //    NetworkAddress,	//адрес
//        //        //    0x21,	//команда
//        //        //    0x04,
//        //        //    0x00,0x01,0x02,0x03,
//        //        //    0x26
//        //        //};
//        //        var response = SendMessageToDevice(request.GetBytes());
//        //        OnSendMessage(string.Join(",", response));
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OnSendMessage(ex.Message);
//        //    }
//        //    return new SurveyResult() { State = SurveyResultState.Success };
//        //}

//        //public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
//        //{
//        //    List<Data> records = null;
//        //    try
//        //    {
//        //        records = ReadTrackDatesByInx(dates, new HourTrack());
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OnSendMessage(ex.Message);
//        //    }
//        //    return new SurveyResultData() { State = SurveyResultState.Success, Records = records };
//        //}

//        //public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
//        //{
//        //    List<Data> records = null;// = new List<Data>();
//        //    try
//        //    {
//        //        records = ReadTrackDatesByInx(dates, new DayTrack());
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        OnSendMessage(ex.Message);
//        //    }
//        //    return new SurveyResultData { State = SurveyResultState.Success, Records = records };
//        //}

//        //public override SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
//        //{
//        //    var events = new List<AbnormalEvents>();

//        //    //OnSendMessage("Запрос событий от {0} до {1}", dateStart, dateEnd);
//        //    for (var i = 0; ; i++)
//        //    {
//        //        var req = new Request(Direction.MasterToSlave, NetworkAddress, 141, BitConverter.GetBytes(i));
//        //        var rsp = SendMessageToDevice(req.GetBytes());
//        //        var evt = new EventResponse(rsp);

//        //        OnSendMessage("Прочтено событие#{0} дата {1}", i, evt.Event);

//        //        if (evt.Event == DateTime.MinValue || evt.Event < dateStart) break;
//        //        if (evt.Event <= dateEnd)
//        //            events.Add(evt.Events);
//        //    }

//        //    return new SurveyResultAbnormalEvents { Records = events, State = SurveyResultState.Success };
//        //}

//        //public override SurveyResultData ReadCurrentValues()
//        //{
//        //    var records = new List<Data>();

//        //    Request req;
//        //    byte[] rsp;
//        //    RegisterResponse reg;
//        //    Data data;

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 23 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    var sec = reg.GetAsULong();
//        //    var dt = new DateTime(1997, 1, 1, 0, 0, 0, 0).AddSeconds(sec);
//        //    OnSendMessage("текушее время в счетчике = {0}", dt);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 0 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("Qr", MeasuringUnitType.m3_h, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 1 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("P", MeasuringUnitType.kgs_kvSm, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 2 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("T", MeasuringUnitType.C, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 3 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("Q", MeasuringUnitType.m3_h, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 4 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("Wm", MeasuringUnitType.GDj, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 5, 6 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("накопленный расход с.у", MeasuringUnitType.Unknown, dt, reg.GetAsULong2());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    //10 – баром.давление (кгс/см2), float
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 10 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("баром.давление", MeasuringUnitType.kgs_kvSm, dt, reg.GetAsFloat());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 24 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("напряжение литиевой батареи", MeasuringUnitType.V, dt, reg.GetAsFloat() / 1000);
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 33, 34 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("накопленная теплота сгорания", MeasuringUnitType.Unknown, dt, reg.GetAsULong2());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 40 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("время наработки от литиевой батареи", MeasuringUnitType.sec, dt, reg.GetAsULong());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 41 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("общее время наработки", MeasuringUnitType.sec, dt, reg.GetAsULong());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 108, 109 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    data = new Data("накопленный расход р.у", MeasuringUnitType.Unknown, dt, reg.GetAsULong2());
//        //    OnSendMessage("{0}", data);
//        //    records.Add(data);

//        //    return new SurveyResultData { Records = records, State = SurveyResultState.Success };
//        //}

//        //public override SurveyResultConstant ReadConstants()
//        //{
//        //    var records = new List<Constant>();

//        //    var req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 7 });
//        //    var rsp = SendMessageToDevice(req.GetBytes());
//        //    var reg = new RegisterResponse(rsp);
//        //    var con = new Constant("Коммерческий час", string.Format("{0}", reg.GetAsULong()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 8 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("скорость отсечки", string.Format("{0} м/сек", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 9 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("плотность н.у.", string.Format("{0} кг/м3", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //11 – содержание СО2 (молярных долей), float
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 11 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("содержание СО2", string.Format("{0} мол.долей", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //12 – содержание N2 (молярных долей), float 
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 12 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("содержание N2", string.Format("{0} мол.долей", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //13 – диаметр трубопровода (мм) н.у., float
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 13 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("диаметр трубопровода", string.Format("{0} мм", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //14 – базовое расстояние в канале А  (мм) при н.у., float
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 14 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("базовое расстояние в канале А", string.Format("{0} мм", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //15 – материал  трубопровода, unsigned long
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 15 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("материал трубопровода", string.Format("{0}", reg.GetAsULong()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //20 – измеряемая среда, unsigned long (1-природный газ, 4-другая)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 20 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    var mat = reg.GetAsULong();
//        //    con = new Constant("измеряемая среда", mat == 1 ? "природный газ" : mat == 4 ? "другое" : string.Format("другое ({0})", mat));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //21 – эмуляция канала P (кгс/см2), float (-800 - выключена)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 21 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    var emu = reg.GetAsFloat();
//        //    con = new Constant("эмуляция канала P", (emu == -800) ? "выключена" : string.Format("{0} кгс/см2", emu));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //22 – эмуляция канала T (град. Ц), float (-800 - выключена)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 33, new byte[] { 22 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    emu = reg.GetAsFloat();
//        //    con = new Constant("эмуляция канала T", (emu == -800) ? "выключена" : string.Format("{0} град. Ц", emu));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //28 – метод расчета коэфф.сжимаемости газа, unsigned long (0-NX19m 1-GERG91)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 28 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    mat = reg.GetAsULong();
//        //    con = new Constant("метод расчета коэфф.сжимаемости газа", (mat == 0) ? "NX19m" : (mat == 1) ? "GERG91" : string.Format("другое ({0})", mat));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //29 – тип термодатчика, unsigned long (0-100М, 1-50М, 2-100П, 3-50П) 
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 29 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    mat = reg.GetAsULong();
//        //    con = new Constant("тип термодатчика",
//        //        (mat == 0) ? "100М" : (mat == 1) ? "50М" : (mat == 2) ? "100П" : (mat == 3) ? "50П" : string.Format("другое ({0})", mat));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //30 - эмуляция канала измерения скорости (м/сек), float (-800 - выключена)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 30 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    emu = reg.GetAsFloat();
//        //    con = new Constant("эмуляция канала измерения скорости", (emu == -800) ? "выключена" : string.Format("{0} м/сек", emu));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //32 – цикл измерения, unsigned long (2 – 30 сек.)
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 32 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("цикл измерения", string.Format("{0}", reg.GetAsULong()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //42 – заводской номер прибора, unsigned long
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 42 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("заводской номер прибора", string.Format("{0}", reg.GetAsULong()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //64 - Направление потока 0-прямое 1-обратное 2-автовыбор (реверс) unsigned long
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 64 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    mat = reg.GetAsULong();
//        //    con = new Constant("Направление потока",
//        //        (mat == 0) ? "прямое" : (mat == 1) ? "обратное" : (mat == 2) ? "автовыбор (реверс)" : string.Format("другое ({0})", mat));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    //121 - базовое расстояние в канале B  (мм) при н.у., float 
//        //    req = new Request(Direction.MasterToSlave, NetworkAddress, 136, new byte[] { 121 });
//        //    rsp = SendMessageToDevice(req.GetBytes());
//        //    reg = new RegisterResponse(rsp);
//        //    con = new Constant("базовое расстояние в канале B", string.Format("{0} мм", reg.GetAsFloat()));
//        //    OnSendMessage("{0}", con);
//        //    records.Add(con);

//        //    return new SurveyResultConstant { Records = records, State = SurveyResultState.Success };
//        //}

//        /*
//        private void ReadArchiveDatesByInx(ArchiveType archiveType, IEnumerable<DateTime> dates, Func<int, DateTime> readRecordByInx)
//        {

//            var sInx = 0;
//            var sDate = readRecordByInx(sInx);//нулевая запись

//            if (sDate == DateTime.MinValue) throw new Exception();
//            var ddates = dates.OrderByDescending(d => d).ToArray();

//            foreach (var tDate in ddates)
//            {
//                if (tDate >= sDate) continue;
//                OnSendMessage(string.Format("Поиск даты {0}", tDate));

//                var offset = GetOffset(sDate, tDate, archiveType);
//                if (offset < 1) continue;

//                var cInx = sInx;
//                for (var coff = (int)offset; coff >= 0; coff--)
//                {
//                    DateTime cDate = readRecordByInx(cInx + coff);
//                    if (coff == (int)offset)
//                    {
//                        sDate = cDate;
//                        sInx = cInx + coff;
//                    }

//                    if (cDate == DateTime.MinValue || cDate > tDate)
//                    {
//                        //next
//                        continue;
//                    }
//                    if (cDate == tDate)
//                    {
//                        //target found 
//                        break;
//                    }
//                    if (cDate < tDate)
//                    {
//                        //тут можно проверить еще одну запись
//                        //
//                        break;
//                    }

//                }

//            }
//        }
//        */
//        /*
//        private DateTime ReadTrackRecordByInx(int sInx, ITrack track)
//        {
//            var response0 = SendMessageToDevice(track.GetRequest(NetworkAddress, sInx).GetBytes());
//            records.AddRange(track.GetData(response0));
//            var sDate = track.GetDate(response0);
//        }*/

//        /*
//         * dates - desc
//         * arch - desc
//         * ---------------------------------
//         * Коротко: бегаем по архиву по индексу
//         * 
//         * ПОДГОТОВКА
//         * startInx = 0
//         * читаем запись в архиве startInx -> добавляем в список records
//         * 
//         * АЛГОРИТМ
//         * Фильтр дат
//         * Находим startdate = dateOf(startInx)
//         * Скипаем dates >= startdate, находим targetdate и разницу со startdate -> это offset
//         * Проверяем, чтобы offset был больше нуля; иначе - скипаем
//         * 
//         * Поиск записи в диапазоне startInx->startInx+offset
//         * читаем запись в архиве startInx -> добавляем в список records
//         * если дата 'раньше' target => шагаем по индексам в сторону startInx
//         * больше - скипаем дату (или читаем еще одну запись для исключения перехода 'зимнее-летнее время')
//         * сохраняем новый startInx=startInx+offset
//         * 
//         */
//        //private List<Data> ReadTrackDatesByInx(IEnumerable<DateTime> dates, ITrack track)
//        //{
//        //    var records = new List<Data>();

//        //    var sInx = 0;
//        //    var response = SendMessageToDevice(track.GetRequest(NetworkAddress, sInx).GetBytes());
//        //    var sDate = track.GetDate(response);
//        //    records.AddRange(track.GetData(response));
//        //    OnSendMessage(string.Format("Прочтена запись#{0} дата {1}", sInx, sDate));

//        //    if (sDate == DateTime.MinValue) throw new Exception();

//        //    foreach (var tDate in dates.OrderByDescending(d => d).ToArray())
//        //    {
//        //        if (tDate >= sDate) continue;
//        //        //OnSendMessage(string.Format("Поиск даты {0}", tDate));

//        //        var offset = track.GetOffset(sDate, tDate);
//        //        if (offset < 1) continue;

//        //        var cInx = sInx;
//        //        for (var coff = offset; coff > 0; coff--)
//        //        {
//        //            //DateTime cDate = readRecordByInx(cInx + coff);
//        //            response = SendMessageToDevice(track.GetRequest(NetworkAddress, cInx + coff).GetBytes());
//        //            var cDate = track.GetDate(response);
//        //            records.AddRange(track.GetData(response));
//        //            OnSendMessage(string.Format("Прочтена запись#{0} дата {1}", cInx + coff, cDate));

//        //            if (coff == offset)
//        //            {
//        //                sDate = cDate;
//        //                sInx = cInx + coff;
//        //            }

//        //            if (cDate == DateTime.MinValue) //вышли за пределы архива
//        //            {
//        //                continue;
//        //            }
//        //            if (cDate < tDate) //проскочили запись (в архиве имеются дыры)
//        //            {
//        //                continue;
//        //            }
//        //            if (cDate == tDate) //попали в "яблочко"
//        //            {
//        //                break;
//        //            }
//        //            if (cDate > tDate) //запись не обнаружили (возможно, был переход летнее/зимнее время)
//        //            {
//        //                break;
//        //            }
//        //        }
//        //    }

//        //    return records;
//        //}
//        /*
//        private DateTime ReadRecordByInx(int inx, ArchiveType archiveType)
//        {
//            var request = new Request(Direction.MasterToSlave, NetworkAddress, (byte)archiveType, BitConverter.GetBytes(inx));
//            var response = SendMessageToDevice(request.GetBytes());
//            var dayResponse = new DayResponse(response);
//            return dayResponse.Day;
//        }
//        private double GetOffset(DateTime sInx, DateTime tInx, ArchiveType archiveType)
//        {
//            int per = 60 * 60 * (archiveType == ArchiveType.Daily ? 24 : 1);
//            OnSendMessage(string.Format("разница между {0} и {1} = {2}", sInx, tInx, (sInx - tInx).TotalSeconds / per));
//            return (sInx - tInx).TotalSeconds / per;
//        }
//        */
//    }
//}
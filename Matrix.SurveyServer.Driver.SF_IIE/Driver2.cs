//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using System.Timers;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common.Crc;


//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    public class Driver2 : BaseDriver
//    {
//        private const int ATTEMPTS_DEFAULT = 3;

//        public static byte ByteLow(int getLow)
//        {
//            return (byte)(getLow & 0xFF);
//        }
//        public static byte ByteHigh(int getHigh)
//        {
//            return (byte)((getHigh >> 8) & 0xFF);
//        }


//        #region serial-настройки
//        //300, 600, 1200, 2400, 4800, 9600 и 19200 бод; 8N1
//        #endregion

//        #region LowLevelFunc
//        //MsgPrefix Data CRC16
//        //   4        N    2        

//        //MsgPrefix
//        //SYNC ADDR MSG_LEN FUN_CODE
//        //SYNC=AA - запрос в суперфлоу
//        //     55 -       из суперфлоу
//        //ADDR = NetAddress (0 - выкл.; 255 - широковещательный, не реализован)
//        //MSG_LEN = MsgPrefix+Data+CRC16
//        //FUN_CODE функция

//        public enum FunCode
//        {
//            ReadSuperFloID = 1,
//            ReadStaticPar_pre_SF2xRU3C,
//            WriteStaticPar_pre_SF2xRU3C,
//            ReadInstCalcData,
//            ReadStaticPar_as_SF2xRU3C,
//            WriteStaticPar_as_SF2xRU3C,
//            ReadInstCalcData_Short,
//            WriteStaticPar_Short,
//            reserverd1,
//            ReadOldMonthStaticPar,
//            ReadOldMonthInstCalcData,
//            ReadDailyHistory = 20,
//            ReadHourlyHistory,
//            ReadAuditTrail,
//            ReadAlarmTrail,
//            ReadRunMinuteHistory,
//            ReadAllRunsMinuteHistory,
//            SetDateTime = 30,
//            Safe_SetDateTime,
//            ReadDayLightSavings,
//            Safe_WriteDayLightSavings,
//            ReadSystemParameters,
//            WriteSystemParameters,
//            ReadSuperFloVer,
//            Safe_WriteSystemParameters,
//            Safe_WriteStaticParameters_Short = 41,
//            ReadStaticPar_SF2xRU7,
//            WriteStaticPar_SF2xRU7,
//            Safe_WriteStaticParameters_SF2xRU7,
//            ReadRunIntantDataAnalog,
//            Safe_WriteRunInstantDataAnalog,
//            ErrorResponse = 0xff
//        };

//        private byte[] SendRequest(FunCode func, byte[] data = null, int attempts = ATTEMPTS_DEFAULT, int timeOut = 5000)
//        {
//            int attempt = 0;
//            byte[] answer = null;
//            byte[] request;

//            if (data == null)
//            {
//                request = new byte[6] { 0xaa, NetworkAddress, 6, (byte)func, 0, 0 };
//            }
//            else if (data.Length <= 256 - 6)
//            {
//                byte[] head = { 0xaa, NetworkAddress, (byte)(4 + data.Length + 2), (byte)func };
//                request = new byte[4 + data.Length + 2];

//                Array.Copy(head, request, head.Length);
//                Array.Copy(data, 0, request, head.Length, data.Length);
//            }
//            else
//            {
//                Show(string.Format("Ошибка: data.length=={0} > 250", data.Length));
//                return null;
//            }

//            var calc = new Crc16Modbus();

//            var crc = Crc.Calc(request, 0, request.Length - 2, calc);
//            request[request.Length - 2] = crc.CrcData[0];
//            request[request.Length - 1] = crc.CrcData[1];

//            do
//            {
//                Show(attempts < 2
//                         ? string.Format("SendRequest {0}:", func)
//                         : string.Format("SendRequest {0}: попытка {1}", func, attempt + 1));

//                OnSendMessage(string.Format("отправлено: [{0}]", string.Join(",", request.Select(b => b.ToString("X2")))));
//                RaiseDataSended(request);

//                Wait(timeOut);

//                if (isDataReceived)
//                {
//                    if (receivedBuffer.Length > 5)
//                    {
//                        crc = Crc.Calc(receivedBuffer, 0, receivedBuffer.Length - 2, calc);
//                        if (
//                            receivedBuffer[0] == 0x55 &&
//                            receivedBuffer[1] == NetworkAddress &&
//                            receivedBuffer[receivedBuffer.Length - 2] == crc.CrcData[0] &&
//                            receivedBuffer[receivedBuffer.Length - 1] == crc.CrcData[1])
//                        {
//                            if (receivedBuffer[3] == ((byte)(func) | (byte)(0x80)))
//                            {
//                                if (receivedBuffer[2] == receivedBuffer.Length)
//                                {
//                                    Show("SendRequest: ответ успешно распознан");
//                                    answer = new byte[receivedBuffer.Length - 5];
//                                    Array.Copy(receivedBuffer, 3, answer, 0, receivedBuffer.Length - 5);
//                                }
//                                else
//                                {
//                                    Show("SendRequest: несовпадение КБ");
//                                }
//                            }
//                            else if (receivedBuffer[3] == (byte)FunCode.ErrorResponse)
//                            {
//                                Show("SendRequest: получена ошибка");
//                                answer = new byte[1] { receivedBuffer[3] };
//                            }
//                            else
//                            {
//                                Show("SendRequest: получен неизвестный ответ");
//                            }
//                        }
//                        else
//                        {
//                            Show("SendRequest: не распознан");
//                        }
//                    }
//                    else
//                    {
//                        Show("SendRequest: размер кадра слишком мал");
//                    }
//                }
//                else
//                {
//                    Show("SendRequest: таймаут");
//                }
//            }
//            while (++attempt < attempts && answer == null);

//            return answer;
//        }

//        #endregion

//        private ID _id;

//        public ID ID
//        {
//            get { return _id ?? (_id = ReadSuperFloID()); }
//        }

//        ID ReadSuperFloID()
//        {
//            Show("ReadSuperFloID:");
//            byte[] answer = SendRequest(FunCode.ReadSuperFloID);
//            ID id = ID.Parse(answer);
//            if (id != null)
//            {
//                Show("ID прочитан");
//                _id = id;
//            }
//            else
//            {
//                Show("ID НЕ прочитан");
//            }
//            return id;
//        }

//        IEnumerable<Constant> ReadStaticParam(byte runNumber)
//        {
//            var datas = new List<Constant>();
//            Show("ReadStaticParam:");
//            byte[] answer = SendRequest(FunCode.ReadStaticPar_SF2xRU7, new byte[] { runNumber });

//            if (answer != null && answer.Length == 59)
//            {

//                //int runNumberGet = answer[1];
//                datas.Add(new Constant("Номер ИТ", answer[1].ToString()));
//                //char[] cur_runName = new char[16];
//                //Array.Copy(answer, 2, cur_runName, 0, 16);
//                datas.Add(new Constant("Наим. ИТ", Encoding.ASCII.GetString(answer, 2, 16)));//string runName = Encoding.ASCII.GetString(answer, 2, 16);//new string(cur_runName);

//                datas.Add(new Constant("Плотность", string.Format("{0} кг/м3", BitConverter.ToSingle(answer, 22 - 4))));//float gasDensity = BitConverter.ToSingle(answer, 22 - 4);
//                datas.Add(new Constant("Мол.% CO2", string.Format("{0} %", BitConverter.ToSingle(answer, 26 - 4)))); //float molePercentCO2 = BitConverter.ToSingle(answer, 26 - 4);
//                datas.Add(new Constant("Мол.% N2", string.Format("{0} %", BitConverter.ToSingle(answer, 30 - 4)))); //float molePercentN2 = BitConverter.ToSingle(answer, 30 - 4);
//                datas.Add(new Constant("Атм. давление", string.Format("{0} кПа", BitConverter.ToSingle(answer, 34 - 4)))); //float AtmosphericPressure = BitConverter.ToSingle(answer, 34 - 4);
//                datas.Add(new Constant("Отсечка по расходу", string.Format("{0} сек.", BitConverter.ToSingle(answer, 38 - 4)))); //float LowFlowCutoff = BitConverter.ToSingle(answer, 38 - 4);
//                datas.Add(new Constant("Отсечка по частоте", string.Format("{0} Гц", BitConverter.ToSingle(answer, 42 - 4)))); //float NoFlowCutoff = BitConverter.ToSingle(answer, 42 - 4);
//                datas.Add(new Constant("А-Коэф.преобр.турбины", string.Format("{0} 1/м3", BitConverter.ToSingle(answer, 46 - 4)))); //float AMeterFactor = BitConverter.ToSingle(answer, 46 - 4);

//                datas.Add(new Constant("Теплотворная способность", string.Format("{0} МДж/м3", BitConverter.ToSingle(answer, 50 - 4)))); //float SpecificEnergy = BitConverter.ToSingle(answer, 50 - 4);

//                datas.Add(new Constant("Коэф.масштабирования", answer[54 - 4].ToString())); //byte ScalingFactor = answer[54 - 4];
//                datas.Add(new Constant("Статус корректир.А", answer[55 - 4].ToString())); //byte CorrectionStatus = answer[55 - 4];
//                datas.Add(new Constant("Тип датч.давления", answer[56 - 4].ToString())); //byte PressureTransmitterType = answer[56 - 4];

//                const int dateOffset = 57 - 4;

//                var archiveRecordDateTimeString = string.Format("{0}.{1}.{2} {3}:{4}:{5}",
//                    answer[dateOffset + 1],//day
//                    answer[dateOffset + 0],//mon
//                    answer[dateOffset + 2],//yr
//                    answer[dateOffset + 3],//hr
//                    answer[dateOffset + 4],//min
//                    answer[dateOffset + 5]//sec                
//                );

//                DateTime current;
//                DateTime.TryParse(archiveRecordDateTimeString, out current);

//                datas.Add(new Constant("Текущая дата", current.ToString("HH:mm:ss dd.MM.yyyy")));
//            }
//            else
//            {
//                Show("Нет ответа");
//            }
//            return datas;
//        }

//        public override SurveyResultData ReadCurrentValues()
//        {
//            var datas = new List<Data>();
//            if (ID != null)
//            {
//                for (byte run = 1; run <= ID.Runs.Count(); run++)
//                {
//                    var answer = SendRequest(FunCode.ReadInstCalcData_Short, new byte[] { run });
//                    if (answer != null)
//                    {
//                        int dataOffset = -4;
//                        if (answer.Length == 48 + dataOffset)
//                        {
//                            //42    дата/время
//                            var current = Archive.ParseDateTime(answer, 42 + dataOffset, 6);


//                            datas.Add(new Data("Volume", MeasuringUnitType.m3, current, BitConverter.ToSingle(answer, dataOffset + 6)));//6     приращение объема при р.у   m3  float
//                            datas.Add(new Data("Pressure", MeasuringUnitType.kPa, current, BitConverter.ToSingle(answer, dataOffset + 10)));//10    статич. давление            кПа float
//                            datas.Add(new Data("Temperature", MeasuringUnitType.C, current, BitConverter.ToSingle(answer, dataOffset + 14)));//14    температура                 *С  float
//                            datas.Add(new Data("Energy", MeasuringUnitType.MDj, current, BitConverter.ToSingle(answer, dataOffset + 18)));//18    энергия                     МДж float
//                            datas.Add(new Data("Consumption", MeasuringUnitType.m3_h, current, BitConverter.ToSingle(answer, dataOffset + 22)));//22    мгновенный расход           m3/h    float
//                            var low = BitConverter.ToSingle(answer, dataOffset + 26); //26    расход за текущие сутки, дробная часть  scaled m3 float
//                            var bcd = ConvertHelper.BinDecToInt32(answer, dataOffset + 30, true);//30    расход за текущие сутки, BCD  scaled m3 BCD
//                            datas.Add(new Data("Volume", MeasuringUnitType.m3, current, low + bcd));
//                            low = BitConverter.ToSingle(answer, dataOffset + 34); //34    расход за прошедшие сутки, дробная часть  scaled m3 float
//                            bcd = ConvertHelper.BinDecToInt32(answer, dataOffset + 38, true);//38    расход за прошедшие сутки, BCD  scaled m3 BCD
//                            datas.Add(new Data("VolumeCoT", MeasuringUnitType.m3, current, low + bcd));

//                            //datas.Add(new Data(ParameterType.VolumeCoT, MeasuringUnitType.m3, current, BitConverter.ToSingle(answer, dataOffset + 6)));    //приращение объема при р.у, м3
//                            //datas.Add(new Data(ParameterType.VolumeCoT, MeasuringUnitType.m3, current, BitConverter.ToSingle(answer, dataOffset + 6)));    //давление, кПа
//                            //datas.Add(new Data(ParameterType.VolumeCoT, MeasuringUnitType.m3, current, BitConverter.ToSingle(answer, dataOffset + 6)));    //температура, *С
//                            //мгновенный расход, м3/ч
//                            //расход за текущие сутки, дробная часть
//                            //расход за текущие сутки, bcd часть
//                            //коэффициент преобразования турбины, 1/м3
//                            //расход при р.у м3/ч
//                            //К
//                            //Hs actual, МДж/м3
//                            //приращение объема при с.у, м3
//                            //энергия за текущие сутки, МДж
//                            //частота, Гц
//                            //Zc фактор сжимаемости при с.у
//                            //накопленный объем с начала работы, дробная часть

//                            /*
//                            int records = answer[6 - 4];
//                            Show(string.Format("{0}) {1:dd.MM.yyyy HH:mm} records={2} in answer[{3}]", requestCounter, dateFirst, records, answer.Length));

//                            for (int record = 0, dataOffset = -4;
//                                 record < records;
//                                 record++, dataOffset += 29)
//                            {
//                                int dateOffset = dataOffset + 8;

//                                datas.Add(new Data(ParameterType.Volume, MeasuringUnitType.m3, current,
//                                                   ConvertHelper.BinDecToInt32(answer, dataOffset + 13)));
//                                datas.Add(new Data(ParameterType.Power, MeasuringUnitType.MDj, current,
//                                                   BitConverter.ToSingle(answer, dataOffset + 17)));
//                                datas.Add(new Data(ParameterType.Consumption, MeasuringUnitType.m3_h, current,
//                                                   BitConverter.ToSingle(answer, dataOffset + 21)));
//                                datas.Add(new Data(ParameterType.PressureAbsolute, MeasuringUnitType.kPa, current,
//                                                   BitConverter.ToSingle(answer, dataOffset + 25)));
//                                datas.Add(new Data(ParameterType.TemperatureAverage, MeasuringUnitType.C, current,
//                                                   BitConverter.ToSingle(answer, dataOffset + 29)));
//                                datas.Add(new Data(ParameterType.VolumeNormal, MeasuringUnitType.m3, current,
//                                                   ConvertHelper.BinDecToInt32(answer, dataOffset + 33)));
//                            }*/
//                        }
//                        else
//                        {
//                            Show("Ошибка: Длина ответа меньше ожидаемой");
//                        }
//                    }
//                    else
//                    {
//                        Show("Ошибка: Нет ответа");
//                    }
//                }
//            }

//            //foreach (var data in datas)
//            //{
//            //    Show(string.Format("{0:yyyy.MM.dd HH:mm} {1}={2} {3}", data.Date, data.SystemParameter, data.Value, data.MeasuringUnit));
//            //}

//            return new SurveyResultData { Records = datas, State = SurveyResultState.Success };
//        }

//        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
//        {
//            var archiveType = FunCode.ReadHourlyHistory;
//            var datas = new List<Data>();
//            if (dates != null && dates.Any() && ID != null)
//            {
//                byte requestCounter = 0;
//                foreach (var dateFirst in dates)
//                {
//                    var dateLast = dateFirst;//.AddHours(1);
//                    for (byte run = 1; run <= ID.Runs.Count(); run++)
//                    {
//                        var answer = SendRequest(archiveType, new byte[]
//                                                                        {
//                                                                            run, requestCounter,
//                                                                            (byte) dateFirst.Month,
//                                                                            (byte) dateFirst.Day,
//                                                                            (byte) (dateFirst.Year%100),
//                                                                            (byte) dateFirst.Hour,
//                                                                            (byte) dateLast.Month,
//                                                                            (byte) dateLast.Day,
//                                                                            (byte) (dateLast.Year%100),
//                                                                            (byte) dateLast.Hour
//                                                                        });

//                        if (answer != null)
//                        {
//                            if (answer.Length > 3)
//                            {
//                                if (answer[3] == 0)
//                                {
//                                    requestCounter = 0;
//                                }
//                                else
//                                {
//                                    requestCounter++;
//                                }
//                                int records = answer[6 - 4];
//                                Show(string.Format("{0}) {1:dd.MM.yyyy HH:mm} records={2} in answer[{3}]", requestCounter, dateFirst, records, answer.Length));

//                                for (int record = 0, dataOffset = -4;
//                                     record < records;
//                                     record++, dataOffset += 29)
//                                {
//                                    var archive = Archive.Parse(archiveType, answer, dataOffset);
//                                    if (archive != null)
//                                    {
//                                        datas.AddRange(archive.Datas);
//                                    }
//                                    /*
//                                    int dateOffset = dataOffset + 8;
//                                    var archiveRecordDateTimeString = string.Format("{0}.{1}.{2} {3}:{4}:00",
//                                                                                    answer[dateOffset + 1], //day
//                                                                                    answer[dateOffset + 0], //mon
//                                                                                    answer[dateOffset + 2], //yr
//                                                                                    answer[dateOffset + 3], //hr
//                                                                                    answer[dateOffset + 4] //min
//                                        );
//                                    DateTime current;
//                                    DateTime.TryParse(archiveRecordDateTimeString, out current);

//                                    datas.Add(new Data(ParameterType.Volume, MeasuringUnitType.m3, current, ConvertHelper.BinDecToInt32(answer, dataOffset + 13, true)));//14*

//                                    byte specBit = (byte)(answer[dataOffset + 17 + 3] & 0x01); answer[dataOffset + 17 + 3] &= 0xfe;
//                                    datas.Add(new Data(ParameterType.Power, MeasuringUnitType.MDj, current, BitConverter.ToSingle(answer, dataOffset + 17)));

//                                    datas.Add(new Data(ParameterType.Consumption, MeasuringUnitType.m3_h, current, BitConverter.ToSingle(answer, dataOffset + 21)));

//                                    datas.Add(new Data(ParameterType.PressureAbsolute, MeasuringUnitType.kPa, current, BitConverter.ToSingle(answer, dataOffset + 25)));

//                                    datas.Add(new Data(ParameterType.TemperatureAverage, MeasuringUnitType.C, current, BitConverter.ToSingle(answer, dataOffset + 29)));

//                                    datas.Add(new Data(ParameterType.VolumeNormal, MeasuringUnitType.m3, current, ConvertHelper.BinDecToInt32(answer, dataOffset + 33)));//14
//                                     * */
//                                }
//                            }
//                            else
//                            {
//                                Show(string.Format("{0}) {1:dd.MM.yyyy HH:mm} NO records in answer[{2}]",
//                                    requestCounter, dateFirst, answer.Length));
//                            }
//                        }
//                        else
//                        {
//                            Show(string.Format("{0}) {1:dd.MM.yyyy HH:mm} NO answer", requestCounter, dateFirst));
//                        }
//                    }
//                }
//            }

//            //foreach (var data in datas)
//            //{
//            //    Show(string.Format("{0:yyyy.MM.dd HH:mm} {1}={2} {3}", data.Date, data.SystemParameter, data.Value, data.MeasuringUnit));
//            //}

//            return new SurveyResultData { State = SurveyResultState.Success, Records = datas };
//        }

//        public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
//        {
//            var records = new List<Data>();

//            var min = dates.Min();
//            var max = dates.Max();

//            DailyHistoryResponse resp;
//            byte n = 0;
//            do
//            {
//                resp = new DailyHistoryResponse(SendMessageToDevice(new DailyHistoryRequest(NetworkAddress, 1, n++, min, max)));
//                records.AddRange(resp.Records);
//            }while(resp.HasMore);

//            //while(resp.HasMore)
//            //{
//            //    var resp = new DailyHistoryResponse(SendMessageToDevice(new DailyHistoryRequest(NetworkAddress, 1, 0, min, max)));
//            //}

//            return new SurveyResultData { Records = records, State = SurveyResultState.Success };

//            //var archiveType = FunCode.ReadDailyHistory;
//            //var datas = new List<Data>();
//            //if (dates != null && dates.Any() && ID != null)
//            //{
//            //    byte requestCounter = 0;
//            //    foreach (var dateFirst in dates)
//            //    {
//            //        var dateLast = dateFirst;//.AddHours(1);
//            //        for (byte run = 1; run <= ID.Runs.Count(); run++)
//            //        {
//            //            var answer = SendRequest(archiveType, new byte[]
//            //                                                            {
//            //                                                                run, requestCounter,
//            //                                                                (byte) dateFirst.Month,
//            //                                                                (byte) dateFirst.Day,
//            //                                                                (byte) (dateFirst.Year%100),
//            //                                                                (byte) dateLast.Month,
//            //                                                                (byte) dateLast.Day,
//            //                                                                (byte) (dateLast.Year%100)
//            //                                                            });

//            //            if (answer != null)
//            //            {
//            //                if (answer.Length > 3)
//            //                {
//            //                    if (answer[3] == 0)
//            //                    {
//            //                        requestCounter = 0;
//            //                    }
//            //                    else
//            //                    {
//            //                        requestCounter++;
//            //                    }
//            //                    int records = answer[6 - 4];
//            //                    Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm} records={2} in answer[{3}]", requestCounter, dateFirst, records, answer.Length));

//            //                    for (int record = 0, dataOffset = -4;
//            //                            record < records;
//            //                            record++, dataOffset += 27)
//            //                    {
//            //                        var archive = Archive.Parse(archiveType, answer, dataOffset);
//            //                        if (archive != null)
//            //                        {
//            //                            datas.AddRange(archive.Datas);
//            //                        }
//            //                    }
//            //                }
//            //                else
//            //                {
//            //                    Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm} NO records in answer[{2}]",
//            //                        requestCounter, dateFirst, answer.Length));
//            //                }
//            //            }
//            //            else
//            //            {
//            //                Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm} NO answer", requestCounter, dateFirst));
//            //            }
//            //        }
//            //    }
//            //}

//            ////foreach (var data in datas)
//            ////{
//            ////    Show(string.Format("{0:yyyy.MM.dd HH:mm} {1}={2} {3}", data.Date, data.SystemParameter, data.Value, data.MeasuringUnit));
//            ////}

//            //return new SurveyResultData { Records = datas, State = SurveyResultState.Success };
//        }

//        private byte[] SendMessageToDevice(Request request)
//        {
//            byte[] response = null;
//            bool success = false;
//            int attemtingCount = 0;

//            while (!success && attemtingCount < 5)
//            {
//                attemtingCount++;
//                isDataReceived = false;
//                receivedBuffer = null;
//                var bytes = request.GetBytes();
//                OnSendMessage(string.Format("отправлено: [{0}]", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//                RaiseDataSended(bytes);
//                Wait(7000);

//                if (isDataReceived)
//                {
//                    response = receivedBuffer;
//                    OnSendMessage(string.Format("получено: [{0}]", string.Join(",", response.Select(b => b.ToString("X2")))));
//                    success = true;
//                }
//            }

//            return response;
//        }

//        private Dictionary<int, string> _alarms = new Dictionary<int, string>()
//                                             {
//                                                 {0, "Снят отказ аналогового входа"},
//                                                 {1, "Окончание градуировки аналогового входа"},
//                                                 {2, "Снята отсечка по отсутствию расхода"},
//                                                 {3, "Переход на показания датчика"},
//                                                 {4, "Загружен отчет"},
//                                                 {5, "Напряжение питания в норме"},
//                                                 {6, "Датчик в пределах градуировки"},
//                                                 {7, "Сняг флаг рестарта СуперФлоу"},
//                                                 {9, "Аналоговый вход разморожен"},
//                                                 {10, "Свойства газа в диапазоне"},
//                                                 {67, "Переход на показания датчика"},
//                                                 {128, "Установлен отказ аналогового входа"},
//                                                 {129, "Аналоговый вход в градуировке"},
//                                                 {130, "Установлена отсечка по отсутствию расхода"},
//                                                 {131, "Введена константа"},
//                                                 {133, "Низкое напряжение питания"},
//                                                 {134, "Превышение предела градуировки датчика"},
//                                                 {135, "Рестарт суперфлоу"},
//                                                 {137, "Аналоговый вход заморожен"},
//                                                 {138, "Ошибка в свойствах газа"},
//                                                 {139, "Сезонное изменение времени"},
//                                                 {195, "Введена константа"}
//                                             };

//        private AlarmPoint GetAlarmPoint(byte aCode, byte aPoint)
//        {
//            switch (aCode)
//            {
//                case 0:
//                case 1:
//                case 3:
//                case 6:
//                case 9:
//                case 63:
//                case 128:
//                case 129:
//                case 131:
//                case 134:
//                case 137:
//                case 195:
//                    return (AlarmPoint)aPoint;
//                default:
//                    return AlarmPoint.NA;
//            }
//        }

//        private enum AlarmPoint
//        {
//            Pressure = 0,
//            Temperature,
//            NA = 255
//        };

//        public override SurveyResultAbnormalEvents ReadAbnormalEvents(DateTime dateStart, DateTime dateEnd)
//        {
//            var datas = new List<AbnormalEvents>();
//            if (dateStart < dateEnd && ID != null)
//            {
//                byte requestCounter = 0;
//                //foreach (var dateFirst in dates)
//                //{
//                for (byte run = 1; run <= ID.Runs.Count(); run++)
//                {
//                    var answer = SendRequest(FunCode.ReadDailyHistory, new byte[]
//                                                                        {
//                                                                            run, requestCounter,
//                                                                            (byte) dateStart.Month,
//                                                                            (byte) dateStart.Day,
//                                                                            (byte) (dateStart.Year%100),
//                                                                            (byte) dateStart.Hour,
//                                                                            (byte) dateEnd.Month,
//                                                                            (byte) dateEnd.Day,
//                                                                            (byte) (dateEnd.Year%100),
//                                                                            (byte) dateEnd.Hour
//                                                                        });

//                    if (answer != null)
//                    {
//                        if (answer.Length > 3)
//                        {
//                            if (answer[3] == 0)
//                            {
//                                requestCounter = 0;
//                            }
//                            else
//                            {
//                                requestCounter++;
//                            }
//                            int records = answer[6 - 4];
//                            Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm}~{2:dd.MM.yyyy HH:mm} alarm records={3} in answer[{4}]", requestCounter, dateStart, dateEnd, records, answer.Length));

//                            for (int record = 0, dataOffset = -4;
//                                    record < records;
//                                    record++, dataOffset += 27)
//                            {
//                                int dateOffset = dataOffset + 8;
//                                var archiveRecordDateTimeString = string.Format("{0}.{1}.{2} {3}:{4}:{5}",
//                                                                                answer[dateOffset + 1], //day
//                                                                                answer[dateOffset + 0], //mon
//                                                                                answer[dateOffset + 2], //yr
//                                                                                answer[dateOffset + 3], //hr
//                                                                                answer[dateOffset + 4], //min
//                                                                                answer[dateOffset + 5] //sec
//                                    );
//                                DateTime current;
//                                DateTime.TryParse(archiveRecordDateTimeString, out current);
//                                var aCode = answer[dateOffset + 6];
//                                var aPoint = answer[dateOffset + 7];
//                                var runNum = answer[dateOffset + 8];

//                                datas.Add(new AbnormalEvents
//                                {
//                                    DateTime = current,
//                                    Description = string.Format("Run#{2}: Alarm={0} Point={1}",
//                                        _alarms.ContainsKey(aCode) ? _alarms[aCode] : aCode.ToString(),
//                                        GetAlarmPoint(aCode, aPoint),
//                                        runNum == 0xFF ? "All" : runNum.ToString()
//                                    ),
//                                    Duration = 0
//                                });
//                            }
//                        }
//                        else
//                        {
//                            Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm}~{2:dd.MM.yyyy HH:mm} NO records in answer[{2}]",
//                                requestCounter, dateStart, dateEnd, answer.Length));
//                        }
//                    }
//                    else
//                    {
//                        Show(string.Format("({0}) {1:dd.MM.yyyy HH:mm}~{2:dd.MM.yyyy HH:mm} NO answer", requestCounter, dateStart, dateEnd));
//                    }
//                }
//            }
//            return new SurveyResultAbnormalEvents { Records = datas, State = SurveyResultState.Success };
//        }

//        public override SurveyResultConstant ReadConstants()
//        {
//            var result = new List<Constant>();

//            if (ID != null)
//            {
//                for (byte i = 1; i <= ID.Runs.Count(); i++)
//                {
//                    result.AddRange(ReadStaticParam(i));
//                }
//            }

//            return new SurveyResultConstant { Records = result, State = SurveyResultState.Success };
//        }

//        public override SurveyResult Ping()
//        {
//            var request = new Request(NetworkAddress, 0x01);
//            var answer = new IdResponse(SendMessageToDevice(request));
//            OnSendMessage(string.Format("текущая дата {0:dd.MM.yyyy HH:mm:ss}, наименование ИТ1 {1}", answer.CurrentDate, answer.Tube1Name));

//            var dr = new DailyHistoryResponse(SendMessageToDevice(new DailyHistoryRequest(NetworkAddress, 1, 0, new DateTime(2015, 04, 01), new DateTime(2015, 04, 02))));

//            foreach (var record in dr.Records)
//            {
//                OnSendMessage(record.ToString());
//            }

//            return new SurveyResult { State = SurveyResultState.Success };
//        }

//        #region (show)

//        private void Show(string msg, MessageType msgtype = MessageType.All)
//        {
//            OnSendMessage(msg);
//            //LOG
//            switch (msgtype)
//            {
//                case MessageType.All:
//                case MessageType.Debug:

//                    break;
//                case MessageType.Info:

//                    break;
//                case MessageType.Warn:
//                    //log.Warn(msg);
//                    break;
//                case MessageType.Error:
//                    //log.Error(msg);
//                    break;
//                case MessageType.User:
//                case MessageType.Tester:
//                    break;
//            }
//            //Show to Interface
//            switch (msgtype)
//            {
//                case MessageType.All:
//                case MessageType.User:
//                case MessageType.Error:
//                    OnSendMessage(msg);
//                    break;
//                case MessageType.Debug:
//                case MessageType.Info:
//                case MessageType.Warn:
//                case MessageType.Tester:
//                    Console.WriteLine(msg);
//                    break;
//            }
//        }

//        private enum MessageType
//        {
//            All,
//            User,   //only user interfaxe
//            Tester, //only console
//            Debug,
//            Info,
//            Warn,
//            Error
//        }
//        #endregion
//    }
//}

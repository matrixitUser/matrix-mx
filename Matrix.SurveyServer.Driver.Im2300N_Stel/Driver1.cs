//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Timers;
//using System.IO;

//namespace Matrix.SurveyServer.Driver.Im2300N_Stel
//{
//    //9173573605
//    // imei == "∙353270040666721#")//ИМ2300
//    public class Driver1
//    {
//        private byte NetworkAddress = 0;
//        private void OnSendMessage(string haa,params object[] args) { }
//        private string Password = "";

//        class Data
//        {
//            public DateTime Date;
//            public string ParameterName;
//            public double Value;
//            public string MeasuringUnit;
//        }

//        /// <summary>
//        /// отправка сообщения устройству
//        /// </summary>
//        /// <typeparam name="TResponse">тип ожидаемого ответа</typeparam>
//        /// <param name="request">запрос</param>
//        /// <returns></returns>
//        private byte[] SendMessageToDevice(IRequest request, int timeout = 10000)
//        {
//            var response = new List<byte>();
//            bool success = false;
//            int attemtingCount = 2;

//            while (!success && attemtingCount < 3)
//            {
//                attemtingCount++;

//                isDataReceived = false;
//                receivedBuffer = null;

//                var bytes = request.GetBytes();

//                OnSendMessage(string.Format("отправлено {0}", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//                RaiseDataSended(bytes);
//                Wait(timeout);
//                if (isDataReceived)
//                {
//                    response.AddRange(receivedBuffer);
//                    OnSendMessage(string.Format("получено {0}", string.Join(",", receivedBuffer.Take(50).Select(b => b.ToString("X2")))));
                    
//                    //недостающие части пакета
//                    do
//                    {
//                        isDataReceived = false;
//                        Wait(500);
//                        if(isDataReceived)
//                        {
//                            response.AddRange(receivedBuffer);
//                            OnSendMessage(string.Format("получена доп. часть {0}", string.Join(",", receivedBuffer.Take(50).Select(b => b.ToString("X2")))));
//                        }
//                    }
//                    while (isDataReceived);
                    
//                    success = true;
//                }
//            }

//            //OnSendMessage(success ? string.Format("ответ получен") : "ответ не получен");
//            return response.ToArray();
//        }

//        private byte[] SendMessageToDevice2(IRequest request, int timeout)
//        {
//            byte[] response = null;

//            isDataReceived = false;
//            receivedBuffer = null;
//            var bytes = request.GetBytes();
//            OnSendMessage(string.Format("отправлено {0}", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//            RaiseDataSended(bytes);

//            Wait(timeout);

//            if (isDataReceived)
//            {
//                response = receivedBuffer;
//                OnSendMessage(string.Format("получено {0}", string.Join(",", response.Take(50).Select(b => b.ToString("X2")))));
//            }


//            //OnSendMessage(success ? string.Format("ответ получен") : "ответ не получен");
//            return response;
//        }

//        /// <summary>
//        /// получение блока данных от прибора
//        /// </summary>
//        /// <param name="cmd"></param>
//        /// <param name="blockCount"></param>
//        /// <param name="blockLength"></param>
//        /// <returns></returns>
//        private byte[] GetBlocks(byte cmd, byte blockCount, ushort blockLength)
//        {
//            try
//            {
//                //var version = new VersionAnswer(SendMessageToDevice(new ResetBufferRequest()));
//                //OnSendMessage("версия контроллера {0}", version.Version);

//                if (blockCount > 1)
//                {
//                    SendMessageToDevice(new StrangeCommand(1, blockCount));
//                }

//                var buffer = new List<byte>();

//                buffer.AddRange(SendMessageToDevice(new Command(NetworkAddress, cmd)));

//                var attempts = 5;
//                while (buffer.Count < blockCount∙blockLength && attempts-- > 0)
//                {
//                    buffer.AddRange(SendMessageToDevice(new Empty()));
//                    OnSendMessage(string.Format("данные блоков получены {0} из {1}", buffer.Count, blockCount∙blockLength));
//                }

//                OnSendMessage(string.Format("данные получены {0} байт", buffer.Count));
//                return buffer.ToArray();
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}"), ex.Message);
//            }
//            return null;
//        }

//        private byte[] GetMultiplyBlocks(byte cmd, byte start, byte end, ushort blockLength)
//        {
//            try
//            {
//                SendMessageToDevice(new StrangeCommand(start, end));
//                var buffer = new List<byte>();

//                buffer.AddRange(SendMessageToDevice(new Command(NetworkAddress, cmd), end∙5000));

//                var need = (1 + end - start)∙blockLength;
//                OnSendMessage(string.Format("получено {0} байт (ожидается {1})", buffer.Count, need));

//                var attempts = 3;
//                while (buffer.Count + 10 < need && attempts-- > 0)
//                {
//                    var part = SendMessageToDevice(new Empty());
//                    if (part != null)
//                        buffer.AddRange(part);
//                }

//                OnSendMessage(string.Format("данные получены {0} байт", buffer.Count));
//                return buffer.ToArray();
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}"), ex.Message);
//            }
//            return null;
//        }

//        private Passport ReadPassport()
//        {
//            OnSendMessage(string.Format("чтение паспорта"));
//            ushort passportLength = 978;
//            if (Password.Contains("Z"))
//            {
//                passportLength = 722;
//            }
//            var blocks = GetBlocks(0x98, 1, passportLength);
//            var passport = new Passport(blocks, msg => OnSendMessage(msg));
//            return passport;
//        }

//        /// <summary>
//        /// начало обмена с адаптером
//        /// </summary>
//        /// <returns></returns>
//        private bool Init()
//        {
//            try
//            {
//                //var okAnswer = SendMessageToDevice(new Empty());
//                //var msg = Encoding.ASCII.GetString(okAnswer);
//                //OnSendMessage(string.Format("от адаптера получено {0}", msg));
//                return true;
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }
//            return false;
//        }


//        public void Ping()
//        {
//            try
//            {
//                Init();
//                var passport = ReadPassport();
//                OnSendMessage(string.Format("расчетный час {0}", passport.ContractHour));
//                OnSendMessage(string.Format("число регистрируемых каналов {0}", passport.ArchChannelCount));

//                OnSendMessage(string.Format("длина блока {0}", passport.BlockLength));
//                var arch = GetBlocks(0x9B, 1, (ushort)passport.BlockLength);
//                var rr = new RecordsResponse(arch, passport, m => OnSendMessage(m));
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}"), ex.Message);
//            }
//           // return new SurveyResult { State = SurveyResultState.Success };
//        }

//        private int Round(double value)
//        {
//            var intValue = (int)value;
//            if (value > intValue) return intValue + 1;
//            return intValue;
//        }

//        private IEnumerable<Data> ReadDiapasone(DateTime start, DateTime end, Passport passport, ref int last)
//        {
//            var recordsInBlock = passport.BlockLength / passport.RecordLength;

//            OnSendMessage(string.Format("используется {0} каналов", passport.Channels.Where(c => !c.NotUsing).Count()));

//            var now = DateTime.Now;
//            var hours = (now - start).TotalHours;
//            int far = Round(hours / recordsInBlock) + 1;

//            var hours1 = (now - end).TotalHours;
//            int near = Round(hours1 / recordsInBlock) - 1;
//            if (near < 0) near = 0;

//            if (far > last) far = last;
//            last = near;
//            if (last < 0) last = 0;

//            if (near == far) return new Data[] { };

//            OnSendMessage(string.Format("блоки с {0} по {1}", near, far));
//            var arch = GetMultiplyBlocks(0x9B, (byte)near, (byte)far, (ushort)(passport.BlockLength + 4));
//            var rr = new RecordsResponse(arch, passport, m => OnSendMessage(m));

//            foreach (var ru in rr.Records)
//            {
//                //OnSendMessage(ru.ToString());
//            }
//            return rr.Records;

//        }

//        private void ReadVersion()
//        {
//            var version = new VersionAnswer(SendMessageToDevice(new ResetBufferRequest()));
//            OnSendMessage("версия контроллера {0}", version.Version);
//        }

//        public void ReadDailyArchive(IEnumerable<DateTime> dates)
//        {
//            var records = new List<Data>();
//            try
//            {
//                Init();

//                ReadVersion();

//                var passport = ReadPassport();
//                var ch = passport.ContractHour;


//                //читаем константы, для определения расчетного часа
//                ushort constsLen = 6 + 3 + 1 + 1;
//                if (passport.Type == Type.K)
//                {
//                    constsLen += 384;
//                }
//                else
//                {
//                    constsLen += 256;
//                }

//                var constants = GetBlocks(0x94, 1, constsLen);
//                if (passport.Type == Type.K)
//                {
//                    ch = (byte)(BitConverter.ToSingle(constants, 22∙6 + 2) / 2);
//                }
//                else
//                {
//                    ch = (byte)(BitConverter.ToSingle(constants, 23∙4) / 2);
//                }

//                OnSendMessage(string.Format("расчетный час {0}", ch));

//                var ready = new List<DateTime>();

//                var hours = new List<Data>();

//                int last = int.MaxValue;

//                foreach (var day in dates)
//                {
//                    try
//                    {
//                        var cDay = day.AddHours(ch);
//                        var dhours = ReadDiapasone(cDay, cDay.AddHours(24), passport, ref last);
//                        OnSendMessage("last={0}", last);
//                        foreach (var dh in dhours)
//                        {
//                            //if (!hours.Any(r => r.ParameterName == dh.ParameterName && r.Date == dh.Date))
//                            //{
//                            //    hours.Add(dh);
//                            //}
//                        }
//                        ReadVersion();
//                    }
//                    catch (Exception ex)
//                    {
//                        OnSendMessage("ошибка при чтении блоков дня {0:dd.MM.yy}", day);
//                    }
//                }

//                foreach (var day in dates)
//                {
//                    var cDay = day.AddHours(ch);
//                    var ddays = CalcDays(hours.Where(h => h.Date >= cDay && h.Date < cDay.AddHours(24)), cDay.Date);
//                    records.AddRange(ddays);
//                }
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка при чтении суток {0}", ex.Message));
//            }
//            //return new SurveyResultData { State = SurveyResultState.Success, Records = records };
//        }

//        public void ReadHourlyArchive(IEnumerable<DateTime> dates)
//        {
//            try
//            {
//                //Init();
//                ReadVersion();

//                int last = int.MaxValue;
//                var passport = ReadPassport();
//                OnSendMessage(string.Format("число регистрируемых каналов {0}", passport.ArchChannelCount));
//                var records = ReadDiapasone(dates.Min(d => d), dates.Max(d => d), passport, ref last);
//                if (records == null || !records.Any())
//                {
//                    OnSendMessage("записи отсутствуют");
//                    //return new SurveyResultData { State = SurveyResultState.Success, Records = new Data[] { } };
//                }
//                foreach (var date in records.Select(r => r.Date).Distinct().OrderBy(d => d))
//                {
//                    OnSendMessage("прочитаны часовые показания {0:dd.MM.yyyy HH:mm}", date);
//                }
//                //return new SurveyResultData { State = SurveyResultState.Success, Records = records };
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка при чтении часов {0}", ex.Message));
//            }
//            //return new SurveyResultData { State = SurveyResultState.NoResponse };
//        }

//        public void ReadCurrentValues()
//        {
//            try
//            {
//                Init();
//                var passport = ReadPassport();
//                OnSendMessage(string.Format("расчетный час {0}", passport.ContractHour));

//                var arch = GetBlocks(0x91, 1, 120 + 8);

//                var records = new List<Data>();


//                //    var length = data.Length;
//                //    archiveRecord = new ArchiveRecord();
//                //    var channelsArray = passport.Channels.ToArray();

//                var curYear = DateTime.Today.Year;
//                var archiveRecordYearLeapOffset = ((arch[119 + 5] & 0xc0) >> 6);
//                var curYearLeapOffset = curYear % 4;
//                var archiveRecordYear = curYear - curYearLeapOffset + archiveRecordYearLeapOffset - ((archiveRecordYearLeapOffset > curYearLeapOffset) ? 4 : 0);

//                var curDateTime = new DateTime(archiveRecordYear,0,0
//                    //ConvertHelper.BinDecToInt((byte)(arch[119 + 6] & 0x1f)), //mon
//                    //ConvertHelper.BinDecToInt((byte)(arch[119 + 5] & 0x3f)), //day
//                    //ConvertHelper.BinDecToInt(arch[119 + 4]), //hh
//                    //ConvertHelper.BinDecToInt(arch[119 + 3]), //mm
//                    //ConvertHelper.BinDecToInt(arch[119 + 2]) //sec
//                   );

//                for (var i = 0; i < 24; i++)//24 канала для K
//                {
//                    //            //по 5 байт [0-3 показания индикатора 4 признак ошибки измерения]
//                    //            //SPBT двоично-дес Остальные float
//                    var channel = passport.Channels.ElementAt(i);

//                    int channelStart = 5∙i;
//                    double curChannelValue = 0;
//                    if (channel.IsChannelSummed == true)
//                    {
//                        for (int j = 0; j < 4; j++)
//                        {
//                            byte curChannelData = arch[channelStart + 3 - j];
//                            curChannelValue ∙= 100f;
//                           // curChannelValue += ConvertHelper.BinDecToInt(curChannelData);
//                        }
//                        curChannelValue /= 100f;
//                    }
//                    else
//                    {
//                        curChannelValue = BitConverter.ToSingle(arch, channelStart + 0) / 2f;
//                    }

//                    var record = new Data(channel.Name, channel.Unit, curDateTime, curChannelValue);
//                    records.Add(record);
//                }

//                foreach (var record in records)
//                {
//                    OnSendMessage(record.ToString());
//                }
//                //records.Clear();

//              //  return new SurveyResultData { State = SurveyResultState.Success, Records = records };
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка {0}", ex.Message));
//            }
//          //  return new SurveyResultData { State = SurveyResultState.NoResponse };
//        }

//        public void ReadConstants()
//        {
//            //try
//            //{
//            //    var constants = new List<Constant>();
//            //    var cnst = GetBlocks(0x9c, 1, 17);
//            //    var charPart = Encoding.ASCII.GetString(cnst, 0, 2);
//            //    var numPart = cnst[2].ToString("000");

//            //    var fn = charPart + numPart;
//            //    constants.Add(new Constant("заводской номер", fn));
//            //    OnSendMessage(string.Format("заводской номер {0}", fn));
//            //    //var rpns = new ConstantsResponse(cnst);
//            //    //
//            //    //foreach (var c in rpns.Constants)
//            //    //{
//            //    //constants.Add(new Constant(c.Key, c.Value.ToString()));
//            //    //}
//            //    return new SurveyResultConstant { State = SurveyResultState.Success, Records = constants };
//            //}
//            //catch (Exception ex)
//            //{
//            //    OnSendMessage(string.Format("ошибка {0}", ex.Message));
//            //}
//            //return new SurveyResultConstant { State = SurveyResultState.NoResponse };
//        }

//        private IEnumerable<Data> CalcDays(IEnumerable<Data> hours, DateTime day)
//        {
//            bool fullDay = true;
//            var days = new List<Data>();
//            foreach (var g in hours.GroupBy(r => r.ParameterName))
//            {
//                if (g.Count() < 24)
//                {
//                    OnSendMessage("данные по параметру {0} неполные", g.Key);
//                    fullDay = false;
//                    break;
//                }

//                if (g.Key.StartsWith(Glossary.T) ||
//                    g.Key.StartsWith(Glossary.P) ||
//                    g.Key.StartsWith(Glossary.Pa) ||
//                    g.Key.StartsWith(Glossary.Pb) ||
//                    g.Key.StartsWith(Glossary.dP))
//                {
//                    var tmpl = g.First();
//                    var val = g.Average(r => r.Value);
//                    //var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, val);
//                    //days.Add(cdata);
//                    OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                }

//                if (g.Key.StartsWith(Glossary.Gm) ||
//                    g.Key.StartsWith(Glossary.Go) ||
//                    g.Key.StartsWith(Glossary.Gn) ||
//                    g.Key.StartsWith(Glossary.Gr) ||
//                    g.Key.StartsWith(Glossary.Gw) ||
//                    g.Key.StartsWith(Glossary.ts) ||
//                    g.Key.StartsWith(Glossary.tm))
//                {

//                    var tmpl = g.OrderBy(r => r.Date).Last();
//                    //var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, tmpl.Value);
//                    //days.Add(cdata);
//                    OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                }

//                if (g.Key.StartsWith(Glossary.Qn) ||
//                    g.Key.StartsWith(Glossary.Qw) ||
//                    g.Key.StartsWith(Glossary.Qo))
//                {

//                    var tmpl = g.First();
//                    var val = g.Sum(r => r.Value);
//                    //var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, val);
//                    //days.Add(cdata);
//                    OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                }


//                //switch (g.Key)
//                //{
//                //    //средние
//                //    case Glossary.T:
//                //    case Glossary.P:
//                //    case Glossary.Pa:
//                //    case Glossary.Pb:
//                //    case Glossary.dP:
//                //        {
//                //            var tmpl = g.First();
//                //            var val = g.Average(r => r.Value);
//                //            var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, val);
//                //            days.Add(cdata);
//                //            OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                //            break;
//                //        }
//                //    //тотальные
//                //    case Glossary.Gm:
//                //    case Glossary.Go:
//                //    case Glossary.Gn:
//                //    case Glossary.Gr:
//                //    case Glossary.Gw:
//                //    case Glossary.ts:
//                //    case Glossary.tm:
//                //        {

//                //            var tmpl = g.OrderBy(r => r.Date).Last();
//                //            var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, tmpl.Value);
//                //            days.Add(cdata);
//                //            OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                //            break;
//                //        }
//                //    //суммируемые
//                //    case Glossary.Qn:
//                //    case Glossary.Qw:
//                //    case Glossary.Qo:
//                //        {

//                //            var tmpl = g.First();
//                //            var val = g.Sum(r => r.Value);
//                //            var cdata = new Data(g.Key, tmpl.MeasuringUnit, day, val);
//                //            days.Add(cdata);
//                //            OnSendMessage(string.Format("расчитан параметр {0}, значение {1}", g.Key, cdata));
//                //            break;
//                //        }
//                //}
//            }
//            if (fullDay)
//                return days;

//            return new Data[] { };
//        }
//    }
//}

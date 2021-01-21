//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;
//using System.Threading;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    public class Driver2 : BaseDriver
//    {
//        const bool TRACE_MODE = false;

//        private byte[] SendMessageToDevice(Request request)
//        {
//            bool success = false;
//            int attemtingCount = 0;

//            while (!success && attemtingCount < 2)
//            {
//                attemtingCount++;

//                isDataReceived = false;
//                receivedBuffer = null;
//                var bytes = request.GetBytes();

//                RaiseDataSended(bytes);
//                if (TRACE_MODE) OnSendMessage(string.Format("туда: [{0}]", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//                Wait(10000);

//                if (isDataReceived)
//                {
//                    if (TRACE_MODE) OnSendMessage(string.Format("сюда: [{0}]", string.Join(",", receivedBuffer.Select(b => b.ToString("X2")))));
//                    return receivedBuffer;
//                }
//            }

//            return null;
//        }

//        private void SendMessageToDeviceWithoutAnswer(Request request)
//        {
//            var bytes = request.GetBytes();
//            //if (TRACE_MODE) OnSendMessage(string.Format("туда: [{0}]", string.Join(",", bytes.Select(b => b.ToString("X2")))));
//            RaiseDataSended(bytes);
//        }

//        private void Init()
//        {
//            OnSendMessage("отправка раккорда (ждите 20 секунд)");
//            ///отправляем по 150 байт (ограничение GPRS или fastrack-а
//            ///соответственно, таких кусочков 168 (V=1200Б/с, T=21с -> S=1200*21Б)

//            //var interval = 500; //мс
//            //var piece = 1200 * interval / 1000; //б
//            //var parts = 1200 * 21 / piece; //б

//            var interval = 125; //мс
//            var piece = 1200 * interval / 1000; //б
//            var parts = 1200 * 21 / piece; //б
//            for (int i = 0; i < parts; i++)
//            {
//                SendMessageToDeviceWithoutAnswer(new RaccordRequest(piece));
//                Thread.Sleep(interval);
//            }
//        }

//        public override SurveyResult Ping()
//        {
//            Init();
//            var foo = SendMessageToDevice(new Request(Password, 0x01));
//            return new SurveyResult { State = SurveyResultState.Success };
//        }

//        public override SurveyResultData ReadHourlyArchive(IEnumerable<DateTime> dates)
//        {
//            var records = new List<Data>();
//            try
//            {
//                Init();

//                var startResponse = new DateResponse(SendMessageToDevice(new MemoryRequest(Password, 0x0e, 6)));
//                var dateStart = startResponse.Date;
//                OnSendMessage(string.Format("начало ведения почасового архива {0:dd.MM.yyyy HH:mm:ss}", dateStart));

//                foreach (var date in dates)
//                {
//                    try
//                    {
//                        var offset = (UInt16)(date - dateStart).TotalHours;
//                        if (offset < 0 || offset > 25 * 45)
//                        {
//                            OnSendMessage(string.Format("запрошенная запись ({0:dd.MM.yyyy HH:mm}) за пределами архива", date));
//                            continue;
//                        }
//                        var start = (UInt16)(0x20 + 20 * offset);
//                        var recordData = new RecordResponse(SendMessageToDevice(new MemoryRequest(Password, start, 20)));

//                        OnSendMessage(string.Format("прочитана запись {0:dd.MM.yyyy HH:mm}", recordData.Date));
//                        records.AddRange(recordData.Records);
//                    }
//                    catch (Exception e0)
//                    {
//                        OnSendMessage(string.Format("ошибка при чтении записи за{0:dd.MM.yyyy HH:mm}: {1}", date, e0.Message));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }

//            return new SurveyResultData() { Records = records, State = SurveyResultState.Success };
//        }

//        public override SurveyResultData ReadDailyArchive(IEnumerable<DateTime> dates)
//        {
//            var records = new List<Data>();
//            try
//            {
//                Init();

//                var startResponse = new DateResponse(SendMessageToDevice(new MemoryRequest(Password, 0x14, 6)));
//                var dateStart = startResponse.Date;
//                OnSendMessage(string.Format("начало ведения посуточного архива {0:dd.MM.yyyy HH:mm:ss}", dateStart));

//                foreach (var date in dates)
//                {
//                    try
//                    {
//                        var offset = (UInt16)(date - dateStart).TotalDays;
//                        if (offset < 0 || offset > 300)
//                        {
//                            OnSendMessage(string.Format("запрошенная запись ({0:dd.MM.yyyy}) за пределами архива", date));
//                            continue;
//                        }
//                        var start = (UInt16)(0x5480 + 20 * offset);
//                        var recordData = new RecordResponse(SendMessageToDevice(new MemoryRequest(Password, start, 20)));
//                        OnSendMessage(string.Format("прочитана запись {0:dd.MM.yyyy}", recordData.Date));
//                        records.AddRange(recordData.Records);
//                    }
//                    catch (Exception e0)
//                    {
//                        OnSendMessage(string.Format("ошибка при чтении записи за{0:dd.MM.yyyy}: {1}", date, e0.Message));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }

//            return new SurveyResultData() { Records = records, State = SurveyResultState.Success };
//        }

//        public override SurveyResultData ReadCurrentValues()
//        {
//            var records = new List<Data>();
//            try
//            {
//                Init();
//                var res = new CurrentsResponse(SendMessageToDevice(new Request(Password, 0x01)));
//                OnSendMessage(string.Format("данные получены, время на вычислителе {0:dd.MM.yyyy HH:mm:ss}", res.Date));
//                records.AddRange(res.Records);
//            }
//            catch (Exception ex)
//            {
//                OnSendMessage(string.Format("ошибка: {0}", ex.Message));
//            }
//            return new SurveyResultData() { Records = records, State = SurveyResultState.Success };
//        }
//    }
//}

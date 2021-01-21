using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        #region SetGetEvents
        private string toStringEvents(UInt16 events)
        {
            string controlMetodName = "***********";
            
            switch (events >> 8)
            {
                case 1:
                    controlMetodName = "Включение";
                    break;
                case 2:
                    controlMetodName = "Выключение";
                    break;
                default:
                    controlMetodName = "Установлен метод управления: ";
                    switch (events & 0x00FF)
                    {
                        case 0:
                            controlMetodName += "'по расписанию'";
                            break;
                        case 1:
                            controlMetodName += "'по фотодачику'";
                            break;
                        case 2:
                            controlMetodName += "'ручное управление'";
                            break;
                        case 3:
                            controlMetodName += "'астрономический таймер контроллера'";
                            break;
                        case 18:
                            controlMetodName += "'ручное управление(hard)'";
                            break;
                    }
                    break;
            }
            return controlMetodName;
        }
        public dynamic SetGetEvents(List<byte> byteSoftConfig, int ver)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();
           
            if(ver == 6)
            {
                var result = Send(MakeEventsRequest((Int32)0x0100, (ushort)byteSoftConfig.Count, byteSoftConfig));
                if (!result.success)
                {
                    log(string.Format("Events не введён: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x4C)
                {
                    log(string.Format("Получен ответ {0} не на 76 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }
                DateTime date = DateTime.Now;
                //string archiveEvents = BitConverter.ToString(result.Body);
                UInt32 uInt32Time; UInt16 events; DateTime dtContollers; string strEvents = "";
                for (int i = 0; i < (int)result.Body[1]; i++)
                {
                    uInt32Time = (UInt32)(result.Body[5 + 6 * i] << 24) | (UInt32)(result.Body[4 + 6 * i] << 16) | (UInt32)(result.Body[3 + 6 * i] << 8) | result.Body[2 + 6 * i];
                    dtContollers = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(uInt32Time);
                    events = Helper.ToUInt16(result.Body, 6 + 6 * i);
                    
                    log(string.Format("Событие: {0}:{1}", dtContollers, toStringEvents(events)), level: 1);
                    strEvents += string.Format("{0}:{1}|", dtContollers, toStringEvents(events));
                }
                
                records.Add(MakeCurrentRecord("Events", 0, strEvents, date, date));
                /*
                
                UInt16 errorCode = Helper.ToUInt16(result.Body, 0);
                int timeManual = (int)result.Body[2];
                int timeInterval = (int)result.Body[3];
                int messageRepeat = (int)result.Body[4];
                int countTry = (int)result.Body[5];
                int countTacts = (int)result.Body[6];
                int IsArchive = (int)result.Body[7];
                log(string.Format("timeManual: {0}; timeInterval: {1}; messageRepeat: {2}; countTry: {3}; countTacts: {4}; IsArchive: {5}", timeManual, timeInterval, messageRepeat, countTry, countTacts, IsArchive), level: 1);

                records.Add(MakeCurrentRecord("timeManual", timeManual, "", date, date));
                records.Add(MakeCurrentRecord("timeInterval", timeInterval, "", date, date));
                records.Add(MakeCurrentRecord("messageRepeat", messageRepeat, "", date, date));
                records.Add(MakeCurrentRecord("countTry", countTry, "", date, date));
                records.Add(MakeCurrentRecord("countTacts", countTacts, "", date, date));
                records.Add(MakeCurrentRecord("IsArchive", IsArchive, "", date, date));
                */

            }

            current.records = records;
            return current;
        }

        #endregion

        
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        #region SetGetAbnormal

        public dynamic SetGetAbnormal(List<byte> byteSoftConfig, int ver)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();
           
            if(ver == 6)
            {
                var result = Send(MakeAbnormalRequest((Int32)0x0100, (ushort)byteSoftConfig.Count, byteSoftConfig));
                if (!result.success)
                {
                    log(string.Format("Abnormal не введён: {0}", result.error), level: 1);
                    current.success = false;
                    return current;
                }

                if (result.Function != 0x4B)
                {
                    log(string.Format("Получен ответ {0} не на 75 команду ", result.Function), level: 1);
                    current.success = false;
                    return current;
                }

                DateTime date = DateTime.Now;
                int timeManual = (int)result.Body[0];
                int timeInterval = (int)result.Body[1];
                int countTry = (int)result.Body[2];
                int countTryEvent = (int)result.Body[3];
                int IsArchive = (int)result.Body[4];
                UInt16 errorCode = Helper.ToUInt16(result.Body, 5);
                UInt16 eventCode = Helper.ToUInt16(result.Body, 7);
                log(string.Format("timeManual:{0}; timeInterval:{1}; countTry:{2}; countTryEvent:{3}; IsArchive:{4}; errorCode:{5}; eventCode:{6}", timeManual, timeInterval, countTry, countTryEvent, IsArchive, errorCode, eventCode), level: 1);

                records.Add(MakeCurrentRecord("timeManual", timeManual, "", date, date));
                records.Add(MakeCurrentRecord("timeInterval", timeInterval, "", date, date));
                records.Add(MakeCurrentRecord("countTry", countTry, "", date, date));
                records.Add(MakeCurrentRecord("countTryEvent", countTryEvent, "", date, date));
                records.Add(MakeCurrentRecord("IsArchive", IsArchive, "", date, date));
                records.Add(MakeCurrentRecord("errorCode", errorCode, "", date, date));
                records.Add(MakeCurrentRecord("eventCode", eventCode, "", date, date));

                /*
                UInt32 uInt32Time = (UInt32)(result.Body[7] << 24) | (UInt32)(result.Body[6] << 16) | (UInt32)(result.Body[5] << 8) | result.Body[4];
                DateTime dtContollers = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(uInt32Time);
                log(string.Format("Время на контроллере: {0}",dtContollers), level: 1);

                double GetLightMK = (double)result.Body[0];
                double GetLightReal = (double)result.Body[1];
                double GetPhotoSensorState = (double)result.Body[2];
                double GetControlMetod = (double)result.Body[3];

                log(string.Format("Фотодатчик: {0}; Выход контроллера: {1}; Состояние контактора: {2}", ((GetPhotoSensorState == 1) ? "включен" : "выключен"), ((GetLightMK == 1) ? "включен" : "выключен"), ((GetLightReal == 1) ? "включено" : "выключено")), level: 1);
                string controlMetodName;
                switch (GetControlMetod)
                {
                    case 0:
                        controlMetodName = "По расписанию";
                        break;
                    case 1:
                        controlMetodName = "По фотодачику";
                        break;
                    case 2:
                        controlMetodName = "Ручное управление";
                        break;
                    case 3:
                        controlMetodName = "Астрономический таймер контроллера";
                        break;
                    case 18:
                        controlMetodName = "Ручное управление(hard)";
                        break;
                    default:
                        controlMetodName = "************";
                        break;
                }

                log(string.Format("Метод управления: {0} ({1})", controlMetodName, GetControlMetod), level: 1);
                records.Add(MakeCurrentRecord("GetLightMK", GetLightMK, "", dtContollers, date));
                records.Add(MakeCurrentRecord("GetLightReal", GetLightReal, "", dtContollers, date));
                records.Add(MakeCurrentRecord("GetPhotoSensorState", GetPhotoSensorState, "", dtContollers, date));
                records.Add(MakeCurrentRecord("GetControlMetod", GetControlMetod, "", dtContollers, date));*/
            }

            current.records = records;
            return current;
        }

        #endregion

        
    }
}

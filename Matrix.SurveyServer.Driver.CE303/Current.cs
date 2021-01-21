using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;
using System.Globalization;

namespace Matrix.SurveyServer.Driver.CE303
{
    public partial class Driver
    {
        dynamic GetCurrent(bool onlyTime = false)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var recs = new List<dynamic>();

            //


            //Считывание данных по протоколу МЭК
            var cdate = ParseResponse(Send(MakeDataRequest("DATE_()")));  //строка ответа счетчика  на запрос DATE_ без парсинга
            if (!cdate.success) return cdate;  //здесь только ошибка
            var ctime = ParseResponse(Send(MakeDataRequest("TIME_()")));   //строка ответа счетчика  на запрос TIME_ без парсинга
            if (!ctime.success) return ctime;  //здесь только ошибка
            current.date = DriverHelper.DateTimeFromCounter(cdate.rsp, ctime.rsp);
            if (!onlyTime)
            {
                //log(string.Format("Текущее время на счетчике {0:dd.MM.yyyy HH:mm:ss}", current.date));
                //Энергия от сброса суммарное 

                var et0pe = ParseResponse(Send(MakeDataRequest("ET0PE()")));
                if (!et0pe.success) return et0pe;

                var sEnergyFromReset = DriverHelper.Parsing("ET0PE", et0pe.rsp)[0].Replace(".", ",");
                current.energy = System.Double.Parse(sEnergyFromReset);

                recs.Add(MakeCurrentRecord("Энергия от сброса", current.energy, "кВт", current.date));
                setIndicationForRowCache(current.energy, "кВт", current.date);
                //log(string.Format("Показание счетчика на {0:dd.MM.yyyy HH:mm:ss} => {1:0.000}", current.date, current.energy));
               
            }
            current.records = recs;
            return current;
        }
    }




    //        private byte[] DataByNameParameter(string NameParameter)
    //        {
    //            byte[] response = new byte[] { };
    //            string identName = Password; //Идентификатор счетчика
    //            //Считывание данных по имени параметра
    //            response = SendMessageToDevice(new RequestData(identName, NameParameter));
    //            if (response == null)
    //            {
    //                OnSendMessage("счетчик не вернул данные");
    //            }

    //            if (response.Length == 1 && response[0] == 0x15)
    //            {
    //                OnSendMessage("Cчетчик вернул отрицательный ответ : NAK");
    //                response = null;
    //            }
    //            //OnSendMessage("Данные со счетчика:" + Encoding.Default.GetString(response));
    //            //OnSendMessage(string.Join("", (response.Select(b => b.ToString("X2")))));
    //            return response;
    //        }

}

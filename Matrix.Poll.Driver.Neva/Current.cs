using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Globalization;

namespace Matrix.Poll.Driver.Neva
{
    public partial class Driver
    {
        dynamic GetCurrent(DateTime date, bool onlyTime = false)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var recs = new List<dynamic>();

            //Считывание данных по протоколу МЭК
            var energyA = ParseValueArray(Send(MakeDataRequest("0F0880FF()")));//Total,T1,T2,T3,T4
            if (!energyA.success) return energyA;

            recs.Add(MakeCurrentRecord("Активная энергия Итого", energyA.values[0], "кВт*ч", date));
            recs.Add(MakeCurrentRecord("Активная энергия Т1", energyA.values[1], "кВт*ч", date));
            recs.Add(MakeCurrentRecord("Активная энергия Т2", energyA.values[2], "кВт*ч", date));
            recs.Add(MakeCurrentRecord("Активная энергия Т3", energyA.values[3], "кВт*ч", date));
            recs.Add(MakeCurrentRecord("Активная энергия Т4", energyA.values[4], "кВт*ч", date));

            try
            {

                var energyRp = ParseValueArray(Send(MakeDataRequest("030880FF()")));//Total,T1,T2,T3,T4
                if (!energyRp.success) return energyRp;
                recs.Add(MakeCurrentRecord("Реактивная энергия+ Итого", energyRp.values[0], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия+ Т1", energyRp.values[1], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия+ Т2", energyRp.values[2], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия+ Т3", energyRp.values[3], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия+ Т4", energyRp.values[4], "кВАр*ч", date));

                var energyRm = ParseValueArray(Send(MakeDataRequest("040880FF()")));//Total,T1,T2,T3,T4
                if (!energyRm.success) return energyRm;
                recs.Add(MakeCurrentRecord("Реактивная энергия- Итого", energyRm.values[0], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия- Т1", energyRm.values[1], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия- Т2", energyRm.values[2], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия- Т3", energyRm.values[3], "кВАр*ч", date));
                recs.Add(MakeCurrentRecord("Реактивная энергия- Т4", energyRm.values[4], "кВАр*ч", date));
            }
            catch(Exception ex)
            {
                log(string.Format("Ошибка при считывании текущей реактивной энергии : {0}:", ex));
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

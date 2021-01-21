using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        #region ConfigSoft 

        public dynamic DHT()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var records = new List<dynamic>();
            //var result = Send(MakeDHTRequest((Int32)0x0100, 0, null)); 
            var result = Send(MakeBaseRequest(75, null));
            if (!result.success)
            {
                log(string.Format("DHT не введён: {0}", result.error), level: 1);
                current.success = false;
                return current;
            }
            
            double GetHumidity = Helper.ToInt16(result.Body, 0) / 10;
            double GetTemperature = Helper.ToInt16(result.Body, 2) / 10;
            
            DateTime date = DateTime.Now;
            log(string.Format("Влажность: {0}; Температура: {1};", GetHumidity, GetTemperature), level: 1);
           
            records.Add(MakeCurrentRecord("GetHumidity", GetHumidity, "", date, date));
            records.Add(MakeCurrentRecord("GetTemperature", GetTemperature, "", date, date));

            current.records = records;
            return current;
        }

        #endregion
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        dynamic GetConstant(DateTime date)
        {
            dynamic constant = new ExpandoObject();
            constant.success = true;
            constant.error = string.Empty;
            constant.errorcode = DeviceError.NO_ERROR;

            var recs = new List<dynamic>();

            string[] aPrecision = new string[] { "0.2", "0.5", "1.0", "2.0" }; //точность
            string[] aUnom = new string[] { "57,7B", "120-230B" }; //номинальное напряжение
            string[] aInom = new string[] { "5A", "1A", "10A" }; //номинальный ток

            int[] aConst = new int[] { 5000, 25000, 1250, 6250, 500, 250, 6400 }; //постоянная счетчика
            string[] aType = new string[] { 
                "СЭТ-4ТМ.02, СЭТ-1М.01, СЭТ-1М.01М", 
                "СЭТ-4ТМ.03", 
                "СЭБ-1ТМ.0x", 
                "ПСЧ-4ТМ.05", 
                "СЭО-1.16", 
                "ПСЧ-3ТМ.05", 
                "ПСЧ-4ТМ.05М", 
                "ПСЧ-3ТМ.05М", 
                "СЭТ-4ТМ.02М, СЭТ-4ТМ.03М", 
                "СЭБ-1ТМ.02Д", 
                "ПСЧ-3ТМ.05Д, ПСЧ-4ТМ.05Д", 
                "ПСЧ-3ТМ.05МК, ПСЧ-4ТМ.05МК", 
                "", "", "", "", "" 
            };


            var response = Send(MakeRequestParameters(0x12, new byte[] { }));

            if (!response.success) return response;

            var precisionAct = (response.Body[0] >> 6) & 0x03; // 1
            var precisionReact = (response.Body[0] >> 4) & 0x03; //2
            var uNom = (response.Body[0] >> 2) & 0x03; // 1
            var iNom = response.Body[0] & 0x03;//0
            var constA = response.Body[1] & 0x0F;//2
            var type = (response.Body[2] >> 4) & 0x0F;//3

            recs.Add(MakeConstRecord("Постоянная счетчика", aConst[constA], date));
            recs.Add(MakeConstRecord("Класс точности по P", aPrecision[precisionAct], date));
            recs.Add(MakeConstRecord("Класс точности по Q", aPrecision[precisionReact], date));
            recs.Add(MakeConstRecord("Uн", aUnom[uNom], date));
            recs.Add(MakeConstRecord("Iн", aInom[iNom], date));
            recs.Add(MakeConstRecord("Тип счетчика", aType[type], date));

            constant.constA = aConst[constA];
            constant.aType = aType[type];
            constant.records = recs;
            return constant;
        }
    }

}

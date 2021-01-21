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
        dynamic GetCurrent()
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var recs = new List<dynamic>();

            var rsp = Send(MakeRequestLogsExt(0x00, 0x00));
            if (rsp.success == false) return rsp;

            current.date = new DateTime(Helper.BinDecToInt(rsp.Body[6]) + 2000, Helper.BinDecToInt(rsp.Body[5]), Helper.BinDecToInt(rsp.Body[4]), Helper.BinDecToInt(rsp.Body[2]), Helper.BinDecToInt(rsp.Body[1]), Helper.BinDecToInt(rsp.Body[0]));

            recs.Add(MakeCurrentRecord("__currentDate", 0, "", current.date));

            //records.AddRange(energy.records);

            current.records = recs;
            return current;
        }

        dynamic GetCurrentEnergy(int constA, DateTime date, string aType)
        {
            dynamic current = new ExpandoObject();
            current.success = true;
            current.error = string.Empty;
            current.errorcode = DeviceError.NO_ERROR;

            var recs = new List<dynamic>();
            dynamic rspEng = new ExpandoObject();
            /*короткий для всех
            if (aType == "ПСЧ-4ТМ.05М")
            {
                rspEng = Send(MakeRequestEnergyShort(0x00, 0x00));
            }
            else
            {
                
                //rspEng = Send(MakeRequestEnergy(0x00, 0x00, 0x00));
            }
            */
            rspEng = Send(MakeRequestEnergyShort(0x00, 0x00));
            if (!rspEng.success) return rspEng;

            current.energy = PQEnergy(rspEng.Body, 0, constA);

            setIndicationForRowCache(current.energy, "кВт", date);
            recs.Add(MakeCurrentRecord("Энергия от сброса", current.energy, "кВт", date));

            var recPowerSumAct = Send(MakeRequestPower(0x00)); // aктив сум
            if (recPowerSumAct.Body.Length == 1 && recPowerSumAct.Body[0] == 0x07)
            {
                log(string.Format("Не готов результат измерения по запрашиваемому параметру (не закончилось время интегрирования после пуска измерителя)"));
                current.records = recs;
                return current;
            }
            int powerPSumSign = recPowerSumAct.Body[0] >> 7;
            double powerPSumValue = (double)((int)((recPowerSumAct.Body[0] & 0x3F) << 16) | (int)(recPowerSumAct.Body[1] << 8) | recPowerSumAct.Body[2]) / 1000d;
            double powerPSum = (powerPSumSign == 0) ? powerPSumValue : 0 - powerPSumValue;
            recs.Add(MakeCurrentRecord("Мощность P по сумме фаз", powerPSum, "Вт", date));
           
            var recPowerSumReact = Send(MakeRequestPower(0x04)); // aктив сум
            int powerQSumSign = recPowerSumAct.Body[0] >> 6;
            double powerQSumValue = (double)((int)((recPowerSumReact.Body[0] & 0x3F) << 16) | (int)(recPowerSumReact.Body[1] << 8) | recPowerSumReact.Body[2]) / 1000d;
            double powerQSum = (powerQSumSign == 0) ? powerQSumValue : 0 - powerQSumValue;
            recs.Add(MakeCurrentRecord("Мощность Q по сумме фаз", powerQSum, "Вт", date));

            var reccosPhiSum = Send(MakeRequestPower(0x30)); // cos phiSum
            double cosPhiSumValue = (double)((int)((reccosPhiSum.Body[0] & 0x3F) << 16) | (int)(reccosPhiSum.Body[1] << 8) | reccosPhiSum.Body[2]) / 100d;
            int cosPhiSumSign = reccosPhiSum.Body[0] >> 7;
            double cosPhiSum = (cosPhiSumSign == 0) ? cosPhiSumValue : 0 - cosPhiSumValue;
            recs.Add(MakeCurrentRecord("cos φ (по сумме фаз)", cosPhiSum, "", date));

            var reccosPhi1 = Send(MakeRequestPower(0x31)); // cos phi1
            double cosPhi1Value = (double)((int)((reccosPhi1.Body[0] & 0x3F) << 16) | (int)(reccosPhi1.Body[1] << 8) | reccosPhi1.Body[2]) / 100d;
            int cosPhi1Sign = reccosPhi1.Body[0] >> 7;
            double cosPhi1 = (cosPhi1Sign == 0) ? cosPhi1Value : 0 - cosPhi1Value;
            recs.Add(MakeCurrentRecord("cos φ (фаза 1)", cosPhi1, "", date));
            
            var reccosPhi2 = Send(MakeRequestPower(0x32)); // cos phi2
            double cosPhi2Value = (double)((int)((reccosPhi2.Body[0] & 0x3F) << 16) | (int)(reccosPhi2.Body[1] << 8) | reccosPhi2.Body[2]) / 100d;
            int cosPhi2Sign = reccosPhi2.Body[0] >> 7;
            double cosPhi2 = (cosPhi2Sign == 0) ? cosPhi2Value : 0 - cosPhi2Value;
            recs.Add(MakeCurrentRecord("cos φ (фаза 2)", cosPhi2, "", date));
           
            var reccosPhi3 = Send(MakeRequestPower(0x33)); // cos phi3
            double cosPhi3Value = (double)((int)((reccosPhi3.Body[0] & 0x3F) << 16) | (int)(reccosPhi3.Body[1] << 8) | reccosPhi3.Body[2]) / 100d;
            int cosPhi3Sign = reccosPhi3.Body[0] >> 7;
            double cosPhi3 = (cosPhi3Sign == 0) ? cosPhi3Value : 0 - cosPhi3Value;
            recs.Add(MakeCurrentRecord("cos φ (фаза 3)", cosPhi3, "", date));
            
            current.records = recs;
            return current;
        }
       
    }

}

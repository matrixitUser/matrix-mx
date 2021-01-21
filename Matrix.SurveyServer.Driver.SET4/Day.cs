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
        dynamic GetDays(DateTime start, DateTime end, DateTime currentDt, int constA, string aType)
        {
            dynamic archive = new ExpandoObject();
            archive.success = true;
            archive.error = string.Empty;
            archive.errorcode = DeviceError.NO_ERROR;
            var recs = new List<dynamic>();

            DateTime minDate = currentDt.AddMonths(-1).Date;
            var date = (start.Date <= minDate) ? minDate.AddDays(1) : start.Date;
            DateTime dtNow = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            dynamic rsp = new ExpandoObject();
            if (aType == "ПСЧ-4ТМ.05М")
            {
                var rspEngCur = Send(MakeRequestEnergyShort(0x00, 0x00));
                if (!rspEngCur.success) return rspEngCur;

                byte tmpByteCur = (byte)((0x04 << 4) | dtNow.Month);
                var rspCur = Send(MakeRequestEnergyShort(tmpByteCur, 0x00));  //Энергия за текущие сутки
                if (!rspCur.success) return rspCur;

                byte tmpByteCurMinusDay = (byte)((0x05 << 4) | dtNow.Month);
                var rspCurMinusDay = Send(MakeRequestEnergyShort(tmpByteCurMinusDay, 0x00));  //Энергия за предыдущие сутки
                if (!rspCurMinusDay.success) return rspCurMinusDay;

                var curEnergyPPlus = PQEnergy(rspEngCur.Body, 0, constA);
                var curEnergyPMinus = PQEnergy(rspEngCur.Body, 4, constA);
                var curEnergyQPlus = PQEnergy(rspEngCur.Body, 8, constA);
                var curEnergyQMinus = PQEnergy(rspEngCur.Body, 12, constA);

                var curEnergyPPlus1 = PQEnergy(rspCur.Body, 0, constA);
                var curEnergyPMinus1 = PQEnergy(rspCur.Body, 4, constA);
                var curEnergyQPlus1 = PQEnergy(rspCur.Body, 8, constA);
                var curEnergyQMinus1 = PQEnergy(rspCur.Body, 12, constA);

                var curEnergyPPlus2 = PQEnergy(rspCurMinusDay.Body, 0, constA);
                var curEnergyPMinus2 = PQEnergy(rspCurMinusDay.Body, 4, constA);
                var curEnergyQPlus2 = PQEnergy(rspCurMinusDay.Body, 8, constA);
                var curEnergyQMinus2 = PQEnergy(rspCurMinusDay.Body, 12, constA);

                var energyPPlus = curEnergyPPlus - curEnergyPPlus1;
                var energyPMinus = curEnergyPMinus - curEnergyPMinus1;
                var energyQPlus = curEnergyQPlus - curEnergyQPlus1;
                var energyQMinus = curEnergyQMinus - curEnergyQMinus1;
                
                DateTime recDate = dtNow.AddDays(-1);
                recs.Add(MakeDayRecord("EnergyP+", energyPPlus, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyP-", energyPMinus, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyQ+", energyQPlus, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyQ-", energyQMinus, "кВт", recDate));

                int count = recs.Count;
                log(string.Format("Cуточные данные на конец {0:dd.MM.yyyy } Энергия: P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}.", recDate, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));

                recDate = dtNow.AddDays(-2);
                recs.Add(MakeDayRecord("EnergyP+", energyPPlus - curEnergyPPlus2, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyP-", energyPMinus - curEnergyPMinus2, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyQ+", energyQPlus - curEnergyQPlus2, "кВт", recDate));
                recs.Add(MakeDayRecord("EnergyQ-", energyQMinus - curEnergyQMinus2, "кВт", recDate));
                
                count = recs.Count;
                log(string.Format("Cуточные данные на конец {0:dd.MM.yyyy} Энергия: P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}.", recDate, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));

            }
            else
            {

                while (date < end)
                {
                    if (cancel())
                    {
                        archive.success = false;
                        archive.error = "опрос отменен";
                        archive.errorcode = DeviceError.NO_ERROR;
                        break;
                    }

                    if (date >= currentDt)
                    {
                        log(string.Format("данные за {0:dd.MM.yyyy} еще не собраны", date));
                        break;
                    }
                    

                    //Каждые сутки опрашиваем отдельно
                    //Считывание мощностей 
                    //log(string.Format("чтение суточных данных за {0:dd.MM.yyyy} ", date));
                    
                    rsp = Send(MakeRequestEnergy(0x86, (byte)date.Day, 0x00)); //на начало текущих суток (конец предыдущих суток)
                    if (rsp.success)
                    {
                        var recDate = date.AddDays(-1);
                        recs.Add(MakeDayRecord("EnergyP+", PQEnergy(rsp.Body, 0, constA), "кВт", recDate));
                        recs.Add(MakeDayRecord("EnergyP-", PQEnergy(rsp.Body, 4, constA), "кВт", recDate));
                        recs.Add(MakeDayRecord("EnergyQ+", PQEnergy(rsp.Body, 8, constA), "кВт", recDate));
                        recs.Add(MakeDayRecord("EnergyQ-", PQEnergy(rsp.Body, 12, constA), "кВт", recDate));

                        int count = recs.Count;
                        log(string.Format("Cуточные данные на конец {0:dd.MM.yyyy} Энергия: P+={1:0.000} P-={2:0.000} Q+={3:0.000} Q-={4:0.000}.", recDate, recs[count - 4].d1, recs[count - 3].d1, recs[count - 2].d1, recs[count - 1].d1));
                    }


                    date = date.AddDays(1);
                }
            }
            
            records(recs);

            archive.records = recs;
            return archive;
        }

    }

}

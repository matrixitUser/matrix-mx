using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.Poll.Driver.VKT7
{
    public partial class Driver
    {
        dynamic GetConstants(DateTime date, dynamic info)
        {
            dynamic constants = new ExpandoObject();
            constants.success = true;
            constants.error = string.Empty;
            constants.errorcode = DeviceError.NO_ERROR;

            if (cancel())
            {
                constants.success = false;
                constants.error = "опрос отменен";
                constants.errorcode = DeviceError.NO_ERROR;
                return constants;
            }

            var recs = new List<dynamic>();

            recs.Add(MakeConstRecord("Версия ПО", string.Format("{0}.{1}", (info.Version >> 4) & 0x0F, info.Version & 0x0F), date));
            recs.Add(MakeConstRecord("Отчётный день", info.TotalDay, date));
            if (info.FactoryNumber != "")
            {
                recs.Add(MakeConstRecord("Заводской номер", info.FactoryNumber, date));
                recs.Add(MakeConstRecord("Схема подключения Тв1", info.connSch1, date));
                recs.Add(MakeConstRecord("Схема подключения Тв2", info.connSch2, date));
                recs.Add(MakeConstRecord("Назначение ТР3 Тв1", info.tr3use1, date));
                recs.Add(MakeConstRecord("Назначение ТР3 Тв2", info.tr3use2, date));
                recs.Add(MakeConstRecord("Назначение t5 Тв1", info.t5use1, date));
                recs.Add(MakeConstRecord("Назначение t5 Тв2", info.t5use2, date));
                recs.Add(MakeConstRecord("Сетевой адрес", info.NA, date));
                recs.Add(MakeConstRecord("Модель исполнения", info.MI, date));
            }
            
            constants.records = recs;
            constants.TotalDay = info.TotalDay;
            return constants;
        }
    }
}
